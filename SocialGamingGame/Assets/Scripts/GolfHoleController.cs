using UnityEngine;
using R3;
using Sensoren;

public class GolfHoleController : MonoBehaviour
{
    
    [SerializeField] private GolfHoleSensor trefferSensor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trefferSensor.SensorTriggered.Subscribe(Score).AddTo(this);
    }
    
    public void Score(System.EventArgs e)
    {
        Debug.Log("Scored! WOOOOOOOO");
    }
}
