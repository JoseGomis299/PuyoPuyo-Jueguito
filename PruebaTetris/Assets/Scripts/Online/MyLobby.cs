using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class MyLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float updateLobbyTimer;
    private string playerName;
    private string playerChampion;
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        playerName = "JugadorSexy" + Random.Range(0,99);
        playerChampion = "";
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 2;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                Player = GetPlayer()
            };
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            hostLobby = lobby;
            joinedLobby = hostLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions()
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    { 
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            
            this.joinedLobby = joinedLobby;
            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void QuickJoinLobby()
    { 
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            this.joinedLobby = joinedLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            CreateLobby();
        }
    }

    private void PrintPlayers(Lobby lobby)
    {
        foreach (var player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);   
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                {"ChosenChampion", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerChampion)}
            }
        };
    }
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = 15;

               await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            updateLobbyTimer -= Time.deltaTime;
            if (updateLobbyTimer < 0f)
            {
                updateLobbyTimer = 1.1f;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }
    }

    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    private async void KickPlayer(string kickedPlayerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, kickedPlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                        { "ChosenChampion", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerChampion) }
                    }
                });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    private async void UpdatePlayerChampion(string newPlayerChampion)
    {
        try
        {
            playerChampion = newPlayerChampion;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                        { "ChosenChampion", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerChampion) }
                    }
                });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void MigrateLobbyHost(string newHostId)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = newHostId
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
