using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Concurrent;
using SGServer.Models;

namespace SGServer.Controllers
{
    /// <summary>
    /// Controller for managing active game sessions. Uses the URL path <code> api/game </code>
    /// Unlike other controllers, games are stored in-memory and not persisted to MongoDB.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly IMongoCollection<Course> _courses;
        private readonly IMongoCollection<User> _users;
        
        // In-memory storage for active games
        private static readonly ConcurrentDictionary<string, Game> ActiveGames = new();

        /// <summary>
        /// Constructor for GameController
        /// </summary>
        /// <param name="logger">Logger for logging</param>
        /// <param name="database">MongoDB database</param>
        /// <param name="mongoDbSettings">MongoDB settings</param>
        public GameController(ILogger<GameController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
        {
            // Initialize Logger
            _logger = logger;

            // Retrieve collection names from settings
            var courseCollectionName = mongoDbSettings.Value.CourseCollectionName;
            var userCollectionName = mongoDbSettings.Value.UserCollectionName;

            // Get collections from the database
            _courses = database.GetCollection<Course>(courseCollectionName);
            _users = database.GetCollection<User>(userCollectionName);
        }

        /// <summary>
        /// Get all active games when calling <code> GET api/game </code>
        /// </summary>
        /// <returns>HTTP Ok with all active games as the body</returns>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Games: GET all active games");
            var games = ActiveGames.Values.ToList();
            return Ok(games);
        }

        /// <summary>
        /// Get a game by id when calling <code> GET api/game/{id} </code>
        /// </summary>
        /// <param name="id">The id of the game</param>
        /// <returns>HTTP Ok/NotFound</returns>
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            _logger.LogInformation("Games: GET game with ID: {Id}", id);

            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found.", id);
                return NotFound();
            }

            _logger.LogInformation("Games: Ok. Game with ID: {Id} found.", id);
            return Ok(game);
        }

        /// <summary>
        /// Create a new game when calling <code>POST api/game</code>.
        /// Requires a course ID and host user ID.
        /// </summary>
        /// <param name="game">The basic game information</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Game game)
        {
            _logger.LogInformation("Games: POST. Creating new game on course: {CourseId} by host: {HostId}", 
                game.CourseId, game.HostId);

            // Check if the course exists
            var course = await _courses.Find(c => c.Id == game.CourseId).FirstOrDefaultAsync();
            if (course == null)
            {
                _logger.LogWarning("Games: NotFound. Course with ID: {CourseId} not found.", game.CourseId);
                return NotFound($"Course with ID {game.CourseId} not found.");
            }

            // Check if the host user exists
            var host = await _users.Find(u => u.Id == game.HostId).FirstOrDefaultAsync();
            if (host == null)
            {
                _logger.LogWarning("Games: NotFound. User with ID: {HostId} not found.", game.HostId);
                return NotFound($"User with ID {game.HostId} not found.");
            }

            // Create a new game
            var gameFull = new Game
            {
                CourseId = game.CourseId,
                HostId = game.HostId,
                MaxPlayers = game.MaxPlayers > 0 ? game.MaxPlayers : 4
            };

            // Add host as first player
            gameFull.PlayerIds.Add(game.HostId);

            // Add to active games
            if (ActiveGames.TryAdd(gameFull.Id, gameFull))
            {
                _logger.LogInformation("Games: Created. New game with ID: {Id} created.", gameFull.Id);
                return CreatedAtAction(nameof(Get), new { id = gameFull.Id }, gameFull);
            }

            _logger.LogError("Games: Error. Failed to add game to active games.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create game.");
            
        }

        /// <summary>
        /// Join an existing game when calling <code>POST api/game/{id}/join</code>
        /// </summary>
        /// <param name="id">The id of the game to join</param>
        /// <param name="userId">The joining user ID</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/Conflict</returns>
        [HttpPost("{id}/join/{userId}")]
        public async Task<IActionResult> Join(string id, string userId)
        {
            _logger.LogInformation("Games: POST. User {UserId} attempting to join game: {Id}", userId, id);

            // Check if the game exists
            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found for join.", id);
                return NotFound($"Game with ID {id} not found.");
            }

            // Check if the game is in a joinable state
            if (game.Status != GameStatus.Waiting)
            {
                _logger.LogWarning("Games: Conflict. Game with ID: {Id} is not in joinable state.", id);
                return Conflict("This game is no longer accepting players.");
            }

            // Check if the game is full
            if (game.PlayerIds.Count >= game.MaxPlayers)
            {
                _logger.LogWarning("Games: Conflict. Game with ID: {Id} is already full.", id);
                return Conflict("This game is already full.");
            }

            // Check if the player is already in the game
            if (game.PlayerIds.Contains(userId))
            {
                _logger.LogWarning("Games: Conflict. User {UserId} is already in game: {Id}", userId, id);
                return Conflict("You are already in this game.");
            }

            // Check if the user exists
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogWarning("Games: NotFound. User with ID: {UserId} not found.", userId);
                return NotFound($"User with ID {userId} not found.");
            }

            // Add player to the game
            game.PlayerIds.Add(userId);
            _logger.LogInformation("Games: Success. User {UserId} joined game: {Id}", userId, id);

            return Ok(game);
        }

        /// <summary>
        /// Leave a game when calling <code>POST api/game/{id}/leave</code>
        /// </summary>
        /// <param name="id">The id of the game to leave</param>
        /// <param name="userId">The leaving user ID</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPost("{id}/leave/{userId}")]
        public IActionResult Leave(string id, string userId)
        {
            _logger.LogInformation("Games: POST. User {UserId} attempting to leave game: {Id}", userId, id);

            // Check if the game exists
            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found for leave.", id);
                return NotFound($"Game with ID {id} not found.");
            }

            // Remove player from the game
            if (game.PlayerIds.Remove(userId))
            {
                _logger.LogInformation("Games: Success. User {UserId} left game: {Id}", userId, id);

                // If the host left, assign a new host or remove the game
                if (userId != game.HostId)
                    return Ok(game);
                
                // If the host left, assign a new host
                if (game.PlayerIds.Count > 0)
                {
                    game.HostId = game.PlayerIds[0];
                    _logger.LogInformation("Games: New host assigned. User {UserId} is now host of game: {Id}", game.HostId, id);
                    return Ok(game);
                }

                // Remove the game if no players are left
                ActiveGames.TryRemove(id, out _);
                _logger.LogInformation("Games: Removed. Game with ID: {Id} removed as no players are left.", id);
                return Ok("Game removed as no players are left.");
            }

            // The user ID cannot leave the game because they are not part of the game
            _logger.LogWarning("Games: Conflict. User {UserId} is not in game: {Id}", userId, id);
            return Conflict("User is not in this game.");
        }

        /// <summary>
        /// Start a game when calling <code>POST api/game/{id}/start</code>
        /// Only the host can start the game
        /// </summary>
        /// <param name="id">The id of the game to start</param>
        /// <param name="userId">The user ID that is starting the game</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/Forbidden</returns>
        [HttpPost("{id}/start/{userId}")]
        public IActionResult Start(string id, string userId)
        {
            _logger.LogInformation("Games: POST. User {UserId} attempting to start game: {Id}", userId, id);

            // Check if the game exists
            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found for start.", id);
                return NotFound($"Game with ID {id} not found.");
            }

            // Check if the user trying to start the game is not the host
            if (userId != game.HostId)
            {
                _logger.LogWarning("Games: Forbidden. User {UserId} is not the host of game: {Id}", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden, "Only the host can start the game.");
            }

            // Check if the game can be started
            if (game.Status != GameStatus.Waiting)
            {
                _logger.LogWarning("Games: Conflict. Game with ID: {Id} is not in a startable state.", id);
                return Conflict("This game cannot be started in its current state.");
            }

            // Start the game
            game.Status = GameStatus.InProgress;
            _logger.LogInformation("Games: Started. Game with ID: {Id} started by host: {HostId}", id, game.HostId);

            return Ok(game);
        }

        /// <summary>
        /// Complete or cancel a game when calling <code>POST api/game/{id}/end</code>
        /// Only the host can end the game
        /// </summary>
        /// <param name="id">The id of the game to end</param>
        /// <param name="userId">The user requesting to end the game</param>
        /// <param name="status">The status to update the game to</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/Forbidden</returns>
        [HttpPost("{id}/end/{userId}/{status}")]
        public IActionResult End(string id, string userId, int status)
        {
            _logger.LogInformation("Games: POST. User {UserId} attempting to end game: {Id} with status: {Status}", 
                userId, id, status);

            // Check if the game exists
            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found for end.", id);
                return NotFound($"Game with ID {id} not found.");
            }

            // Check if the user is the host
            if (userId != game.HostId)
            {
                _logger.LogWarning("Games: Forbidden. User {UserId} is not the host of game: {Id}", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden, "Only the host can end the game.");
            }

            // Update game status
            game.Status = (GameStatus) status;
            _logger.LogInformation("Games: Ended. Game with ID: {Id} ended with status: {Status} by host: {HostId}", 
                id, game.Status, game.HostId);

            // Remove completed or canceled games after a delay to allow clients to get the final state
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                ActiveGames.TryRemove(id, out _);
                _logger.LogInformation("Games: Removed. Game with ID: {Id} removed after completion.", id);
            });

            return Ok(game);
        }

        /// <summary>
        /// Delete an active game when calling <code>DELETE api/game/{id}</code>
        /// Only the host can delete a game
        /// </summary>
        /// <param name="id">The id of the game to delete</param>
        /// <param name="hostId">The ID of the host user</param>
        /// <returns>HTTP NoContent/NotFound/Forbidden</returns>
        [HttpDelete("{id}/{hostId}")]
        public IActionResult Delete(string id, string hostId)
        {
            _logger.LogInformation("Games: DELETE. User {HostId} attempting to delete game: {Id}", hostId, id);
            
            // Check if the game exists
            if (!ActiveGames.TryGetValue(id, out var game))
            {
                _logger.LogWarning("Games: NotFound. Game with ID: {Id} not found for deletion.", id);
                return NotFound();
            }

            // Check if the user is the host
            if (hostId != game.HostId)
            {
                _logger.LogWarning("Games: Forbidden. User {HostId} is not the host of game: {Id}", hostId, id);
                return StatusCode(StatusCodes.Status403Forbidden, "Only the host can delete the game.");
            }

            // Remove the game
            if (ActiveGames.TryRemove(id, out _))
            {
                _logger.LogInformation("Games: Deleted. Game with ID: {Id} deleted by host: {HostId}", id, hostId);
                return NoContent();
            }

            _logger.LogError("Games: Error. Failed to delete game with ID: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete game.");
        }
    }
}
