namespace SGServer.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; init; } = null!;
        public string DatabaseName { get; init; } = null!;
        public string CourseCollectionName { get; init; } = null!;
        public string UserCollectionName { get; init; } = null!;
        public string ClubCollectionName { get; init; } = null!;
    }
}
