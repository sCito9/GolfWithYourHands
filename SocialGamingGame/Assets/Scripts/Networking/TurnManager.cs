using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBackend;
using DedicatedServer;
using QFSW.QC;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking
{
    #region STRUCTS

    [Serializable]
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString32Bytes id;
        public FixedString32Bytes name;

        public int hits;
        public ulong clientId;

        

        public PlayerData(string id, string name, int hits, ulong clientId)
        {
            this.id = new FixedString32Bytes(id);
            this.name = new FixedString32Bytes(name);
            this.hits = hits;
            this.clientId = clientId;
        }

        public PlayerData(ulong clientId)
        {
            id = new FixedString32Bytes("");
            name = new FixedString32Bytes("");
            hits = 0;
            this.clientId = clientId;
        }


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref hits);
            serializer.SerializeValue(ref clientId);
        }

        public bool Equals(PlayerData other)
        {
            return clientId == other.clientId;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, hits, clientId);
        }
    }

    #endregion

    public class TurnManager : NetworkBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public NetworkVariable<ulong> CurrentTurnClientId = new();
        public event Action<ulong> OnTurnChange;

        private List<ulong> players = new();
        public NetworkList<PlayerData> _finishedClients = new();
        private int currentTurnIndex;
        private bool _finished = false;
        private bool _gotInvite = false;
        private string _clubId = String.Empty;
        
        [SerializeField] private AudioMangagerScript _audioManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnTransportFailure += () =>
            {
                if (IsServer)
                {
                    NetworkManager.Singleton.Shutdown();
                    SceneManager.LoadScene("MainMenu");
                }
                else
                {
                    NetworkManager.Singleton.Shutdown();
                    SceneManager.LoadScene("MainMenu");
                }
            };
            CurrentTurnClientId.OnValueChanged += (oldValue, newValue) => OnTurnChange?.Invoke(newValue);
            if (IsServer)
            {
                // Initialize with currently connected clients
                players = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
                if (players.Count > 0) CurrentTurnClientId.Value = players[0];

                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                

                StartCoroutine(FirstValueChange());
            }
        }

        private IEnumerator FirstValueChange()
        {
            if (IsServer)
            {
                int timer = 0;
                while (players.Count != ClientBootstrap._maxPlayer)
                {
                    if (timer > 7)
                    {
                        QuitGame();
                    }
                    ChatMessageClientRpc(
                        $"Waiting for players... Currently {players.Count}/{ClientBootstrap._maxPlayer}",
                        "Server");
                    yield return new WaitForSeconds(5f);
                    timer++;
                }

                ChatMessageClientRpc("Game full, starting in 10 seconds...", "Server");
                yield return new WaitForSeconds(10f);

                AdvanceTurn();
                ChatMessageClientRpc("Write m [Your message] to chat with the other players", "Server");
            }
        }

        #region TURN_And_CONNECTION

        private void OnClientDisconnected(ulong clientId)
        {
            if (_finished) return;
            var pos = players.IndexOf(clientId);
            players.Remove(clientId);
            
            if (players.Count == 0)
            {
                _finished = true;
                StartEndscreen();
                return;
            }

            if (players.Count == 1)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("LastPlayerNotice", players[0],
                    new FastBufferWriter(1, Allocator.Temp), NetworkDelivery.Reliable);
                return;
            }
            
                // Adjust turn index so it stays aligned with new list
                if (pos <= currentTurnIndex)
                {
                    currentTurnIndex--; // Shift back one since list shrank before our current player
                }
                
                AdvanceTurn();
        }
        
        private void AdvanceTurn()
        {
            if (players.Count == 0) return;
            currentTurnIndex = (currentTurnIndex + 1) % players.Count;
            CurrentTurnClientId.Value = players[currentTurnIndex];
        }
        

        private void OnClientConnected(ulong clientId)
        {
            if (!players.Contains(clientId)) players.Add(clientId);
        }


        [ServerRpc(RequireOwnership = false)]
        public void FinishTurnServerRpc(ulong clientId)
        {
            if (!IsServer || players.Count == 0 || !players.Contains(clientId)) return;

            AdvanceTurn();
        }

        

        #endregion

        #region FINISH

        public void SetFinishedClient(ulong clientId, string id, string playerName, int hits)
        {
            if (!IsServer || players.Count == 0) return;
            _finishedClients.Add(new PlayerData(id, playerName, hits, clientId));
            OnClientDisconnected(clientId); //löscht player aus der Liste der Leute die schlagen dürfen
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClientFinishedServerRpc(string id, string playerName, int hits, ulong clientId)
        {
            SetFinishedClient(clientId, id, playerName, hits);
            ClientFinishedSoundClientRpc();
        }

        private void StartEndscreen()
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("GameEndedNotice",
                new FastBufferWriter(1, Allocator.Temp), NetworkDelivery.Reliable);
        }

        public async Task InvitePlayer_onPlace(int to)
        {
            //_place: platz vom sender
            //place: platz vom empfänger
            if (await RemoteBackend.SendFriendRequest(
                    _finishedClients[_finishedClients.IndexOf(new PlayerData(NetworkManager.Singleton.LocalClientId))]
                        .id.ToString(),
                    _finishedClients[to - 1].id.ToString()) == false)
                //invitePlayerButtons[place - 1].GetComponent<Image>().color = Color.red;
                QuantumConsole.Instance.LogToConsoleAsync("Failed to sent Friendrequest");
            else
                QuantumConsole.Instance.LogToConsoleAsync("Friendrequest sent!");
        }

        public void InvitePlayerToClub(int to)
        {
            if (SessionManager.Instance.IsInClub)
            {
                InviteToClubServerRpc(_finishedClients[to - 1].clientId, SessionManager.Instance.User.clubId, SessionManager.Instance.User.name);
            }
            else
            {
                QuantumConsole.Instance.LogToConsoleAsync("You are not in a club yourself!");
            }
                
        }

        [ServerRpc(RequireOwnership = false)]
        private void InviteToClubServerRpc(ulong clientId, string clubId, string name)
        {
            InviteToClubClientRpc(clientId, clubId, name);
        }

        [ClientRpc]
        private void InviteToClubClientRpc(ulong clientId, string clubId, string name)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId && !SessionManager.Instance.IsInClub)
            {
                _gotInvite = true;
                _clubId = clubId;
                QuantumConsole.Instance.LogToConsoleAsync($"You have been invited to a club by {name}. Type Join to join the last invite!");
            }
        }

        #endregion

        #region SOUND

        [ClientRpc]
        private void ClientFinishedSoundClientRpc()
        {
            //sound für fertigen client
            _audioManager.playFinishCourse();
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbschlagSoundServerRpc()
        {
            AbschlagSoundClientRpc();
        }

        [ClientRpc]
        private void AbschlagSoundClientRpc()
        {
            _audioManager.playAbchlag();
            //sound für abschlag hier
        }

        #endregion

        #region CHAT

        [Command]
        public void Join()
        {
            if (_gotInvite && _clubId != String.Empty && !SessionManager.Instance.IsInClub)
            {
                StartCoroutine(DataLoader.JoinClub(_clubId, success => {
                    if (!success) {
                        QuantumConsole.Instance.LogToConsoleAsync("Something went wrong!");
                        return;
                    }
                    QuantumConsole.Instance.LogToConsoleAsync("You have joined a club!");
                    // On Success
                }));
                
            }
            else
            {
                QuantumConsole.Instance.LogToConsoleAsync("You have no active invite!");
            }
        }

        [Command]
        public void ResetPosition()
        {
            SendResetNoticeServerRpc();
        }

        [Command]
        public void AdvanceTurnManually()
        {
            FinishTurnServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendResetNoticeServerRpc()
        {
            var writer = new FastBufferWriter(sizeof(ulong), Allocator.Temp);
            writer.WriteValueSafe(CurrentTurnClientId.Value);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ResetNotice", writer);
        }

        [Command]
        public void GetTurnVariable()
        {
            PrintTurnVariableServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void PrintTurnVariableServerRpc()
        {
            if (IsServer)
                ChatMessageClientRpc($"turn: {CurrentTurnClientId.Value}", "Server");
        }


        [Command]
        public void QuitGame()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ShutdownNotice",
                    new FastBufferWriter(1, Allocator.Temp), NetworkDelivery.Reliable);
                NetworkManager.Singleton.Shutdown();
            }
            else
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("MainMenu");
            }
        }

        [Command("m")]
        public void ChatMessage(params string[] message)
        {
            ChatMessageServerRpc(string.Join(" ", message), SessionManager.Instance.User.name);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChatMessageServerRpc(string message, string sender)
        {
            ChatMessageClientRpc(message, sender);
        }

        [ClientRpc]
        private void ChatMessageClientRpc(string message, string sender)
        {
            if (sender != SessionManager.Instance.User.name)
                QuantumConsole.Instance.LogToConsoleAsync($"[{sender}]: {message}");
        }

        #endregion
    }
}