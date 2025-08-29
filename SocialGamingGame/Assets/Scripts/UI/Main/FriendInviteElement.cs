using DataBackend;
using DataBackend.Models;
using QFSW.QC;
using TMPro;
using UnityEngine;

namespace UI.Main
{
    public class FriendInviteElement : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
    
        private Invite _invite;
    
        
        /// <summary>
        ///     Set the invite that should be sent when confirming.
        /// </summary>
        /// <param name="invite">All details needed for the invite</param>
        public void SetInvite(Invite invite)
        {
            _invite = invite;
        }
    
        
        /// <summary>
        ///     Set the display name for the player.
        /// </summary>
        /// <param name="playerName">Display name</param>
        public void SetName(string playerName)
        {
            nameLabel.text = playerName;
        }


        /// <summary>
        ///     Send the invite. Needs to be set with <c>SetInvite</c> first.
        /// </summary>
        public void OnSendInvite()
        {
            QuantumConsole.Instance.LogToConsoleAsync($"Send invite to {nameLabel.text}");
            StartCoroutine(DataLoader.SendInvite(_invite, result => { }));
        }
    }
}
