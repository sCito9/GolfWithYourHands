using UnityEngine;

namespace EventArgs
{
    public class AbschlagTrajectoryEventArgs : System.EventArgs
    {
        public readonly Vector3 AssociatedStartPositionPayload;
        public readonly Vector3 AssociatedLinearVelocityPayload;

        public AbschlagTrajectoryEventArgs(Vector3 associatedStartPositionPayload, Vector3 associatedLinearVelocityPayload)
        {
            AssociatedStartPositionPayload = associatedStartPositionPayload;
            AssociatedLinearVelocityPayload = associatedLinearVelocityPayload;
        }
    }
}
