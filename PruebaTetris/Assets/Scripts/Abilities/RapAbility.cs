using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RapAbility : ICharacterAbility
{
    public void UseAbility(PieceController myPieceController, PieceController enemyPieceController)
    {
        if (NetworkManager.Singleton != null)
        {
            AbilitiesNetwork.Instance.PerformAbility(new NetworkObjectReference(myPieceController.NetworkObject),
                new NetworkObjectReference(enemyPieceController.NetworkObject), 1);
        }
        else
        {
            int garbageCount = myPieceController.RemoveGarbage();
            if(garbageCount == 0) return;

            enemyPieceController.ThrowGarbage(garbageCount / 2, 0);
        }
    }
}
