using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectMenuUI : MonoBehaviour
{
    [SerializeField] private Button localButton;
    [SerializeField] private Button onlineButton;
    [SerializeField] private Button quitButton;

    public static SelectMenuUI Instance;

    private void Awake()
    {
        Instance = this;
        quitButton.onClick.AddListener(()=>
        {
            gameObject.SetActive(false);
            StartMenuUI.Instance.gameObject.SetActive(true);
        });
        localButton.onClick.AddListener(()=>SceneManager.LoadScene("PlayerSelectorOffline"));
        onlineButton.onClick.AddListener(()=>SceneManager.LoadScene("LobbyOnline"));
        gameObject.SetActive(false);

    }
}
