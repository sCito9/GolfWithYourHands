using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataBackend;
using DataBackend.Models;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Club
{
    public class ClubBattleMenu : MonoBehaviour
    {
        private const int GolfCoursesPerBattle = 3;

        [SerializeField] private TMP_Text battleEndTimeText;
        [SerializeField] private TMP_Text battleStatusText;

        [SerializeField] private TMP_Text name1Text;
        [SerializeField] private TMP_Text name2Text;
        [SerializeField] private TMP_Text id1Text;
        [SerializeField] private TMP_Text id2Text;
        [SerializeField] private TMP_Text score1Text;
        [SerializeField] private TMP_Text score2Text;
        [SerializeField] private TMP_Text description1Text;
        [SerializeField] private TMP_Text description2Text;
        [SerializeField] private Transform participants1Container;
        [SerializeField] private Transform participants2Container;
        [SerializeField] private GameObject participantPrefab;

        [SerializeField] private Button[] playCourseButtons;

        private Coroutine _pollClubBattleStatusCoroutine;
        private ClubBattle _clubBattle;

        private void Start()
        {
            SessionManager.Instance.RefreshClubBattleStatus();
        }

        public void OnEnable()
        {
            SessionManager.Instance.OnClubBattleStatusChanged += RefreshUI;
            _pollClubBattleStatusCoroutine = StartCoroutine(PollClubBattleStatusRegularly());
        }

        public void OnDisable()
        {
            SessionManager.Instance.OnClubBattleStatusChanged -= RefreshUI;
            StopCoroutine(_pollClubBattleStatusCoroutine);
        }

        private IEnumerator PollClubBattleStatusRegularly()
        {
            while (true)
            {
                SessionManager.Instance.RefreshClubBattleStatus();
                yield return new WaitForSeconds(5);
            }
        }

        private void RefreshUI()
        {
            var clubBattle = SessionManager.Instance.ClubBattle;
            _clubBattle = clubBattle;
            // Update clubBattle information
            DateTime now = DateTime.UtcNow;
            if (now > clubBattle.AbsoluteEndTime)
            {
                StartCoroutine(DataLoader.DeleteClubBattle(clubBattle.id, success =>
                {
                    if (!success)
                    {
                        Debug.LogError("Failed to delete club battle.");
                        return;
                    }

                    SessionManager.Instance.ClubBattle = null;
                    SceneManager.LoadScene("ClubOverviewMenu");
                }));
                return;
            }

            // Calculate scores
            int score1 = 0;
            int score2 = 0;
            clubBattle.club1Scores.ForEach(entry => score1 += entry.score.Sum());
            clubBattle.club2Scores.ForEach(entry => score2 += entry.score.Sum());
            // Update playCourseButtons
            SetPlayCourseButtonsInteractability(true);
            if (!clubBattle.participantIdsClub1.Contains(SessionManager.Instance.User.id) &&
                !clubBattle.participantIdsClub2.Contains(SessionManager.Instance.User.id))
            {
                SetPlayCourseButtonsInteractability(false);
            }

            if (now > clubBattle.EndTime)
            {
                SetPlayCourseButtonsInteractability(false);
                if (score1 < score2)
                {
                    battleStatusText.text = "Club 1 won!";
                }
                else if (score1 > score2)
                {
                    battleStatusText.text = "Club 2 won!";
                }
                else
                {
                    battleStatusText.text = "The battle ended in a draw!";
                }

                battleEndTimeText.text =
                    "Battle ended at: " + clubBattle.EndTime.ToLocalTime().ToString("MMM dd HH:mm");
            }
            else
            {
                battleStatusText.text = "Battle active";
                battleEndTimeText.text =
                    "Battle ends at: " + clubBattle.EndTime.ToLocalTime().ToString("MMM dd HH:mm");
            }

            // Update club1 information
            StartCoroutine(DataLoader.GetClub(clubBattle.club1Id, club =>
            {
                if (club == null)
                {
                    Debug.LogError("Failed to get club 1");
                    return;
                }

                name1Text.text = club.name;
                id1Text.text = club.id;
                score1Text.text = "Overall score: " + score1;
                description1Text.text = club.description;
            }));
            // Update club2 information
            StartCoroutine(DataLoader.GetClub(clubBattle.club2Id, club =>
            {
                if (club == null)
                {
                    Debug.LogError("Failed to get club 2");
                    return;
                }

                name2Text.text = club.name;
                id2Text.text = club.id;
                score2Text.text = "Overall score: " + score2;
                description2Text.text = club.description;
            }));


            StartCoroutine(DataLoader.GetUserList(clubBattle.participantIdsClub1, participants =>
            {
                if (participants == null)
                {
                    Debug.LogError("Failed to get club members of club 1");
                    return;
                }

                UpdateParticipantList1(participants, clubBattle.club1Scores);
            }));
            StartCoroutine(DataLoader.GetUserList(clubBattle.participantIdsClub2, participants =>
            {
                if (participants == null)
                {
                    Debug.LogError("Failed to get club members of club 2");
                    return;
                }

                UpdateParticipantList2(participants, clubBattle.club2Scores);
            }));
        }

        private void UpdateParticipantList1(List<User> participants, List<ScoreEntry> club1Scores)
        {
            // Clear previously displayed participants
            foreach (Transform child in participants1Container)
            {
                Destroy(child.gameObject);
            }

            // Display participants
            foreach (var participant in participants)
            {
                var participantObj = Instantiate(participantPrefab, participants1Container);
                var scores = club1Scores.First(entry => entry.userId == participant.id).score;

                participantObj.GetComponent<BattleMember>().SetParticipantInfo(participant, scores.ToArray());
            }
        }

        private void UpdateParticipantList2(List<User> participants, List<ScoreEntry> club2Scores)
        {
            // Clear previously displayed participants
            foreach (Transform child in participants2Container)
            {
                Destroy(child.gameObject);
            }

            // Display participants
            foreach (var participant in participants)
            {
                var participantObj = Instantiate(participantPrefab, participants2Container);
                var scores = club2Scores.First(entry => entry.userId == participant.id).score;
                participantObj.GetComponent<BattleMember>().SetParticipantInfo(participant, scores.ToArray());
            }
        }

        private void SetPlayCourseButtonsInteractability(bool interactable)
        {
            foreach (var button in playCourseButtons)
            {
                button.interactable = interactable;
            }
        }

        // Buttons
        public void
            PlayCourse(int courseIdx) // courseIdx here 1-golfCoursesPerBattle instead of 0-golfCoursesPerBattle-1
        {
            // Load the course scene
            StartCoroutine(DataLoader.GetCourse(_clubBattle.courseIds[courseIdx - 1], course =>
            {
                if (course == null)
                {
                    Debug.LogError("ClubBattleMenu: Failed to get course");
                    return;
                }

                SessionManager.Instance.currentCourse = course;
                SessionManager.Instance.currentClubBattleCourseIndex = courseIdx - 1;
                SceneManager.LoadScene("ClubBattleScene");
            }));
        }

        public void BackToClubOverviewMenu()
        {
            SceneManager.LoadScene("ClubOverviewMenu");
        }
    }
}