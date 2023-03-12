using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAbility
{
   public Sprite characterBody;
   public ICharacterAbility ability;
   public int abilityId;

   public CharacterAbility(Sprite characterBody, int ability)
   {
      abilityId = ability;
      this.characterBody = characterBody;
      switch (ability)
      {
         case 0: this.ability = new ReaguetonAbility();
            break;
         case 1: this.ability = new RapAbility();
            break;
         case 2: this.ability = new MetalAbility();
            break;
         case 3: this.ability = new KpopAbility();
            break;
      }
      
   }
}


