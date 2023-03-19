using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnController : NetworkBehaviour
{
    public static PlayerSpawnController Instance;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject waitingPlayers;
    public int playerCount { get; private set; }
    private int[] _playerIDs;
    
    public class StringContainer : INetworkSerializable
    {
        public string SomeText;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                serializer.GetFastBufferWriter().WriteValueSafe(SomeText);
            }
            else
            {
                serializer.GetFastBufferReader().ReadValueSafe(out SomeText);
            }
        }
    }
    
    private void Awake()
    {
        if(Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        _playerIDs = new int[] { -1, -1 };

       if(NetworkManager != null) NetworkManager.OnClientConnectedCallback += SpawnPlayers;
    }

    private void SpawnPlayers(ulong obj)
    {
        SpawnPlayerServerRpc();
    }

    public void OnPlayerJoined()
    {
        playerCount++;
    }

    public void OnPlayerExit()
    {
        playerCount--;
    }

    public int SetPlayerID()
    {
        for(int i = 0; i<_playerIDs.Length; i++)
        {
            if (_playerIDs[i] == -1)
            {
                _playerIDs[i] = i;
                return i;
            }
        }

        return -1;
    }

    public void SetPlayerData(AbilityController abilityController, PlayerUI playerUI, bool playerTwo)
    {
        string json = "";
        
        if (playerTwo)
        {
            json = File.ReadAllText(Application.persistentDataPath + "/PlayerDataFile.json");
        }
        else
        {
            json = File.ReadAllText(Application.persistentDataPath + "/PlayerDataFile.json");
        }
        
        CharacterData characterData = JsonUtility.FromJson<CharacterData>(json); 
        abilityController.SetAbility(characterData.abilityId);
        playerUI.SetValues(characterData.playerName, characterData.characterProfile, characterData.characterBody);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);

        SetPlayerDataClientRpc(new NetworkObjectReference(player));
    }
    
    [ClientRpc]
    private void SetPlayerDataClientRpc(NetworkObjectReference reference)
    {
        reference.TryGet(out var player);
        
        if (player.IsOwner)
        {
            string json = File.ReadAllText(Application.persistentDataPath + "/PlayerDataFile.json");
            var characterData = JsonUtility.FromJson<CharacterData>(json);
            player.GetComponent<AbilityController>().SetAbility(characterData.abilityId);
            player.GetComponent<PlayerUI>().SetValues(characterData.playerName, characterData.characterProfile, characterData.characterBody);
            waitingPlayers.SetActive(false);

            StringContainer playerData = new StringContainer
            {
                SomeText = json
            };
            
            SendPlayerDataServerRpc(reference, playerData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPlayerDataServerRpc(NetworkObjectReference playerReference, StringContainer playerData)
    {
        SetOtherPlayersDataClientRpc(playerReference, playerData);
    }
    
    [ClientRpc]
    private void SetOtherPlayersDataClientRpc(NetworkObjectReference playerReference, StringContainer playerData)
    {
        playerReference.TryGet(out var player);
        
        if(player.IsOwner) return;
        
        CharacterData characterData = JsonUtility.FromJson<CharacterData>(playerData.SomeText);
        player.GetComponent<AbilityController>().SetAbility(characterData.abilityId);
        player.GetComponent<PlayerUI>().SetValues(characterData.playerName, characterData.characterProfile, characterData.characterBody);
        waitingPlayers.SetActive(false);
    }
}
