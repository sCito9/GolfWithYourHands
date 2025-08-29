using UnityEngine;

namespace EventArgs
{
    public class AbschlagStartEventArgs : System.EventArgs
    {
        public readonly Vector3 AssociatedBallPos;
        public readonly Vector3 AssociatedTargetPos;

        public AbschlagStartEventArgs(Vector3 ballPos, Vector3 targetPos) : base()
        {
            AssociatedBallPos = ballPos;
            AssociatedTargetPos = targetPos;
        }
    }
}