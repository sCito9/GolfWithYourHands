using R3;
using UnityEngine;

namespace Sensoren
{
    public abstract class Sensor : MonoBehaviour
    {
        public Observable<System.EventArgs> SensorTriggered;
    }
}