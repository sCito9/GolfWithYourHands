using EventArgs;
using R3;
using R3.Triggers;

namespace Sensoren
{
    public class PointerUpSensor : Sensor
    {
        private void Awake()
        {
            SensorTriggered = this.gameObject.AddComponent<ObservablePointerUpTrigger>()
                .OnPointerUpAsObservable()
                .Select(e => new PointerEventArgs(e, false) as System.EventArgs);
        }
    }
}
