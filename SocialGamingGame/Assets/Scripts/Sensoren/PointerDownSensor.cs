using EventArgs;
using R3;
using R3.Triggers;

namespace Sensoren
{
    public class PointerDownSensor : Sensor
    {
        private void Awake()
        {
            SensorTriggered = this.gameObject.AddComponent<ObservablePointerDownTrigger>()
                .OnPointerDownAsObservable()
                .Select(e => new PointerEventArgs(e, true) as System.EventArgs);
        }
    }
}