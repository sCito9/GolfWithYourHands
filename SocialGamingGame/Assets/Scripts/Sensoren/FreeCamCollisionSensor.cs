using EventArgs;
using R3;
using R3.Triggers;
using UnityEngine;

namespace Sensoren
{
    public class FreeCamCollisionSensor : Sensor
    {
        private BehaviorSubject<Collider> _latestTrigger;
        
        private void Awake()
        {
            var dummy = new GameObject().AddComponent<SphereCollider>();
            _latestTrigger = new BehaviorSubject<Collider>(dummy);

            gameObject.AddComponent<ObservableTriggerTrigger>()
                .OnTriggerStayAsObservable()
                .Subscribe(_latestTrigger.OnNext).AddTo(this);
            
            SensorTriggered = _latestTrigger
                .Select(c => new SensorEventArgs(c) as System.EventArgs);
        }
    }
}