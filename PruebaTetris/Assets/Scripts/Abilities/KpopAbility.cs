using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KpopAbility : ICharacterAbility
{
    public void UseAbility(PieceController myPieceController, PieceController enemyPieceController)
    {
        if (NetworkManager.Singleton != null)
        {
            AbilitiesNetwork.Instance.PerformAbility(new NetworkObjectReference(myPieceController.NetworkObject),
                new NetworkObjectReference(enemyPieceController.NetworkObject), 3);
            return;
        }
        
        myPieceController.AddHealth(myPieceController.maxHealth/2f);
    }
}
