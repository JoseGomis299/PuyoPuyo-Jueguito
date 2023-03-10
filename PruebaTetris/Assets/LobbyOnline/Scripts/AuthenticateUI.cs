using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticateUI : MonoBehaviour {


    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button quickDrawButton;
    
    public static event Action onQuickJoin;
    private bool quickJoin;


    private void Awake()
    {
        LobbyListUI.onLeaveLobbyList += () => gameObject.SetActive(true);
        
        lobbyButton.onClick.AddListener(() =>
        {
            quickJoin = false;
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
            Hide();
        });
        quickDrawButton.onClick.AddListener(() =>
        {
            quickJoin = true;
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
        });
    }

    private void Start()
    {
        LobbyManager.Instance.onAuthenticationComplete += StartGame;
    }

    private void StartGame()
    {
        if(!quickJoin) return;
        LobbyManager.Instance.QuickJoinLobby();
        onQuickJoin?.Invoke();
        Hide();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

}