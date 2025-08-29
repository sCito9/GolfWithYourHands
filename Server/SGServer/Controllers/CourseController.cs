using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using SGServer.Models;

namespace SGServer.Controllers
{
    /// <summary>
    /// Controller for serving course data from MongoDB. Uses the URL path <code> api/course </code>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ILogger<CourseController> _logger;
        private readonly IMongoCollection<Course> _courses;
        private readonly IMongoCollection<User> _users;

        /// <summary>
        /// Constructor for CourseController
        /// </summary>
        /// <param name="logger">Logger for logging</param>
        /// <param name="database">MongoDB database</param>
        /// <param name="mongoDbSettings">MongoDB settings</param>
        public CourseController(ILogger<CourseController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
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
        /// Get all courses when calling <code> GET api/course </code>
        /// </summary>
        /// <returns>HTTP Ok with all courses as the body</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Courses: GET all courses");
            var items = await _courses.Find(Builders<Course>.Filter.Empty).ToListAsync();
            
            return Ok(items);
        }

        /// <summary>
        /// Get a course by id when calling <code> GET api/course/{id} </code>
        /// </summary>
        /// <param name="id">The id of the course</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            _logger.LogInformation("Courses: GET course at {Id}", id);
            if (!ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning("Courses: BadRequest. Invalid ID format received: {Id}", id);
                return BadRequest("Invalid ID format. ID must be a 24-character hex string.");
            }

            var item = await _courses.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (item == null)
            {
                _logger.LogWarning("Courses: NotFound. Item with ID: {Id} not found.", id);
                return NotFound();
            }
            
            _logger.LogInformation("Courses: Ok. Item with ID: {Id} found.", id);
            return Ok(item);
        }

        /// <summary>
        /// Create a new course when calling <code>POST api/course</code>.
        /// Automatically fills out the id and created fields.
        /// </summary>
        /// <param name="item">The course to create as JSON in the body</param>
        /// <returns>HTTP Ok/BadRequest/InternalServerError</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Course item)
        {
            item.Id = null; // MongoDB will generate the ID
            item.Created = DateTime.UtcNow; // Set the created date to now

            try
            {
                await _courses.InsertOneAsync(item);
                _logger.LogInformation("Courses: Ok. Created new course with ID: {Id}. Course: {Course}", item.Id, JsonSerializer.Serialize(item));
                return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Courses: InternalServerError. Error creating course: {Course}", JsonSerializer.Serialize(item));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the course.");
            }
        }

        /// <summary>
        /// Update a course by id when calling <code> PUT api/course/{id} </code>
        /// </summary>
        /// <param name="id">The id of the course</param>
        /// <param name="item">The course to update as JSON in the body</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/InternalServerError</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Course item)
        {
            _logger.LogInformation("Courses: PUT. Attempting to update course with ID: {Id}. Course: {Course}", id, JsonSerializer.Serialize(item));
            if (id != item.Id)
            {
                _logger.LogWarning("Courses: BadRequest. ID mismatch in PUT request. Route ID: {Id}, Course ID: {CourseId}", id, item.Id);
                return BadRequest("ID mismatch: Route ID and course ID do not match.");
            }
            
            var filter = Builders<Course>.Filter.Eq(i => i.Id, id);
            var result = await _courses.ReplaceOneAsync(filter, item);

            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("Courses: NotFound. Course with ID: {Id} not found for update.", id);
                return NotFound();
            }
            _logger.LogInformation("Courses: Ok. Successfully updated course with ID: {Id}.", id);
            return NoContent();
        }

        /// <summary>
        /// Delete a course by id when calling <code> DELETE api/course/{id} </code>
        /// </summary>
        /// <param name="id">The id of the course to be deleted</param>
        /// <returns>HTTP Ok/BadRequest/NotFound/InternalServerError</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Courses: DELETE. Attempting to delete course with ID: {Id}", id);
            if (!ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning("Courses: BadRequest. Invalid ID format for delete: {Id}", id);
                return BadRequest("Invalid ID format. ID must be a 24-character hex string.");
            }

            var filter = Builders<Course>.Filter.Eq(i => i.Id, id);
            var result = await _courses.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("Courses: NotFound. Course with ID: {Id} not found for deletion.", id);
                return NotFound();
            }
            _logger.LogInformation("Courses: Ok. Successfully deleted course with ID: {Id}.", id);
            return NoContent();
        }
    }
}
