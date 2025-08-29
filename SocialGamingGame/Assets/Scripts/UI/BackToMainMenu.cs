using Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class BackToMainMenu : MonoBehaviour
    {
        public void LoadMainMenu()
        {
            TurnManager.Instance.QuitGame();
        }
    }
}
