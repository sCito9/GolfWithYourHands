using System;
using System.Collections;
using System.Collections.Generic;
using DataBackend;
using DataBackend.Models;
using R3;
using Sensoren;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CreateNewCourse
{
    public class CreateNewCourseLoop : MonoBehaviour
    {
        #if !UNITY_SERVER
        [Header("GPS accuracy")]
        [SerializeField] private float desiredAccuracyInMeters = 10f;
        [SerializeField] private float desiredDistanceInMeters = 10f;
        [Space]
        
        [Header("Prefabs")]
        [SerializeField] private GameObject startPrefab;
        [SerializeField] private GameObject finishPrefab;
        [SerializeField] private GameObject sandPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject cancelButton;
        [Space]
        
        [Header("Debug")]
        public float maxRaycastDistance = 100f;
        [SerializeField] private GameObject start;
        [SerializeField] private GameObject finish;
        [SerializeField] private GameObject sand;
        [SerializeField] private GameObject canvas;
        [SerializeField] private GolfHoleSensor trefferSensor;
        [Space]
        
        [Header("Variables")]
        public static bool Validated = false;
        
        
        private GpsLocation _gpsLocation;
        private TMP_InputField _inputField;
        private bool _startPlaced = false;
        private bool _finishPlaced = false;
        [SerializeField] int _actSelected = -1;

        private List<GameObject> _sandPositions;
        private void Awake()
        {
            _gpsLocation = GetComponent<GpsLocation>();
            _sandPositions = new List<GameObject>();
            _inputField = FindAnyObjectByType<TMP_InputField>();
        }

        private void Start()
        {
            StartCoroutine(_gpsLocation.GetRealLifeLocation(desiredAccuracyInMeters, desiredDistanceInMeters));
            trefferSensor.SensorTriggered.Subscribe(Finished).AddTo(this);

            Observable.TimerFrame(1).Take(1).Subscribe(_ =>
            {
                var abschlagController = playerPrefab.GetComponentInChildren<GolfAbschlagController>();
                abschlagController?.ForceFreeCam();
            }).AddTo(this);
        }

        public void UpdateLocation()
        {
            StartCoroutine(_gpsLocation.GetRealLifeLocation(desiredAccuracyInMeters, desiredDistanceInMeters));
        }

        public void OnPrefabSelected(int prefabIndex)
        {
            if (_actSelected == prefabIndex)
            {
                OnPrefabDeselected();
                return;
            }
            else if (_actSelected != -1)
            {
                StopAllCoroutines();
            }
            _actSelected = prefabIndex;
            switch (_actSelected)
            {
                case 0:
                    EventSystem.current.SetSelectedGameObject(start);
                    break;
                case 1:
                    EventSystem.current.SetSelectedGameObject(finish);
                    break;
                case 2:
                    EventSystem.current.SetSelectedGameObject(sand);
                    break;
                case -1: 
                    EventSystem.current.SetSelectedGameObject(null);
                    break;
            }
            //lock Input
            Debug.LogWarning("Not yet implemented");
            StartCoroutine(WaitForInput(prefabIndex));
        }

        private IEnumerator WaitForInput(int prefabIndex)
        {
            bool forme = false;
            Vector2 touch = Vector2.zero;
            while (!forme)
            {
                yield return new WaitUntil(() => Input.touchCount > 0);
                try
                {
                    touch = Input.GetTouch(0).position;
                    float margin = Screen.width * 0.24f;
                    if (touch.x >= Screen.width - margin || touch.x <= margin)
                    {
                        switch (_actSelected)
                        {
                            case 0:
                                EventSystem.current.SetSelectedGameObject(start);
                                break;
                            case 1:
                                EventSystem.current.SetSelectedGameObject(finish);
                                break;
                            case 2:
                                EventSystem.current.SetSelectedGameObject(sand);
                                break;
                        }
                        continue;
                    }
                    forme = true;
                    _actSelected = -1;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            var ray = Camera.main.ScreenPointToRay(touch);
            if (!Physics.Raycast(ray, out var hit, maxRaycastDistance)) yield break;
            
            if (hit.collider.gameObject.CompareTag("Sandbunker"))
            {
                Destroy(hit.collider.gameObject);
            }
            else
            {
                switch (prefabIndex)
                {
                    case 0:
                        startPrefab.gameObject.transform.position = hit.point + new Vector3(0, 0.05f, 0);
                        _startPlaced = true;
                        break;
                    case 1:
                        finishPrefab.gameObject.transform.position = hit.point + new Vector3(0, 0.05f, 0);
                        _finishPlaced = true;

                        break;
                    case 2:
                        _sandPositions.Add(Instantiate(sandPrefab, hit.point, Quaternion.Euler(90, 0, 0)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(prefabIndex), prefabIndex, null);
                }
            }


            Validated = false;
        }
        
        public void OnPrefabDeselected()
        {
            _actSelected = -1;
            //unlock Input
            Debug.LogWarning("Not yet implemented");
            switch (_actSelected)
            {
                case 0:
                    EventSystem.current.SetSelectedGameObject(start);
                    break;
                case 1:
                    EventSystem.current.SetSelectedGameObject(finish);
                    break;
                case 2:
                    EventSystem.current.SetSelectedGameObject(sand);
                    break;
                case -1: 
                    EventSystem.current.SetSelectedGameObject(null);
                    break;
            }
            StopAllCoroutines();
        }

        private void Finished(System.EventArgs e)
        {
            playerPrefab.transform.position = Vector3.zero;
            Validated = true;
            canvas.SetActive(true);
            cancelButton.SetActive(false);
            startPrefab.SetActive(true);
            var abschlagController = playerPrefab.GetComponentInChildren<GolfAbschlagController>();
            abschlagController?.ForceFreeCam();
        }

        public void CancelValidation()
        {
            playerPrefab.transform.position = Vector3.zero;
            canvas.SetActive(true);
            cancelButton.SetActive(false);
            startPrefab.SetActive(true);
            var abschlagController = playerPrefab.GetComponentInChildren<GolfAbschlagController>();
            abschlagController?.ForceFreeCam();
        }

        public void StartValidation()
        {
            if (!_startPlaced || !_finishPlaced)
            {
                Debug.LogWarning("You need to place both a start and a finish");
                return;
            }
            playerPrefab.transform.position = startPrefab.transform.position;
            var golfballController = playerPrefab.GetComponentInChildren<GolfBallController>();
            golfballController.SetSandbunker(_sandPositions);
            var abschlagController = playerPrefab.GetComponentInChildren<GolfAbschlagController>();
            abschlagController?.ForceFreeCam(false, true);
            startPrefab.SetActive(false);
            canvas.SetActive(false);
            cancelButton.SetActive(true);
        }
        
        public void FinishValidation()
        {
            if (!Validated)
            {
                Debug.Log("You've not validated your course yet");
                return;
            }

            if (_inputField.text.Equals(String.Empty))
            {
                Debug.Log("You need to enter a name for your course");
                return;
            }
            Debug.LogWarning("Missing Check if course already exists");
            var sandPositions = new float3[_sandPositions.Count];
            for (var i = 0; i < _sandPositions.Count; i++)
            {
                sandPositions[i] = new float3(_sandPositions[i].transform.position);
            }


            var course = new Course(
                _inputField.text,
                SessionManager.Instance.User.id, 
                new float3(startPrefab.transform.position),
                new float3(finishPrefab.transform.position),
                GpsLocation.Location,
                sandPositions);
            
            //JsonHandler.WriteGolfCourse(course, _inputField.text);
            StartCoroutine(DataLoader.CreateCourse(course, createdCourse =>
            {
                SessionManager.Instance.currentCourse = createdCourse;
                BackToMainMenu();
            }));
        }

        public void BackToMainMenu()
        {
            StopAllCoroutines();
            SceneManager.LoadScene("MainMenu");
        }
        
        
        #endif
    }
}
