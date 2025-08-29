using EventArgs;
using R3;
using R3.Triggers;

namespace Sensoren
{
    public class PositionSensor : Sensor
    {
        private void Awake()
        {
            SensorTriggered = this.UpdateAsObservable()
                .Select(_ => new CameraPosEventArgs(transform.position, 0f) as System.EventArgs);
        }
    }
}
