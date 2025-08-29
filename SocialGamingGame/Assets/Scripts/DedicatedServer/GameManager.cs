using System;
#if !UNITY_SERVER
#endif
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace DedicatedServer
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
            this.id = new FixedString32Bytes("");
            this.name = new FixedString32Bytes("");
            this.hits = 0;
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
    
    
    [RequireComponent(typeof(NetworkObject))]
    public class GameManager : NetworkBehaviour
    {
        
        /*
        [ClientRpc]
        private void NotifyTurnChangedClientRpc(int turn)
        {
            Debug.Log("Notify turn changed: "+ turn);
            NextTurn(turn); 
        }

        private void GetReferences()
        {
            georeference = _reference.georeference;
            holeSensor = _reference.holeSensor;
            finishScreeen = _reference.finishScreeen;
            playerFields = _reference.playerFields;
            hitFields = _reference.hitFields;
            invitePlayerButtons = _reference.invitePlayerButtons;
            podium = _reference.podium;
            finish = _reference.finish;
        }
        
        #endregion

        /*
         * Finish stuff
         
        
        #region FINISH
         [ServerRpc(RequireOwnership = false)]
        private void WriteFinishListServerRpc(PlayerData data)
        {
             
            Debug.Log("WriteFinishListServerRpc called");
            _finishedClients.Add(data);
        }
        
        private void ClientFinished(System.EventArgs e)
        {
             
            Debug.Log("Client Finished method called");
            _hits++;
            WriteFinishListServerRpc(new PlayerData(SessionManager.Instance.User.id, SessionManager.Instance.User.name, 
                _hits, NetworkManager.Singleton.LocalClientId));
        }

        
        private void ClientFinishedServer(NetworkListEvent<PlayerData> finisherData)
        {
             if (IsServer) {
            Debug.Log("Client FinishedServer called");
            if (_finishedClients.Count == _connectedClientsCount)
            {
                ActivateFinishScreenClientRpc();
            }
            else
            {
                //Finish turn server rpc
                int temp = _turn.Value;
                //_turn.Value = -1;
                Debug.Log("_turn changed to -1 in ClientFinishedServer.");
                while (_finishedClients.Contains(new PlayerData((ulong)temp)))
                {
                    temp = (temp + 1) % _connectedClientsCount;
                }

                // _turn.Value = temp;
                _turn.Value = temp;
                NotifyTurnChangedClientRpc(_turn.Value);
                Debug.Log($"_turn changed to {temp} in ClientFinishedServer.");
            }

             }
        }

        private IEnumerator FinishRoutine()
        {
            ChatMessageClientRpc("[Server]: Server shuts down in 60 seconds.", String.Empty);
            yield return new WaitForSeconds(30f);
            ChatMessageClientRpc("[Server]: Server shuts down in 30 seconds.", String.Empty);
            yield return new WaitForSeconds(15f);
            ChatMessageClientRpc("[Server]: Server shuts down in 15 seconds.", String.Empty);
            yield return new WaitForSeconds(10f);
            ChatMessageClientRpc("[Server]: Server shuts down in 5 seconds.", String.Empty);
            yield return new WaitForSeconds(5f);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ShutdownNotice", new FastBufferWriter(1, Allocator.Temp), NetworkDelivery.Reliable);
            yield return new WaitForSeconds(30f);   //make sure everyones gone
            NetworkManager.Singleton.Shutdown();
            Application.Quit();
        }

        
        private void ClientFinishedClient(NetworkListEvent<PlayerData> finisherData)
        {
             
            Debug.Log($"[Server]: {finisherData.Value.name.ToString()} finished with {finisherData.Value.hits} hits.");
        }


        
        #endregion

        /*
         * Connection stuff
         *
         *
         *
         *
         *
         * 
         

        //#region CONNECTION
        
        private void OnClientConnected(ulong clientId)
        {
             
            _connectedClientsCount = (int)clientId + 1;
            Debug.Log($"Client connected: {clientId}");
            Debug.Log($"Client count: {_connectedClientsCount}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
             
            if (_turn.Value == (int)clientId)
            {
                int temp = _turn.Value;
                _turn.Value = -1;
                Debug.Log("Turn changed to -1 in OnClientDisconnected.");
                _connectedClientsCount--;
                _turn.Value = temp;
                Debug.Log($"Turn changed to {temp} in OnClientDisconnected.");
            }
            else
            {
                _connectedClientsCount--;
            }
            
        }
        
        
        public void ReturnToMainMenu()
        {
             
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
        
        
        private void OnShutdownNoticeReceived(ulong senderClientId, FastBufferReader reader)
        {
            ReturnToMainMenu();
        }

        #endregion
        
        /*
         * Chat stuff
         *
         *
         *
         *
         *
         * 
         
        
        //#region CHAT
        
        [Command]
        public void GetTurnVariable()
        {
             
            //Debug.Log("_turn: " + _turn.Value);
            //Debug.Log("_turn: " + turn);
            PrintTurnVariableServerRpc();
        }

         [ServerRpc(RequireOwnership = false)]
        private void PrintTurnVariableServerRpc()
        {
            if (IsServer)
                ChatMessageClientRpc($"turn: {_turn}", "Server");
        }
        
        
        
        [Command]
        public void QuitGame()
        {
             
            if (IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ShutdownNotice", new FastBufferWriter(1, Allocator.Temp), NetworkDelivery.Reliable);
                NetworkManager.Singleton.Shutdown();
            }
            else
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("MainMenu");
            }
            
        }

        [Command]
        public void SetTurnVariable(int turn)
        {
            SetTurnVariableServerRpc(turn);
        }

         [ServerRpc(RequireOwnership = false)]
        private void SetTurnVariableServerRpc(int turn)
        {
            if (IsServer)
            {
                this._turn.Value = turn;
                NotifyTurnChangedClientRpc(turn);
            }
        }
        
        [Command("c")]
        public void ChatMessage(params string[] message)
        {
             
            Debug.Log("Chat message called");
            ChatMessageServerRpc(string.Join(" ", message), SessionManager.Instance.User.name);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChatMessageServerRpc(string message, string sender)
        {
             
            ChatMessageClientRpc(message, sender);
        }

        [ClientRpc]
        private void ChatMessageClientRpc(string message, string sender)
        {
             
            if (sender != SessionManager.Instance.User.name)
                Debug.Log($"[{sender}]: {message}");
        }
        
        #endregion

        
        /*
         * Turn stuff
         
        
        //#region TURN
        
        
        private void FinishTurn()
        {
             
            Debug.Log("Finish turn called");
            _hits++;
            FinishTurnServerRpc();
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void FinishTurnServerRpc()
        {
            if (IsServer)
            {
                Debug.Log("FinishTurnServerRpc called");
                if (!IsOwner) return;
                if (_finishedClients.Count >= _connectedClientsCount)
                    return; //should be safe, but better safe than sorry

                Debug.Log("Connected Clients count: " + _connectedClientsCount);
                int temp = (_turn.Value + 1) % _connectedClientsCount;
                Debug.Log("Turn changed to temp: " + temp);
                //_turn.Value = -1;
                while (_finishedClients.Contains(new PlayerData((ulong)temp)))
                {
                    Debug.Log("attempting to find player who hasn't finished yet.");
                    temp = (temp + 1) % _connectedClientsCount;
                }

                //_turn.Value = temp;

                _turn.Value = temp;
                Debug.Log($"Turn changed to {_turn.Value} in FinishTurnServerRpc end.");
                NotifyTurnChangedClientRpc(_turn.Value);

            }
        }

        
        private void NextTurn(int currentValue)
        {
            if (!IsClient && !IsOwner)
            {
                Debug.Log("I'm not the owner or client.");
                return;
            }
            Debug.Log("NextTurn called");
             
            Debug.Log("\n[Next Turn] owner");
            Debug.Log($"Turn changed to {currentValue}");
            if (currentValue == -1)
            {
                Debug.Log("Turn changed to -1. Should be the first turn, returning...");
                return;
            }
            Debug.Log("Owner: "+IsOwner + " await IsMyTurn");
            if (IsMyTurn(currentValue))
            {
                Debug.Log("It's my turn");
                //Activate movement
                _abschlagController?.StartAbschlag();
            }
            else
            {
                    Debug.Log("It's not my turn. Ignoring changing view for now");
                    var client = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId ==
                        (ulong)currentValue);
                    if (client == null)
                    {
                        Debug.Log("Client not found");
                        return;
                    }
                    var playerObject = client.PlayerObject.transform;

                    var otherPlayersGolfBall = playerObject.GetComponentInChildren<GolfBallController>();
                    _abschlagController?.SpectateAbschlag(otherPlayersGolfBall?.gameObject);
                    
            }
            
        }
        
        private bool IsMyTurn(int currentValue)
        {
            Debug.Log("IsMyTurn called");
            Debug.Log(" currVal " + currentValue);
            Debug.Log(NetworkManager.Singleton.LocalClientId + " local Client Id");
            return (ulong)currentValue == NetworkManager.Singleton.LocalClientId;
        }
        
        
        #endregion

        
        
        */
        
    }

}
