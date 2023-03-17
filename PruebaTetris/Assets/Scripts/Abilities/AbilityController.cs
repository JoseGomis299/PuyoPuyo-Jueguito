using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbilityController : NetworkBehaviour
{
    private ICharacterAbility myAbility;
    public PieceController enemyPieceController { get; private set; }

    private void Start()
    {
        {
            var grids = GameObject.FindGameObjectsWithTag("grid");
            foreach (var grid in grids)
            {
                if (grid.Equals(gameObject)) continue;
                enemyPieceController = grid.GetComponent<PieceController>();
            }
        }
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

    public void UseAbility()
    {
        if(NetworkManager.Singleton != null && !IsOwner) return;
        
        myAbility.UseAbility(GetComponent<PieceController>(), enemyPieceController);
    }
}
