using System;
using EventArgs;
using R3;
using R3.Triggers;
using Sensoren;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace InputAssets
{
    public class InputActionBasedFirstPersonControllerInput : MonoBehaviour
    {
        private InputSystem_Actions controls;
    
        private Observable<CameraLookControlEventArgs> _cameraControl;
        public Observable<CameraLookControlEventArgs> CameraControl
        {
            get {
                return _cameraControl;
            }
        }
        
        [SerializeField] private PointerDownSensor switchFreeCamSensor;
        private Observable<bool> _switchFreeCam;
        public Observable<bool> SwitchFreeCam
        {
            get {
                return _switchFreeCam;
            }
        }
    
        private Observable<Vector2> _movementControl;
        public Observable<Vector2> MovementControl
        {
            get {
                return _movementControl;
            }
        }
    
        [SerializeField] private GameObject virtualStick;
        private RectTransform _virtualStickTransform;
        [SerializeField] private GameObject virtualStickHandle;
        private RectTransform _virtualStickExcursion;
    
        private PointerDownSensor _virtualStickDownSensor;
        private PointerUpSensor _virtualStickUpSensor;
        private PointerEventData _virtualStickPointerData;
        private bool _virtualStickDown;
        private Vector2 _virtualStickCenter;
        private float _virtualStickRadius;
    
    
        private Observable<DragControlEventArgs> _dragToSetStrControl;
        public Observable<DragControlEventArgs> DragToSetStrControl
        {
            get {
                return _dragToSetStrControl;
            }
        }
        [SerializeField] private PointerDownSensor abschlagDragButtonDownSensor;
        [SerializeField] private PointerUpSensor abschlagDragButtonUpSensor;

    
        private Observable<bool> _abschlagCancelled;
        public Observable<bool> AbschlagCancelled
        {
            get {
                return _abschlagCancelled;
            }
        }
    
        private void OnEnable()
        {
            //Error here
            controls.Enable();
        }

        private void OnDisable()
        {
            controls.Disable();
        }

        private bool _flip;
    
        private bool _contextToggleButton;
        private void SetContextToggleButton(bool value)
        {
            _contextToggleButton = value;
        }

        private void SetMovementControl(System.EventArgs args)
        {
            var e = (PointerEventArgs)args;
            _virtualStickDown = e.Pressed;
            _virtualStickPointerData = e.AssociatedPointerPayload;
        }

        private void SetUpVirtualStick()
        {
            _virtualStickTransform = virtualStick.transform as RectTransform;
            _virtualStickExcursion = virtualStickHandle.transform as RectTransform;

            if (_virtualStickExcursion == null || _virtualStickTransform == null) return;
        
            _virtualStickCenter = _virtualStickExcursion.anchoredPosition;
            _virtualStickRadius = _virtualStickTransform.rect.width / 2;
        
            _virtualStickDownSensor = virtualStick.GetComponent<PointerDownSensor>();
            _virtualStickUpSensor = virtualStick.GetComponent<PointerUpSensor>();
        }

        private void Awake()
        {
            controls = new InputSystem_Actions();
            
            var internalTouchInputStream = this.UpdateAsObservable()
                .Select(_ =>
                {
                    var finger1 = Touchscreen.current.touches[0];
                    var position1 = finger1.press.isPressed ? finger1.position.ReadValue() : Vector2.positiveInfinity;
                    var delta1 = finger1.delta.ReadValue();
                    
                    var finger2 = Touchscreen.current.touches[1];
                    var position2 = finger2.press.isPressed ? finger2.position.ReadValue() : Vector2.positiveInfinity;
                    var delta2 = finger2.delta.ReadValue();
                    
                    return (position1, delta1, position2, delta2);
                })
                .Share();
            
            _cameraControl = internalTouchInputStream
                .Where(_ => !controls.Player.EditorContextToggle.inProgress && !_contextToggleButton)
                .Select(inputData =>
                {
                    var pointerDrag = _virtualStickDown && inputData.position1.x < inputData.position2.x ?
                        inputData.delta2 : inputData.delta1;
                    return new CameraLookControlEventArgs(pointerDrag);
                })
                .Share();
        
            SetUpVirtualStick();
            _movementControl = internalTouchInputStream
                .Select(inputData =>
                {
                    var wasd = controls.Player.Move.ReadValue<Vector2>();
                    _virtualStickExcursion.anchoredPosition = _virtualStickCenter;
                    if (!_virtualStickDown) return wasd;
                    
                    var d1 = Vector2.Distance(_virtualStickCenter, inputData.position1);
                    var d2 = Vector2.Distance(_virtualStickCenter, inputData.position2);
                    var stickPos = d1 < d2 ? inputData.position1 : inputData.position2;
                
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_virtualStickTransform, stickPos,
                        _virtualStickPointerData.pressEventCamera, out var handlePos);
                
                    handlePos = Vector2.ClampMagnitude(handlePos, _virtualStickRadius);
                    _virtualStickExcursion.anchoredPosition = handlePos;

                    wasd = handlePos / _virtualStickRadius;

                    return wasd;
                })
                .Share();
            
            if (abschlagDragButtonDownSensor == null) return;
        
            _dragToSetStrControl = internalTouchInputStream
                .Where(_ => controls.Player.EditorContextToggle.inProgress || _contextToggleButton)
                .Select(inputData =>
                {
                    var drag1 = inputData.position1;
                    if (float.IsPositiveInfinity(drag1.x)) drag1 = Vector2.zero;
                    var drag2 = inputData.position2;
                    if (float.IsPositiveInfinity(drag2.x)) drag2 = Vector2.zero;
                    
                    var dragPos = drag2.x > drag1.x ? drag2 : drag1;

                    return new DragControlEventArgs(dragPos,
                        controls.Player.EditorContextToggle.inProgress || _contextToggleButton);
                }).Share();

            _abschlagCancelled = this.UpdateAsObservable().Where(_ =>
                    (controls.Player.EditorContextToggle.inProgress || _contextToggleButton) != _flip)
                .Select(_ => 
                {
                    _flip = !_flip; 
                    return !_flip; 
                })
                .Share();
            
            _switchFreeCam = switchFreeCamSensor.SensorTriggered
                .Chunk(TimeSpan.FromMilliseconds(400))
                .Where(clicks => clicks.Length >= 2)
                .Select(_ => true)
                .Share();
        }

        private void Start()
        {
            _virtualStickDownSensor.SensorTriggered.Subscribe(SetMovementControl).AddTo(this);
            _virtualStickUpSensor.SensorTriggered.Subscribe(SetMovementControl).AddTo(this);
            
            if (abschlagDragButtonDownSensor == null) return;
            
            abschlagDragButtonDownSensor.SensorTriggered.Select(_ => true).Subscribe(SetContextToggleButton).AddTo(this);
            abschlagDragButtonUpSensor.SensorTriggered.Select(_ => false).Subscribe(SetContextToggleButton).AddTo(this);
        }
    }
}