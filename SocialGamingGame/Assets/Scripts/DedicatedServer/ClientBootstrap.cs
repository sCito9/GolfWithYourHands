using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBackend;
using DataBackend.Models;
using QFSW.QC;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DedicatedServer
{
    public class ClientBootstrap : MonoBehaviour
    {
        [Header("Server-Verbindung")] public string serverIP = "127.0.0.1"; // Auf Mobile: IP des echten Servers
        public ushort serverPort = 7777;

        public static Course course;
        private Lobby _currentLobby;
        public static int _maxPlayer;

        public event Action OnKickedFromLobby;
        public event Action OnGameStarted;
        public event Action<List<string>> OnPlayerJoined;

        private bool _joinedRelay;
        
        public static ClientBootstrap Instance { private set; get; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private async void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            _joinedRelay = false;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            var options = new InitializationOptions().SetOption("analytics-enabled", false)
                .SetOption("cloud-save-enabled", false).SetProfile("default");

            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
        }

        public async void CreateLobby(int max, string name)
        {
            try
            {
                _joinedRelay = false;
                var lobbyName = course.name + " hosted by " + name;
                _maxPlayer = max;

                var options = new CreateLobbyOptions
                {
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            {
                                "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                                    SessionManager.Instance.User.name)
                            }
                        }
                    },
                    Data = new Dictionary<string, DataObject>
                    {
                        { "relay_key", new DataObject(DataObject.VisibilityOptions.Member, string.Empty) },
                        { "course", new DataObject(DataObject.VisibilityOptions.Member, JsonUtility.ToJson(course)) }
                    }
                };

                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, _maxPlayer, options);
                _currentLobby = lobby;
                QuantumConsole.Instance.LogToConsoleAsync("Created Lobby");
                OnPlayerJoined?.Invoke(lobby.Players.ConvertAll(player => player.Data["name"].Value));
                StartCoroutine(LobbyHeartbeat());
                StartCoroutine(UpdateLobbyRoutine());
            }
            catch (LobbyServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
                QuantumConsole.Instance.LogToConsoleAsync("Error creating Lobby");
            }
        }

        public int GetMaxPlayer()
        {
            return _currentLobby.MaxPlayers;
        }

        public bool IsHost()
        {
            return _currentLobby.HostId == AuthenticationService.Instance.PlayerId;
        }

        private IEnumerator UpdateLobbyRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.2f);
                Fetch();
            }
        }

        private async void Fetch()
        {
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
                if (lobby.Data["relay_key"].Value != string.Empty)
                {
                    _currentLobby = lobby;
                    if (lobby.HostId != AuthenticationService.Instance.PlayerId && !_joinedRelay)
                    {
                        _joinedRelay = true;
                        JoinRelay(_currentLobby.Data["relay_key"].Value);
                    }
                }
                else if (!lobby.Players.ConvertAll(player => player.Data["name"].Value)
                             .SequenceEqual(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value)))
                {
                    //Player joined / left
                    _currentLobby = lobby;
                    OnPlayerJoined?.Invoke(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value));
                    QuantumConsole.Instance.LogToConsoleAsync("Player joined/left");
                }
                else
                {
                    _currentLobby = lobby;
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    _currentLobby = null;
                    OnKickedFromLobby?.Invoke();
                    StopAllCoroutines();
                }
                else
                {
                    QuantumConsole.Instance.LogToConsoleAsync("You probably lost your internet connection, returning to MainMenu...");
                    LeaveLobby();
                    SceneManager.LoadScene("MainMenu");
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }

        private IEnumerator LobbyHeartbeat()
        {
            while (true)
            {
                yield return new WaitForSeconds(15f);
                OnPlayerJoined?.Invoke(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value));
                Beat();
            }
        }

        private async void Beat()
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        private async void UpdateRelayKey(string key)
        {
            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "relay_key", new DataObject(DataObject.VisibilityOptions.Member, key) }
                    }
                };
                var lobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, options);
                _currentLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        private async void ListLobbies()
        {
            try
            {
                var queryLobbiesOptions = new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };
                var queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

                foreach (var lobby in queryResponse.Results)
                {
                    //listLobbieshere
                }
            }
            catch (LobbyServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        public async void JoinLobbyWithCode(string code)
        {
            try
            {
                _joinedRelay = false;
                var options = new JoinLobbyByCodeOptions
                {
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            {
                                "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                                    SessionManager.Instance.User.name)
                            }
                        }
                    }
                };
                _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
                course = JsonUtility.FromJson<Course>(_currentLobby.Data["course"].Value);
                OnPlayerJoined?.Invoke(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value));
                QuantumConsole.Instance.LogToConsoleAsync("Joined Lobby");
                StartCoroutine(UpdateLobbyRoutine());
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.LobbyFull)
                    QuantumConsole.Instance.LogToConsoleAsync("Lobby is full");
                else
                    QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        public async Task<bool> JoinLobbyWithId(string id)
        {
            try
            {
                _joinedRelay = false;
                var options = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            {
                                "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                                    SessionManager.Instance.User.name)
                            }
                        }
                    }
                };
                _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(id, options);
                course = JsonUtility.FromJson<Course>(_currentLobby.Data["course"].Value);
                OnPlayerJoined?.Invoke(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value));
                QuantumConsole.Instance.LogToConsoleAsync("Joined Lobby");
                StartCoroutine(UpdateLobbyRoutine());
                return true;
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.LobbyFull)
                    QuantumConsole.Instance.LogToConsoleAsync("Lobby is full");
                else
                    QuantumConsole.Instance.LogToConsoleAsync(e.ToString());

                return false;
            }
        }

        public async void JoinLobby(Lobby lobby)
        {
            try
            {
                _joinedRelay = false;
                var options = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            {
                                "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                                    SessionManager.Instance.User.name)
                            }
                        }
                    }
                };
                _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
                course = JsonUtility.FromJson<Course>(_currentLobby.Data["course"].Value);
                OnPlayerJoined?.Invoke(_currentLobby.Players.ConvertAll(player => player.Data["name"].Value));
                QuantumConsole.Instance.LogToConsoleAsync("Joined Lobby");
                StartCoroutine(UpdateLobbyRoutine());
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.LobbyFull)
                    QuantumConsole.Instance.LogToConsoleAsync("Lobby is full");
                else
                    QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        public async void LeaveLobby()
        {
            try
            {
                _joinedRelay = false;
                if (_currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                    _currentLobby = null;
                    QuantumConsole.Instance.LogToConsoleAsync("Killed Lobby");
                    
                    // Delete all pending invites that weren't accepted or rejected on the DataServer
                    StartCoroutine(DataLoader.DeleteAllInvites(SessionManager.Instance.User.id, _ => { }));
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id,
                        AuthenticationService.Instance.PlayerId);
                    _currentLobby = null;
                    QuantumConsole.Instance.LogToConsoleAsync("Left Lobby");
                }

                OnKickedFromLobby?.Invoke();
                StopAllCoroutines();
            }
            catch (LobbyServiceException e)
            {
                OnKickedFromLobby?.Invoke();
            }
        }

        /*
         *
         *Relay
         *
         */


        public async void CreateRelay()
        {
            try
            {
                _joinedRelay = true;
                _maxPlayer = _currentLobby.Players.Count;
                var allocation = await RelayService.Instance.CreateAllocationAsync(3);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                NetworkManager.Singleton.GetComponent<UnityTransport>()
                    .SetRelayServerData(allocation.ToRelayServerData("dtls"));
                NetworkManager.Singleton.StartHost();
                OnGameStarted?.Invoke();

                UpdateRelayKey(joinCode);
                StartCoroutine(KillLobby());
            }
            catch (RelayServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        private IEnumerator KillLobby()
        {
            yield return new WaitForSeconds(20);
            LeaveLobby();
        }

        public async void JoinRelay(string joinCode)
        {
            try
            {
                _joinedRelay = true;
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();


                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                
                NetworkManager.Singleton.GetComponent<UnityTransport>()
                    .SetRelayServerData(allocation.ToRelayServerData("dtls"));

                NetworkManager.Singleton.StartClient();
                OnGameStarted?.Invoke();
            }
            catch (RelayServiceException e)
            {
                QuantumConsole.Instance.LogToConsoleAsync(e.ToString());
            }
        }

        private void OnDestroy()
        {
            AuthenticationService.Instance.SignOut();
        }

        private void OnDisable()
        {
            try
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            catch (Exception e)
            {
                // ignored
            }

            StopAllCoroutines();
        }


        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
                QuantumConsole.Instance.LogToConsoleAsync("[Client] Erfolgreich mit Server verbunden.");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                course = null;
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("MainMenu");
            }
        }

        public string GetLobbyId()
        {
            if (_currentLobby == null) return string.Empty;

            return _currentLobby.Id;
        }
    }
}