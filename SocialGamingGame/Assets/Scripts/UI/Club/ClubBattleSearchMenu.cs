using System.Collections;
using UnityEngine;
using DataBackend;
using DataBackend.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI.Club
{
    public class ClubBattleSearchMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text idText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject activeSearchPanel;
        [SerializeField] private GameObject inactiveSearchPanel;
        [SerializeField] private Transform notSelectedContainer;
        [SerializeField] private Transform selectedContainer;
        [SerializeField] private GameObject notSelectedPrefab;
        [SerializeField] private GameObject selectedPrefab;
        [SerializeField] private Button findOpponentButton;
        [SerializeField] private Button cancelSearchButton;
        private List<string> currentlySelectedMemberIds = new List<string>();

        // Fixed values
        private List<int> possibleNumParticicpants = new List<int> {2, 5, 10, 15, 20, 25, 30};

        // Refreshh stuff
        private Coroutine _pollClubBattleStatusCoroutine;
        

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
            var user = SessionManager.Instance.User;
            var club = SessionManager.Instance.Club;
            var clubBattleStatus = SessionManager.Instance.ClubBattleStatus;

            nameText.text = club.name;
            idText.text = club.id;
            descriptionText.text = club.description;

            // Clear previously displayed selected and not selected members
            foreach (Transform child in selectedContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in notSelectedContainer)
            {
                Destroy(child.gameObject);
            }

            // Inactive search
            // Give possibility to move members between selected and not selected
            // if leader then show also findOpponentButton
            if (clubBattleStatus.status == "idle")
            {
                activeSearchPanel.SetActive(false);
                inactiveSearchPanel.SetActive(true);
                // Display members
                StartCoroutine(DataLoader.GetClubMembers(club.id, RefreshMemberList));
                // Display buttons
                findOpponentButton.gameObject.SetActive(user.id == club.leaderId);
            }
            // Active search
            // if leader then show cancelSearchbutton
            else if (clubBattleStatus.status == "searching")
            {
                activeSearchPanel.SetActive(true);
                inactiveSearchPanel.SetActive(false);
                cancelSearchButton.gameObject.SetActive(user.id == club.leaderId);
            }
            else
            {
                SceneManager.LoadScene("ClubBattleMenu");
            }
        }

        private void RefreshMemberList(List<User> members)
        {
            // Display all members as not selected
            foreach (var member in members)
            {
                if (currentlySelectedMemberIds.Contains(member.id))
                {
                    var memberObj = Instantiate(selectedPrefab, selectedContainer);
                    memberObj.GetComponent<ClubMember>().SetMemberInfoAndReference(member, this);
                }
                else
                {
                    var memberObj = Instantiate(notSelectedPrefab, notSelectedContainer);
                    memberObj.GetComponent<ClubMember>().SetMemberInfoAndReference(member, this);
                }
            }
        }

        public void OnMemberSelected(ClubMember clubMemberScript)
        {
            User member = clubMemberScript.GetMember();
            currentlySelectedMemberIds.Add(member.id);
            Destroy(clubMemberScript.gameObject);
            var memberObj = Instantiate(selectedPrefab, selectedContainer);
            memberObj.GetComponent<ClubMember>().SetMemberInfoAndReference(member, this);
        }

        public void OnMemberDeselected(ClubMember clubMemberScript)
        {
            User member = clubMemberScript.GetMember();
            currentlySelectedMemberIds.Remove(member.id);
            Destroy(clubMemberScript.gameObject);
            var memberObj = Instantiate(notSelectedPrefab, notSelectedContainer);
            memberObj.GetComponent<ClubMember>().SetMemberInfoAndReference(member, this);
        }


        // Buttons
        public void CreateClubBattleSearch()
        {
            int numSelected = currentlySelectedMemberIds.Count;
            if (!possibleNumParticicpants.Contains(numSelected))
            {
                Debug.Log("Invalid number of participants selected for club battle.");
                return;
            }
            ClubBattleSearch clubBattleSearch = new ClubBattleSearch(SessionManager.Instance.Club.id, numSelected, currentlySelectedMemberIds);
            StartCoroutine(DataLoader.CreateClubBattleSearch(clubBattleSearch, createdClubBattleSearch =>
            {
                if (createdClubBattleSearch == null)
                {
                    Debug.Log("Failed to create club battle search.");
                    return;
                }
                SessionManager.Instance.RefreshClubBattleStatus();
            }));
        }
        public void DeleteClubBattleSearch()
        {
            StartCoroutine(DataLoader.DeleteClubBattleSearch(SessionManager.Instance.ClubBattleStatus.searchId, success =>
            {
                if (!success)
                {
                    Debug.Log("Failed to remove club battle search.");
                    return;
                }
                Debug.Log("Club battle search removed successfully.");
                SessionManager.Instance.RefreshClubBattleStatus();
            }));
        }
        public void BackToClubOverviewMenu()
        {
            SceneManager.LoadScene("ClubOverviewMenu");
        }
    }
}