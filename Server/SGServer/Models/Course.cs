using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SGServer.Models
{
    public class Course
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Created")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Created { get; set; }

        [BsonElement("Name")]
        public string? Name { get; set; }

        [BsonElement("AuthorId")]
        public string? AuthorId { get; set; }
        
        [BsonElement("StartPosition")]
        public Float3? StartPosition { get; set; }
        
        [BsonElement("EndPosition")]
        public Float3? EndPosition { get; set; }
        
        [BsonElement("MapOrigin")]
        public Double3? MapOrigin { get; set; }
        
        [BsonElement("SandPositions")]
        public Float3[]? SandPositions { get; set; }
    }

    public class Float3
    {
        [BsonElement("X")]
        public float X { get; set; }
        
        [BsonElement("Y")]
        public float Y { get; set; }
        
        [BsonElement("Z")]
        public float Z { get; set; }
    }

    public class Double3
    {
        [BsonElement("X")]
        public double X { get; set; }
        
        [BsonElement("Y")]
        public double Y { get; set; }
        
        [BsonElement("Z")]
        public double Z { get; set; }
    }
}