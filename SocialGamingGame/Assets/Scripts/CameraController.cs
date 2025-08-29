using System.Collections;
using EventArgs;
using InputAssets;
using R3;
using Sensoren;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GolfBallController golfballController;
    private float _golfballScale;
    [SerializeField] private GolfAbschlagController golfAbschlagController;
    [SerializeField] private bool waitForStartSignal = true;
    private bool _started;
    
    
    [Header("Input")]
    [SerializeField] private InputActionBasedFirstPersonControllerInput input;
    [SerializeField] private float sensitivity = 5f;
    [SerializeField] private float sensitivityBaseMultiplier = 0.001f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float distanceToBall = 1f;
    
    [SerializeField] private float timeToSmoothCam = .8f;
    private Coroutine _waitCoroutine;
    
    private Vector3 _camAnker;
    private float _camDistance;
    public ReactiveProperty<Vector3> CamTargetDir { get; } = new(Vector3.zero);
    private Vector2 _camRotation;
    
    private bool _isFreeCam;
    private SphereCollider _freeCamCollider;
    private FreeCamCollisionSensor _freeCamCollisionSensor;
    
    private void OnEnable()
    {
        if (golfballController != null) return;
        _isFreeCam = true;
    }

    private void GetCam()
    {
        var cameraObj = Camera.main?.gameObject;
        if (cameraObj != null)
        {
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("No MainCamera");
        }
    }
    

    private void Start()
    {
        if (waitForStartSignal)
        {
            return;
        }
        StartUp();
    }
    

    public void StartUp()
    {
        
        if (_started) return;
        Debug.Log("STARTING CAMERA");
        _started = true;
        
        _camAnker = transform.position;
        _camDistance = distanceToBall;
        
        GetCam();
        
        input.CameraControl
            .Where(v => v.AssociatedCameraControlInput != Vector3.zero)
            .Where(_ => _waitCoroutine == null)
            .Select(args => new CameraLookControlEventArgs(
                args.AssociatedCameraControlInput * sensitivity * sensitivityBaseMultiplier))
            .Subscribe(UpdateCameraLook).AddTo(this);
        UpdateCameraLook(new CameraLookControlEventArgs(Vector2.zero));
        
        _freeCamCollider = gameObject.GetComponent<SphereCollider>();
        _freeCamCollider.enabled = _isFreeCam;
        _freeCamCollisionSensor = gameObject.GetComponent<FreeCamCollisionSensor>();
        input.MovementControl
            .Where(_ => _isFreeCam)
            .Where(_ => _waitCoroutine == null)
            .Where(moveInput => moveInput != Vector2.zero)
            .WithLatestFrom(_freeCamCollisionSensor.SensorTriggered.Select(args => ((SensorEventArgs)args).AssociatedColliderPayload), (moveInput, other) =>
            {
                if (other == null || !other) return moveInput;
                
                var p = other.ClosestPoint(_camAnker);
                if (Vector3.Distance(p, _camAnker) > _freeCamCollider.radius) return moveInput;

                var moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y));
                var n = (_camAnker - p).normalized;
                if (p == _camAnker && Physics.Raycast(_camAnker, moveDirection, out var hit,
                        _freeCamCollider.radius /* moveDirection.magnitude */))
                {
                    n = (_camAnker - hit.point).normalized;
                }

                var dot = Vector3.Dot(moveDirection.normalized, n);
                if (dot >= 0) return moveInput;

                moveDirection = transform.InverseTransformDirection(moveDirection - dot * n);
                return new Vector2(moveDirection.x, moveDirection.z);
            })
            .Subscribe(FreeCamMove).AddTo(this);

        if (golfAbschlagController == null) return;
        
        golfAbschlagController.SwitchFreeCamStream
            .Skip(1)
            .Where(_ => _waitCoroutine == null)
            .Subscribe(SetFreeCam).AddTo(this);

        golfAbschlagController.BallPositionStream
            .Switch()
            .Where(_ => !_isFreeCam)
            .Subscribe(UpdateCameraPos).AddTo(this);
        
        golfAbschlagController.AbschlagStartStream
            .Subscribe(StartAbschlagMinigame).AddTo(this);
        
        if (golfballController == null) return;
        
        _golfballScale = golfballController.transform.lossyScale.x;
        
        golfballController.BallStoppedStream
            .Subscribe(RotateCamAtBallToFaceMark).AddTo(this);
    }

    
    private void StartAbschlagMinigame(AbschlagStartEventArgs args)
    {
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        _waitCoroutine = StartCoroutine(PlaceCamAtBallSmooth(timeToSmoothCam, args.AssociatedBallPos));
    }
    
    private void UpdateCameraLook(CameraLookControlEventArgs args)
    {
        var lookX = args.AssociatedCameraControlInput.x;
        var lookY = args.AssociatedCameraControlInput.y;
        
        if (_isFreeCam)
        {
            FreeCamLook(lookX, lookY);
            return;
        }
        
        _camDistance = Mathf.Clamp(_camDistance - args.AssociatedCameraControlInput.z, 0.2f, 1000f);

        _camRotation.x = (_camRotation.x + lookX) % (2*Mathf.PI);
        _camRotation.y = Mathf.Clamp(_camRotation.y + lookY, -.95f * Mathf.PI/2, .95f * Mathf.PI/2);

        CamTargetDir.Value = Quaternion.AngleAxis(_camRotation.x * Mathf.Rad2Deg, Vector3.up) 
                        * new Vector3(0, Mathf.Sin(_camRotation.y), Mathf.Cos(_camRotation.y));

        var groundAvoidance = _camRotation.y > 0 ?
            Mathf.Lerp(_camDistance, _golfballScale + .3f /*near*/, _camRotation.y/2/Mathf.PI)
            : _camDistance;
        
        transform.position = _camAnker - CamTargetDir.Value.normalized * groundAvoidance;
        transform.LookAt(_camAnker);
    }

    public void UpdateCameraPos(CameraPosEventArgs args)
    {
        UpdateCameraPos(args.AssociatedCamAnker, args.AssociatedCamDistance);
    }

    private void UpdateCameraPos(Vector3 position, float r = 0f)
    {
        _camAnker = position + Vector3.up * (_golfballScale / distanceToBall * 1.35f);
        if (r > 0) _camDistance = r;
        
        UpdateCameraLook(new CameraLookControlEventArgs(Vector2.zero));
    }

    private IEnumerator PlaceCamAtBallSmooth(float smoothTime, Vector3 ballPos)
    {
        var t = 0f;
        var startDistance = _camDistance;
        var startPos = _camAnker;
        while (t <= smoothTime)
        {
            t += Time.deltaTime;
            UpdateCameraPos(new CameraPosEventArgs(Vector3.Lerp(startPos, ballPos, t/smoothTime),
                Mathf.Lerp(startDistance, distanceToBall, t/smoothTime)));
            yield return new WaitForEndOfFrame();
        }
        _waitCoroutine = null;
    }

    private void RotateCamAtBallToFaceMark(AbschlagStartEventArgs args)
    {
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        UpdateCameraPos(args.AssociatedBallPos, distanceToBall);

        var targetDir = (args.AssociatedTargetPos - args.AssociatedBallPos).normalized;
        var curDir = Quaternion.AngleAxis(_camRotation.x * Mathf.Rad2Deg, Vector3.up) 
                     * new Vector3(0, Mathf.Sin(_camRotation.y), Mathf.Cos(_camRotation.y));
        
        var rotateX = Vector2.SignedAngle(new Vector2(curDir.x, curDir.z), new Vector2(targetDir.x, targetDir.z)) * -Mathf.Deg2Rad;
        var rotateY = GetYAngleBetween(args.AssociatedBallPos, args.AssociatedTargetPos) - _camRotation.y;
        
        _waitCoroutine = StartCoroutine(RotateCamSmooth(timeToSmoothCam, new Vector2(rotateX, rotateY)));
    }

    private IEnumerator RotateCamSmooth(float smoothTime, Vector2 targetRotationDegrees)
    {
        var t = 0f;
        while (t <= smoothTime)
        {
            t += Time.deltaTime;
            UpdateCameraLook(new CameraLookControlEventArgs(
                targetRotationDegrees * Time.deltaTime / smoothTime));
            yield return new WaitForEndOfFrame();
        }
        _waitCoroutine = null;
    }
    
    private void RotateCamAndPlaceAtTargetSmooth(Vector3 targetPos, Vector3 targetDir)
    {
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        
        var curDir = Quaternion.AngleAxis(_camRotation.x * Mathf.Rad2Deg, Vector3.up) 
                     * new Vector3(0, Mathf.Sin(_camRotation.y), Mathf.Cos(_camRotation.y));
        
        var rotateX = Vector2.SignedAngle(new Vector2(curDir.x, curDir.z), new Vector2(targetDir.x, targetDir.z)) * -Mathf.Deg2Rad;
        var rotateY = GetYAngleBetween(targetPos, targetPos + targetDir) - _camRotation.y;
        
        _waitCoroutine = StartCoroutine(RotateAndPlaceCamSmooth(timeToSmoothCam,
            new Vector2(rotateX, rotateY), targetPos));
    }
    
    private IEnumerator RotateAndPlaceCamSmooth(float smoothTime, Vector2 targetRotationDegrees, Vector3 targetPosition)
    {
        var t = 0f;
        var startDistance = _camDistance;
        var startPos = _camAnker;
        while (t <= smoothTime)
        {
            t += Time.deltaTime;
            UpdateCameraPos(new CameraPosEventArgs(Vector3.Lerp(startPos, targetPosition, t/smoothTime),
                Mathf.Lerp(startDistance, distanceToBall, t/smoothTime)));
            UpdateCameraLook(new CameraLookControlEventArgs(
                targetRotationDegrees * Time.deltaTime / smoothTime));
            yield return new WaitForEndOfFrame();
        }
        _waitCoroutine = null;
    }

    private static float GetYAngleBetween(Vector3 from, Vector3 to)
    {
        var d = (to - from).magnitude;
        var h = to.y - from.y;
        if (h == 0 || d == 0) 
            return 0;
        return Mathf.Asin(Mathf.Abs(h) / d) * (h > 0 ? 1 : -1);
    }

    private void FreeCamMove(Vector2 args)
    {
        var deltaPos = movementSpeed * Time.deltaTime * transform.TransformDirection(
            Vector3.forward * args.y + Vector3.right * args.x);

        transform.position = _camAnker += deltaPos;
    }

    private void FreeCamLook(float lookX, float lookY)
    {
        _camRotation.x = (_camRotation.x + lookX) % (2*Mathf.PI);
        _camRotation.y = Mathf.Clamp(_camRotation.y + lookY, -.95f * Mathf.PI/2, .95f * Mathf.PI/2);
        
        var dir = Quaternion.AngleAxis(_camRotation.x * Mathf.Rad2Deg, Vector3.up) 
                             * new Vector3(0, Mathf.Sin(_camRotation.y), Mathf.Cos(_camRotation.y));
        
        transform.position = _camAnker;
        transform.LookAt(_camAnker + dir);
    }
    
    private void SetFreeCam(bool freeCam)
    {
        _isFreeCam = freeCam;
        if (_isFreeCam)
        {
            RotateCamAndPlaceAtTargetSmooth(_camAnker - CamTargetDir.Value.normalized * 3 * distanceToBall,
                CamTargetDir.Value);
            _freeCamCollider.enabled = true;
        }
        else
        {
            RotateCamAndPlaceAtTargetSmooth(golfballController.transform.position,
                CamTargetDir.Value);
            _freeCamCollider.enabled = false;
        }
    }

    public GolfBallController GetGolfBallController()
    {
        return golfballController;
    }
    
    public GolfAbschlagController GetGolfAbschlagController()
    {
        return golfAbschlagController;
    }
    
}
