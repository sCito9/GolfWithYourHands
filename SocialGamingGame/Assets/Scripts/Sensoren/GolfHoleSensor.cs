using EventArgs;
using R3;
using R3.Triggers;
using UnityEngine;

namespace Sensoren
{
    public class GolfHoleSensor : Sensor
    {
        private void Awake()
        {
            SensorTriggered = gameObject.AddComponent<ObservableCollisionTrigger>()
                .OnCollisionEnterAsObservable()
                .Where(c => c.transform.CompareTag("GolfBall"))
                .Select(e => new SensorEventArgs(e) as System.EventArgs)
                .Share();
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
    }
}
