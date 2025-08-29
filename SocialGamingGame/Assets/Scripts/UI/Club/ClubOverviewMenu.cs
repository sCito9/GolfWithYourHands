using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataBackend;
using DataBackend.Models;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Club
{
    public class ClubOverviewMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Transform memberContainer;
        [SerializeField] private Transform clubBattleButton;
        [SerializeField] private TMP_Text clubBattleButtonText;

        [SerializeField] private GameObject memberPrefab;

        private Coroutine _pollClubCoroutine;
        private List<string> _cachedMemberIds = new();
        
        private void Start()
        {
            SessionManager.Instance.RefreshClub();
        }

        private static IEnumerator PollClubRegularly()
        {
            while (true)
            {
                SessionManager.Instance.RefreshClub();
                SessionManager.Instance.RefreshClubBattleStatus();
                yield return new WaitForSeconds(5);
            }
        }
        
        public void OnEnable()
        {
            SessionManager.Instance.OnRefreshClub += RefreshUI;
            SessionManager.Instance.OnClubBattleStatusChanged += RefreshClubBattleButton;
            _pollClubCoroutine = StartCoroutine(PollClubRegularly());
        }

        public void OnDisable()
        {
            SessionManager.Instance.OnRefreshClub -= RefreshUI;
            SessionManager.Instance.OnClubBattleStatusChanged -= RefreshClubBattleButton;
            StopCoroutine(_pollClubCoroutine);
        }

        private void RefreshUI()
        {
            var club = SessionManager.Instance.Club;

            nameText.text = club.name;
            idText.text = club.id;
            descriptionText.text = club.description;

            // Check if the member list has no changes
            if (club.memberIds.Count == _cachedMemberIds.Count && _cachedMemberIds.All(club.memberIds.Contains))
                return;

            _cachedMemberIds = new List<string>(club.memberIds);
            // If the list has changed, update it
            StartCoroutine(DataLoader.GetClubMembers(club.id, RefreshMemberList));
        }

        private void RefreshMemberList(List<User> users)
        {
            foreach (Transform child in memberContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var user in users)
            {
                var memberObj = Instantiate(memberPrefab, memberContainer);
                memberObj.GetComponent<ClubMember>().SetMemberInfo(user);
            }
        }
        
        private void RefreshClubBattleButton()
        {
            if (SessionManager.Instance.ClubBattleStatus.status.Equals("in_battle"))
            {
                clubBattleButton.gameObject.SetActive(true);
                clubBattleButtonText.text = "Join Club Battle";
            }
            else if (SessionManager.Instance.Club.leaderId.Equals(SessionManager.Instance.User.id))
            {
                clubBattleButton.gameObject.SetActive(true);

                if (SessionManager.Instance.ClubBattleStatus.status.Equals("searching"))
                {
                    clubBattleButtonText.text = "Modify Club Battle Search";
                }
                else
                {
                    clubBattleButtonText.text = "Search for Club Battle Opponents";
                }
                
            }
            else
            {
                clubBattleButton.gameObject.SetActive(false);
            }
        }

        #region Button Functions

        public void OnBackButtonPressed()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OnLeaveClubButtonPressed()
        {
            StartCoroutine(DataLoader.LeaveClub(SessionManager.Instance.Club.id, success =>
            {
                if (!success)
                    Debug.Log("Failed to leave club.");
                else
                    SceneManager.LoadScene("MainMenu");
            }));
        }

        public void OnOpenClubBattleMenuButtonPressed()
        {
            if (SessionManager.Instance.ClubBattleStatus == null)
            {
                Debug.Log("[ClubOverviewMenu] ClubBattleStatus not set on SessionManager.");
                return;
            }

            switch (SessionManager.Instance.ClubBattleStatus.status)
            {
                case "searching":
                    SceneManager.LoadScene("ClubBattleSearchMenu");
                    break;
                case "in_battle":
                    SceneManager.LoadScene("ClubBattleMenu");
                    break;
                default:
                    SceneManager.LoadScene("ClubBattleSearchMenu");
                    break;
            }
        }

        #endregion
    }
}
