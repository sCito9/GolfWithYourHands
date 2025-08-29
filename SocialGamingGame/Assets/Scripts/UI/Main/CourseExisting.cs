using DedicatedServer;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Main
{
    public class CourseExisting : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private GameObject loadingScreen;
        
        private StartGameScript startGameScript;
        private ClientBootstrap clientBootstrap;
        private Lobby lobby;
        
        private string _textString = "Unknown";

        private void Start()
        {
            clientBootstrap = FindAnyObjectByType<ClientBootstrap>();
        }

        public void SetGame(Lobby lobby, string name)
        {
            this.lobby = lobby;
            _textString = name;
            UpdateText();
            
        }

        private void UpdateText()
        {
            text.text = _textString;
        }
        /*
        public void SetCourse(Course course, StartGameScript startGameScript, int numberOfPlayers)
        {
            this.course = course;
            this.startGameScript = startGameScript;
            StartCoroutine(DataLoader.GetUser(course.authorId, user =>
            {
                if (user == null)
                    text.text = course.name;
                else
                    text.text = course.name + " by " + user.name;

                text.text += " " + numberOfPlayers + "/4";
            }));
            
            //this.startGameScript.updateCourseList += DeleteObjects;
        }
        */
        public void DeleteObjects(object sender, System.EventArgs e)
        {
            //startGameScript.updateCourseList -= DeleteObjects;
            //Destroy(gameObject);
        }

        private void OnDisable()
        {
            //startGameScript.updateCourseList -= DeleteObjects;
        }

        public void ChooseCourse()
        {
            Instantiate(loadingScreen, SceneManager.GetActiveScene());
            clientBootstrap.JoinLobby(lobby);
        }
    }
}
