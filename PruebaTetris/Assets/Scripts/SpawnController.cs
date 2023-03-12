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

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);
        
        SetAbilityClientRpc();
    }
    
    [ClientRpc]
    private void SetAbilityClientRpc()
    {
        if(!IsOwner) return;
      
        string json = File.ReadAllText(Application.persistentDataPath + "/AbilitieDataFile.json");
        CharacterAbility characterAbility = JsonUtility.FromJson<CharacterAbility>(json);

        Debug.Log(characterAbility.abilityId);
    }
}
