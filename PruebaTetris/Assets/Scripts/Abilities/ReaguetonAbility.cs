using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ReaguetonAbility : ICharacterAbility
{
    private bool _usingAbility;
    public void UseAbility(PieceController myPieceController, PieceController enemyPieceController)
    {
        if(_usingAbility) return;
        if (NetworkManager.Singleton != null)
        {
            AbilitiesNetwork.Instance.PerformAbility(new NetworkObjectReference(myPieceController.NetworkObject),
                new NetworkObjectReference(enemyPieceController.NetworkObject), 0);
            return;
        }
        
        _usingAbility = true;
        enemyPieceController.fallSpeed *= 0.1f;
        enemyPieceController.fallSpeedDelta *= 0.1f;
        Timer.Instance.WaitForAction(()=>{ 
            enemyPieceController.fallSpeed /= 0.1f;
            enemyPieceController.fallSpeedDelta /= 0.1f;
            _usingAbility = false;
        }, 5f);
    }
}