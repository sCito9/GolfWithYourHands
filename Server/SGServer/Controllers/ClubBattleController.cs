using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SGServer.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace SGServer.Controllers;

[ApiController]
[Route("api/club-battle")]
public class ClubBattleController : ControllerBase
{
    private const int ParticipantsPerClub = 2;
    
    private static readonly ConcurrentDictionary<string, ClubBattleSearch> SearchingClubs = new();
    private static readonly ConcurrentDictionary<string, ClubBattle> ActiveGames = new();
    
    private readonly ILogger<ClubBattleController> _logger;
    private readonly IMongoCollection<Course> _courses;

    /// <summary>
    /// Constructor for ClubBattleController
    /// </summary>
    /// <param name="logger">Logger for logging</param>
    /// <param name="database">MongoDB database</param>
    /// <param name="mongoDbSettings">MongoDB settings</param>
    public ClubBattleController(ILogger<ClubBattleController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
    {
        _logger = logger;
        
        // Get course collection from the database
        var courseCollectionName = mongoDbSettings.Value.CourseCollectionName;
        _courses = database.GetCollection<Course>(courseCollectionName);
    }
    
    // GET: api/club-battle/search - List all clubs currently searching for matches
    [HttpGet("search")]
    public IActionResult ListSearchingClubs()
    {
        _logger.LogInformation("ClubBattle: GET all searching clubs");
        return Ok(SearchingClubs.Values.ToList());
    }

    // POST: api/club-battle/search - Create a new search request
    [HttpPost("search")]
    public async Task<IActionResult> CreateSearchRequest([FromBody] ClubBattleSearch clubBattleSearch)
    {
        _logger.LogInformation("ClubBattle: POST create search request for club {ClubId}", clubBattleSearch.ClubId);
        
        if (string.IsNullOrEmpty(clubBattleSearch.ClubId))
        {
            _logger.LogWarning("ClubBattle: BadRequest. Invalid search request with empty ClubId");
            return BadRequest("Invalid search request");
        }

        // Check if club is already searching or in an active game
        var existingSearch = SearchingClubs.Values.FirstOrDefault(s => s.ClubId == clubBattleSearch.ClubId);
        if (existingSearch != null)
        {
            _logger.LogWarning("ClubBattle: Conflict. Club {ClubId} is already searching for a match", clubBattleSearch.ClubId);
            return Conflict("Club is already searching for a match");
        }

        var activeGame = ActiveGames.Values.FirstOrDefault(g => g.Club1Id == clubBattleSearch.ClubId || g.Club2Id == clubBattleSearch.ClubId);
        if (activeGame != null)
        {
            _logger.LogWarning("ClubBattle: Conflict. Club {ClubId} is already in an active battle", clubBattleSearch.ClubId);
            return Conflict("Club is already in an active battle");
        }

        // Generate ID if not provided
        if (string.IsNullOrEmpty(clubBattleSearch.Id))
        {
            clubBattleSearch.Id = Guid.NewGuid().ToString();
        }

        if (!SearchingClubs.IsEmpty)
        {
            _logger.LogInformation("ClubBattle: Found existing search request, attempting to create a match");
            return await ChallengeClub(SearchingClubs.Values.First().Id, clubBattleSearch);
        }

        SearchingClubs[clubBattleSearch.Id] = clubBattleSearch;
        _logger.LogInformation("ClubBattle: Created new search request with ID {SearchId} for club {ClubId}", 
            clubBattleSearch.Id, clubBattleSearch.ClubId);
        return Created($"api/club-battle/search/{clubBattleSearch.Id}", clubBattleSearch);
    }

    // DELETE: api/club-battle/search/{searchId} - Cancel a search request
    [HttpDelete("search/{searchId}")]
    public IActionResult CancelSearchRequest(string searchId)
    {
        _logger.LogInformation("ClubBattle: DELETE search request with ID {SearchId}", searchId);
        
        if (SearchingClubs.TryRemove(searchId, out var removedSearch))
        {
            _logger.LogInformation("ClubBattle: Successfully cancelled search request with ID {SearchId} for club {ClubId}", 
                searchId, removedSearch.ClubId);
            return Ok(new { message = "Search request cancelled", searchId = searchId });
        }
        
        _logger.LogWarning("ClubBattle: NotFound. Search request with ID {SearchId} not found", searchId);
        return NotFound("Search request not found");
    }

    // Get: api/club-battle/challenge/{searchId} - Challenge a club that's searching
    [HttpPost("challenge/{searchId}")]
    public async Task<IActionResult> ChallengeClub(string searchId, [FromBody] ClubBattleSearch challengeRequest)
    {
        _logger.LogInformation("ClubBattle: POST challenge club with search ID {SearchId} from club {ClubId}", 
            searchId, challengeRequest.ClubId);
        
        if (!SearchingClubs.TryGetValue(searchId, out var targetSearch))
        {
            _logger.LogWarning("ClubBattle: NotFound. Search request with ID {SearchId} not found", searchId);
            return NotFound("Search request not found");
        }
        
        var club1Scores = new List<ScoreEntry>();
        var club2Scores = new List<ScoreEntry>();

        for (var i = 0; i < targetSearch.NumParticipants; i++)
        {
            club1Scores.Add(new ScoreEntry { UserId = targetSearch.ParticipantsIds[i], Score = [10, 10, 10] });
            club2Scores.Add(new ScoreEntry { UserId = challengeRequest.ParticipantsIds[i], Score = [10, 10, 10] });
        }
        
        // Create new ClubBattle
        var clubBattle = new ClubBattle
        {
            Id = $"{targetSearch.ClubId}-{challengeRequest.ClubId}-{DateTime.UtcNow.Ticks}",
            Club1Id = targetSearch.ClubId,
            Club2Id = challengeRequest.ClubId,
            ParticipantsPerClub = targetSearch.NumParticipants,
            ParticipantIdsClub1 = targetSearch.ParticipantsIds,
            ParticipantIdsClub2 = challengeRequest.ParticipantsIds,
            Club1Scores = club1Scores,
            Club2Scores = club2Scores,
            CourseIds = await GetRandomCourseIdsAsync(),
            StartTime = DateTime.UtcNow.ToBinary(),
            EndTime = DateTime.UtcNow.AddHours(0.5).ToBinary(),
            AbsoluteEndTime = DateTime.UtcNow.AddHours(1).ToBinary()
        };

        // Remove the search request and add to active games
        SearchingClubs.TryRemove(searchId, out _);
        ActiveGames[clubBattle.Id] = clubBattle;

        _logger.LogInformation("ClubBattle: Created new battle with ID {BattleId} between clubs {Club1Id} and {Club2Id}",
            clubBattle.Id, clubBattle.Club1Id, clubBattle.Club2Id);
        return Created($"api/club-battle/{clubBattle.Id}", clubBattle);
    }

    /// <summary>
    /// Get 3 random course IDs from the database
    /// </summary>
    /// <returns>Array of 3 random course IDs</returns>
    private async Task<string[]> GetRandomCourseIdsAsync()
    {
        try
        {
            // Get total count of courses
            var totalCourses = await _courses.CountDocumentsAsync(Builders<Course>.Filter.Empty);
            
            if (totalCourses < 3)
            {
                _logger.LogWarning("ClubBattle: Not enough courses in database. Found {Count}, need at least 3", totalCourses);
                // Return empty array if not enough courses
                return new string[3];
            }
            
            // Generate 3 random skip values
            var random = new Random();
            var skipValues = new HashSet<int>();
            
            while (skipValues.Count < 3)
            {
                skipValues.Add(random.Next(0, (int)totalCourses));
            }
            
            var courseIds = new List<string>();
            
            // Get one course for each skip value
            foreach (var skip in skipValues)
            {
                var course = await _courses.Find(Builders<Course>.Filter.Empty)
                    .Skip(skip)
                    .Limit(1)
                    .FirstOrDefaultAsync();
                
                if (course?.Id != null)
                {
                    courseIds.Add(course.Id);
                }
            }
            
            // Fill remaining slots with empty strings if we couldn't get 3 courses
            while (courseIds.Count < 3)
            {
                courseIds.Add("");
            }
            
            _logger.LogInformation("ClubBattle: Selected random course IDs: {CourseIds}", string.Join(", ", courseIds));
            return courseIds.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClubBattle: Error getting random course IDs");
            return new string[3]; // Return empty array on error
        }
    }

    // GET: api/club-battle/status/{clubId} - Check club's current status
    [HttpGet("status/{clubId}")]
    public IActionResult GetClubStatus(string clubId)
    {
        _logger.LogInformation("ClubBattle: GET status for club {ClubId}", clubId);
        
        // Check if club is searching
        var searchRequest = SearchingClubs.Values.FirstOrDefault(s => s.ClubId == clubId);
        if (searchRequest != null)
        {
            _logger.LogInformation("ClubBattle: Club {ClubId} is currently searching with search ID {SearchId}", 
                clubId, searchRequest.Id);
            return Ok(new ClubStatus
            {
                ClubId = clubId,
                Status = "searching",
                SearchId = searchRequest.Id,
                Details = searchRequest
            });
        }

        // Check if club is in an active battle
        var activeBattle = ActiveGames.Values.FirstOrDefault(g => g.Club1Id == clubId || g.Club2Id == clubId);
        if (activeBattle != null)
        {
            _logger.LogInformation("ClubBattle: Club {ClubId} is currently in battle with ID {BattleId}", 
                clubId, activeBattle.Id);
            return Ok(new ClubStatus
            {
                ClubId = clubId,
                Status = "in_battle",
                BattleId = activeBattle.Id,
                Details = activeBattle
            });
        }

        // Club is available
        _logger.LogInformation("ClubBattle: Club {ClubId} is currently idle", clubId);
        return Ok(new ClubStatus
        {
            ClubId = clubId,
            Status = "idle"
        });
    }

    [HttpGet("{clubBattleId}")]
    public IActionResult Get(string clubBattleId)
    {
        _logger.LogInformation("ClubBattle: GET battle with ID {BattleId}", clubBattleId);
        
        if (!ActiveGames.TryGetValue(clubBattleId, out var game))
        {
            _logger.LogWarning("ClubBattle: NotFound. Battle with ID {BattleId} not found", clubBattleId);
            return NotFound();
        }
        
        _logger.LogInformation("ClubBattle: Successfully retrieved battle with ID {BattleId}", clubBattleId);
        return Ok(game);
    }
    
    [HttpGet("update-score/{clubBattleId}/{userId}/{courseIndex}/{score}")]
    public IActionResult UpdateScore(string clubBattleId, string userId, int courseIndex, int score)
    {
        _logger.LogInformation("ClubBattle: GET update score for battle {BattleId}, user {UserId}, course {CourseIndex}, score {Score}", 
            clubBattleId, userId, courseIndex, score);
        
        if (!ActiveGames.TryGetValue(clubBattleId, out var clubBattle))
        {
            _logger.LogWarning("ClubBattle: NotFound. Battle with ID {BattleId} not found", clubBattleId);
            return NotFound();
        }
        
        for (var i = 0; i < clubBattle.ParticipantsPerClub; i++)
        {
            if (!clubBattle.Club1Scores[i].UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)) continue;
            
            clubBattle.Club1Scores[i].Score[courseIndex] = score;
            _logger.LogInformation("ClubBattle: Updated score for Club1 user {UserId} in battle {BattleId}", userId, clubBattleId);
            break;
        }
        
        for (var i = 0; i < clubBattle.ParticipantsPerClub; i++)
        {
            if (!clubBattle.Club2Scores[i].UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)) continue;
            
            clubBattle.Club2Scores[i].Score[courseIndex] = score;
            _logger.LogInformation("ClubBattle: Updated score for Club1 user {UserId} in battle {BattleId}", userId, clubBattleId);
            break;
        }
        
        _logger.LogInformation("ClubBattle: Successfully updated score for battle {BattleId}", clubBattleId);
        return Ok(clubBattle);
    }
    
    [HttpDelete("{clubBattleId}")]
    public IActionResult Delete(string clubBattleId)
    {
        _logger.LogInformation("ClubBattle: DELETE battle with ID {BattleId}", clubBattleId);
        
        if (!ActiveGames.Remove(clubBattleId, out _))
        {
            _logger.LogWarning("ClubBattle: NotFound. Battle with ID {BattleId} not found", clubBattleId);
            return NotFound();
        }
        
        _logger.LogInformation("ClubBattle: Successfully deleted battle with ID {BattleId}", clubBattleId);
        return Ok();
    }
}

public class ClubStatus
{
    [BsonElement("ClubId")]
    public string ClubId { get; set; }
    [BsonElement("Status")]
    public string Status { get; set; } // "idle", "searching", "in_battle"
    [BsonElement("SearchId")]
    public string SearchId { get; set; }
    [BsonElement("BattleId")]
    public string BattleId { get; set; }
    [BsonElement("Details")]
    public object Details { get; set; }
}