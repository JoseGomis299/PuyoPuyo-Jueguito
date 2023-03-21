using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour
{
    private Slider _healthSlider;
    private Slider _abilitySlider;
    private TMP_Text _playerName;
    private Image _playerProfile;
    private Image _playerBody;

    private string _playerNameValue;
    private Sprite _playerProfileValue;
    private Sprite _playerBodyValue;
    public void SetReferences(Slider health, Slider ability, TMP_Text name, Image profile, float maxHealth, float maxAbility)
    {
        _healthSlider = health;
        _abilitySlider = ability;
        _playerName = name;
        _playerProfile = profile;

        _abilitySlider.maxValue = maxAbility;
        _healthSlider.maxValue = maxHealth;
        _healthSlider.value = maxHealth;

        if (_playerProfileValue != null)
        {
            _playerName.text = _playerNameValue;
            _playerProfile.sprite = _playerProfileValue;
        }

        GetComponent<AbilityController>().OnAbilityPointsChanged += (newValue) => { _abilitySlider.value = newValue;};
        GetComponent<PieceController>().OnHealthChanged += (newValue) => { _healthSlider.value = newValue; };
    }

    public void SetValues(string name, Sprite profile, Sprite body)
    {
        if (_playerProfile == null)
        {
            _playerNameValue = name;
            _playerProfileValue = profile;
            _playerBodyValue = body;
        }
        else
        {
            _playerName.text = name;
            _playerProfile.sprite = profile;
        }
    }
}
