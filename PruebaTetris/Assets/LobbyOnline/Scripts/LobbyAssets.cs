using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAssets : MonoBehaviour {
    public static LobbyAssets Instance { get; private set; }

    [SerializeField] private Sprite KpopSprite;
    [SerializeField] private Sprite MetalSprite;
    [SerializeField] private Sprite RapSprite;
    [SerializeField] private Sprite ReggaetonSprite;


    private void Awake() {
        Instance = this;
    }

    public Sprite GetSprite(LobbyManager.PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case LobbyManager.PlayerCharacter.Kpop:   return KpopSprite;
            case LobbyManager.PlayerCharacter.Metal:    return MetalSprite;
            case LobbyManager.PlayerCharacter.Rap:   return RapSprite;
            case LobbyManager.PlayerCharacter.Reggaeton:   return ReggaetonSprite;
        }
    }

}