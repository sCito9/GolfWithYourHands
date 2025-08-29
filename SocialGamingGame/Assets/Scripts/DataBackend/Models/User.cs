using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataBackend.Models
{
    [Serializable]
    public class User
    {
        public string id;
        public string name;
        public string clubId;

        public List<string> friendIds;
        public List<string> pendingFriendRequestIds;
        public List<string> sentFriendRequestIds;

        public User()
        {
        }

        public User(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}