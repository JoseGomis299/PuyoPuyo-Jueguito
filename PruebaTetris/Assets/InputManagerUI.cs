using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private void Awake()
    {
        var inputManager = GetComponent<PlayerInputManager>();
        inputManager.playerPrefab = playerPrefab;
        PlayerInput player1 = inputManager.JoinPlayer(1);
        PlayerInput player2 = inputManager.JoinPlayer(2);
        player1.SwitchCurrentActionMap("UI1");
        player2.SwitchCurrentActionMap("UI2");
    }
}
