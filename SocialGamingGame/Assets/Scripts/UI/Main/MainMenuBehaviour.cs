using System.Collections;
using DataBackend;
using QFSW.QC;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Main
{
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Transform notificationPanel;
        [SerializeField] private GameObject notificationPrefab;
        
        private Coroutine _pollInvitesRoutine;
        
        private void Start()
        {
            if (QuantumConsole.Instance != null) Destroy(QuantumConsole.Instance.gameObject);
            
            if (SessionManager.Instance.IsLoggedIn == false)
            {
                Logout();
                return;
            }
            
            var user = SessionManager.Instance.User;
            text.text = "Player: "+ user.name + "\nUID: " + user.id;

        }

        private IEnumerator PollInvitesRegularly()
        {
            while (true)
            {
                PollInvites();
                yield return new WaitForSeconds(5);
            }
        }
        
        private void OnEnable()
        {
            SessionManager.Instance.OnLogOut += OnLogout;
            
            _pollInvitesRoutine = StartCoroutine(PollInvitesRegularly());
        }
        
        private void OnDisable()
        {
            SessionManager.Instance.OnLogOut -= OnLogout;
            
            StopCoroutine(_pollInvitesRoutine);
        }
        
        private static async void OnLogout()
        {
            var obj = GameObject.FindGameObjectWithTag("UserProfile");
            Destroy(obj);
            SceneManager.LoadScene(0);
            
        }

        public void Logout()
        {
            SessionManager.Instance.LogOut();
        }
        

        public async void ExitGame()
        {
            SessionManager.Instance.SaveSession();
            Application.Quit();
        }


        public void OpenClubMenu()
        {
            SceneManager.LoadScene("CreateJoinClubMenu");
        }


        public void CreateCourse()
        {
            SceneManager.LoadScene(2);
        }

        public void DeleteAccount()
        {
            StartCoroutine(DataLoader.DeleteUser(SessionManager.Instance.User.id, success =>
            {
                if (success) SessionManager.Instance.LogOut();
            }));
        }

        private void PollInvites()
        {
            var notification = notificationPanel.GetComponentInChildren<NotificationScript>();
            if (notification)
            {
                return;
            }
            
            StartCoroutine(DataLoader.HasInvite(SessionManager.Instance.User.id, invite =>
            {
                if (invite == null) return;
                
                var obj = Instantiate(notificationPrefab, notificationPanel);
                obj.GetComponent<NotificationScript>().SetInvite(invite);
            }));
        }
        

        public void ActivateLoadingScreen()
        {
            Instantiate(loadingScreen, SceneManager.GetActiveScene());
        }
    }
}
