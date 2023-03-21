using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    public bool paused { get; private set; }

    private void Awake()
    {
        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            if(NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Menu");
        });

        if (NetworkManager.Singleton == null)
        {
            restartButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        gameObject.SetActive(false);
    }

    public void Resume()
    {
        Time.timeScale = 1;
        gameObject.SetActive(false);
        paused = false;
    }
    private void OnEnable()
    {
        resumeButton.Select();
        paused = true;
    }
}
