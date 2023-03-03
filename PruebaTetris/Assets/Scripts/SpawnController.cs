using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    public static SpawnController Instance;
    public int playerCount { get; private set; }
    private int[] _playerIDs;
    private void Awake()
    {
        if(Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        _playerIDs = new int[] { -1, -1 };
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
}
