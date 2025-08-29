using UnityEngine;

namespace EventArgs
{
    public class CameraLookControlEventArgs : System.EventArgs
    {
        public readonly Vector3 AssociatedCameraControlInput;

        public CameraLookControlEventArgs(Vector2 look, float zoom = 0f)
        {
            AssociatedCameraControlInput = new Vector3(look.x, look.y, zoom);
        }
    }
}