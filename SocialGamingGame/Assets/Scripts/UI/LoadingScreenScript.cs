using System;
using System.Collections.Generic;
using DedicatedServer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class LoadingScreenScript : MonoBehaviour
    {
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private GameObject startEarlyButton;

        private List<GameObject> _playersInLobby;

        public GameObject playerPrefab;
        public Transform location;

        private Action _onKickedHandler;
        private Action _onStartHandler;
        private ClientBootstrap clientBootstrap;

        private int maxPlayer = 4;
        private int currentPlayer = 1;


        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _playersInLobby = new List<GameObject>();
            SceneManager.LoadScene("GameScene");
            clientBootstrap = ClientBootstrap.Instance;
            _onKickedHandler = () =>
            {
                SceneManager.LoadScene("MainMenu");
                loadingText.text = "Leaving Lobby...";
                Destroy(gameObject, 3);
            };
            clientBootstrap.OnKickedFromLobby += _onKickedHandler;

            _onStartHandler = () =>
            {
                loadingText.text = "Starting Game...";
                Destroy(gameObject, 5);
            };
            clientBootstrap.OnGameStarted += _onStartHandler;
            clientBootstrap.OnPlayerJoined += UpdateScreen;
            //have to activate StartEarly
            // Disable the start early button
            startEarlyButton.SetActive(false);
        }

        private void UpdateScreen(List<String> playerList)
        {
            foreach (GameObject go in _playersInLobby)
            {
                Destroy(go);
            }
            _playersInLobby.Clear();

            maxPlayer = clientBootstrap.GetMaxPlayer();
            
            
            if (playerList.Count >= 2 && clientBootstrap.IsHost())
            {
                if (startEarlyButton != null)
                    startEarlyButton.SetActive(true);
            }
            else
            {
                if (startEarlyButton != null)
                    startEarlyButton.SetActive(false);
            }
            loadingText.text = "Waiting... " + playerList.Count + "/" + maxPlayer;
            foreach (String s in playerList)
            {
                var instance = Instantiate(playerPrefab, location);
                instance.GetComponent<TMP_Text>().text = s;
                _playersInLobby.Add(instance);
            }
        }
        
        private void OnDestroy()
        {
            if (clientBootstrap != null && _onKickedHandler != null)
            {
                clientBootstrap.OnKickedFromLobby -= _onKickedHandler;
            }

            if (clientBootstrap != null && _onStartHandler != null)
            {
                clientBootstrap.OnGameStarted -= _onStartHandler;
            }
        }
        

        
        public void StartEarly()
        {
            clientBootstrap.CreateRelay();
        }

        public void Cancel()
        {
            clientBootstrap.LeaveLobby();
        }
    }
}
