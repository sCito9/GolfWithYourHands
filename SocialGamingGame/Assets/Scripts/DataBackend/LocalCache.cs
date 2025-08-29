using System.IO;
using DataBackend.Models;
using UnityEngine;

namespace DataBackend
{
    public static class LocalCache
    {
        private const string SessionName = "session";

        #region Sessions

        /// <summary>
        ///     Persist the provided session overwriting any existing session
        /// </summary>
        /// <param name="session">Session to save</param>
        public static void SaveSession(SessionData session)
        {
            var jsonString = JsonUtility.ToJson(session);

            File.WriteAllText($"{Application.persistentDataPath}/{SessionName}.json", jsonString);
        }

        /// <summary>
        ///     Try to load the last saved session. Return null if it doesn't exist
        /// </summary>
        /// <returns>Local Session or null if it doesn't exist</returns>
        public static SessionData TryLoadSession()
        {
            if (!File.Exists($"{Application.persistentDataPath}/{SessionName}.json"))
                return null;

            var fileContent = File.ReadAllText($"{Application.persistentDataPath}/{SessionName}.json");
            return JsonUtility.FromJson<SessionData>(fileContent);
        }

        /// <summary>
        ///     Delete the current session from local storage
        /// </summary>
        public static void DeleteSession()
        {
            if (!File.Exists($"{Application.persistentDataPath}/{SessionName}.json"))
                return;
            File.Delete($"{Application.persistentDataPath}/{SessionName}.json");
        }

        #endregion Sessions
    }
}