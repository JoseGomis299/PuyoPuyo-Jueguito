using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAssets : MonoBehaviour {
    public static LobbyAssets Instance { get; private set; }

    [SerializeField] private LobbyCharacterSO KpopSo;
    [SerializeField] private LobbyCharacterSO MetalSo;
    [SerializeField] private LobbyCharacterSO RapSo;
    [SerializeField] private LobbyCharacterSO ReggaetonSo;


    private void Awake() {
        Instance = this;
    }

    public LobbyCharacterSO GetSO(LobbyManager.PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case LobbyManager.PlayerCharacter.Kpop:   return KpopSo;
            case LobbyManager.PlayerCharacter.Metal:    return MetalSo;
            case LobbyManager.PlayerCharacter.Rap:   return RapSo;
            case LobbyManager.PlayerCharacter.Reggaeton:   return ReggaetonSo;
        }
    }

}