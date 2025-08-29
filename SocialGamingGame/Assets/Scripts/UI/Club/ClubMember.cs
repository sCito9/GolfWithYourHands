using DataBackend.Models;
using TMPro;
using UnityEngine;
using DataBackend;
using UnityEngine.UI;

namespace UI.Club
{
    public class ClubMember : MonoBehaviour
    {
        private User member;
        [SerializeField]
        private TMP_Text memberNameText;
        private ClubBattleSearchMenu clubBattleSearchMenu;

        [SerializeField] private Button sendFriendRequestButton = null;
        [SerializeField] private Button removeMemberButton = null;
        [SerializeField] private Button selectMemberButton = null;
        [SerializeField] private Button deselectMemberButton = null;

        private void Awake()
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (var button in buttons)
            {
                SetButtons(button);
            }
        }

        private void SetButtons(Button button)
        {
            if (button.gameObject.name == "SendFriendRequestButton")
            {
                sendFriendRequestButton = button;
            }
            else if (button.gameObject.name == "RemoveMemberFromClubButton")
            {
                removeMemberButton = button;
            }
            else if (button.gameObject.name == "MoveToSelectedButton")
            {
                selectMemberButton = button;
            }
            else if (button.gameObject.name == "MoveToNotSelectedButton")
            {
                deselectMemberButton = button;
            }
        }

        public User GetMember()
        {
            return member;
        }

        public void SetMemberInfo(User member)
        {
            this.member = member;
            memberNameText.text = member.name;
            SetActiveStateButtons();
        }
        public void SetMemberInfoAndReference(User member, ClubBattleSearchMenu clubBattleSearchMenu)
        {
            this.member = member;
            memberNameText.text = member.name;

            this.clubBattleSearchMenu = clubBattleSearchMenu;
            SetActiveStateButtons();
        }

        private void SetActiveStateButtons()
        {
            User user = SessionManager.Instance.User;
            ClubBattleStatus clubBattleStatus = SessionManager.Instance.ClubBattleStatus;
            if (sendFriendRequestButton != null)
            {
                if (user.id == member.id || user.friendIds.Contains(member.id) || user.sentFriendRequestIds.Contains(member.id))
                {
                    sendFriendRequestButton.gameObject.SetActive(false);
                }
            }
            if (removeMemberButton != null)
            {
                if (SessionManager.Instance.Club.leaderId != user.id || member.id == user.id)
                {
                    removeMemberButton.gameObject.SetActive(false);
                }
            }
            if (selectMemberButton != null)
            {
                if (clubBattleStatus.status == "searching")
                {
                    selectMemberButton.gameObject.SetActive(false);
                }
            }
            if (deselectMemberButton != null)
            {
                if (clubBattleStatus.status == "searching")
                {
                    deselectMemberButton.gameObject.SetActive(false);
                }
            }
        }


        // Buttons
        public void SendFriendRequest()
        {
            StartCoroutine(DataLoader.SendFriendRequest(SessionManager.Instance.User.id, member.id, success =>
            {
                if (!success)
                    Debug.LogError("Failed to send friend request");
                else
                {
                    Debug.Log("Friend request sent");
                    sendFriendRequestButton.gameObject.SetActive(false);
                }
            }
            ));
        }
        
        public void RemoveMemberFromClub()
        {
            StartCoroutine(DataLoader.KickFromClub(SessionManager.Instance.Club.id, member.id, success =>
            {
                if (!success)
                {
                    Debug.Log("Failed to remove member from club.");
                    return;
                }
                Destroy(gameObject);
            }));
        }

        public void SelectMember()
        {
            if (clubBattleSearchMenu != null)
            {
                clubBattleSearchMenu.OnMemberSelected(this);
            }
        }

        public void DeselectMember()
        {
            if (clubBattleSearchMenu != null)
            {
                clubBattleSearchMenu.OnMemberDeselected(this);
            }
        }
    }
}
