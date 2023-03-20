using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText;
    [SerializeField] private Image characterImage;

    public void UpdatePlayer(LobbyCharacterSO characterSo) {
        characterImage.sprite = characterSo.characterBody;
        characterNameText.text = characterSo.name;
        abilityNameText.text = characterSo.abilityName;
        abilityDescriptionText.text = characterSo.abilityDescription;
    }
}
