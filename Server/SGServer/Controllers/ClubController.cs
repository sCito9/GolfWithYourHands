using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text.Json;
using SGServer.Models;

namespace SGServer.Controllers;

/// <summary>
/// Controller for serving club data from MongoDB. Uses the URL path <code> api/club </code>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClubController : ControllerBase
{
    private readonly ILogger<ClubController> _logger;
    private readonly IMongoCollection<Club> _clubs;
    private readonly IMongoCollection<User> _users;

    /// <summary>
    /// Constructor for ClubController
    /// </summary>
    /// <param name="logger">Logger for logging</param>
    /// <param name="database">MongoDB database</param>
    /// <param name="mongoDbSettings">MongoDB settings</param>
    public ClubController(ILogger<ClubController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
    {
        // Initialize Logger
        _logger = logger;

        // Get the club collection from the database
        _clubs = database.GetCollection<Club>(mongoDbSettings.Value.ClubCollectionName);
        _users = database.GetCollection<User>(mongoDbSettings.Value.UserCollectionName);
    }

    /// <summary>
    /// Get all clubs when calling <code> GET api/club </code>
    /// </summary>
    /// <returns>HTTP Ok with all clubs as the body</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("Clubs: GET all clubs");
        var items = await _clubs.Find(Builders<Club>.Filter.Empty).ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Get a club by id when calling <code> GET api/club/{id} </code>
    /// </summary>
    /// <param name="id">The id of the club</param>
    /// <returns>HTTP Ok/NotFound</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        _logger.LogInformation("Clubs: GET club at {Id}", id);

        var item = await _clubs.Find(i => i.Id == id).FirstOrDefaultAsync();

        if (item == null)
        {
            _logger.LogWarning("Clubs: NotFound. Item with ID: {Id} not found.", id);
            return NotFound();
        }
        _logger.LogInformation("Clubs: Ok. Item with ID: {Id} found.", id);
        return Ok(item);
    }
    
    /// <summary>
    /// Resolve all club members when calling <code> GET api/club/{id}/members </code>
    /// </summary>
    /// <param name="id">The id of the club</param>
    /// <returns>HTTP Ok/NotFound</returns>
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(string id)
    {
        _logger.LogInformation("Clubs: GET members of club at {Id}", id);

        var item = await _clubs.Find(i => i.Id == id).FirstOrDefaultAsync();

        if (item == null)
        {
            _logger.LogWarning("Clubs: NotFound. Item with ID: {Id} not found.", id);
            return NotFound();
        }
        
        var members = await _users.Find(i => item.MemberIds!.Contains(i.Id)).ToListAsync();
        
        _logger.LogInformation("Clubs: Ok. Members for club with ID: {Id} found.", id);
        return Ok(members);
    }

    /// <summary>
    /// Create a new club when calling <code>POST api/club</code>.
    /// Automatically fills out the id and created fields.
    /// </summary>
    /// <param name="item">The course to create as JSON in the body</param>
    /// <returns>HTTP Ok/BadRequest/InternalServerError</returns>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Club item)
    {
        item.Id = Club.GenerateClubId(); // Generate the ID
        item.Created = DateTime.UtcNow; // Set the created date to now
            
        if (item.LeaderId == null)
        {
            _logger.LogWarning("Clubs: BadRequest. Leader ID invalid in POST request. Club: {Club}", JsonSerializer.Serialize(item));
            return BadRequest("Leader ID invalid in POST request.");
        }
        
        
        var leader = await _users.Find(i => i.Id == item.LeaderId).FirstOrDefaultAsync();
        if (leader == null)
        {
            _logger.LogWarning("Clubs: BadRequest. Leader doesn't exist. Club: {Club}",
                JsonSerializer.Serialize(item));
            return BadRequest("Leader does not exist.");
        }
        
        item.MemberIds = [item.LeaderId];
        leader.ClubId = item.Id;
        
        var filter = Builders<User>.Filter.Eq(i => i.Id, leader.Id);
        var result = await _users.ReplaceOneAsync(filter, leader);
        // The result should be fine, because we already checked that the leader exists

        try
        {
            await _clubs.InsertOneAsync(item);
            _logger.LogInformation("Clubs: Ok. Created new club with ID: {Id}. Club: {Club}", item.Id, JsonSerializer.Serialize(item));
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clubs: InternalServerError. Error creating club: {Club}", JsonSerializer.Serialize(item));
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the club.");
        }
    }

    /// <summary>
    /// Update a club by id when calling <code> PUT api/club/{id} </code>
    /// </summary>
    /// <param name="id">The id of the club</param>
    /// <param name="item">The club to update as JSON in the body</param>
    /// <returns>HTTP Ok/BadRequest/NotFound</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Club item)
    {
        _logger.LogInformation("Clubs: PUT. Attempting to update club with ID: {Id}. Club: {Club}", id, JsonSerializer.Serialize(item));
        if (id != item.Id)
        {
            _logger.LogWarning("Clubs: BadRequest. ID mismatch in PUT request. Route ID: {Id}, Club ID: {Club.Id}", id, item.Id);
            return BadRequest("ID mismatch: Route ID and club ID do not match.");
        }
            
        var filter = Builders<Club>.Filter.Eq(i => i.Id, id);
        var result = await _clubs.ReplaceOneAsync(filter, item);

        if (result.MatchedCount == 0)
        {
            _logger.LogWarning("Clubs: NotFound. Club with ID: {Id} not found for update.", id);
            return NotFound();
        }
        _logger.LogInformation("Clubs: Ok. Successfully updated club with ID: {Id}.", id);
        return Ok();
    }

    /// <summary>
    /// Delete a club by id when calling <code> DELETE api/club/{id} </code>
    /// </summary>
    /// <param name="id">The id of the club to be deleted</param>
    /// <returns>HTTP Ok/BadRequest/NotFound/InternalServerError</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Clubs: DELETE. Attempting to delete club with ID: {Id}", id);

        var filter = Builders<Club>.Filter.Eq(i => i.Id, id);
        var result = await _clubs.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
        {
            _logger.LogWarning("Clubs: NotFound. Club with ID: {Id} not found for deletion.", id);
            return NotFound();
        }
        _logger.LogInformation("Clubs: Ok. Successfully deleted club with ID: {Id}.", id);
        return NoContent();
    }
    
    [HttpPost("{id}/join/{userId}")]
    public async Task<IActionResult> Join(string id, string userId)
    {
        _logger.LogInformation("Clubs: POST. User {UserId} attempting to join club: {Id}", userId, id);
        
        var filter = Builders<Club>.Filter.Eq(i => i.Id, id);
        var club = await _clubs.Find(filter).FirstOrDefaultAsync();
        if (club == null)
        {
            _logger.LogWarning("Clubs: NotFound. Club with ID: {Id} not found for join.", id);
            return NotFound($"Club with ID {id} not found.");
        }
        
        var user = await _users.Find(i => i.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            _logger.LogWarning("Clubs: NotFound. User with ID: {Id} not found for join.", userId);
            return NotFound($"User with ID {userId} not found.");
        }
        
        if (!string.IsNullOrEmpty(user.ClubId))
        {
            _logger.LogWarning("Clubs: Conflict. User with ID: {Id} is already in a club.", userId);
            return Conflict($"User with ID {userId} is already in a club.");
        }
        
        user.ClubId = id;
        var filter2 = Builders<User>.Filter.Eq(i => i.Id, userId);
        var result2 = await _users.ReplaceOneAsync(filter2, user);
        _logger.LogInformation("Clubs: Ok. User {UserId} joined club: {Id}", userId, id);
        
        club.MemberIds.Add(userId);
        var filter3 = Builders<Club>.Filter.Eq(i => i.Id, id);
        var result3 = await _clubs.ReplaceOneAsync(filter3, club);
        _logger.LogInformation("Clubs: Ok. User {UserId} joined club: {Id}", userId, id);
        
        return Ok();
    }
    
    [HttpPost("{id}/leave/{userId}")]
    public async Task<IActionResult> Leave(string id, string userId)
    {
        _logger.LogInformation("Clubs: POST. User {UserId} attempting to leave club: {Id}", userId, id);
        
        var filter = Builders<Club>.Filter.Eq(i => i.Id, id);
        var club = await _clubs.Find(filter).FirstOrDefaultAsync();
        if (club == null)
        {
            _logger.LogWarning("Clubs: NotFound. Club with ID: {Id} not found for leave.", id);
            return NotFound($"Club with ID {id} not found.");
        }
        
        var user = await _users.Find(i => i.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            _logger.LogWarning("Clubs: NotFound. User with ID: {Id} not found for leave.", userId);
            return NotFound($"User with ID {userId} not found.");
        }
        
        if (user.ClubId != id)
        {
            _logger.LogWarning("Clubs: Conflict. User with ID: {Id} is not in this club.", userId);
            return Conflict($"User with ID {userId} is not in this club.");
        }
        
        user.ClubId = null;
        var filter2 = Builders<User>.Filter.Eq(i => i.Id, userId);
        var result2 = await _users.ReplaceOneAsync(filter2, user);
        _logger.LogInformation("Clubs: Ok. User {UserId} left club: {Id}", userId, id);
        
        club.MemberIds.Remove(userId);
        var filter3 = Builders<Club>.Filter.Eq(i => i.Id, id);
        var result3 = await _clubs.ReplaceOneAsync(filter3, club);
        _logger.LogInformation("Clubs: Ok. User {UserId} left club: {Id}", userId, id);
        
        // If the club leader left, set the new leader
        if (club.LeaderId == userId)
        {
            var newLeader = await _users.Find(i => i.ClubId == id).FirstOrDefaultAsync();
            if (newLeader == null)
            {
                _logger.LogWarning("Clubs: Leave: No new leader found for club: {Id}", id);
                return Ok();
            }
            
            club.LeaderId = newLeader.Id;
            var filter4 = Builders<Club>.Filter.Eq(i => i.Id, id);
            var result4 = await _clubs.ReplaceOneAsync(filter4, club);
            _logger.LogInformation("Clubs: Ok. New leader set for club: {Id}", id);
        }
        
        return Ok();
    }
}