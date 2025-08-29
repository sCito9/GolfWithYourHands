using DataBackend;
using UnityEngine;

namespace UI.Main
{
    public class FriendRequests : MonoBehaviour
    {
        [SerializeField] private Transform location;
        [SerializeField] private GameObject friendRequestPrefab;

        private void OnEnable()
        {
            UpdateFriendRequests();
            SessionManager.Instance.OnRefreshUser += UpdateFriendRequests;
        }

        private void OnDisable()
        {
            SessionManager.Instance.OnRefreshUser -= UpdateFriendRequests;
        }
        
        private void UpdateFriendRequests()
        {
            // Delete all children of location
            foreach (Transform child in location)
                Destroy(child.gameObject);
            
            
            var friendRequests = SessionManager.Instance.User.pendingFriendRequestIds;
            StartCoroutine(DataLoader.GetUserList(friendRequests, users =>
            {
                if (users == null) return;
                
                foreach (var user in users)
                {
                    var obj = Instantiate(friendRequestPrefab, location);
                    obj.GetComponent<FriendScript>().SetUidName(user.id, user.name);
                }
            }));
        }
    }
}
