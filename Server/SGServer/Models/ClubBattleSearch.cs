using MongoDB.Bson.Serialization.Attributes;

namespace SGServer.Models;

public class ClubBattleSearch
{
    [BsonElement("Id")]
    public string Id { get; set; }
    [BsonElement("ClubId")]
    public string ClubId { get; set; }
    [BsonElement("NumParticipants")]
    public int NumParticipants { get; set; }
    [BsonElement("ParticipantsIds")]
    public List<string> ParticipantsIds { get; set; }
}