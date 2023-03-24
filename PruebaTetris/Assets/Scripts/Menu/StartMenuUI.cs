using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    public static StartMenuUI Instance;
    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            SelectMenuUI.Instance.gameObject.SetActive(true);
        });
        quitButton.onClick.AddListener(Application.Quit);
        
    }

    private void Start()
    {
        AudioManager.Instance.ChangeMusic(AudioManager.Instance.backgroundMusics[0]);
    }
}
