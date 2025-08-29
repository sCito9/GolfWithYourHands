using System;
using UnityEngine;

namespace DataBackend.Models
{
    [Serializable]
    public class ClubBattleStatus
    {
        public static string clubId;
        public string status; // searching, in_battle, idle
        public string searchId;
        public string battleId;
        public object details;
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}