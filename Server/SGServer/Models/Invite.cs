using MongoDB.Bson.Serialization.Attributes;

namespace SGServer.Models;

public class Invite
{
    [BsonElement("HostId")]
    public required string HostId { get; set; }
    
    [BsonElement("ReceiverId")]
    public required string ReceiverId { get; set; }
    
    [BsonElement("CourseName")]
    public required string CourseName { get; set; }
    
    [BsonElement("HostName")]
    public required string HostName { get; set; }
    
    [BsonElement("LobbyId")]
    public required string LobbyId { get; set; }
}