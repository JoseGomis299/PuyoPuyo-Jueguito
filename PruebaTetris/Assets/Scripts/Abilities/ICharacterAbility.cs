using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterAbility
{
   public void UseAbility(PieceController myPieceController, PieceController enemyPieceController);
}
