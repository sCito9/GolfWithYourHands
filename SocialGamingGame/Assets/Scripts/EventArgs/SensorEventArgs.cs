using UnityEngine;

namespace EventArgs
{
    public class SensorEventArgs : System.EventArgs
    {
        public readonly Collision AssociatedCollisionPayload;
        public readonly Collider AssociatedColliderPayload;

        public SensorEventArgs(Collision collisionPayload) : base()
        {
            AssociatedCollisionPayload = collisionPayload;
            AssociatedColliderPayload = collisionPayload.collider;
        }
        
        public SensorEventArgs(Collider colliderPayload) : base()
        {
            AssociatedCollisionPayload = null;
            AssociatedColliderPayload = colliderPayload;
        }
    }
}