using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerListManager : MonoBehaviour {

    public Dictionary<ulong, Tuple<string, string, string>> playerList { get; private set; } = new Dictionary<ulong, Tuple<string, string, string>>();


    // Start is called before the first frame update
    void Start() {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;        
    }

    // Update is called once per frame
    void Update() {

    }

    private bool AddPlayerToList(ulong clientId, string playerId, string playerName, string playerColor) {
        if (playerList.ContainsKey(clientId)) {
            return false;
        }
        playerList.Add(clientId, new Tuple<string, string, string>(playerId, playerName, playerColor));
        Debug.Log("Adding player " + playerList[clientId].Item2 + " (" + playerList[clientId].Item3 + ")");
        return true;
    }

    private bool RemovePlayerFromList(ulong clientId) {
        if (playerList.ContainsKey(clientId)) {
            Debug.Log("Removing player " + playerList[clientId].Item2 + " (" + playerList[clientId].Item3 + ")");
        } else {
            Debug.Log("Removing player " + clientId);
        }
        return playerList.Remove(clientId);
    }

    void OnClientDisconnectCallback(ulong clientId) {
        RemovePlayerFromList(clientId);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        // The client identifier to be authenticated
        ulong clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        byte[] connectionData = request.Payload;
        string payload = System.Text.Encoding.UTF8.GetString(connectionData);
        RelayManager.ConnectionPayload connectionPayload = JsonUtility.FromJson<RelayManager.ConnectionPayload>(payload);
        if (connectionData == null || connectionData.Length == 0
               || string.IsNullOrEmpty(payload)
               || connectionPayload == null
               || playerList.ContainsKey(clientId)) {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Pending = false;
            return;
        }
        AddPlayerToList(clientId, connectionPayload.playerId, connectionPayload.playerName, connectionPayload.playerColor);

        // Your approval logic determines the following values
        response.Approved = true;
        response.CreatePlayerObject = true;

        // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }
}
