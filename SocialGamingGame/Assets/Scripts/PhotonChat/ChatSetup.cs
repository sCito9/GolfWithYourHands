using System;
using System.Collections;
using System.Collections.Generic;
using DataBackend;
using ExitGames.Client.Photon;
using Photon.Chat;
using QFSW.QC;
using UnityEngine;

public class ChatSetup : MonoBehaviour, IChatClientListener
{
    public const String AppID = "b3d4e3b6-05f3-4dac-90eb-f8d93113a911";
    
    public static ChatSetup Instance {private set; get;}
    
    private ChatClient _chatClient;
    private string _userName;
    private string _clubId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _userName = SessionManager.Instance.User.name;
        _clubId = SessionManager.Instance.Club.id;
        _chatClient = new ChatClient(this);
        _chatClient.Connect(AppID, "1.0", new AuthenticationValues(_clubId));
        QuantumConsole.Instance.LogToConsoleAsync("Write c [Your message] to chat with your club members");
    }

    private void Update()
    {
        _chatClient?.Service();
    }

    public void LeaveClubMenu()
    {
        _chatClient.Disconnect();
    }


    public void DebugReturn(DebugLevel level, string message)
    {
        switch (level)
        {
            case DebugLevel.ERROR:
                UnityEngine.Debug.LogError("[PhotonChat] " + message);
                break;
            case DebugLevel.WARNING:
                UnityEngine.Debug.LogWarning("[PhotonChat] " + message);
                break;
            case DebugLevel.INFO:
                UnityEngine.Debug.Log("[PhotonChat] " + message);
                break;
        }
    }

    public void OnDisconnected()
    {
        QuantumConsole.Instance.LogToConsoleAsync("[PhotonChat] Disconnected");
    }

    public void OnConnected()
    {
        //might need to log for yourself
        _chatClient.Subscribe(_clubId,0, 20);
        _chatClient.Subscribe(new string[] { $"{_clubId}notification" }, 0);
    }

    public void OnChatStateChange(ChatState state)
    { }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            QuantumConsole.Instance.LogToConsoleAsync($"{messages[i]}");
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            if (results[i] && channels[i] == $"{_clubId}notification")
            {
                _chatClient.PublishMessage(channels[i], $"{_userName} is now online.");
            }
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
    }

    public void OnUserSubscribed(string channel, string user)
    {
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        
    }
    
    [Command("c")]
    public void SendChatMessage(params string[] parameters)
    {
        string message = string.Join(" ", parameters);
        if (!string.IsNullOrEmpty(message))
        {
            _chatClient.PublishMessage(_clubId, $"{SessionManager.Instance.User.name}: {message}");
        }
    }

    public void Cleanup()
    {
        _chatClient.PublishMessage($"{_clubId}notification", $"{_userName} is now offline.");
        Destroy(QuantumConsole.Instance.gameObject);
        StartCoroutine(DelayDC());
    }

    private IEnumerator DelayDC()
    {
        yield return new WaitForSeconds(0.5f);
        _chatClient.Disconnect();
    }

    void OnApplicationQuit()
    {
        _chatClient.PublishMessage($"{_clubId}notification", $"{_userName} is now offline.");
        _chatClient.Disconnect();
    }
}
