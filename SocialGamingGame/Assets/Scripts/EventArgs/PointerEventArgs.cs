using UnityEngine.EventSystems;

namespace EventArgs
{
    public class PointerEventArgs : System.EventArgs
    {
        public readonly PointerEventData AssociatedPointerPayload;
        public readonly bool Pressed;

        public PointerEventArgs(PointerEventData pointerPayload, bool pressed = true) : base()
        {
            AssociatedPointerPayload = pointerPayload;
            Pressed = pressed;
        }
    }
}