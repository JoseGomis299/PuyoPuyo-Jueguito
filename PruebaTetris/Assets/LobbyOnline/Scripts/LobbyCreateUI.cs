using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour {


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button publicPrivateButton;
    [SerializeField] private TextMeshProUGUI publicPrivateText;
    [SerializeField] private TMP_InputField lobbyNameText;


    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers;

    private void Awake() {
        Instance = this;

        quitButton.onClick.AddListener(()=>Hide());
        
        createButton.onClick.AddListener(() => {
            LobbyManager.Instance.CreateLobby(
                lobbyName,
                maxPlayers,
                isPrivate
            );
            Hide();
        });

        publicPrivateButton.onClick.AddListener(() => {
            isPrivate = !isPrivate;
            UpdateText();
        });
        Hide();
    }

    private void Start()
    {
        AuthenticateUI.onQuickJoin += Hide;
    }

    public void ChangeLobbyName(string name)
    {
        lobbyName = name;
    }

    private void UpdateText() {
        publicPrivateText.text = isPrivate ? "Private" : "Public";
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);

        lobbyName = LobbyManager.Instance.playerName+"'s Game";
        lobbyNameText.text = lobbyName;
        isPrivate = false;
        maxPlayers = 2;

        UpdateText();
    }

}