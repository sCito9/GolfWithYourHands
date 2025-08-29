using System;
using System.Collections.Generic;
using System.Linq;
using DataBackend;
using EventArgs;
using UnityEngine;
using R3;
using Sensoren;
using Unity.Mathematics;
using Unity.Netcode;

public class GolfBallController : MonoBehaviour
{
    private Rigidbody _rb;
    
    [SerializeField] private GroundSensor bodenSensor;
    [SerializeField] private float friction = 3f;
    [SerializeField] private float sandbunkerFrictionMultiplier = 5f;
    [SerializeField] private float sandbunkerSeitenlänge = 30f;
    private List<Vector3> _sandbunkerPositionen = new();
    
    public GameObject golfHole;
    
    [SerializeField] private GolfAbschlagController golfAbschlagController;
    [SerializeField] private bool waitForStartSignal = true;
    private bool _started;
    
    private AudioMangagerScript _audioManager;
    
    public enum AbschlagZustände
    {
        Bereit, Gestartet, Ausgeführt
    }
    private AbschlagZustände AbschlagZustand { get; set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeAll;
        _rb.isKinematic = true;
        _audioManager = FindAnyObjectByType<AudioMangagerScript>();
    }

    private void Start()
    {
        if (waitForStartSignal)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
            return;
        }
        StartUp();
    }
    
    private void OnSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if (sceneName == "GameScene")
        {
            StartUp();
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
        }
    }

    public void StartUp()
    {
        if (_started) return;
        Debug.Log("STARTING BALL");
        _started = true;
        
        _rb.isKinematic = false;
        AbschlagZustand = AbschlagZustände.Bereit;
        if (golfHole == null)
        {
            golfHole = GameObject.FindGameObjectWithTag("GolfHole");
        }
        golfAbschlagController?.AbschlagTriggeredStream
            .Subscribe(AbschlagDurchführen).AddTo(this);
        golfAbschlagController?.PauseBallStream
            .Subscribe(PauseBall).AddTo(this);
        
        bodenSensor.SensorTriggered
            .Where(_ => AbschlagZustand == AbschlagZustände.Ausgeführt)
            .Subscribe(Friction).AddTo(this);

        /*if (SessionManager.Instance.currentCourse != null)
            SetSandbunker(SessionManager.Instance.currentCourse.sandPositions);*/
        
        PauseBall();
    }
        
    public void AbschlagDurchführen(AbschlagControlEventArgs e)
    {
        PauseBall(false);
        var dir = e.Direction.normalized;
        _rb.AddForce(dir * e.Strength, ForceMode.Impulse);
        
        AbschlagZustand = AbschlagZustände.Ausgeführt;
    }

    public void Friction(System.EventArgs e)
    {
        var frictionMultiplier = IsInSandbunker(transform.position) ? sandbunkerFrictionMultiplier : 1f;
        var speed = _rb.linearVelocity.magnitude;
        var deceleration = (_rb.linearVelocity.normalized * friction * frictionMultiplier * Time.deltaTime).magnitude;
        if (deceleration >= speed - .05f)
        {
            OnBallStopped();
            return;
        }
        _rb.linearVelocity -= _rb.linearVelocity.normalized * friction * frictionMultiplier * Time.deltaTime;
        _rb.angularVelocity -= _rb.angularVelocity.normalized * friction * frictionMultiplier * 10f * Time.deltaTime;
    }

    private bool IsInSandbunker(Vector3 position)
    {
        var maxCenterOffsetInSandbunker = sandbunkerSeitenlänge * 0.5f;
        var i = _sandbunkerPositionen.FindIndex(sandbunker // AABB-like check
            => Mathf.Abs(position.x - sandbunker.x) <= maxCenterOffsetInSandbunker
               && Mathf.Abs(position.z - sandbunker.z) <= maxCenterOffsetInSandbunker);
        switch (i)
        {
            case < 0:
                return false;
            case 0:
                return true;
            default:
                var match = _sandbunkerPositionen[i];
                _sandbunkerPositionen.RemoveAt(i);
                _sandbunkerPositionen.Insert(0, match);
                return true;
        }
    }

    public void SetSandbunker(List<GameObject> sandbunkerListe)
    {
        if (sandbunkerListe == null) return;
        _sandbunkerPositionen.Clear();
        foreach (var sandbunker in sandbunkerListe)
        {
            _sandbunkerPositionen.Add(sandbunker.transform.position);
        }
    }
    
    public void SetSandbunker(float3[] sandbunkerListe)
    {
        if (sandbunkerListe == null) return;
        _sandbunkerPositionen.Clear();
        foreach (var sandbunker in sandbunkerListe)
        {
            _sandbunkerPositionen.Add(sandbunker);
        }
    }

    private BehaviorSubject<AbschlagStartEventArgs> BallStoppedSubject { get; } = new(null);
    public Observable<AbschlagStartEventArgs> BallStoppedStream => BallStoppedSubject.Skip(1);

    public void OnBallStopped()
    {
        //Debug.Log("Ball stopped in Golf Ball Controller");
        PauseBall();
        
        AbschlagZustand = AbschlagZustände.Bereit;
        BallStoppedSubject
            .OnNext(new AbschlagStartEventArgs(transform.position, golfHole.transform.position));
    }

    public void PauseBall(bool paused = true)
    {
        _rb.linearVelocity = _rb.angularVelocity = Vector3.zero;
        _rb.constraints = paused ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        _rb.isKinematic = paused;
    }

    private void OnCollisionEnter(Collision other)
    {
        _audioManager.playCollision();
    }
}
