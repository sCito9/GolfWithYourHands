using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text.Json;
using SGServer.Models;

namespace SGServer.Controllers
{
    /// <summary>
    /// Controller for serving user data from MongoDB. Uses the URL path <code> api/user </code>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IMongoCollection<User> _users;

        /// <summary>
        /// Constructor for UserController
        /// </summary>
        /// <param name="logger">Logger for logging</param>
        /// <param name="database">MongoDB database</param>
        /// <param name="mongoDbSettings">MongoDB settings</param>
        public UserController(ILogger<UserController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
        {
            // Initialize Logger
            _logger = logger;

            // Get the user collection from the database
            _users = database.GetCollection<User>(mongoDbSettings.Value.UserCollectionName);
        }

        /// <summary>
        /// Get all users when calling <code> GET api/user </code>
        /// </summary>
        /// <returns>HTTP Ok with all users as the body</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Users: GET all users");
            var items = await _users.Find(Builders<User>.Filter.Empty).ToListAsync();
            return Ok(items);
        }

        /// <summary>
        /// Get a user by id when calling <code> GET api/user/{id} </code>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            _logger.LogInformation("Users: GET user at {Id}", id);

            var item = await _users.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (item == null)
            {
                _logger.LogWarning("Users: NotFound. Item with ID: {Id} not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Users: Ok. Item with ID: {Id} found.", id);
            return Ok(item);
        }

        /// <summary>
        /// Get a list of users by id when calling <code> GET api/user/list/{ids} </code>
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost("list")]
        public async Task<IActionResult> Post([FromBody] List<string> ids)
        {
            _logger.LogInformation("Users: GET list of users at {Ids}", ids);
            var filter = Builders<User>.Filter.In(i => i.Id, ids);
            var items = await _users.Find(filter).ToListAsync();
            
            return Ok(items);
        }

        /// <summary>
        /// Create a new user when calling <code>POST api/user</code>.
        /// Automatically fills out the id and created fields.
        /// </summary>
        /// <param name="item">The course to create as JSON in the body</param>
        /// <returns>HTTP Ok/BadRequest/InternalServerError</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User item)
        {
            item.Id = SGServer.Models.User.GenerateUserId(); // Generate the ID
            item.Created = DateTime.UtcNow; // Set the created date to now

            try
            {
                await _users.InsertOneAsync(item);
                _logger.LogInformation("Users: Ok. Created new user with ID: {Id}. User: {User}", item.Id, JsonSerializer.Serialize(item));
                return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Users: InternalServerError. Error creating user: {User}", JsonSerializer.Serialize(item));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the user.");
            }
        }

        /// <summary>
        /// Update a user by id when calling <code> PUT api/user/{id} </code>
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="item">The user to update as JSON in the body</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] User item)
        {
            _logger.LogInformation("Users: PUT. Attempting to update user with ID: {Id}. User: {User}", id, JsonSerializer.Serialize(item));
            if (id != item.Id)
            {
                _logger.LogWarning("Users: BadRequest. ID mismatch in PUT request. Route ID: {Id}, User ID: {User.Id}", id, item.Id);
                return BadRequest("ID mismatch: Route ID and user ID do not match.");
            }
            
            var filter = Builders<User>.Filter.Eq(i => i.Id, id);
            var result = await _users.ReplaceOneAsync(filter, item);

            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("Users: NotFound. User with ID: {Id} not found for update.", id);
                return NotFound();
            }
            _logger.LogInformation("Users: Ok. Successfully updated user with ID: {Id}.", id);
            return NoContent();
        }

        /// <summary>
        /// Delete a user by id when calling <code> DELETE api/user/{id} </code>
        /// </summary>
        /// <param name="id">The id of the user to be deleted</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/InternalServerError</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Users: DELETE. Attempting to delete user with ID: {Id}", id);

            var filter = Builders<User>.Filter.Eq(i => i.Id, id);
            var result = await _users.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("Users: NotFound. User with ID: {Id} not found for deletion.", id);
                return NotFound();
            }
            _logger.LogInformation("Users: Ok. Successfully deleted user with ID: {Id}.", id);
            return NoContent();
        }
    }
}
