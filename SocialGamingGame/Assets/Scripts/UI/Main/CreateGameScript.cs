using System;
using DataBackend;
using DataBackend.Models;
using DedicatedServer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Main
{
    public class CreateGameScript : MonoBehaviour
    {
        [SerializeField] private Transform location;
        [SerializeField] private GameObject coursePrefab;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private GameObject loadingScreen;

        private ClientBootstrap clientBootstrap;
        private int _maxPlayers = 4;
        [HideInInspector]public Course course = null;

        private void Start()
        {
            clientBootstrap = FindAnyObjectByType<ClientBootstrap>();
            inputField.text = _maxPlayers+"";
            inputField.onSubmit.AddListener(ChangeMaxPlayers);
            UpdateCourses();
        }

        public void ChangeMaxPlayers(string number)
        {
            try
            {
                int newMaxPlayers = int.Parse(number);
                if (newMaxPlayers > 0 && newMaxPlayers <= 4)
                {
                    _maxPlayers = newMaxPlayers;
                }
            }
            catch (Exception e)
            {
                //no changes
                Debug.Log("Input is not allowed");

            }
            finally
            {
                inputField.text = _maxPlayers+"";
            }
        }

        public void CreateGame()
        {
            if (course == null)
            {
                Debug.LogWarning("No course selected");
                return;
            }

            ClientBootstrap.course = course;
            Instantiate(loadingScreen, SceneManager.GetActiveScene());
            clientBootstrap.CreateLobby(_maxPlayers, SessionManager.Instance.User.name);
        }


        public void UpdateCourses()
        {
            // Clear course list
            foreach (Transform child in location)
                Destroy(child.gameObject);
            
            
            StartCoroutine(DataLoader.GetCourses(courses =>
            {
                if (courses == null) return; // TODO: Handle error
                
                foreach (var course in courses)
                {
                    var obj = Instantiate(coursePrefab, location);
                    obj.GetComponent<CourseScript>().SetCourse(course, this); //need to get current nr of players in here
                }
            }));
        }
    }
}
