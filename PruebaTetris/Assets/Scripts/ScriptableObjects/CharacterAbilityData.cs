using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAbilityData
{
   public Sprite characterBody;
   public int abilityId;

   public CharacterAbilityData(Sprite characterBody, int ability)
   {
      abilityId = ability;
      this.characterBody = characterBody;
   }
}


