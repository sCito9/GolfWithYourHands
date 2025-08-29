
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataBackend.Models
{
    [Serializable]
    public class Club
    {
        public string id;
        public string name;
        public string description;
        
        public string leaderId;
        public List<string> memberIds;
        
        /// <summary>
        /// Constructor for Clubs
        /// </summary>
        /// <param name="id">ID of the Club</param>
        /// <param name="name">Name of the Club</param>
        /// <param name="description">Description of the Club</param>
        /// <param name="leaderId">ID of the leader</param>
        /// <param name="memberIds">IDs of the members</param>
        public Club(string id, string name, string description, string leaderId, List<string> memberIds)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.leaderId = leaderId;
            this.memberIds = memberIds;
        }

        public Club(string name, string description, string leaderId)
        {
            this.name = name;
            this.description = description;
            this.leaderId = leaderId;
        }
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
