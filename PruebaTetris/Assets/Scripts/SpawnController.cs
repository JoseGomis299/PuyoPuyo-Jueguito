using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class SpawnController : NetworkBehaviour
{
    public static SpawnController Instance;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject waitingPlayers;
    public int playerCount { get; private set; }
    private int[] _playerIDs;
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

    public void SetPlayerAbilities(AbilityController abilityController, bool playerTwo)
    {
        string json = "";
        
        if (playerTwo)
        {
            json = File.ReadAllText(Application.persistentDataPath + "/AbilitieDataFile.json");
        }
        else
        {
            json = File.ReadAllText(Application.persistentDataPath + "/AbilitieDataFile.json");
        }
        
        CharacterAbilityData characterAbilityData = JsonUtility.FromJson<CharacterAbilityData>(json); 
        abilityController.SetAbility(characterAbilityData.abilityId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);

        SetAbilityClientRpc(new NetworkObjectReference(player));
    }
    
    [ClientRpc]
    private void SetAbilityClientRpc(NetworkObjectReference reference)
    {
        reference.TryGet(out var abilityController);
      
        string json = File.ReadAllText(Application.persistentDataPath + "/AbilitieDataFile.json");
        CharacterAbilityData characterAbilityData = JsonUtility.FromJson<CharacterAbilityData>(json);

        abilityController.GetComponent<AbilityController>().SetAbility(characterAbilityData.abilityId);
        waitingPlayers.SetActive(false);
    }
}
