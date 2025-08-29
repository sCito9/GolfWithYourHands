using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataBackend.Models;
using UnityEngine;
using UnityEngine.Networking;
using User = DataBackend.Models.User;


namespace DataBackend
{
    public static class RemoteBackend
    {
        private const string CoursePath = "course";
        private const string UserPath = "user";
        private const string FriendPath = "friend";
        private const string ClubPath = "club";
        private const string InvitePath = "invite";
        private const string ClubBattlePath = "club-battle";
        private const string ApiKey = "XWmlbVirisi332ROtPxKsJNXz3p7OZLMwDeUrKT0foSvAPsLV2pJ0AzmssCFv1OP";

        private const string BaseUrl = "https://sgs.mr-jackal.com/api";
        private const int TimeoutSeconds = 30;


        private const bool EnableLogging = false;
        private const bool EnableCheckCertificates = true;

        private class DummyCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }


        #region User

        /// <summary>
        ///     Create a new user using a POST request
        /// </summary>
        /// <param name="user">User to create</param>
        public static async Task<User> CreateUser(User user)
        {
            var url = $"{BaseUrl}/{UserPath}";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(user), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to create user: {request.error}");
                return null;
            }

            var createdUser = JsonUtility.FromJson<User>(request.downloadHandler.text);
            return createdUser;
        }


        /// <summary>
        ///     Retrieve a user by ID using GET request
        /// </summary>
        /// <param name="userId">ID of the user to retrieve</param>
        public static async Task<User> GetUser(string userId)
        {
            var url = $"{BaseUrl}/{UserPath}/{userId}";

            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to retrieve user {userId}: {request.error}");
                return null;
            }

            var user = JsonUtility.FromJson<User>(request.downloadHandler.text);
            return user;
        }

        /// <summary>
        ///     Retrieve a list of users by their IDs
        /// </summary>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>List of User objects</returns>
        public static async Task<List<User>> GetUserList(List<string> userIds)
        {
            var url = $"{BaseUrl}/{UserPath}/list";

            using var request = UnityWebRequest.Post(url, $"[\"{string.Join("\",\"", userIds)}\"]", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to retrieve user list: {request.error}");
                return null;
            }

            var userList = JsonUtility.FromJson<SerializableList<User>>("{\"list\": " + request.downloadHandler.text + "}");
            return userList.list;
        }

        public static async Task<bool> DeleteUser(string userId)
        {
            var url = $"{BaseUrl}/{UserPath}/{userId}";
            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to delete user {userId}: {request.error}");
                return false;
            }

            return true;
        }

        #endregion

        #region Friends

        /// <summary>
        ///     Send a friend request from user to user
        /// </summary>
        /// <param name="fromId">User ID that initiated the request</param>
        /// <param name="toId">User ID to receive the request</param>
        /// <returns>True on success</returns>
        public static async Task<bool> SendFriendRequest(string fromId, string toId)
        {
            var url = $"{BaseUrl}/{FriendPath}/request/{fromId}/{toId}";

            using var request = UnityWebRequest.Post(url, "", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to send friend request: {request.error}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Accept a pending friend request
        /// </summary>
        /// <param name="fromId">User ID accepting the request</param>
        /// <param name="toId">User ID that sent the request</param>
        /// <returns>True on success</returns>
        public static async Task<bool> AcceptFriendRequest(string fromId, string toId)
        {
            var url = $"{BaseUrl}/{FriendPath}/accept/{fromId}/{toId}";

            using var request = UnityWebRequest.Post(url, "", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to accept friend request: {request.error}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Decline a pending friend request
        /// </summary>
        /// <param name="fromId">User ID declining the request</param>
        /// <param name="toId">User ID that sent the request</param>
        /// <returns>True on success</returns>
        public static async Task<bool> DeclineFriendRequest(string fromId, string toId)
        {
            var url = $"{BaseUrl}/{FriendPath}/reject/{fromId}/{toId}";

            using var request = UnityWebRequest.Post(url, "", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to decline friend request: {request.error}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Remove a friend
        /// </summary>
        /// <param name="fromId">Issuer</param>
        /// <param name="toId">Unfriended person</param>
        /// <returns>True on success</returns>
        public static async Task<bool> RemoveFriend(string fromId, string toId)
        {
            var url = $"{BaseUrl}/{FriendPath}/{fromId}/{toId}";

            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to remove friend: {request.error}");
                return false;
            }

            return true;
        }

        #endregion Friends

        #region Course

        /// <summary>
        ///     Retrieve a golf course by ID using GET request
        /// </summary>
        /// <param name="courseId">ID of course to retrieve</param>
        public static async Task<Course> GetCourse(string courseId)
        {
            var url = $"{BaseUrl}/{CoursePath}/{courseId}";

            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to retrieve golf course {courseId}: {request.error}");
                return null;
            }

            var course = JsonUtility.FromJson<Course>(request.downloadHandler.text);
            return course;
        }


        /// <summary>
        ///     Retrieve all courses
        /// </summary>
        /// <returns>List of all courses or null</returns>
        public static async Task<List<Course>> GetCourses()
        {
            var url = $"{BaseUrl}/{CoursePath}";

            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to retrieve golf courses: {request.error}");
                return null;
            }

            var courseList = JsonUtility.FromJson<SerializableList<Course>>("{\"list\": " + request.downloadHandler.text + "}");
            return courseList.list;
        }


        /// <summary>
        ///     Create a new golf course
        /// </summary>
        /// <param name="course">The course to persist</param>
        /// <returns>The persisted course or null</returns>
        public static async Task<Course> CreateCourse(Course course)
        {
            var url = $"{BaseUrl}/{CoursePath}";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(course), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to create golf course: {request.error}");
                return null;
            }

            var createdCourse = JsonUtility.FromJson<Course>(request.downloadHandler.text);
            return createdCourse;
        }

        #endregion Course

        #region Clubs

        public static async Task<Club> CreateClub(Club club)
        {
            var url = $"{BaseUrl}/{ClubPath}";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(club), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to create club: {request.error}");
                return null;
            }

            var createdClub = JsonUtility.FromJson<Club>(request.downloadHandler.text);
            return createdClub;
        }
        
        public static async Task<Club> GetClub(string clubId)
        {
            var url = $"{BaseUrl}/{ClubPath}/{clubId}";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get club: {request.error}");
                return null;
            }

            var club = JsonUtility.FromJson<Club>(request.downloadHandler.text);
            return club;
        }
        
        public static async Task<bool> JoinClub(string clubId, string userId)
        {
            var url = $"{BaseUrl}/{ClubPath}/{clubId}/join/{userId}";
            using var request = UnityWebRequest.Post(url, "", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to join club: {request.error}");
                return false;
            }

            return true;
        }
        
        public static async Task<bool> LeaveClub(string clubId, string userId)
        {
            var url = $"{BaseUrl}/{ClubPath}/{clubId}/leave/{userId}";
            using var request = UnityWebRequest.Post(url, "", "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to leave club: {request.error}");
                return false;
            }

            return true;
        }
        
        public static async Task<List<User>> GetClubMembers(string clubId)
        {
            var url = $"{BaseUrl}/{ClubPath}/{clubId}/members";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get club members: {request.error}");
                return null;
            }

            var userList = JsonUtility.FromJson<SerializableList<User>>("{\"list\": " + request.downloadHandler.text + "}");
            return userList.list;
        }
        
        #endregion Clubs

        #region Invites

        public static async Task<bool> SendInvite(Invite invite)
        {
            var url = $"{BaseUrl}/{InvitePath}";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(invite), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to send invite: {request.error}");
                return false;
            }

            return true;
        }

        public static async Task<Invite> HasInvite(string userId)
        {
            var url = $"{BaseUrl}/{InvitePath}/{userId}";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;
            

            return JsonUtility.FromJson<Invite>(request.downloadHandler.text);
        }

        public static async Task<bool> DeleteAllInvites(string hostId)
        {
            var url = $"{BaseUrl}/{InvitePath}/{hostId}";
            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to delete all invites: {request.error}");
                return false;
            }
            
            LogMessage("Deleted all invites for " + hostId);
            return true;
        }
        
        public static async Task<bool> DeleteInvite(string hostId, string userId)
        {
            var url = $"{BaseUrl}/{InvitePath}/{hostId}/{userId}";
            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to delete invite: {request.error}");
                return false;
            }
            
            return true;
        }
        
        #endregion Invites
        
        #region ClubBattleSearch
        
        public static async Task<List<ClubBattleSearch>> GetSearchingClubs()
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/search";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get searching clubs: {request.error}");
                return null;
            }

            var clubBattleList = JsonUtility.FromJson<SerializableList<ClubBattleSearch>>("{\"list\": " + request.downloadHandler.text + "}");
            return clubBattleList.list;
        }

        public static async Task<ClubBattleSearch> CreateClubBattleSearch(ClubBattleSearch search)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/search";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(search), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to create searching clubs: {request.error}");
                return null;
            }

            return JsonUtility.FromJson<ClubBattleSearch>(request.downloadHandler.text);
        }
        
        public static async Task<bool> DeleteClubBattleSearch(string searchId)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/search/{searchId}";
            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to delete searching clubs: {request.error}");
                return false;
            }

            return true;
        }


        public static async Task<ClubBattle> ChallengeClub(string searchId, ClubBattleSearch challengeRequest)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/challenge/{searchId}";
            using var request = UnityWebRequest.Post(url, JsonUtility.ToJson(challengeRequest), "application/json");
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to challenge searching clubs: {request.error}");
                return null;
            }

            return JsonUtility.FromJson<ClubBattle>(request.downloadHandler.text);
        }

        public static async Task<ClubBattleStatus> GetClubBattleStatus(string clubId)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/status/{clubId}";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get searching clubs: {request.error}");
                return null;
            }

            return JsonUtility.FromJson<ClubBattleStatus>(request.downloadHandler.text);
        }


        public static async Task<ClubBattle> GetClubBattle(string clubBattleId)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/{clubBattleId}";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get searching clubs: {request.error}");
                return null;
            }
            return JsonUtility.FromJson<ClubBattle>(request.downloadHandler.text);
        }

        public static async Task<ClubBattle> UpdateClubBattleScore(string clubBattleId, string userId, int courseIndex,
            int score)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/update-score/{clubBattleId}/{userId}/{courseIndex}/{score}";
            using var request = UnityWebRequest.Get(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to get searching clubs: {request.error}");
                return null;
            }

            return JsonUtility.FromJson<ClubBattle>(request.downloadHandler.text);
        }
        
        public static async Task<bool> DeleteClubBattle(string clubBattleId)
        {
            var url = $"{BaseUrl}/{ClubBattlePath}/{clubBattleId}";
            using var request = UnityWebRequest.Delete(url);
            PrepareRequest(request);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogMessage($"Failed to delete searching clubs: {request.error}");
                return false;
            }

            return true;
        }
        
        #endregion

        #region Utility Methods

        private static void PrepareRequest(UnityWebRequest request)
        {
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-API-key", ApiKey);
            if (!EnableCheckCertificates)
                request.certificateHandler = new DummyCertificateHandler();
        }

        private static void LogMessage(string message, bool isError = false)
        {
            if (!EnableLogging) return;

            if (isError)
                Debug.LogError($"[RemoteBackend] {message}");
            else
                Debug.Log($"[RemoteBackend] {message}");
        }


        /// Hack because JsonUtility doesn't deserialize pure arrays
        [Serializable]
        private class SerializableList<T>
        {
            public List<T> list = new();
        }
        
        #endregion
    }
}
