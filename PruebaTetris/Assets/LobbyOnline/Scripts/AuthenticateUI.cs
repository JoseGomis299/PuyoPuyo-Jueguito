using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthenticateUI : MonoBehaviour {


    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button quickDrawButton;
    [SerializeField] private Button quitButton;
    
    public static event Action onQuickJoin;
    private bool quickJoin;


    private void Awake()
    {
        quitButton.onClick.AddListener(()=>
        {
            Destroy(NetworkManager.Singleton.gameObject);
            SceneManager.LoadScene("Menu");
        });
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
        LobbyListUI.Instance.onLeaveLobbyList += () => gameObject.SetActive(true);
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