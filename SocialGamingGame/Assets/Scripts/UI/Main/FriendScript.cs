using System;
using DataBackend;
using DedicatedServer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Main
{
    public class FriendScript : MonoBehaviour
    {
        private string _uid;
        [SerializeField] private TMP_Text text;
        
        public void SetUidName(string uid, string playerName)
        {
            _uid = uid;
            text.text = playerName;
        }

        public void AddToParty()
        {
            ClientBootstrap clientBootstrap = ClientBootstrap.Instance;
            //Send clientBootstrap.GetLobbyId(); to friend and do something with it there
        }

        public void RemoveFriend()
        {
            StartCoroutine(DataLoader.RemoveFriend(SessionManager.Instance.User.id, _uid, success =>
            {
                if (!success)
                    Debug.LogError("Failed to remove friend"); // TODO: Display Error
                else
                {
                    Debug.Log("Friend removed");
                    SessionManager.Instance.RefreshUser();
                }
            }));
        }

        public void AcceptFriendRequest()
        {
            StartCoroutine(DataLoader.AcceptFriendRequest(SessionManager.Instance.User.id, _uid, success =>
            {
                if (!success)
                    Debug.LogError("Failed to accept friend request"); // TODO: Display Error
                else
                {
                    Debug.Log("Friend request accepted");
                    SessionManager.Instance.RefreshUser();
                }
            }));
        }
        
        public void DeclineFriendRequest()
        {
            StartCoroutine(DataLoader.DeclineFriendRequest(SessionManager.Instance.User.id, _uid, success =>
            {
                if (!success)
                    Debug.LogError("Failed to decline friend request"); // TODO: Display Error
                else
                {
                    Debug.Log("Friend request declined");
                    SessionManager.Instance.RefreshUser();
                }
            }));
        }
    }
}
