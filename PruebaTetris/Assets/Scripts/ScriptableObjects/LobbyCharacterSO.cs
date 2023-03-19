using UnityEngine;

[CreateAssetMenu(menuName = "Lobby Character",fileName = "LobbyCharacter")]
public class LobbyCharacterSO :ScriptableObject
{
    public new string name;
    public string abilityName;
    [Multiline]public string abilityDescription;

    public Sprite characterBody;
    public Sprite characterProfile;
    public int id;
}
