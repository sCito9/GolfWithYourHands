using UnityEngine;

namespace EventArgs
{
    public class DragControlEventArgs : System.EventArgs
    {
        public readonly Vector2 AssociatedPositionPayload;
        public readonly bool AssociatedTogglePayload;

        public DragControlEventArgs(Vector2 pos, bool toggle) : base()
        {
            AssociatedPositionPayload = pos;
            AssociatedTogglePayload = toggle;
        }
    }
}