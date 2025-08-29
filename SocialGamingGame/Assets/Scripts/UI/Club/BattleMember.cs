using DataBackend.Models;
using TMPro;
using UnityEngine;

namespace UI.Club
{
    public class BattleMember : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text participantNameText;
        [SerializeField]
        private TMP_Text participantScoreText;

        public void SetParticipantInfo(User participant, int[] scores)
        {
            participantNameText.text = participant.name;
            participantScoreText.text = string.Join(", ", scores);
        }
    }
}
