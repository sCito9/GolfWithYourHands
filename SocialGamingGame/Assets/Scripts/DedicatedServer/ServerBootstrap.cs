using DataBackend;
using DataBackend.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DedicatedServer
{/*
    public class ServerBootstrap : MonoBehaviour
    {
        //Beispiel für Server start
        //./MyServer.x86_64 -batchmode -nographics -course {courseId}
        
        [Header("Server Konfiguration")]
        private string gameSceneName = "GameScene";
        public ushort port = 7777;
        //public int maxPlayers = 4;
        private bool started = false;

        private Game _game;
        public static Course _course;
        
        void Start()
        {
            #if UNITY_SERVER

                SceneManager.LoadScene("MainMenu");

                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 10;
            
                Debug.Log("[Server]: Initializing Server...");
                
                string gameId = GetArg("-game");
                
                #if !UNITY_EDITOR
                StartCoroutine(DataLoader.GetGame(gameId, receivedGame =>
                {
                    if (receivedGame == null)
                    {
                        Debug.LogError("[Server]: Could not load game " + gameId);
                        return;
                    }

                    _game = receivedGame;

                    StartCoroutine(DataLoader.GetCourse(receivedGame.courseId, receivedCourse =>
                    {
                        if (receivedCourse == null)
                        {
                            Debug.LogError("[Server]: Could not load course " + receivedGame.courseId);
                            return;
                        }

                        _course = receivedCourse;
                        StartServer();
                    }));
                }));
                #else

                _course = new Course("Test", "Testauthor", new float3(0, 0, 0), new float3(0, 0, 10), new double3(39.7364, -105.2574, 2250), new float3[0]);

                #endif
                //start in server mode
                //StartServer();
                
            #else // not UNITY_SERVER -> Client

            Destroy(this);

            #endif
        }

        private void StartServer()
        {
                    
            // Transport configuration(IP and port)
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData("0.0.0.0", port); //akzeptiert alle eingehenden Verbindungen
                
            // Spieleranzahl begrenzen
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            
            // Server starten
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log($"[Server]: Server started successfully. Listening on port {port}.");
            }
            else
            {
                Debug.LogError("[Server]: Server failed to start");
                return;
            }
            
            Debug.Log("[Server]: Server started and listens for connections.");
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var maxPlayers = _game.maxPlayers;
            if (NetworkManager.Singleton.ConnectedClientsList.Count >= maxPlayers || started)
            {
                Debug.Log("[Server]: Server is full. Connection declined.");
                response.Approved = false;
                response.Reason = "Server is full";
            }
            else if (NetworkManager.Singleton.ConnectedClientsList.Count == maxPlayers - 1)
            {
                response.Approved = true;

                started = true;
                
                StartCoroutine(DataLoader.StartGame(_game.id, _game.hostId, success =>
                {
                    // #TODO: FIX THIS AT THE DATA SERVER
                    //if (!success)
                    //{
                    //    Debug.LogError("[Server]: Failed to start game");
                    //    return;
                    //}
                    //
                    Debug.Log("[Server]: Started game!");
                    NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                }));
            }else
            {
                response.Approved = true;
            }
        }

        public void StartGameEarly()
        {
            started = true;
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        
        private string GetArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }
    }*/
}
