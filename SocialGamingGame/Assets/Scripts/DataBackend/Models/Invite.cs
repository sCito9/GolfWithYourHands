using System;
using UnityEngine;

namespace DataBackend.Models
{
    [Serializable]
    public class Invite
    {
        public string hostId;
        public string receiverId;
        public string courseName;
        public string hostName;
        public string lobbyId;

        public Invite(string hostId, string receiverId, string courseName, string hostName, string lobbyId)
        {
            this.hostId = hostId;
            this.receiverId = receiverId;
            this.courseName = courseName;
            this.hostName = hostName;
            this.lobbyId = lobbyId;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}