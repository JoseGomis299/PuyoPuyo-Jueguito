using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MetalAbility : ICharacterAbility
{
    public void UseAbility(PieceController myPieceController, PieceController enemyPieceController)
    {
        if (NetworkManager.Singleton != null)
        {
            AbilitiesNetwork.Instance.PerformAbility(new NetworkObjectReference(myPieceController.NetworkObject),
                new NetworkObjectReference(enemyPieceController.NetworkObject), 2);
            return;
        }
        
        enemyPieceController.ThrowGarbage(0, 12);
    }
}
