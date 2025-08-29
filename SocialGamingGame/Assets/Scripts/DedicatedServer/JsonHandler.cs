using System;
using System.IO;
using UI;
using UnityEngine;

namespace DedicatedServer
{
    public class JsonHandler : MonoBehaviour
    {
        public static void WritePlayerSettings(PlayerSettings playerSettings)
        {
            try
            {
                String jsonString = JsonUtility.ToJson(playerSettings);
                File.WriteAllText(Application.persistentDataPath + "PlayerSettings.json", jsonString);
            }
            catch (Exception e)
            {
                Debug.Log("Error while creating player settings: " + e.Message + "");
            }
        }

        public static PlayerSettings ReadPlayerSettings()
        {
            try
            {
                string jsonString = File.ReadAllText(Application.persistentDataPath + "PlayerSettings.json");
                return JsonUtility.FromJson<PlayerSettings>(jsonString);
            }
            catch (Exception e)
            {
                Debug.Log("Player Settings might not exist yet");
                return null;
            }
        }
    }
}
