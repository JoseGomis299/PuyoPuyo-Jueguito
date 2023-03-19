using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterData
{
   public Sprite characterBody;
   public Sprite characterProfile;
   public string playerName;
   public int abilityId;

   public CharacterData(Sprite characterBody, int ability, Sprite characterProfile, string playerName)
   {
      abilityId = ability;
      this.characterBody = characterBody;
      this.characterProfile = characterProfile;
      this.playerName = playerName;
   }
}


