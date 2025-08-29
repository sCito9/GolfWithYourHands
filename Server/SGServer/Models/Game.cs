using MongoDB.Bson.Serialization.Attributes;

namespace SGServer.Models
{
    /// <summary>
    /// Represents an active game session
    /// </summary>
    public class Game
    {

        /// <summary>
        /// Unique identifier for the game
        /// </summary>
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// When the game was created
        /// </summary>
        [BsonElement("Created")]
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Reference to the course being played
        /// </summary>
        [BsonElement("CourseId")]
        public required string CourseId { get; set; }
        
        /// <summary>
        /// IDs of players who have joined this game
        /// </summary>
        [BsonElement("PlayerIds")]
        public List<string> PlayerIds { get; set; } = [];
        
        /// <summary>
        /// Current status of the game
        /// </summary>
        [BsonElement("Status")]
        public GameStatus Status { get; set; } = GameStatus.Waiting;
        
        /// <summary>
        /// Maximum number of players allowed in this game
        /// </summary>
        [BsonElement("MaxPlayers")]
        public int MaxPlayers { get; set; } = 4;
        
        /// <summary>
        /// ID of the user who created the game
        /// </summary>
        [BsonElement("HostId")]
        public required string HostId { get; set; }
    }
    
    /// <summary>
    /// Possible states of a game
    /// </summary>
    public enum GameStatus
    {
        Waiting,    // Waiting for players to join
        InProgress, // The Game has started
        Completed,  // The Game has finished
        Cancelled   // The Game was canceled
    }
}
