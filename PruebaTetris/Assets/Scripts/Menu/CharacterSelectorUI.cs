using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectorUI : MonoBehaviour {
    
    [SerializeField] private Button changeKpopButton;
    [SerializeField] private Button changeMetalButton;
    [SerializeField] private Button changeRapButton;
    [SerializeField] private Button changeReggaetonButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button PlayGameButton;
    
    private LobbyCharacterSO[] playerCharacters = new LobbyCharacterSO[2];
    [SerializeField] private CharacterUI[] characters;
    private int index;

    private void Awake()
    {
        SetPlayButton(false);
        
        changeKpopButton.onClick.AddListener(() => {
            UpdatePlayerCharacter(LobbyAssets.Instance.GetSO(LobbyManager.PlayerCharacter.Kpop));
        });
        changeMetalButton.onClick.AddListener(() => {
           UpdatePlayerCharacter(LobbyAssets.Instance.GetSO(LobbyManager.PlayerCharacter.Metal));
        });
        changeRapButton.onClick.AddListener(() => {
            UpdatePlayerCharacter(LobbyAssets.Instance.GetSO(LobbyManager.PlayerCharacter.Rap));
        });
        changeReggaetonButton.onClick.AddListener(() => {
            UpdatePlayerCharacter(LobbyAssets.Instance.GetSO(LobbyManager.PlayerCharacter.Reggaeton));
        });

        leaveLobbyButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Menu");
        });
        PlayGameButton.onClick.AddListener(() => {
           SetPlayButton(false);
           GeneratePlayersData();
           SceneManager.LoadScene("1v1");
        });

    }
    private void SetPlayButton(bool value)
    {
        PlayGameButton.interactable = value;
    }
    
    private void UpdatePlayerCharacter(LobbyCharacterSO characterSo)
    {
        if(!characters[index].gameObject.activeInHierarchy) characters[index].gameObject.SetActive(true);
        playerCharacters[index] = characterSo;
        characters[index].UpdatePlayer(characterSo);
        index = (index + 1) % playerCharacters.Length;

        foreach (var player in playerCharacters)
        {
            if(player == null) return;
        }
        SetPlayButton(true);
    }

    private void GeneratePlayersData()
    {
        for (int i = 0; i<playerCharacters.Length; i++)
        {
            CharacterData characterData = new CharacterData(playerCharacters[i].characterBody, playerCharacters[i].id, playerCharacters[i].characterProfile, "Player "+(i+1));
            string json = JsonUtility.ToJson(characterData, true);
            File.WriteAllText(Application.persistentDataPath + "/PlayerDataFile"+i+".json", json);
        }
    }
}