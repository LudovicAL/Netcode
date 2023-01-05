using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour {

    [Serializable]
    public class ConnectionPayload {
        public string playerId;
        public string playerName;
        public string playerColor;
    }
    public static RelayManager instance { get; private set; }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    private void SetConnectionPayload() {
        string payload = JsonUtility.ToJson(new ConnectionPayload() {
            playerId = AuthenticationService.Instance.PlayerId,
            playerName = LobbyManager.instance.GetPlayerName(AuthenticationService.Instance.PlayerId),
            playerColor = LobbyManager.instance.GetPlayerColor(AuthenticationService.Instance.PlayerId)
        });
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
    }

    public async Task<HttpReturnCode> CreateRelay() {
        SetConnectionPayload();
        string joinCode;
        try {

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

            bool success = NetworkManager.Singleton.StartHost();
            if (!success) {
                throw new Exception("Host did not start successfully");
            }
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
        return new HttpReturnCode("Relay created successfully.", joinCode);
    }

    public async Task<HttpReturnCode> JoinRelay(string joinCode) {
        SetConnectionPayload();
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

            NetworkManager.Singleton.StartClient();
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
        return new HttpReturnCode(200, "Relay joined successfully.");
    }
}
