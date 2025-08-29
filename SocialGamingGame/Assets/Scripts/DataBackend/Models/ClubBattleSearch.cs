using System;
using System.Collections.Generic;


namespace DataBackend.Models
{
    [Serializable]
    public class ClubBattleSearch
    {
        public string id;
        public string clubId;
        public int numParticipants;
        public List<string> participantsIds;

        public ClubBattleSearch(string clubId, int numParticipants, List<string> participantsIds)
        {
            this.clubId = clubId;
            this.numParticipants = numParticipants;
            this.participantsIds = participantsIds;
        }
    }
}