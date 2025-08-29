using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace SGServer.Models;

public class ClubBattle
{
    [BsonElement("Id")]
    public string Id { get; set; }
    
    [BsonElement("Club1Id")]
    public string Club1Id { get; set; }
    [BsonElement("Club2Id")]
    public string Club2Id { get; set; }
    
    [BsonElement("ParticipantsPerClub")]
    public int ParticipantsPerClub { get; set; }
    
    [BsonElement("ParticipantIdsClub1")]
    public List<string> ParticipantIdsClub1 { get; set; }
    [BsonElement("ParticipantIdsClub2")]
    public List<string> ParticipantIdsClub2 { get; set; }
    
    [BsonElement("Club1Scores")]
    public List<ScoreEntry> Club1Scores { get; set; }
    [BsonElement("Club2Scores")]
    public List<ScoreEntry> Club2Scores { get; set; } 
    
    [BsonElement("CourseIds")]
    public string[] CourseIds { get; set; }
    
    [BsonElement("StartTime")]
    public long StartTime { get; set; }
    [BsonElement("EndTime")]
    public long EndTime { get; set; }
    [BsonElement("AbsoluteEndTime")]
    public long AbsoluteEndTime { get; set; }
}

public class ScoreEntry
{
    [BsonElement("UserId")]
    public string UserId { get; set; }
    [BsonElement("Score")]
    public List<int> Score { get; set; }
}