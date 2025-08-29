using System;
using System.Collections;
using DataBackend;
using TMPro;
using UnityEngine;

namespace UI.Main
{
    public class FriendListScript : MonoBehaviour
    {
        public GameObject friendPrefab;
        [SerializeField] private Transform location;
        [SerializeField] private TMP_InputField input;
        
        private Coroutine _pollFriendsRoutine;
        
        private void Start()
        {
            UpdateFriendList();
            input.onEndEdit.AddListener(EnterFriendUid);
        }

        private static IEnumerator PollFriendsRegularly()
        {
            while (true)
            {
                SessionManager.Instance.RefreshUser();
                yield return new WaitForSeconds(5);
            }
        }

        private void OnEnable()
        {
            SessionManager.Instance.OnRefreshUser += UpdateFriendList;
            _pollFriendsRoutine = StartCoroutine(PollFriendsRegularly());
        }

        private void OnDisable()
        {
            SessionManager.Instance.OnRefreshUser -= UpdateFriendList;
            StopCoroutine(_pollFriendsRoutine);
        }


        private void UpdateFriendList()
        {
            var friendIds = SessionManager.Instance.User.friendIds;

            StartCoroutine(DataLoader.GetUserList(friendIds, users =>
                {
                    // Delete all children of location
                    foreach (Transform child in location)
                    {
                        Destroy(child.gameObject);
                    }
                    
                    if (users == null) return;
                    
                    foreach (var user in users)
                    {
                        var obj = Instantiate(friendPrefab, location);
                        obj.GetComponent<FriendScript>().SetUidName(user.id, user.name);
                    }
                })
            );
        }
        
        private void AddFriend(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;
            
            StartCoroutine(DataLoader.SendFriendRequest(SessionManager.Instance.User.id, uid.ToLower(), success =>
                {
                if (!success)
                    Debug.LogError("Failed to send friend request");
                else
                {
                    Debug.Log("Friend request sent");
                    SessionManager.Instance.RefreshUser();
                }
                }
            ));
        }
        
        public void EnterFriendUid(string text)
        {
            if (input.text.Equals(SessionManager.Instance.User.id))
            {
                Debug.LogWarning("Can't send a friend request to yourself");
                return;
            }
            AddFriend(text);
            input.text = string.Empty;
        }
    }
}
