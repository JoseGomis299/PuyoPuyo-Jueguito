using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditPlayerName : MonoBehaviour {
    public static EditPlayerName Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI playerNameText;

    private string playerName = "Player";
    
    private void Awake() {
        Instance = this;
        playerNameText.text = playerName;
    }

    public void ChangeName(string name)
    {
        playerName = name;
        LobbyManager.Instance.UpdatePlayerName(GetPlayerName());
    }

    public string GetPlayerName() {
        return playerName;
    }


}