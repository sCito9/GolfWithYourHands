using System.Collections.Generic;
using CesiumForUnity;
using Sensoren;
using TMPro;
using UnityEngine;

public class References : MonoBehaviour
{
    public CesiumGeoreference georeference;
    [Header("Client Stuff")]

    [Header("Endscreen Stuff")]
    public GameObject finishScreeen;
    public List<TMP_Text> playerFields;
    public List<TMP_Text> hitFields;
    public List<GameObject> invitePlayerButtons;
    public List<GameObject> clubInviteButtons;
    public GameObject podium;
    public GolfHoleSensor golfHoleSensor;
    public GameObject golfHole;
    public ArrowNavigatorScript navigationArrow;
}
