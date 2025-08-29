using System.Collections;
using EventArgs;
using InputAssets;
using UnityEngine;
using R3;
using Sensoren;
using UnityEngine.UI;

public class GolfAbschlagController : MonoBehaviour
{
    [SerializeField] private GolfBallController golfBallController;
    private PositionSensor _golfBallPositionSensor;
    public BehaviorSubject<Observable<CameraPosEventArgs>> BallPositionStream { get; }
        = new (Observable.Never<CameraPosEventArgs>());
    
    [Header("Input")]
    [SerializeField] private InputActionBasedFirstPersonControllerInput input;
    [SerializeField] private Canvas inputCanvas;
    [SerializeField] private GameObject virtualStick;
    [SerializeField] private GameObject abschlagDragPointerArea;
    private PointerUpSensor _abschlagDragPointerSensor;
    [SerializeField] private GameObject abschlagDragButton;
    [SerializeField] private Image abschlagStärkeVisualizer;
    public BehaviorSubject<bool> EnableAbschlagVizStream { get; } = new (false);
    public BehaviorSubject<bool> SwitchFreeCamStream { get; } = new (false);
    
    private bool _isPlayerFreeCam;
    public ReactiveProperty<float> AbschlagStärke = new(1f);
    [SerializeField] private float baseStrength = 2.5f;
    
    [SerializeField] private float waitTime = .8f;
    private Coroutine _waitCoroutine;
    private bool _abschlagsbereit;
    private bool _abschlagInBewegung;
    
    private bool _isMp = false;
    private bool _locked = false;
    public BehaviorSubject<bool> MyTurn { get; } = new(false);
    
    [SerializeField] private bool waitForStartSignal = true;
    private bool _started;
    
    private void Start()
    {
        inputCanvas.gameObject.SetActive(false);
        if (waitForStartSignal)
        {
            return;
        }
        StartUp();
    }
    

    public void StartUp()
    {
        if (_started) return;
        Debug.Log("STARTING GAMEPLAY");
        _started = true;
        inputCanvas.gameObject.SetActive(true);
        
        var inputAbschlagCancelled = input.AbschlagCancelled
                .Where(_ => !_locked)
                .WithLatestFrom(MyTurn, (args,turn) => (args,turn))
                .Where(x => x.turn)
                .Select(x => x.args)
                .Share();
        inputAbschlagCancelled
            .Subscribe(OnAbschlagCancelled).AddTo(this);
        inputAbschlagCancelled
            .Select(disable => _abschlagInBewegung || disable)
            .Subscribe(disable => EnableAbschlagVizStream.OnNext(!disable)).AddTo(this);
        EnableAbschlagVizStream
            .Subscribe(enable => abschlagStärkeVisualizer.gameObject.SetActive(enable)).AddTo(this);
        input.DragToSetStrControl
            .Where(_ => !_locked)
            .Where(_ => _waitCoroutine == null)
            .Where(args => args.AssociatedTogglePayload)
            .Subscribe(SetStaerke).AddTo(this);
        AbschlagStärke
            .Subscribe(str => abschlagStärkeVisualizer.fillAmount = str).AddTo(this);
        
        _golfBallPositionSensor = golfBallController.gameObject.GetComponent<PositionSensor>();
        Debug.Log($"pos sensor: {_golfBallPositionSensor != null}");
        golfBallController.BallStoppedStream
            .Where(_ => !_locked)
            .Subscribe(OnBallStopped).AddTo(this);
        input.SwitchFreeCam
            .Where(_ => !_locked)
            .Where(_ => !_abschlagInBewegung && _waitCoroutine == null)
            .Select(_ => SwitchFreeCam())
            .Subscribe(SwitchFreeCamStream.OnNext).AddTo(this);
        virtualStick.SetActive(false);
        abschlagDragButton.SetActive(true);
        
        _abschlagDragPointerSensor = abschlagDragPointerArea.GetComponent<PointerUpSensor>();
        abschlagDragPointerArea.SetActive(false);
    }

    private void SetStaerke(System.EventArgs args)
    {
        if (args is not DragControlEventArgs eventArgs) return;
        
        AbschlagStärke.Value = Mathf.Pow((Screen.height - eventArgs.AssociatedPositionPayload.y) /
                                             UnityEngine.Device.Screen.height, .8f);
    }
    
    private BehaviorSubject<AbschlagControlEventArgs> AbschlagTriggeredSubject { get; } = new(null);
    public Observable<AbschlagControlEventArgs> AbschlagTriggeredStream => AbschlagTriggeredSubject.Skip(1);
    protected virtual void OnAbschlagTriggered(System.EventArgs ignored)
    {
        if (!_abschlagsbereit) return;
        
        _abschlagInBewegung = true;
        _abschlagsbereit = false;
        AbschlagTriggeredSubject
            .OnNext(new AbschlagControlEventArgs(transform.forward, baseStrength * AbschlagStärke.Value));
    }
    
    private void OnAbschlagCancelled(bool e)
    {
        if (e)
        {
            abschlagDragPointerArea.SetActive(false);
        }
        else
        {
            abschlagDragPointerArea.SetActive(true);
            _abschlagDragPointerSensor.SensorTriggered
                .Take(1)
                .TakeUntil(input.AbschlagCancelled
                    .WithLatestFrom(MyTurn, (args,turn) => (args,turn))
                    .Where(x => x is { turn: true, args: true }))
                .Subscribe(OnAbschlagTriggered).AddTo(this);
        }
    }

    private BehaviorSubject<AbschlagStartEventArgs> AbschlagStartSubject { get; } = new(null);
    public Observable<AbschlagStartEventArgs> AbschlagStartStream => AbschlagStartSubject.Skip(1);
    public void StartAbschlag()
    {
        Debug.Log("Playing");
        _abschlagInBewegung = false;
        MyTurn.OnNext(true);
        inputCanvas.gameObject.SetActive(true);
        BallPositionStream.OnNext(
            _golfBallPositionSensor.SensorTriggered
            .Where(_ => _abschlagInBewegung)
            .Select(args => (CameraPosEventArgs)args));
        AbschlagStartSubject
            .OnNext(new AbschlagStartEventArgs(golfBallController.transform.position, Vector3.zero));
        
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        _waitCoroutine = StartCoroutine(WaitForEnableAbschlagControls(waitTime));
    }
    
    public void SpectateAbschlag(GameObject activeBall)
    {
        //Debug.Log("Spectating");
        inputCanvas.gameObject.SetActive(false);
        if (!activeBall)
        {
            Debug.LogError("Ball to spectate is null.");
            //StartAbschlag();
            return;
        }
        
        MyTurn.OnNext(false); /* Just in case */
        var sensor = activeBall.GetComponent<PositionSensor>();
        if (sensor)
            BallPositionStream.OnNext(
                sensor.SensorTriggered
                .Select(args => (CameraPosEventArgs)args)
                .Distinct());
        AbschlagStartSubject
            .OnNext(new AbschlagStartEventArgs(activeBall.transform.position, Vector3.zero));
    }
    
    private IEnumerator WaitForEnableAbschlagControls(float time)
    {
        yield return new WaitForSeconds(time);
        _abschlagsbereit = true;
        if (!_isMp) MyTurn.OnNext(true);
        _waitCoroutine = null;
    }

    private void OnBallStopped(System.EventArgs ignored)
    {
        _abschlagInBewegung = false;
        MyTurn.OnNext(false);
        FinishTurnSubject.OnNext(true);
        if (_isMp) return;
        
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        _waitCoroutine = StartCoroutine(WaitForEnableAbschlagControls(waitTime));
    }
    
    private BehaviorSubject<bool> FinishTurnSubject { get; } = new (false);
    public Observable<bool> FinishTurnStream => FinishTurnSubject.Skip(1);

    public void SetMp()
    {
        _isMp = true;
    }

    private bool SwitchFreeCam()
    {
        _isPlayerFreeCam = !_isPlayerFreeCam;
        if (!_isPlayerFreeCam)
        {
            virtualStick.SetActive(false);
            abschlagDragButton.SetActive(true);
            if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
            _waitCoroutine = StartCoroutine(WaitForEnableAbschlagControls(waitTime));
        }
        else
        {
            virtualStick.SetActive(true);
            abschlagDragButton.SetActive(false);
            _abschlagsbereit = false;
        }
        return _isPlayerFreeCam;
    }

    public void ForceFreeCam(bool enable = true, bool zuAbschlag = false)
    {
        _isPlayerFreeCam = _locked = enable;
        virtualStick.SetActive(enable);
        abschlagDragButton.SetActive(!enable);
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        
        if (enable)
        {
            SwitchFreeCamStream.OnNext(true);
            PauseBall();
            Observable.TimerFrame(1).Take(1).Subscribe(_ =>
                golfBallController.transform.localPosition = new Vector3(0, -10_000, 0)).AddTo(this);
        }
        else
        {
            golfBallController.transform.localPosition = Vector3.zero;
            SwitchFreeCamStream.OnNext(false);
            if (zuAbschlag) StartAbschlag();
        }
    }

    private void PauseBall(bool pause = true)
    {
        _abschlagsbereit = false;
        _abschlagInBewegung = false;
        PauseBallSubject.OnNext(true);
    }
    private BehaviorSubject<bool> PauseBallSubject { get; } = new(true);
    public Observable<bool> PauseBallStream => PauseBallSubject.Skip(1);

    public GolfBallController GetBallController()
    {
        return golfBallController;
    }
}
