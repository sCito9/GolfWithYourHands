using DataBackend;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Login
{
    public class ProfileScript : MonoBehaviour
    {
        [SerializeField] private TMP_InputField loginInputField;
        [SerializeField] private TMP_InputField createNewInputField;
        [SerializeField] private int gamerTagLimit = 14;
        
        private void Awake()
        {
            if (SessionManager.Instance.IsLoggedIn)
                OnLogin();
            
            createNewInputField.characterLimit = gamerTagLimit;
            DontDestroyOnLoad(gameObject);
            loginInputField.onEndEdit.AddListener(TryLoadUser);
            createNewInputField.onEndEdit.AddListener(CreateNewUser);

        }
        

        private void OnEnable()
        {
            SessionManager.Instance.OnLogIn += OnLogin;
        }

        private void OnDisable()
        {
            SessionManager.Instance.OnLogIn -= OnLogin;
        }

        private void OnLogin()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void TryLoadUser(string userString)
        {
            var oldUser = new DataBackend.Models.User
            {
                id = userString
            };
            Debug.Log(userString);
            SessionManager.Instance.LogIn(oldUser);
        }

        public void CreateNewUser(string gamerTag)
        {
            if (gamerTag.Length < 4 || gamerTag.Length > gamerTagLimit)
            {
                createNewInputField.text = "";
                Debug.LogWarning("GamerTag length must be between 4 and 14."); // TODO: Display Error
                return;
            }
            
            var newUser = new DataBackend.Models.User(gamerTag);
            
            SessionManager.Instance.LogIn(newUser);
        }
    }
}