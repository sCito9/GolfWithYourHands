using DataBackend;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Club
{
    public class CreateJoinClubMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField createNameInputField;
        [SerializeField] private TMP_InputField createDescriptionInputField;
        [SerializeField] private TMP_InputField joinIdInputField;

        [SerializeField] private GameObject popUpWindow;
        private TMP_Text popUpText;

        // Constants
        private const int maxNumMembers = 10;
        private const int minCharsClubName = 3;
        private const int maxCharsClubName = 20;
        private const int minCharsDescription = 5;
        private const int maxCharsDescription = 50;

        private void Start()
        {
            popUpText = popUpWindow.transform.Find("PopUpText").GetComponent<TMP_Text>();
            CheckAlreadyPartOfClub();
        }
    
        private void OnEnable()
        {
            SessionManager.Instance.OnRefreshClub += CheckAlreadyPartOfClub;
        }

        private void OnDisable()
        {
            SessionManager.Instance.OnRefreshClub -= CheckAlreadyPartOfClub;
        }
        
        /// <summary>
        /// Check if the current user is already part of a club. In this case
        /// go to the club overview.
        /// </summary>
        private void CheckAlreadyPartOfClub()
        {
            if (SessionManager.Instance.IsInClub)
                SceneManager.LoadScene("ClubOverviewMenu");
        }

        private void OnJoinClub()
        {
            var id = joinIdInputField.text.Trim().ToLower();
            // Get current members and check if max members are reached
            StartCoroutine(DataLoader.GetClubMembers(id, members =>
            {
                if (members == null)
                {
                    Debug.Log("Failed to join club.");
                    popUpText.text = "Failed to join club. Please verify the Id and try again.";
                    popUpWindow.SetActive(true);
                    return;
                }
                if (members.Count >= maxNumMembers)
                {
                    Debug.Log("Failed to join club.");
                    popUpText.text = "Failed to join club. The club is already full.";
                    popUpWindow.SetActive(true);
                    return;
                }
                // Join the club
                StartCoroutine(DataLoader.JoinClub(id, success =>
                {
                    if (!success)
                    {
                        Debug.Log("Failed to join club.");

                        popUpText.text = "Failed to join club. Please verify the Id and try again.";
                        popUpWindow.SetActive(true);
                        return;
                    }
                    SessionManager.Instance.RefreshUser();
                }));
            }));
        }
    
        private void OnCreateClub()
        {
            var clubName = createNameInputField.text.Trim();
            var description = createDescriptionInputField.text.Trim();

            if (string.IsNullOrEmpty(clubName) || clubName.Length < minCharsClubName || clubName.Length > maxCharsClubName)
            {
                popUpText.text = $"Club name must be between {minCharsClubName} and {maxCharsClubName} characters.";
                popUpWindow.SetActive(true);
                return;
            }
            if (string.IsNullOrEmpty(description) || description.Length < minCharsDescription || description.Length > maxCharsDescription)
            {
                popUpText.text = $"Description must be between {minCharsDescription} and {maxCharsDescription} characters.";
                popUpWindow.SetActive(true);
                return;
            }
        
            var club = new DataBackend.Models.Club(clubName, description, SessionManager.Instance.User.id);
        
            StartCoroutine(DataLoader.CreateClub(club, createdClub =>
            {
                if (createdClub == null)
                {
                    Debug.Log("Failed to create club.");
                    popUpText.text = "Failed to create club. Please try again.";
                    popUpWindow.SetActive(true);
                    return;
                }
                SessionManager.Instance.RefreshUser();
            }));
        }
    
        public void OnCreateButtonPressed()
        {
            OnCreateClub();
        }
    
        public void OnJoinButtonPressed()
        {
            OnJoinClub();
        }

        public void OnBackButtonPressed()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
