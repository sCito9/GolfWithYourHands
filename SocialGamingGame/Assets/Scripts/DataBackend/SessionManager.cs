using System;
using System.Collections;
using DataBackend.Models;
using UnityEngine;

namespace DataBackend
{
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;

        public Course currentCourse;
        public int currentClubBattleCourseIndex;

        public SessionData sessionData;

        public User User => sessionData.user;
        public Club Club => sessionData.club;
        
        public ClubBattleSearch ClubBattleSearch {get; set;}
        public ClubBattle ClubBattle { get; set; }
        public ClubBattleStatus ClubBattleStatus { get; set; }
        
        public bool IsInClub => !string.IsNullOrEmpty(sessionData.user.clubId);
        public bool IsLoggedIn => sessionData.user != null;

        public static SessionManager Instance
        {
            get
            {
                if (_instance) return _instance;

                var gObject = new GameObject("SessionManager");
                _instance = gObject.AddComponent<SessionManager>();
                DontDestroyOnLoad(gObject);
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Try to restore the previous session
            sessionData = LocalCache.TryLoadSession() ?? new SessionData();
        }

        public event Action OnLogIn;
        public event Action OnLogOut;
        public event Action OnRefresh;
        public event Action OnRefreshUser;
        public event Action OnRefreshClub;
        public event Action OnClubBattleStatusChanged;

        /// <summary>
        ///     Persist the current session in local storage
        /// </summary>
        public void SaveSession()
        {
            LocalCache.SaveSession(sessionData);
        }

        /// <summary>
        ///     Log in the provided user and persist the session
        /// </summary>
        public void LogIn(User user)
        {
            StartCoroutine(user.id == null
                ? DataLoader.CreateUser(user, ProcessResponse)
                : DataLoader.GetUser(user.id, ProcessResponse));
            return;

            // Local Function
            void ProcessResponse(User u)
            {
                if (u == null) return; // TODO: Display error message

                sessionData.user = u;
                SaveSession();
                OnLogIn?.Invoke();
                Refresh();
            }
        }


        /// <summary>
        ///     Log out the current user and destroy the session
        /// </summary>
        public void LogOut()
        {
            LocalCache.DeleteSession();
            sessionData = new SessionData();
            ClubBattle = null;
            ClubBattleSearch = null;
            ClubBattleStatus = null;
            currentCourse = null;
            //currentGame = null;
            currentClubBattleCourseIndex = -1;
            OnLogOut?.Invoke();
        }


        /// <summary>
        ///     Refresh the session. This deletes the cached users and courses and fetches them from the server.
        /// </summary>
        public void Refresh()
        {
            var id = sessionData.user?.id;
            
            Debug.Log("SessionManager: Refreshing session");
            StartCoroutine(DataLoader.GetUser(id, user =>
            {
                sessionData.user = user;
                if (IsInClub)
                {
                    StartCoroutine(DataLoader.GetClub(sessionData.user.clubId, club =>
                    {
                        if (club == null)
                        {
                            Debug.LogError("Failed to get club.");
                            SaveSession();
                            OnRefresh?.Invoke();
                            return;
                        }
                        
                        sessionData.club = club;

                        StartCoroutine(DataLoader.GetClubBattleStatus(club.id, battleStatus =>
                        {
                            if (battleStatus == null)
                            {
                                Debug.Log("Failed to get club battle status.");
                                return;
                            }
                            
                            Debug.Log("ClubBattleStatus: " + battleStatus);
                            
                            ClubBattleStatus = battleStatus;
                            OnClubBattleStatusChanged?.Invoke();
                        }));
                        
                        SaveSession();
                        OnRefresh?.Invoke();
                    }));
                }
                else
                {
                    SaveSession();
                    OnRefresh?.Invoke();
                }
            }));

            return;
            
            // Local Function
            void OnGetUser(User user)
            {
                sessionData.user = user;
                SaveSession();
                OnRefresh?.Invoke();
            }
        }
        
        public void RefreshUser()
        {
            var id = User?.id;
            StartCoroutine(DataLoader.GetUser(id, user =>
            {
                if (user == null)
                {
                    Debug.LogError("Failed to get user.");
                    return;
                }
                sessionData.user = user;
                SaveSession();
                OnRefreshUser?.Invoke();
                
                if (IsInClub)
                    RefreshClub();
            }));
        }

        public void RefreshClub()
        {
            var id = User?.clubId;
            if (id == null) return;
            
            StartCoroutine(DataLoader.GetClub(id, club =>
            {
                if (club == null)
                {
                    Debug.LogError("Failed to get club.");
                    return;
                }
                sessionData.club = club;
                SaveSession();
                OnRefreshClub?.Invoke();
            }));
        }
        
        public void RefreshClubBattleStatus()
        {
            var id = User?.clubId;
            if (id == null) return;
            
            StartCoroutine(DataLoader.GetClubBattleStatus(id, battleStatus =>
            {
                if (battleStatus == null)
                {
                    Debug.Log("Failed to get club battle status.");
                    return;
                }
                
                ClubBattleStatus = battleStatus;

                if (ClubBattleStatus.status == "in_battle")
                {
                    StartCoroutine(DataLoader.GetClubBattle(ClubBattleStatus.battleId, battle =>
                    {
                        if (battle == null)
                        {
                            Debug.Log("Failed to get club battle.");
                            return;
                        }
                        
                        ClubBattle = battle;
                        OnClubBattleStatusChanged?.Invoke();
                    }));
                } else if (ClubBattleStatus.status == "searching")
                {
                    /* #TODO: Missing on DataServer
                    StartCoroutine(DataLoader.GetClubBattleSearch(ClubBattleStatus.battleId, search => {}));
                    */
                    OnClubBattleStatusChanged?.Invoke();
                }
                else
                {
                    OnClubBattleStatusChanged?.Invoke();
                }
            }));
        }
    }
}