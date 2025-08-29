using System;
using System.Collections;
using System.Collections.Generic;
using DataBackend.Models;
using UnityEngine;

namespace DataBackend
{
    public static class DataLoader
    {
        #region User

        /// <summary>
        ///     Create a new user. Passes null to the callback on error or timeout
        /// </summary>
        /// <param name="user">User with partial fields filled out</param>
        /// <param name="callback">The callback that will be called once the user is created</param>
        public static IEnumerator CreateUser(User user, Action<User> callback)
        {
            var task = RemoteBackend.CreateUser(user);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdUser = task.Result;

            callback(createdUser);
        }

        /// <summary>
        ///     Get a persisted user. Passes null to the callback on error or timeout
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="callback">Callback that will be called with the returned user</param>
        public static IEnumerator GetUser(string userId, Action<User> callback)
        {
            // Get the user from the backend
            var task = RemoteBackend.GetUser(userId);
            yield return new WaitUntil(() => task.IsCompleted);
            var user = task.Result;

            callback(user);
        }

        /// <summary>
        /// Delete a user by ID. Passes false if unsuccessful
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="callback">Callback will be called with success value</param>
        /// <returns></returns>
        public static IEnumerator DeleteUser(string userId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteUser(userId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;
            
            callback(result);
        }

        #endregion User

        #region Friends

        /// <summary>
        ///     Get a list of users from their IDs
        /// </summary>
        /// <param name="userIds">List of user IDs</param>
        /// <param name="callback">Function to handle the returned Users</param>
        /// <returns></returns>
        public static IEnumerator GetUserList(List<string> userIds, Action<List<User>> callback)
        {
            if (userIds == null || userIds.Count == 0)
            {
                callback(null);
                yield break;
            }

            var task = RemoteBackend.GetUserList(userIds);
            yield return new WaitUntil(() => task.IsCompleted);
            var users = task.Result;
            callback(users);
        }


        /// <summary>
        ///     Send a friend request
        /// </summary>
        /// <param name="fromId">User sending the request</param>
        /// <param name="toId">User receiving the request</param>
        /// <param name="callback">Gets passed true on success</param>
        /// <returns></returns>
        public static IEnumerator SendFriendRequest(string fromId, string toId, Action<bool> callback)
        {
            var task = RemoteBackend.SendFriendRequest(fromId, toId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            // Refresh the session on success
            if (result)
                SessionManager.Instance.Refresh();

            callback(result);
        }

        /// <summary>
        ///     Accept a friend request
        /// </summary>
        /// <param name="fromId">User accepting the request</param>
        /// <param name="toId">User that gets accepted</param>
        /// <param name="callback">Gets passed true on success</param>
        /// <returns></returns>
        public static IEnumerator AcceptFriendRequest(string fromId, string toId, Action<bool> callback)
        {
            var task = RemoteBackend.AcceptFriendRequest(fromId, toId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            // Refresh the session on success
            if (result)
                SessionManager.Instance.Refresh();

            callback(result);
        }


        /// <summary>
        ///     Decline a friend request
        /// </summary>
        /// <param name="fromId">User issuing the decline</param>
        /// <param name="toId">User that gets rejected</param>
        /// <param name="callback">Gets passed true on success</param>
        /// <returns></returns>
        public static IEnumerator DeclineFriendRequest(string fromId, string toId, Action<bool> callback)
        {
            var task = RemoteBackend.DeclineFriendRequest(fromId, toId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            // Refresh the session on success
            if (result)
                SessionManager.Instance.Refresh();

            callback(result);
        }


        /// <summary>
        ///     Remove a friend
        /// </summary>
        /// <param name="fromId">User issuing the removal</param>
        /// <param name="toId">User to be unfriended</param>
        /// <param name="callback">Gets passed true on success</param>
        /// <returns></returns>
        public static IEnumerator RemoveFriend(string fromId, string toId, Action<bool> callback)
        {
            var task = RemoteBackend.RemoveFriend(fromId, toId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            // Refresh the session on success
            if (result)
                SessionManager.Instance.Refresh();

            callback(result);
        }

        #endregion Friends

        #region Courses

        /// <summary>
        ///     Create a new course. Passes null to the callback on error
        /// </summary>
        /// <param name="course">The course to persist</param>
        /// <param name="callback">Gets passed the created course or null</param>
        /// <returns></returns>
        public static IEnumerator CreateCourse(Course course, Action<Course> callback)
        {
            var task = RemoteBackend.CreateCourse(course);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdCourse = task.Result;

            callback(createdCourse);
        }
        
        /// <summary>
        ///     Get the data of a single course
        /// </summary>
        /// <param name="courseId">The course to retrieve</param>
        /// <param name="callback">Gets passed the course or null</param>
        /// <returns></returns>
        public static IEnumerator GetCourse(string courseId, Action<Course> callback)
        {
            var task = RemoteBackend.GetCourse(courseId);
            yield return new WaitUntil(() => task.IsCompleted);
            var course = task.Result;

            callback(course);
        }

        /// <summary>
        ///     Get all courses. Passes null to the callback on error
        /// </summary>
        /// <param name="callback">Gets passed the list of courses or null</param>
        /// <returns></returns>
        public static IEnumerator GetCourses(Action<List<Course>> callback)
        {
            var task = RemoteBackend.GetCourses();
            yield return new WaitUntil(() => task.IsCompleted);
            var courses = task.Result;

            callback(courses);
        }

        #endregion Courses
        
        #region Clubs

        /// <summary>
        ///     Create a new club. The ID will be generated
        /// </summary>
        /// <param name="club">The boilerplate for the club</param>
        /// <param name="callback">Gets passed the created club on success, null otherwise</param>
        /// <returns></returns>
        public static IEnumerator CreateClub(Club club, Action<Club> callback)
        {
            var task = RemoteBackend.CreateClub(club);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdClub = task.Result;

            if (createdClub != null)
            {
                SessionManager.Instance.Refresh();
            }
            
            callback(createdClub);
        }
        
        public static IEnumerator GetClub(string clubId, Action<Club> callback)
        {
            var task = RemoteBackend.GetClub(clubId);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdClub = task.Result;

            callback(createdClub);
        }
        
        public static IEnumerator JoinClub(string clubId, Action<bool> callback)
        {
            var task = RemoteBackend.JoinClub(clubId, SessionManager.Instance.User.id);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            if (result)
            {
                SessionManager.Instance.Refresh();
            }

            callback(result);
        }
        
        public static IEnumerator LeaveClub(string clubId, Action<bool> callback)
        {
            var task = RemoteBackend.LeaveClub(clubId, SessionManager.Instance.User.id);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            if (result)
            {
                SessionManager.Instance.Refresh();
            }

            callback(result);
        }
        
        
        public static IEnumerator KickFromClub(string clubId, string userId, Action<bool> callback)
        {
            var task = RemoteBackend.LeaveClub(clubId, userId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            if (result)
            {
                SessionManager.Instance.Refresh();
            }

            callback(result);
        }

        
        public static IEnumerator GetClubMembers(string clubId, Action<List<User>> callback)
        {
            var task = RemoteBackend.GetClubMembers(clubId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        /*
        public static IEnumerator CreateClubBattleSearch(ClubBattleSearch clubBattleSearch, Action<bool> callback)
        {
            var task = RemoteBackend.CreateClubBattleSearch(clubBattleSearch);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdClubBattleSearch = task.Result;

            if (createdClubBattleSearch != null)
            {
                SessionManager.Instance.Refresh();
            }
            
            callback(createdClubBattleSearch);
        }
        
        public static IEnumerator DeleteClubBattleSearch(string clubId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteClubBattleSearch(clubId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            if (result)
            {
                SessionManager.Instance.Refresh();
            }

            callback(result);
        }

        public static IEnumerator DeleteUser(string userId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteUser(userId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        */

        
        #endregion Clubs

        #region Invite
        public static IEnumerator SendInvite(Invite invite, Action<bool> callback)
        {
            var task = RemoteBackend.SendInvite(invite);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        public static IEnumerator HasInvite(string userId, Action<Invite> callback)
        {
            var task = RemoteBackend.HasInvite(userId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }


        public static IEnumerator DeleteAllInvites(string hostId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteAllInvites(hostId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        
        public static IEnumerator DeleteInvite(string hostId, string receiverId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteInvite(hostId, receiverId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        
        #endregion Invite
        
        #region ClubBattles

        public static IEnumerator GetSearchingClubs(Action<List<ClubBattleSearch>> callback)
        {
            var task = RemoteBackend.GetSearchingClubs();
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }

        public static IEnumerator CreateClubBattleSearch(ClubBattleSearch clubBattleSearch, Action<ClubBattleSearch> callback)
        {
            var task = RemoteBackend.CreateClubBattleSearch(clubBattleSearch);
            yield return new WaitUntil(() => task.IsCompleted);
            var createdClubBattleSearch = task.Result;

            callback(createdClubBattleSearch);
        }
        
        public static IEnumerator DeleteClubBattleSearch(string clubId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteClubBattleSearch(clubId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        public static IEnumerator ChallengeClub(string searchId, ClubBattleSearch challengeRequest, Action<ClubBattle> callback)
        {
            var task = RemoteBackend.ChallengeClub(searchId, challengeRequest);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        public static IEnumerator GetClubBattleStatus(string clubId, Action<ClubBattleStatus> callback)
        {
            var task = RemoteBackend.GetClubBattleStatus(clubId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }

        public static IEnumerator GetClubBattle(string clubBattleId, Action<ClubBattle> callback)
        {
            var task = RemoteBackend.GetClubBattle(clubBattleId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }
        
        public static IEnumerator UpdateClubBattleScore(string clubBattleId, string userId, int courseIndex, int score, Action<ClubBattle> callback)
        {
            var task = RemoteBackend.UpdateClubBattleScore(clubBattleId, userId, courseIndex, score);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }

        public static IEnumerator DeleteClubBattle(string clubBattleId, Action<bool> callback)
        {
            var task = RemoteBackend.DeleteClubBattle(clubBattleId);
            yield return new WaitUntil(() => task.IsCompleted);
            var result = task.Result;

            callback(result);
        }

        #endregion ClubBattles
    }
}