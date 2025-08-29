using System;
using Unity.Mathematics;
using UnityEngine;

namespace DataBackend.Models
{
    [Serializable]
    public class Course
    {
        public string id;
        public string name;
        public string authorId;

        public float3 startPosition;
        public float3 endPosition;
        public double3 mapOrigin;
        public float3[] sandPositions;

        public Course(string name, string authorId, float3 startPosition, float3 endPosition,
            double3 mapOrigin, float3[] sandPositions)
        {
            this.name = name;
            this.authorId = authorId;
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.mapOrigin = mapOrigin;
            this.sandPositions = sandPositions;
        }


        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}