using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelJoined : MonoBehaviour {

    private bool resetSelectionOnNextUpdate = true;
    private static PlayerComparer playerComparer = new PlayerComparer();
    private Dictionary<Player, GameObject> playersInCanvasLobbyDictionnary = new Dictionary<Player, GameObject>(playerComparer);

    [Header("LOBBY INFO")]
    [SerializeField]
    [IfNullTryFetch("Text LobbyName")]
    private TextMeshProUGUI currentLobbyName;
    [SerializeField]
    [IfNullTryFetch("Text LobbyOccupancy")]
    private TextMeshProUGUI currentLobbyOccupancy;
    [SerializeField]
    [IfNullTryFetch("Text LobbyPrivacy")]
    private TextMeshProUGUI currentLobbyPrivacy;
    [Header("PLAYERS IN LOBBY")]
    [SerializeField]
    [IfNullTryFetch("Panel PlayerList")]
    private Transform panelPlayers;
    [SerializeField]
    private GameObject panelPlayerNamePrefab;
    [Header("OTHERS")]
    [SerializeField]
    [IfNullTryFetch("Text LobbyCode")]
    private TextMeshProUGUI currentLobbyCode;
    [SerializeField]
    [IfNullTryFetch("Button Leave")]
    private ExtendedButton leaveLobbyButton;
    [SerializeField]
    [IfNullTryFetch("Button StartGame")]
    private ExtendedButton startGameButton;

    // Start is called before the first frame update
    void Start() {
        LobbyManager.instance.lobbyPolledEvent.AddListener(UpdateJoinedLobbyPanel);
        if (leaveLobbyButton) {
            leaveLobbyButton.onClick.AddListener(() => {
                LeaveLobby();
            });
        }
        if (startGameButton) {
            startGameButton.onClick.AddListener(() => {
                StartGame();
            });
        }
    }

    // Update is called once per frame
    void Update() {
        if (resetSelectionOnNextUpdate) {
            ResetSelection();
        }
    }

    private void ResetSelection() {
        resetSelectionOnNextUpdate = false;
        Selectable[] selectables = GetComponentsInChildren<Selectable>();
        foreach (Selectable selectable in selectables) {
            if (selectable.gameObject.activeSelf && selectable.interactable) {
                selectable.Select();
                break;
            }
        }
    }

    //Requests to leave the current lobby
    private async void LeaveLobby() {
        HttpReturnCode httpReturnCode = await LobbyManager.instance.LeaveLobby();
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Search");
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            leaveLobbyButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Request game start
    private async void StartGame() {
        HttpReturnCode httpReturnCode = await RelayManager.instance.CreateRelay();
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            bool privacy = LobbyManager.instance.GetCurrentLobbyPrivacy();
            int maxPlayer = LobbyManager.instance.GetCurrentLobbyMaxPlayer();
            HttpReturnCode secondHttpReturnCode = await LobbyManager.instance.UpdateHostedLobbyOptions(privacy, maxPlayer, httpReturnCode.joinCode);
            secondHttpReturnCode.Log();
            if (secondHttpReturnCode.IsSuccess()) {
                await LobbyManager.instance.LeaveLobby();
                CanvasCoordinator.instance.SwitchPanel("Panel Game");
                return;
            }
        }
        startGameButton.ShakeButtonSideways();
        AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
    }

    //Updates the informations displayed on the Joined Panel
    private void UpdateJoinedLobbyPanel() {
        if (!LobbyManager.instance.IsInLobby()) {
            return;
        }
        Debug.Log("Refreshing joined lobby");
        if (currentLobbyName) {
            currentLobbyName.text = LobbyManager.instance.GetCurrentLobbyName();
        }
        if (currentLobbyOccupancy) {
            currentLobbyOccupancy.text = LobbyManager.instance.GetCurrentLobbyOccupancy();
        }
        if (currentLobbyPrivacy) {
            currentLobbyPrivacy.text = LobbyManager.instance.GetCurrentLobbyPrivacy() ? "Private" : "Public";
        }
        if (panelPlayers && panelPlayerNamePrefab) {
            UpdatePanelPlayers();
        }
        if (currentLobbyCode) {
            currentLobbyCode.text = LobbyManager.instance.GetCurrentLobbyCode();
        }
        if (startGameButton) {
            startGameButton.interactable = LobbyManager.instance.IsHost();
            startGameButton.GetComponentInParent<UITooltip>().enabled = !LobbyManager.instance.IsHost();
        }
    }

    private void UpdatePanelPlayers() {
        List<Player> currentLobbyPlayerList = LobbyManager.instance.GetCurrentLobbyPlayerList();
        bool atLeastOnePlayerLeft = false;
        bool atLeastOnePlayerJoined = false;

        //REMOVE PLAYERS THAT HAVE LEFT THE LOBBY
        for (int i = playersInCanvasLobbyDictionnary.Count - 1; i >= 0; i--) {
            if (!currentLobbyPlayerList.Contains(playersInCanvasLobbyDictionnary.ElementAt(i).Key, playerComparer)) {
                atLeastOnePlayerLeft = true;
                GameObject panelPlayerName = playersInCanvasLobbyDictionnary.ElementAt(i).Value;
                GameObject selectableGameObject = panelPlayerName.GetComponentInChildren<Selectable>().gameObject;
                if (EventSystem.current.currentSelectedGameObject == selectableGameObject) {
                    resetSelectionOnNextUpdate = true;
                }
                Destroy(panelPlayerName);
                playersInCanvasLobbyDictionnary.Remove(playersInCanvasLobbyDictionnary.ElementAt(i).Key);
            }
        }

        foreach (Player player in currentLobbyPlayerList) {
            GameObject panelPlayerName = null;
            //UPDATE PLAYERS THAT STAYED IN THE LOBBY
            List<Player> playersInCanvasLobbyList = playersInCanvasLobbyDictionnary.Keys.ToList();
            if (playersInCanvasLobbyList.Contains(player, playerComparer)) {
                panelPlayerName = playersInCanvasLobbyDictionnary[player];
                panelPlayerName.transform.Find("Button PlayerColor").GetComponent<Button>().interactable = true;
                panelPlayerName.transform.Find("Button PlayerColor/Image PlayerColor/Image PlayerColorLoader").gameObject.SetActive(false);
                //ADD PLAYERS THAT HAVE JOINED THE LOBBY
            } else {
                atLeastOnePlayerJoined = true;
                panelPlayerName = Instantiate(panelPlayerNamePrefab, panelPlayers);
                panelPlayerName.name = player.Data["PlayerName"].Value;
                playersInCanvasLobbyDictionnary.Add(player, panelPlayerName);
                Button colorButton = panelPlayerName.transform.Find("Button PlayerColor").GetComponent<Button>();
                bool isCurrentPlayer = player.Id.Equals(AuthenticationService.Instance.PlayerId);
                colorButton.GetComponent<UITooltip>().enabled = isCurrentPlayer;
                colorButton.interactable = isCurrentPlayer;
                if (isCurrentPlayer) {
                    colorButton.onClick.AddListener(async delegate {
                        Player currentPlayer = LobbyManager.instance.GetCurrentLobbyPlayerList().Find(x => x.Id.Equals(player.Id));
                        string nextColorKey = ColorUtility.GetNextColorKey(currentPlayer.Data["PlayerColor"].Value);
                        colorButton.transform.Find("Image PlayerColor").GetComponent<Image>().color = ColorUtility.GetColorFromKey(nextColorKey);
                        colorButton.interactable = false;
                        resetSelectionOnNextUpdate = true;
                        colorButton.transform.Find("Image PlayerColor/Image PlayerColorLoader").gameObject.SetActive(true);
                        HttpReturnCode httpReturnCode = await LobbyManager.instance.UpdatePlayerColor(nextColorKey);
                        httpReturnCode.Log();
                    });
                }
            }
            panelPlayerName.transform.Find("Image HostIndicator").gameObject.SetActive(LobbyManager.instance.IsHost(player.Id));
            panelPlayerName.transform.Find("Button PlayerColor/Image PlayerColor").GetComponent<Image>().color = ColorUtility.GetColorFromKey(player.Data["PlayerColor"].Value);
            panelPlayerName.transform.Find("Text PlayerName").GetComponent<TextMeshProUGUI>().text = player.Data["PlayerName"].Value;

        }
        if (atLeastOnePlayerLeft) {
            AudioManager.Instance.PlayClip(AudioManager.Instance.playerLeavingClip);
        }
        if (atLeastOnePlayerJoined) {
            AudioManager.Instance.PlayClip(AudioManager.Instance.playerJoiningClip);
        }
    }
}

public class PlayerComparer : IEqualityComparer<Player> {
    public bool Equals(Player x, Player y) {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(Player obj) {
        return obj.Id.GetHashCode();
    }
}
