using System;

namespace DataBackend.Models
{
    /// <summary>
    ///     Structure for session data that should be persisted
    ///     between game sessions
    /// </summary>
    [Serializable]
    public class SessionData
    {
        /// <summary>
        ///     The currently logged-in user
        /// </summary>
        public User user;

        /// <summary>
        ///     The club of the logged-in user
        /// </summary>
        public Club club;
    }
}