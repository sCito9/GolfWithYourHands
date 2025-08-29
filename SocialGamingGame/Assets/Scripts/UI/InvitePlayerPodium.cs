using System;
using Networking;
using UnityEngine;

namespace UI
{
    public class InvitePlayerPodium : MonoBehaviour
    {
        public async void InvitePlayer(int place)
        {
            try
            {
                await TurnManager.Instance.InvitePlayer_onPlace(place);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to invite player");
            }
        }

        public void InvitePlayerToClub(int place)
        {
            TurnManager.Instance.InvitePlayerToClub(place);
        }
    }
} 