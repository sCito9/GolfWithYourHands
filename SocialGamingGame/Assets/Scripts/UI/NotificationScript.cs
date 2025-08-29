using DataBackend;
using DataBackend.Models;
using DedicatedServer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NotificationScript : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text inviteText;
    private ClientBootstrap clientBootstrap;
    public Invite invite;
    
    private void Awake()
    {
        clientBootstrap = FindAnyObjectByType<ClientBootstrap>();
    }

    public void SetInvite(Invite invite)
    {
        this.invite = invite;
        inviteText.text = $"{invite.hostName} invited you to play\n{invite.courseName}";
    }
    
    public async void AcceptInvite()
    {
        StartCoroutine(DataLoader.DeleteInvite(invite.hostId, invite.receiverId, result => { }));
        
        var go = Instantiate(loadingScreen, SceneManager.GetActiveScene());
        
        if (!await clientBootstrap.JoinLobbyWithId(invite.lobbyId))
        {
            inviteText.text = $"Game is no longer available";
            Destroy(go, 3);
        }
    }
    
    public void DeclineInvite()
    {
        StartCoroutine(DataLoader.DeleteInvite(invite.hostId, invite.receiverId, result =>
        {
            // Ignore result
            Destroy(gameObject);
        }));
        
    }
    
}
