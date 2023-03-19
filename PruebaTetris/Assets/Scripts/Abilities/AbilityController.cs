using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbilityController : NetworkBehaviour
{
    private ICharacterAbility myAbility;
    public PieceController enemyPieceController { get; private set; }
    
    public event Action<float> OnAbilityPointsChanged;
    private float _abilityPoints;
    private NetworkVariable<float> _networkAbiliityPoints = new(writePerm: NetworkVariableWritePermission.Owner);
    public float maxAbilityPoints { get; private set; } = 10;

    private void Awake()
    {
        maxAbilityPoints = 10;
    }

    private void Start()
    {
        var grids = GameObject.FindGameObjectsWithTag("grid");
        foreach (var grid in grids)
        {
            if (grid.Equals(gameObject)) continue;
            enemyPieceController = grid.GetComponent<PieceController>();
        }
        if(NetworkManager != null) _networkAbiliityPoints.OnValueChanged = (value, newValue) => { OnAbilityPointsChanged?.Invoke(newValue); };
    }

    public void SetAbility(int characterAbilityAbilityId)
    {
        switch (characterAbilityAbilityId)
        {
            case 0: myAbility = new ReaguetonAbility();
                break;
            case 1: myAbility = new RapAbility();
                break;
            case 2: myAbility = new MetalAbility();
                break;
            case 3: myAbility = new KpopAbility();
                break;
        }
    }

    public void AddAbilityPoints(float value)
    {
        if(NetworkManager.Singleton != null && !IsOwner) return;            
        _abilityPoints += value;
        if (_abilityPoints > maxAbilityPoints) _abilityPoints = maxAbilityPoints;
        else if (_abilityPoints < 0) _abilityPoints = 0;

        if(IsOwner) _networkAbiliityPoints.Value = _abilityPoints;
        else OnAbilityPointsChanged?.Invoke(_abilityPoints);
    }
    public void UseAbility()
    {
        if(NetworkManager.Singleton != null && !IsOwner) return;
        if(_abilityPoints < maxAbilityPoints) return;
        
        AddAbilityPoints(-_abilityPoints);
        myAbility.UseAbility(GetComponent<PieceController>(), enemyPieceController);
    }
}
