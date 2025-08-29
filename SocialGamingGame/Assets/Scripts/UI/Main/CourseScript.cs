using System;
using DataBackend;
using DataBackend.Models;
using TMPro;
using UnityEngine;

namespace UI.Main
{
    public class CourseScript : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        
        private CreateGameScript startGameScript;
        private Course course;

        private void Awake()
        {
            
                Debug.Log($"[SPAWN DEBUG] {gameObject.name} wurde gespawnt. Stacktrace:\n" + Environment.StackTrace);
            
        }

        public void SetCourse(Course course, CreateGameScript startGameScript)
        {
            this.course = course;
            this.startGameScript = startGameScript;
            StartCoroutine(DataLoader.GetUser(course.authorId, user =>
            {
                if (user == null)
                    text.text = course.name;
                else
                    text.text = course.name + " by " + user.name;
            }));
            
            //this.startGameScript.updateCourseList += DeleteObjects;
        }
        
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
            startGameScript.course = course;
        }
    }
}
