using EventArgs;
using R3;
using UnityEngine;

public class AbschlagTrajectoryGuide : MonoBehaviour
{
    [SerializeField] private int nLineVertices = 20;
    [SerializeField] [Range(1, 100)] private float totalTime = 2f;
    [SerializeField] private float width = 0.001f;
    private Vector3 g;
    private LineRenderer _lineRenderer;
    
    [SerializeField] private Gradient gradient;
    
    [SerializeField] private GolfAbschlagController golfAbschlagController;
    [SerializeField] private CameraController cameraController;

    private void Awake()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = 2 * totalTime * width;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.colorGradient = gradient;
        
        g = new Vector3(0, -9.81f, 0);
    }

    private void Start()
    {
        golfAbschlagController.EnableAbschlagVizStream.
            Subscribe(OnAbschlagButtonChange).AddTo(this);
        golfAbschlagController.AbschlagTriggeredStream
            .Subscribe(_ => OnAbschlagCancelled(true)).AddTo(this);
    }
    
    private void OnAbschlagButtonChange(bool enable)
    {
        if (enable)
            RegisterTrajectory();
        else
            OnAbschlagCancelled(true);
    }
    
    private BehaviorSubject<bool> _abschlagCancelled = new(false);
    private void OnAbschlagCancelled(bool ignored)
    {
        _lineRenderer.positionCount = 0;
        _abschlagCancelled.OnNext(ignored);
    }
    
    private void RegisterTrajectory()
    {
        if (golfAbschlagController == null || cameraController == null)
        {
            Debug.Log("Required observable is NULL");
            return;
        }

        golfAbschlagController.AbschlagStärke.WithLatestFrom(
                second: cameraController.CamTargetDir,
                (str, v) =>
                    new AbschlagTrajectoryEventArgs(transform.position,
                        v.normalized * str * 2.5f * 20f))
            .TakeUntil(_abschlagCancelled.Skip(1).Do(_ => Debug.Log("Abschlag cancelled")))
            .Subscribe(DrawTrajectoryGuide)
            .AddTo(this);
    }
    
    private void DrawTrajectoryGuide(AbschlagTrajectoryEventArgs e)
    {
        var launchPos = e.AssociatedStartPositionPayload;
        var v = e.AssociatedLinearVelocityPayload;
        
        var timeStep = totalTime / (nLineVertices - 1);
        
        _lineRenderer.positionCount = nLineVertices;
        for (var i = 0; i < nLineVertices; i++)
        {
            var t = i * timeStep;
            var point = launchPos + v * t + .5f * g * t * t;
            _lineRenderer.SetPosition(i, point);
        }
    }

    private void OnDisable()
    {
        OnAbschlagCancelled(true);
    }
}
