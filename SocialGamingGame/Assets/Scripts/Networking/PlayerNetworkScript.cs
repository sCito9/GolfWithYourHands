using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CesiumForUnity;
using DataBackend;
using DedicatedServer;
using QFSW.QC;
using R3;
using Sensoren;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking
{
    public struct NetworkString : INetworkSerializeByMemcpy
    {
        private ForceNetworkSerializeByMemcpy<FixedString32Bytes> _info;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public override string ToString()
        {
            return _info.Value.ToString();
        }

        public static implicit operator string(NetworkString s)
        {
            return s.ToString();
        }

        public static implicit operator NetworkString(string s)
        {
            return new NetworkString { _info = new FixedString32Bytes(s) };
        }
    }

    public class PlayerNetworkScript : NetworkBehaviour
    {
        #region REFERENCES

        public NetworkVariable<NetworkString> playerName = new("", NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly List<PlayerData> _finishedClients = new();
        private CesiumGeoreference georeference;
        [SerializeField] private Transform canvas;
        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private GameObject sandtrapPrefab;
        [SerializeField] private Material[] _playerMaterial;
        [Header("Endscreen Stuff")] private GameObject finishScreeen;
        private List<TMP_Text> playerFields;
        private List<TMP_Text> hitFields;
        private List<GameObject> invitePlayerButtons;
        private List<GameObject> _clubInviteButtons;
        private GameObject podium;

        private GolfAbschlagController _abschlagController;
        private GolfBallController _ballController;
        private CameraController _cameraController;
        private GameObject _golfBall;
        private int _hits;
        private int _place;
        private References _reference;
        private GolfHoleSensor _golfHoleSensor;
        private GameObject _golfHole;
        private readonly List<GameObject> _sandtraps = new();
        private Vector3 _lastPosition;
        private Coroutine _falling;
        private GameObject _mainCam;
        private readonly float maxDistance = 1100;


        private ArrowNavigatorScript _navigationArrow;

        #endregion

        public override void OnNetworkSpawn()
        {
            _mainCam = Camera.main?.gameObject;
            _cameraController = gameObject.GetComponentInChildren<CameraController>();
            _abschlagController = _cameraController.GetGolfAbschlagController();
            _ballController = _cameraController.GetGolfBallController();
            _golfBall = _ballController.gameObject;
            if (IsOwner && IsClient)
            {
                //Get references in Start
                _reference = FindAnyObjectByType<References>();
                GetReferences();
                _navigationArrow.SetParent(transform);

                gameObject.transform.position = ClientBootstrap.course.startPosition;
                _cameraController.StartUp();
                _abschlagController.StartUp();
                _ballController.StartUp();
                _abschlagController?.SetMp();

                _abschlagController?.FinishTurnStream
                    .TakeUntil(_golfHoleSensor.SensorTriggered
                        .Where(_ => TurnManager.Instance.CurrentTurnClientId.Value
                                    == NetworkManager.Singleton.LocalClientId))
                    .Subscribe(_ => FinishTurn()).AddTo(this);
                _abschlagController?.AbschlagTriggeredStream
                    .Subscribe(_ =>
                    {
                        _falling = StartCoroutine(TestFellOutOfMap());
                        TurnManager.Instance.AbschlagSoundServerRpc();
                    }).AddTo(this);
                TurnManager.Instance.OnTurnChange += HandleNextTurn;
                _golfHoleSensor.SensorTriggered.Subscribe(ClientFinished)
                    .AddTo(this);

                _lastPosition = _golfBall.transform.position;
                playerName.Value = SessionManager.Instance.User.name;
                //_finishedClients.OnListChanged += ClientFinishedClient;
                //setup stuff

                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ShutdownNotice",
                    OnShutdownNoticeReceived
                );

                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "GameEndedNotice",
                    OnGameEndedNoticeReceived
                );

                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "ResetNotice",
                    OnResetNoticeReceived
                );

                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "LastPlayerNotice",
                    (senderClientId, reader) =>
                    {
                        TurnManager.Instance.ClientFinishedServerRpc(SessionManager.Instance.User.id,
                            SessionManager.Instance.User.name,
                            -1,
                            NetworkManager.Singleton.LocalClientId);
                    });

                if (ClientBootstrap.course == null)
                {
                    Debug.LogError("Error fetching course data.");
                    ReturnToMainMenu();
                }

                georeference.SetOriginLongitudeLatitudeHeight(ClientBootstrap.course.mapOrigin.y,
                    ClientBootstrap.course.mapOrigin.x,
                    ClientBootstrap.course.mapOrigin.z);

                _golfHole.transform.position = ClientBootstrap.course.endPosition;
                foreach (var sandtrap in ClientBootstrap.course.sandPositions)
                {
                    var trap = Instantiate(sandtrapPrefab,
                        sandtrap,
                        Quaternion.Euler(90, 0, 0));
                    _sandtraps.Add(trap);
                }

                _ballController.SetSandbunker(_sandtraps);
            }

            playerNameLabel.text = playerName.Value.ToString();
            playerName.OnValueChanged += (oldName, newName) => { playerNameLabel.text = newName.ToString(); };
            _golfBall.GetComponent<MeshRenderer>().material =
                _playerMaterial[(int)OwnerClientId % 4];
        }

        private void LateUpdate()
        {
            canvas.position = _golfBall.transform.position + Vector3.up;
            var scaleVar = Mathf.Lerp(0.1f, 100f,
                Mathf.Clamp01(Vector3.Distance(_golfBall.transform.position, _mainCam.transform.position) /
                              maxDistance));
            canvas.localScale = new Vector3(scaleVar, scaleVar, scaleVar);
            canvas.LookAt(_mainCam.transform.position);
            canvas.Rotate(0, 180f, 0);
        }

        private void GetReferences()
        {
            georeference = _reference.georeference;
            finishScreeen = _reference.finishScreeen;
            playerFields = _reference.playerFields;
            hitFields = _reference.hitFields;
            invitePlayerButtons = _reference.invitePlayerButtons;
            _clubInviteButtons = _reference.clubInviteButtons;
            podium = _reference.podium;
            _golfHole = _reference.golfHole;
            _golfHoleSensor = _reference.golfHoleSensor;
            _navigationArrow = _reference.navigationArrow;
        }

        private void FinishTurn()
        {
            StopCoroutine(_falling);
            _hits++;
            _lastPosition = _golfBall.transform.position;
            TurnManager.Instance.FinishTurnServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        public void ReturnToMainMenu()
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnShutdownNoticeReceived(ulong senderClientId,
            FastBufferReader reader)
        {
            ReturnToMainMenu();
        }

        #region FINISH_STUFF

        private void OnGameEndedNoticeReceived(ulong senderClientId,
            FastBufferReader reader)
        {
            QuantumConsole.Instance.LogToConsoleAsync("Game Ended");
            TurnManager.Instance.OnTurnChange -= HandleNextTurn;
            StartCoroutine(DelayFinishScreen());
        }

        private IEnumerator DelayFinishScreen()
        {
            yield return new WaitForSecondsRealtime(3f);
            //_golfBall.SetActive(true);
            ActivateFinishScreen();
        }

        private void OnResetNoticeReceived(ulong senderClientId,
            FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong receivedValue);
            if (receivedValue == NetworkManager.Singleton.LocalClientId)
            {
                StopCoroutine(_falling);
                //Kill momentum vom Golfball
                QuantumConsole.Instance.LogToConsoleAsync("Resetting GolfBall...");
                _ballController.PauseBall();
                _golfBall.transform.position = _lastPosition + new Vector3(0f, 3f, 0f);
                FinishTurn();
            }
        }

        private IEnumerator TestFellOutOfMap()
        {
            yield return new WaitForSeconds(35f);
            TurnManager.Instance.ChatMessageServerRpc("Player moved for too long, resetting position...", "Server");
            _ballController.PauseBall();
            _golfBall.transform.position = _lastPosition + new Vector3(0f, 3f, 0f);
            FinishTurn();
        }


        private void ActivateFinishScreen()
        {
            foreach (var data in TurnManager.Instance._finishedClients) _finishedClients.Add(data);

            for (var i = 0;
                 i < _finishedClients.Count;
                 i++)
            {
                if (_finishedClients[i].clientId == NetworkManager.Singleton.LocalClientId)
                {
                    _place = i + 1;
                }
                else
                {
                    invitePlayerButtons[i]
                        .SetActive(true);
                    _clubInviteButtons[i].SetActive(true);
                }

                playerFields[i].text = _finishedClients[i]
                    .name.ToString();
                if (_finishedClients[i].hits != -1)
                    hitFields[i].text = _finishedClients[i]
                        .hits.ToString();
                else
                    hitFields[i].text = "-";
            }

            var playerObject = gameObject.transform;
            _golfHole.SetActive(false);
            var _podium = Instantiate(podium,
                _golfHole.transform.position,
                Quaternion.identity);
            foreach (Transform child in playerObject)
                if (child.CompareTag("GolfBall"))
                    switch (_place)
                    {
                        case 1:
                            child.position = GameObject.FindWithTag("Place 1")
                                .transform.position;
                            break;
                        case 2:
                            child.position = GameObject.FindWithTag("Place 2")
                                .transform.position;
                            break;
                        case 3:
                            child.position = GameObject.FindWithTag("Place 3")
                                .transform.position;
                            break;
                        case 4:
                            child.position = GameObject.FindWithTag("Place 4")
                                .transform.position;
                            break;
                    }
                else if (child.CompareTag("Navigation Arrow"))
                    Destroy(child.gameObject);

            var cameraObj = _mainCam?.gameObject;
            if (cameraObj != null)
            {
                cameraObj.transform.SetParent(_podium.transform);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                cameraObj.transform.position = GameObject.FindWithTag("Camera Anchor Podium").transform.position;
                cameraObj.transform.rotation = Quaternion.Euler(new Vector3(13, 90, 0));
            }
            else
            {
                Debug.LogWarning("No MainCamera");
            }

            finishScreeen.SetActive(true);
        }

        #endregion

        private void HandleNextTurn(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                TurnManager.Instance.ChatMessageServerRpc($"It's {SessionManager.Instance.User.name}'s turn!",
                    "Server");
                if (_hits == 0) _golfBall.GetComponent<Rigidbody>().isKinematic = false;
                _abschlagController?.StartAbschlag();
            }
            else
            {
                var client = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId ==
                    clientId);
                if (client == null) return;

                var playerObject = client.PlayerObject.transform;

                var otherPlayersGolfBall = playerObject.GetComponentInChildren<GolfBallController>();
                _abschlagController?.SpectateAbschlag(otherPlayersGolfBall?.gameObject);
            }
        }

        private void ClientFinished(System.EventArgs args)
        {
            if (TurnManager.Instance.CurrentTurnClientId.Value == NetworkManager.Singleton.LocalClientId)
            {
                StopCoroutine(_falling);
                _ballController.PauseBall();
                //_golfBall.SetActive(false);
                //_ballController.OnBallStoppedEvent -= FinishTurn;
                _hits++;
                TurnManager.Instance.ChatMessageServerRpc(
                    $"{SessionManager.Instance.User.name} finished with {_hits} hits. ",
                    "");
                TurnManager.Instance.ClientFinishedServerRpc(SessionManager.Instance.User.id,
                    SessionManager.Instance.User.name,
                    _hits,
                    NetworkManager.Singleton.LocalClientId);
            }
        }
    }
}