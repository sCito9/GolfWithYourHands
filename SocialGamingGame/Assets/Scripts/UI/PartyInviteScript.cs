using System.Collections;
using DataBackend;
using DataBackend.Models;
using DedicatedServer;
using UI.Main;
using UnityEngine;

namespace UI
{
    public class PartyInviteScript : MonoBehaviour
    {
        [SerializeField] private Transform location;
        [SerializeField] private GameObject friendInvitePrefab;

        private ClientBootstrap _clientBootstrap;
        private string _lobbyId;
        private string _courseId;

        private void Start()
        {
            _clientBootstrap = ClientBootstrap.Instance;
     
            StartCoroutine(UpdateOnlineFriends());

            if(ClientBootstrap.course != null)
                _courseId = ClientBootstrap.course.name;
        }

        private void OnEnable()
        {
            SessionManager.Instance.OnRefresh += UpdateList;
        }
    
        private void OnDisable()
        {
            SessionManager.Instance.OnRefresh -= UpdateList;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator UpdateOnlineFriends()
        {
            while (true)
            {
                yield return new WaitForSeconds(3);
                _lobbyId = _clientBootstrap.GetLobbyId();
                SessionManager.Instance.Refresh();
                yield return new WaitForSeconds(17);
            }
        }

        private void UpdateList()
        {
            // Cleanup list
            foreach (Transform child in location)
                Destroy(child.gameObject);

            // Update list
            StartCoroutine(DataLoader.GetUserList(SessionManager.Instance.User.friendIds, users =>
            {
                foreach (var user in users)
                {
                    var obj = Instantiate(friendInvitePrefab, location);
                    obj.GetComponent<FriendInviteElement>().SetName(user.name);
                    obj.GetComponent<FriendInviteElement>().SetInvite(new Invite(
                        SessionManager.Instance.User.id,
                        user.id,
                        _courseId,
                        SessionManager.Instance.User.name,
                        _lobbyId
                    ));
                }
            }));
        }
    }
}