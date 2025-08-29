using UnityEngine;

namespace EventArgs
{
    public class AbschlagControlEventArgs : System.EventArgs
    {
        public readonly Vector3 Direction;
        public readonly float Strength;

        public AbschlagControlEventArgs(Vector3 dir, float str) : base()
        {
            Direction = dir;
            Strength = str;
        }
    }
}