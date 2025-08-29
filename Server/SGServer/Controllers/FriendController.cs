using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SGServer.Models;

namespace SGServer.Controllers
{
    /// <summary>
    /// Controller for managing friend relationships between users. Uses the URL path <code> api/friend </code>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FriendController : ControllerBase
    {
        private readonly ILogger<FriendController> _logger;
        private readonly IMongoCollection<User> _users;

        /// <summary>
        /// Constructor for FriendController
        /// </summary>
        /// <param name="logger">Logger for logging</param>
        /// <param name="database">MongoDB database</param>
        /// <param name="mongoDbSettings">MongoDB settings</param>
        public FriendController(ILogger<FriendController> logger, IMongoDatabase database, IOptions<MongoDbSettings> mongoDbSettings)
        {
            _logger = logger;
            _users = database.GetCollection<User>(mongoDbSettings.Value.UserCollectionName);
        }

        /// <summary>
        /// Send a friend request when calling <code> POST api/friend/request/{fromUserId}/{toUserId} </code>
        /// </summary>
        /// <param name="fromUserId">The id of the user sending the request</param>
        /// <param name="toUserId">The id of the user receiving the request</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPost("request/{fromUserId}/{toUserId}")]
        public async Task<IActionResult> SendFriendRequest(string fromUserId, string toUserId)
        {
            _logger.LogInformation("Friends: POST friend request from {FromUserId} to {ToUserId}", fromUserId, toUserId);

            // Check if users exist
            var fromUserFilter = Builders<User>.Filter.Eq(u => u.Id, fromUserId);
            var toUserFilter = Builders<User>.Filter.Eq(u => u.Id, toUserId);
            
            var fromUser = await _users.Find(fromUserFilter).FirstOrDefaultAsync();
            var toUser = await _users.Find(toUserFilter).FirstOrDefaultAsync();

            if (fromUser == null || toUser == null)
            {
                _logger.LogWarning("Friends: NotFound. One or both users not found");
                return NotFound("One or both users not found");
            }

            // Check if users are already friends
            if (fromUser.FriendIds != null && fromUser.FriendIds.Contains(toUserId))
            {
                _logger.LogWarning("Friends: BadRequest. Users are already friends");
                return BadRequest("Users are already friends");
            }

            // Check if the request already exists
            if (toUser.PendingFriendRequestIds != null && toUser.PendingFriendRequestIds.Contains(fromUserId))
            {
                _logger.LogWarning("Friends: BadRequest. Friend request already exists");
                return BadRequest("Friend request already exists");
            }

            // Add request to the appropriate lists
            var updateFromUser = Builders<User>.Update.AddToSet(u => u.SentFriendRequestIds, toUserId);
            var updateToUser = Builders<User>.Update.AddToSet(u => u.PendingFriendRequestIds, fromUserId);

            await _users.UpdateOneAsync(fromUserFilter, updateFromUser);
            await _users.UpdateOneAsync(toUserFilter, updateToUser);

            _logger.LogInformation("Friends: Ok. Friend request sent from {FromUserId} to {ToUserId}", fromUserId, toUserId);
            return Ok("Friend request sent");
        }

        /// <summary>
        /// Accept a friend request when calling <code> POST api/friend/accept/{toUserId}/{fromUserId} </code>
        /// </summary>
        /// <param name="toUserId">The id of the user accepting the request</param>
        /// <param name="fromUserId">The id of the user who sent the request</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPost("accept/{toUserId}/{fromUserId}")]
        public async Task<IActionResult> AcceptFriendRequest(string toUserId, string fromUserId)
        {
            _logger.LogInformation("Friends: POST accept friend request from {FromUserId} by {ToUserId}", fromUserId, toUserId);

            // Check if users exist
            var fromUserFilter = Builders<User>.Filter.Eq(u => u.Id, fromUserId);
            var toUserFilter = Builders<User>.Filter.Eq(u => u.Id, toUserId);
            
            var fromUser = await _users.Find(fromUserFilter).FirstOrDefaultAsync();
            var toUser = await _users.Find(toUserFilter).FirstOrDefaultAsync();

            if (fromUser == null || toUser == null)
            {
                _logger.LogWarning("Friends: NotFound. One or both users not found");
                return NotFound("One or both users not found");
            }

            // Check if the request exists
            if (toUser.PendingFriendRequestIds == null || !toUser.PendingFriendRequestIds.Contains(fromUserId))
            {
                _logger.LogWarning("Friends: BadRequest. No pending friend request found");
                return BadRequest("No pending friend request found");
            }

            // Initialize lists if null
            fromUser.FriendIds ??= new List<string>();
            toUser.FriendIds ??= new List<string>();

            // Create updates: add each user to the other's friend list, remove from pending/sent lists
            var updateFromUser = Builders<User>.Update
                .AddToSet(u => u.FriendIds, toUserId)
                .Pull(u => u.SentFriendRequestIds, toUserId);

            var updateToUser = Builders<User>.Update
                .AddToSet(u => u.FriendIds, fromUserId)
                .Pull(u => u.PendingFriendRequestIds, fromUserId);

            await _users.UpdateOneAsync(fromUserFilter, updateFromUser);
            await _users.UpdateOneAsync(toUserFilter, updateToUser);

            _logger.LogInformation("Friends: Ok. Friend request accepted from {FromUserId} by {ToUserId}", fromUserId, toUserId);
            return Ok("Friend request accepted");
        }

        /// <summary>
        /// Reject a friend request when calling <code> POST api/friend/reject/{toUserId}/{fromUserId} </code>
        /// </summary>
        /// <param name="toUserId">The id of the user rejecting the request</param>
        /// <param name="fromUserId">The id of the user who sent the request</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpPost("reject/{toUserId}/{fromUserId}")]
        public async Task<IActionResult> RejectFriendRequest(string toUserId, string fromUserId)
        {
            _logger.LogInformation("Friends: POST reject friend request from {FromUserId} by {ToUserId}", fromUserId, toUserId);

            // Check if users exist
            var fromUserFilter = Builders<User>.Filter.Eq(u => u.Id, fromUserId);
            var toUserFilter = Builders<User>.Filter.Eq(u => u.Id, toUserId);
            
            var fromUser = await _users.Find(fromUserFilter).FirstOrDefaultAsync();
            var toUser = await _users.Find(toUserFilter).FirstOrDefaultAsync();

            if (fromUser == null || toUser == null)
            {
                _logger.LogWarning("Friends: NotFound. One or both users not found");
                return NotFound("One or both users not found");
            }

            // Check if the request exists
            if (toUser.PendingFriendRequestIds == null || !toUser.PendingFriendRequestIds.Contains(fromUserId))
            {
                _logger.LogWarning("Friends: BadRequest. No pending friend request found");
                return BadRequest("No pending friend request found");
            }

            // Create updates: remove from pending/sent lists
            var updateFromUser = Builders<User>.Update
                .Pull(u => u.SentFriendRequestIds, toUserId);

            var updateToUser = Builders<User>.Update
                .Pull(u => u.PendingFriendRequestIds, fromUserId);

            await _users.UpdateOneAsync(fromUserFilter, updateFromUser);
            await _users.UpdateOneAsync(toUserFilter, updateToUser);

            _logger.LogInformation("Friends: Ok. Friend request rejected from {FromUserId} by {ToUserId}", fromUserId, toUserId);
            return Ok("Friend request rejected");
        }

        /// <summary>
        /// Remove a friend when calling <code> DELETE api/friend/{userId}/{friendId} </code>
        /// </summary>
        /// <param name="userId">The id of the user removing a friend</param>
        /// <param name="friendId">The id of the friend to be removed</param>
        /// <returns>HTTP Ok/BadRequest/NotFound</returns>
        [HttpDelete("{userId}/{friendId}")]
        public async Task<IActionResult> RemoveFriend(string userId, string friendId)
        {
            _logger.LogInformation("Friends: DELETE friend {FriendId} from user {UserId}", friendId, userId);

            // Check if users exist
            var userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var friendFilter = Builders<User>.Filter.Eq(u => u.Id, friendId);
            
            var user = await _users.Find(userFilter).FirstOrDefaultAsync();
            var friend = await _users.Find(friendFilter).FirstOrDefaultAsync();

            if (user == null || friend == null)
            {
                _logger.LogWarning("Friends: NotFound. One or both users not found");
                return NotFound("One or both users not found");
            }

            // Check if they are actually friends
            if (user.FriendIds == null || !user.FriendIds.Contains(friendId) ||
                friend.FriendIds == null || !friend.FriendIds.Contains(userId))
            {
                _logger.LogWarning("Friends: BadRequest. Users are not friends");
                return BadRequest("Users are not friends");
            }

            // Create updates: remove each user from the other's friend list
            var updateUser = Builders<User>.Update
                .Pull(u => u.FriendIds, friendId);

            var updateFriend = Builders<User>.Update
                .Pull(u => u.FriendIds, userId);

            await _users.UpdateOneAsync(userFilter, updateUser);
            await _users.UpdateOneAsync(friendFilter, updateFriend);

            _logger.LogInformation("Friends: Ok. Friend {FriendId} removed from user {UserId}", friendId, userId);
            return Ok("Friend removed");
        }
    }
}
