using EventArgs;
using R3;
using R3.Triggers;

namespace Sensoren
{
    public class GroundSensor : Sensor
    {
        private void Awake()
        {
            SensorTriggered = gameObject.AddComponent<ObservableCollisionTrigger>()
                .OnCollisionStayAsObservable()
                .Select(e => new SensorEventArgs(e) as System.EventArgs);
        }
    }
}
