using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbilitiesNetwork : NetworkBehaviour
{
    public static AbilitiesNetwork Instance { get; private set; }
    private bool usingAbility;

    private void Awake()
    {
        Instance = this;
    }

    public void PerformAbility(NetworkObjectReference myPieceController, NetworkObjectReference enemyPieceController, int abilityId)
    {
        if(usingAbility) return;
        usingAbility = true;
        PerformAbilityServerRpc(myPieceController, enemyPieceController, abilityId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PerformAbilityServerRpc(NetworkObjectReference myPieceController,
        NetworkObjectReference enemyPieceController, int abilityId)
    {
        PerformAbilityClientRpc(myPieceController, enemyPieceController, abilityId);
    }

    [ClientRpc]
    private void PerformAbilityClientRpc(NetworkObjectReference mine, NetworkObjectReference enemy, int abilityId)
    {
        enemy.TryGet(out var enemyPieceController);
        mine.TryGet(out var myPieceController);
        GetAbility(abilityId)(myPieceController.GetComponent<PieceController>(), enemyPieceController.GetComponent<PieceController>());
    }

    private Action<PieceController, PieceController> GetAbility(int abilityId)
    {
        switch (abilityId)
        {
            case 0: return (mine, enemy) => {  
                if (enemy.IsOwner)
                {   
                    enemy.fallSpeed *= 0.1f;
                    enemy.fallSpeedDelta *= 0.1f;
                    Timer.Instance.WaitForAction(() =>
                    {
                        enemy.fallSpeed /= 0.1f;
                        enemy.fallSpeedDelta /= 0.1f;
                    }, 5f);
                }
                else
                {
                    Timer.Instance.WaitForAction(() =>
                    {
                        usingAbility = false;
                    }, 5f);
                }
            };
            case 1: return (mine, enemy) => { 
                if(enemy.IsOwner) return;
                
                int garbageCount = mine.RemoveGarbage();
                if(garbageCount == 0) return;
                
                mine.EnemyThrowGarbageServerRpc(garbageCount / 2, 0);
                usingAbility = false;
            };
            case 2: return (mine, enemy) =>
            {
                if(enemy.IsOwner) return;
                
                mine.EnemyThrowGarbageServerRpc(0, 12);
                usingAbility = false;
            };
            case 3: return (mine, enemy) =>
            {
                if(enemy.IsOwner) return;

                mine.AddHealth(mine.maxHealth/2f);
                usingAbility = false;
            };
        }

        return null;
    }
}

