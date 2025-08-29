using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


namespace DataBackend.Models
{
    [Serializable]
    public class ClubBattle
    {
        // Dealt with on the server
        //public const int golfCoursesPerBattle = 5;
        //public const int maxScorePerHole = 10;
        public string id;
        public string club1Id;
        public string club2Id;
        public int participantsPerClub;
        public List<string> participantIdsClub1;
        public List<string> participantIdsClub2;
        public List<ScoreEntry> club1Scores;
        public List<ScoreEntry> club2Scores;
        public List<string> courseIds;

        // Time properties
        public static readonly TimeSpan battleDuration = TimeSpan.FromDays(5);
        public static readonly TimeSpan timeAfterBattle = TimeSpan.FromDays(2);
        public long startTime;
        public long endTime;
        public long absoluteEndTime;

        public DateTime StartTime => DateTime.FromBinary(startTime);
        public DateTime EndTime => DateTime.FromBinary(endTime);
        public DateTime AbsoluteEndTime => DateTime.FromBinary(absoluteEndTime);
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class ScoreEntry
    {
        public string userId;
        public List<int> score;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}