using UnityEngine;

namespace EventArgs
{
    public class CameraPosEventArgs : System.EventArgs
    {
        public readonly Vector3 AssociatedCamAnker;
        public readonly float AssociatedCamDistance;

        public CameraPosEventArgs( Vector3 camAnker, float camDistance)
        {
            AssociatedCamAnker = camAnker;
            AssociatedCamDistance = camDistance;
        }
    }
}
