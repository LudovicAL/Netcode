using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;

public class LobbyManager : MonoBehaviour {
    public static LobbyManager instance { get; private set; }

    [SerializeField]
    private float delayBetweenLobbyPolls;

    private Lobby currentLobby;
    private float heartBeatTimer;
    private float lobbyUpdateTimer;

    private const string PLAYER_COLOR = "PlayerColor";
    private const string PLAYER_NAME = "PlayerName";
    private const string JOIN_CODE = "JoinCode";

    public UnityEvent lobbyPolledEvent = new UnityEvent();

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    void Start() {
    }

    void Update() {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    async void OnApplicationQuit() {
        (await LeaveLobby()).Log();
    }

    //Creates a lobby
    public async Task<HttpReturnCode> CreateLobby(string lobbyName, int maxPlayers, CreateLobbyOptions createLobbyOptions) {
        try {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            return new HttpReturnCode(200, "Lobby " + currentLobby.Name + " created successfully");
        } catch (Exception e) {
            return new HttpReturnCode(500, "An error occured while creating the lobby:\n" + e.ToString());
        }
    }

    //Searches for availables lobbies
    public async Task<HttpReturnCode> SearchForLobby(QueryLobbiesOptions queryLobbiesOptions) {
        try {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            return new HttpReturnCode("Available lobbies retrieved successfully", queryResponse);
        } catch (Exception e) {
            return new HttpReturnCode(400, "An error occured while searching for lobbies:\n" + e.ToString());
        }
    }

    //Joins a lobby identified by its code
    public async Task<HttpReturnCode> JoinLobbyByCode(string lobbyCode) {
        try {
            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, DefineJoinLobbyByCodeOptions());
            return new HttpReturnCode(200, "Lobby joined by code successfully");
        } catch (Exception e) {
            return new HttpReturnCode(500, "An error occured while joining lobby with code " + lobbyCode + ":\n" + e.ToString());
        }
    }

    //Joins a lobby identified by its Id
    public async Task<HttpReturnCode> JoinLobbyById(string lobbyId) {
        try {
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, DefineJoinLobbyByIdOptions());
            return new HttpReturnCode(200, "Lobby joined by Id successfully");
        } catch (Exception e) {
            return new HttpReturnCode(500, "An error occured while joining lobby with Id " + lobbyId + ":\n" + e.ToString());
        }
    }

    //Joins the first available lobby
    private async void QuickJoinLobby() {
        try {
            currentLobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            Debug.Log("Quickjoined a lobby.");
        } catch (Exception e) {
            Debug.LogWarning("An error occured while quickjoining a lobby:\n" + e.ToString());
        }
    }

    //Updates the current lobby option (host only)
    public async Task<HttpReturnCode> UpdateHostedLobbyOptions(bool lobbyIsPrivate, int maxPlayers, string joinCode) {
        if (!IsHost()) {
            return new HttpReturnCode(400, "Only the host of the lobby can update the lobby.");
        }
        try {
            currentLobby = await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions {
                IsPrivate = lobbyIsPrivate,
                MaxPlayers = maxPlayers,
                Data = new Dictionary<string, DataObject> {
                    { JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            });
            return new HttpReturnCode(200, "The lobby was updated successfully.");
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
    }

    //Updates the player's name
    public async Task<HttpReturnCode> UpdatePlayerName() {
        try {
            currentLobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                    { PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.Profile) }
                }
            });
            return new HttpReturnCode(200, "The player's name was updated successfully.");
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
    }

    //Updates the player's color
    public async Task<HttpReturnCode> UpdatePlayerColor(string newPlayerColor) {
        try {
            currentLobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                    { PLAYER_COLOR, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newPlayerColor) }
                }
            });
            return new HttpReturnCode(200, "The player's color was updated successfully (" + newPlayerColor + ").");
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
    }

    //Leaves the current lobby
    public async Task<HttpReturnCode> LeaveLobby() {
        if (currentLobby == null) {
            return new HttpReturnCode(200, "Player is not in a lobby.");
        }
        try {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            currentLobby = null;
            return new HttpReturnCode(200, "Lobby left successfully.");
        } catch (Exception e) {
            return new HttpReturnCode(500, "An error occured while leaving the lobby:\n" + e.ToString());
        }
    }

    //Request the lastest lobby upates
    private async void HandleLobbyPollForUpdates() {
        if (currentLobby == null) {
            return;
        }
        lobbyUpdateTimer -= Time.deltaTime;
        if (lobbyUpdateTimer < 0f) {
            lobbyUpdateTimer = delayBetweenLobbyPolls;
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            lobbyPolledEvent.Invoke();
            if (currentLobby.Data[JOIN_CODE].Value != "0") {
                HttpReturnCode httpReturnCode = await RelayManager.instance.JoinRelay(currentLobby.Data[JOIN_CODE].Value);
                httpReturnCode.Log();
                if (httpReturnCode.IsSuccess()) {
                    CanvasCoordinator.instance.SwitchPanel("Panel Game");
                } else {
                    CanvasCoordinator.instance.SwitchPanel("Panel Search");
                }
                HttpReturnCode secondHttpReturnCode = await LeaveLobby();
                secondHttpReturnCode.Log();
            }
        }
    }

    //Sends a frequent heartbeat to keep the lobby active
    private async void HandleLobbyHeartbeat() {
        if (currentLobby == null || !IsHost()) {
            return;
        }
        heartBeatTimer -= Time.deltaTime;
        if (heartBeatTimer < 0f) {
            heartBeatTimer = 14f;
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
    }

    //Defines a CreateLobbyOptions object
    public CreateLobbyOptions DefineCreateLobbyOption(bool lobbyIsPrivate) {
        return new CreateLobbyOptions {
            Player = DefineNewPlayerObject(),
            IsPrivate = lobbyIsPrivate,
            Data = new Dictionary<string, DataObject> {
                { JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };
    }

    //Defines a QueryLobbiesOptions object
    public QueryLobbiesOptions DefineQueryLobbiesOptions(int maxNumberOfResults) {
        return new QueryLobbiesOptions {
            Count = maxNumberOfResults,
            //Add the following line if you want your search to return only lobbies with available slots
            //Filters = new List<QueryFilter> {
            //    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            //},
            Order = new List<QueryOrder> {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)

            }
        };
    }

    //Defines a JoinLobbyByCodeOptions object
    private JoinLobbyByCodeOptions DefineJoinLobbyByCodeOptions() {
        return new JoinLobbyByCodeOptions {
            Player = DefineNewPlayerObject()
        };
    }

    //Defines a JoinLobbyByIdOptions object
    private JoinLobbyByIdOptions DefineJoinLobbyByIdOptions() {
        return new JoinLobbyByIdOptions {
            Player = DefineNewPlayerObject()
        };
    }

    //Defines a Player object
    private Player DefineNewPlayerObject() {
        string profile = "playerName";
        try {
            profile = AuthenticationService.Instance.Profile;
        } catch (Exception e) {
            Debug.LogWarning("An error occured while defining a new player object:\n" + e.ToString());
        }
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                { PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, profile) },
                { PLAYER_COLOR, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ColorUtility.GetRandomColorKey()) }
            }
        };
    }

    //Returns true if the user is currently in a lobby
    public bool IsInLobby() {
        return currentLobby != null;
    }


    //Returns the current lobby name
    public string GetCurrentLobbyName() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a lobby name from.");
        }
        return currentLobby.Name;
    }

    //Returns the current lobby occupancy
    public string GetCurrentLobbyOccupancy() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull occupancy data from.");
        }
        return (currentLobby.Players.Count.ToString() + "/" + currentLobby.MaxPlayers.ToString());
    }

    //Returns the current lobby privacy setting
    public bool GetCurrentLobbyPrivacy() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a privacy setting from.");
        }
        return currentLobby.IsPrivate;
    }

    //Returns the current lobby code
    public string GetCurrentLobbyCode() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a lobby code from.");
        }
        return currentLobby.LobbyCode;
    }

    //Returns the current lobby player list
    public List<Player> GetCurrentLobbyPlayerList() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a player list from.");
        }
        return currentLobby.Players;
    }

    public int GetCurrentLobbyMaxPlayer() {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a player max from.");
        }
        return currentLobby.MaxPlayers;
    }

    //Returns true if the player is host of the current lobby
    public bool IsHost() {
        string playerId = "";
        try {
            playerId = AuthenticationService.Instance.PlayerId;
        } catch (Exception e) {
            Debug.LogWarning("An error occured while verifying if the current player is host of the lobby:\n" + e.ToString());
        }
        return IsHost(playerId);
    }

    public bool IsHost(string playerId) {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to host.");
        }
        return currentLobby.HostId == playerId;
    }

    public string GetPlayerColor(string playerId) {
        if (currentLobby == null) {
            throw new Exception("There is no lobby to pull a player color from.");
        }
        Player player = currentLobby.Players.Find(x => x.Id.Equals(playerId));
        if (player == null) {
            throw new Exception("Player could not be found in the current lobby.");
        }
        return player.Data[PLAYER_COLOR].Value;
    }
}
