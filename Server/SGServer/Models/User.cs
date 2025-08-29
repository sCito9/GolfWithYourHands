using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text;

namespace SGServer.Models
{
    public class User
    {
        private static int _idCounter;
        private static readonly object CounterLock = new();
        private static bool _isInitialized;
        private static IMongoCollection<CounterDocument>? _countersCollection;
        private const string Base36Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const int IdLength = 5;
        
        
        [BsonId]
        public string? Id { get; set; }

        [BsonElement("Created")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Created { get; set; }

        [BsonElement("Name")]
        public string? Name { get; set; }
        
        [BsonElement("ClubId")]
        public string? ClubId { get; set; }

        [BsonElement("FriendIds")]
        public List<string>? FriendIds { get; set; } = [];

        [BsonElement("PendingFriendRequestIds")]
        public List<string>? PendingFriendRequestIds { get; set; } = [];

        [BsonElement("SentFriendRequestIds")]
        public List<string>? SentFriendRequestIds { get; set; } = [];
        
        #region IDGeneration

        /// <summary>
        /// Initialize the ID counter from the database to ensure persistence between server restarts
        /// </summary>
        /// <param name="mongoClient">MongoDB client instance</param>
        /// <param name="databaseName">The name of the Database</param>
        public static void InitializeIdCounter(IMongoClient mongoClient, string databaseName = "SGDatabase")
        {
            if (_isInitialized) return;

            lock (CounterLock)
            {
                if (_isInitialized) return; // Double-check inside lock
                
                var database = mongoClient.GetDatabase(databaseName);
                _countersCollection = database.GetCollection<CounterDocument>("counters");
                
                // Find or create the user ID counter document
                var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, "userId");
                var counterDoc = _countersCollection.Find(filter).FirstOrDefault();
                
                if (counterDoc == null)
                {
                    // First time initialization - start from 0
                    counterDoc = new CounterDocument { Id = "userId", SequenceValue = 0 };
                    _countersCollection.InsertOne(counterDoc);
                }
                
                // Initialize the in-memory counter from the database value
                _idCounter = counterDoc.SequenceValue;
                _isInitialized = true;
            }
        }
        
        /// <summary>
        /// Generate a unique and readable user ID
        /// </summary>
        /// <returns>Number encoded in base 36</returns>
        public static string GenerateUserId()
        {
            int newId;
            lock (CounterLock)
            {
                newId = Interlocked.Increment(ref _idCounter);
                
                var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, "userId");
                var update = Builders<CounterDocument>.Update.Set(c => c.SequenceValue, newId);
                
                _countersCollection?.UpdateOne(filter, update);
            }
            
            var sb = new StringBuilder();
            do
            {
                sb.Insert(0, Base36Chars[newId % 36]);
                newId /= 36;
            } while (newId > 0);
            
            return sb.ToString().PadLeft(IdLength, Base36Chars[0]);
        }

        private class CounterDocument
        {
            [BsonId] public string Id { get; init; } = string.Empty;
            
            [BsonElement("sequence_value")]
            public int SequenceValue { get; init; }
        }
        
        #endregion IDGeneration
    }
}