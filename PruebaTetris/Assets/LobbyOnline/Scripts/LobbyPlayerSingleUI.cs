using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button kickPlayerButton;
    private LobbyCharacterSO _characterSo;


    private Player player;


    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible) {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Player player) {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
        LobbyManager.PlayerCharacter playerCharacter = 
            System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
        _characterSo = LobbyAssets.Instance.GetSO(playerCharacter);
        characterImage.sprite = _characterSo.characterBody;
        characterNameText.text = _characterSo.name;
        abilityNameText.text = _characterSo.abilityName;
        abilityDescriptionText.text = _characterSo.abilityDescription;
    }

    private void KickPlayer() {
        if (player != null) {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }
}