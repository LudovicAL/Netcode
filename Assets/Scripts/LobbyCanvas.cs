using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyCanvas : MonoBehaviour {
    [Tooltip("List of all panels direct children of the Panel Lobby")]
    [SerializeField]
    private List<GameObject> panelList;
    [Tooltip("The default maximum number of players in a lobby")]
    [SerializeField]
    private int defaultMaxNumberOfPlayers;
    [Tooltip("The maximum number of results to a query to get available lobbies")]
    [SerializeField]
    private int maxNumberOfResults;
    [Tooltip("The delay in seconds between automatic updates of the available lobbies list")]
    [SerializeField]
    private float delayBetweenLobbiesListAutomaticRefresh;

    private List<Player> currentLobbyPlayerList = new List<Player>();
    private float refreshTimer;
    private bool resetSelectionOnNextUpdate = true;
    private static LobbyComparer lobbyComparer = new LobbyComparer();
    private Dictionary<Lobby, GameObject> canvasLobbyDictionnary = new Dictionary<Lobby, GameObject>(lobbyComparer);
    private static PlayerComparer playerComparer = new PlayerComparer();
    private Dictionary<Player, GameObject> playersInCanvasLobbyDictionnary = new Dictionary<Player, GameObject>(playerComparer);

    [Header("SEARCH")]
    [SerializeField]
    private TMP_InputField lobbyNameInputField;
    [SerializeField]
    private ExtendedButton createLobbyButton;
    [SerializeField]
    private Toggle privateLobbyToggle;
    [SerializeField]
    private TMP_InputField lobbyCodeInputField;
    [SerializeField]
    private ExtendedButton joinLobbyByCodeButton;
    [SerializeField]
    private Transform availableLobbiesPanel;
    [SerializeField]
    private GameObject panelAvailableLobbyPrefab;
    [SerializeField]
    private Button refreshAvailableLobbiesButton;
    [SerializeField]
    private ExtendedButton logoutButton;

    [Header("JOINED")]
    [SerializeField]
    private TextMeshProUGUI currentLobbyName;
    [SerializeField]
    private TextMeshProUGUI currentLobbyOccupancy;
    [SerializeField]
    private TextMeshProUGUI currentLobbyPrivacy;
    [SerializeField]
    private Transform panelPlayers;
    [SerializeField]
    private GameObject panelPlayerNamePrefab;
    [SerializeField]
    private TextMeshProUGUI currentLobbyCode;
    [SerializeField]
    private ExtendedButton leaveLobbyButton;
    [SerializeField]
    private ExtendedButton startGameButton;

    void Start() {
        LobbyManager.instance.lobbyPolledEvent.AddListener(UpdateJoinedLobbyPanel);
        if (createLobbyButton) {
            createLobbyButton.onClick.AddListener(() => {
                CreateLobby();
            });
        }
        if (joinLobbyByCodeButton) {
            joinLobbyByCodeButton.onClick.AddListener(() => {
                JoinLobbyByCode();
            });
        }
        if (refreshAvailableLobbiesButton) {
            refreshAvailableLobbiesButton.onClick.AddListener(() => {
                if (refreshTimer < (delayBetweenLobbiesListAutomaticRefresh / 2f)) {
                    refreshTimer = delayBetweenLobbiesListAutomaticRefresh;
                    RefreshAvailableLobbies();
                }
                AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
            });
        }
        if (logoutButton) {
            logoutButton.onClick.AddListener(() => {
                Logout();
            });
            logoutButton.gameObject.SetActive(AuthenticationManager.instance.IsManualAuthenticationOn());
        }
        if (leaveLobbyButton) {
            leaveLobbyButton.onClick.AddListener(() => {
                LeaveLobby();
            });
        }
    }

    void Update() {
        if (availableLobbiesPanel.gameObject.activeSelf) {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer < 0f) {
                refreshTimer = delayBetweenLobbiesListAutomaticRefresh;
                RefreshAvailableLobbies();
            }
        }
        if (resetSelectionOnNextUpdate) {
            ResetSelection();
        }
    }

    void OnEnable() {
        SwitchPanel(panelList.First().name);
    }

    //Initializes the panel's input fields
    private void InitializeInputFields() {
        lobbyNameInputField.text = "";
        lobbyCodeInputField.text = "";
    }

    //Switches to the specified UI Panel
    private void SwitchPanel(string nameOfPanelToDisplay) {
        foreach (GameObject panel in panelList) {
            panel.SetActive(panel.name.Equals(nameOfPanelToDisplay));
        }
        resetSelectionOnNextUpdate = true;
        InitializeInputFields();
    }

    private void ResetSelection() {
        resetSelectionOnNextUpdate = false;
        foreach (GameObject panel in panelList) {
            if (panel.activeSelf) {
                Selectable[] selectables = panel.GetComponentsInChildren<Selectable>();
                foreach (Selectable selectable in selectables) {
                    if (selectable.gameObject.activeSelf && selectable.interactable) {
                        selectable.Select();
                        break;
                    }
                }
                break;
            }
        }
    }

    //Requests a lobby creation
    private async void CreateLobby() {
        if (!lobbyNameInputField) {
            Debug.LogWarning("Missing a GameObject reference");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        string lobbyName = lobbyNameInputField.text;
        if (string.IsNullOrWhiteSpace(lobbyName)) {
            Debug.LogWarning("Enter a lobby name first");
            createLobbyButton.HighlightLinkedInputField();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        bool privateLobby = false;
        if (privateLobbyToggle) {
            privateLobby = privateLobbyToggle.isOn;
        }
        CreateLobbyOptions createLobbyOptions = LobbyManager.instance.DefineCreateLobbyOption(privateLobby);
        HttpReturnCode httpReturnCode = await LobbyManager.instance.CreateLobby(lobbyName, Mathf.Max(defaultMaxNumberOfPlayers, 2), createLobbyOptions);
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            SwitchPanel("Panel Joined");
            UpdateJoinedLobbyPanel();
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            createLobbyButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Requests to join a lobby by code
    private void JoinLobbyByCode() {
        if (lobbyCodeInputField) {
            JoinLobbyByCode(lobbyCodeInputField.text);
        } else {
            Debug.LogWarning("Missing a GameObject reference");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }


    //Requests to join a lobby by code
    private async void JoinLobbyByCode(string lobbyCode) {
        if (string.IsNullOrEmpty(lobbyCode)) {
            Debug.LogWarning("Enter a lobby code first");
            joinLobbyByCodeButton.HighlightLinkedInputField();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        HttpReturnCode httpReturnCode = await LobbyManager.instance.JoinLobbyByCode(lobbyCode);
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            SwitchPanel("Panel Joined");
            UpdateJoinedLobbyPanel();
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            joinLobbyByCodeButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Requests to join a lobby by Id
    private async void JoinLobbyById(string lobbyId, ExtendedButton button) {
        if (string.IsNullOrEmpty(lobbyId)) {
            Debug.LogWarning("Enter a lobby id first");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        HttpReturnCode httpReturnCode = await LobbyManager.instance.JoinLobbyById(lobbyId);
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            SwitchPanel("Panel Joined");
            UpdateJoinedLobbyPanel();
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            button.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Request an update of the available lobbies
    private async void RefreshAvailableLobbies() {
        if (!availableLobbiesPanel || !panelAvailableLobbyPrefab) {
            Debug.LogWarning("Missing a GameObject reference");
            return;
        }
        QueryLobbiesOptions queryLobbiesOptions = LobbyManager.instance.DefineQueryLobbiesOptions(Mathf.Max(maxNumberOfResults, 5));
        HttpReturnCode httpReturnCode = await LobbyManager.instance.SearchForLobby(queryLobbiesOptions);
        httpReturnCode.Log();
        if (!httpReturnCode.IsSuccess() || httpReturnCode.queryResponse == null) {
            return;
        }
        //REMOVE LOBBIES THAT DISAPEARED
        for (int i = canvasLobbyDictionnary.Count - 1; i >= 0; i--) {
            KeyValuePair<Lobby, GameObject> canvasLobby = canvasLobbyDictionnary.ElementAt(i);
            if (!httpReturnCode.queryResponse.Results.Contains(canvasLobby.Key, lobbyComparer)) {
                if (canvasLobby.Value.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject) {
                    resetSelectionOnNextUpdate = true;
                }
                Destroy(canvasLobby.Value);
                canvasLobbyDictionnary.Remove(canvasLobby.Key);
            }
        }
        List<Lobby> canvasLobbyList = canvasLobbyDictionnary.Keys.ToList();
        foreach (Lobby lobby in httpReturnCode.queryResponse.Results) {
            GameObject panelAvailableLobby = null;
            ExtendedButton panelAvailableLobbyButton = null;
            //UPDATE LOBBIES THAT REMAINED
            if (canvasLobbyList.Contains(lobby, lobbyComparer)) {
                panelAvailableLobby = canvasLobbyDictionnary[lobby];
                panelAvailableLobbyButton = panelAvailableLobby.transform.Find("Button JoinAvailableLobby").GetComponentInChildren<ExtendedButton>();
                //ADD LOBBIES THAT APPEARED
            } else {
                panelAvailableLobby = Instantiate(panelAvailableLobbyPrefab, availableLobbiesPanel);
                panelAvailableLobby.name = lobby.Name;
                canvasLobbyDictionnary.Add(lobby, panelAvailableLobby);
                panelAvailableLobbyButton = panelAvailableLobby.transform.Find("Button JoinAvailableLobby").GetComponentInChildren<ExtendedButton>();
                string lobbyId = lobby.Id;
                panelAvailableLobbyButton.onClick.AddListener(delegate {
                    JoinLobbyById(lobbyId, panelAvailableLobbyButton);
                });
            }
            panelAvailableLobbyButton.interactable = (lobby.AvailableSlots > 0);
            panelAvailableLobby.transform.Find("Text AvailableLobbyName").GetComponent<TextMeshProUGUI>().text = lobby.Name;
            panelAvailableLobby.transform.Find("Text AvailableLobbyOccupancy").GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        }
    }

    //Logs out and returns to authentication
    private void Logout() {
        if (AuthenticationManager.instance.Logout()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Authentication");
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            logoutButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Requests to leave the current lobby
    private async void LeaveLobby() {
        if (!leaveLobbyButton) {
            Debug.LogWarning("Missing a GameObject reference");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        HttpReturnCode httpReturnCode = await LobbyManager.instance.LeaveLobby();
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            currentLobbyPlayerList.Clear();
            SwitchPanel("Panel Search");
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            leaveLobbyButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Updates the informations displayed on the Joined Panel
    private void UpdateJoinedLobbyPanel() {
        Debug.Log("Refreshing joined lobby");
        if (currentLobbyName) {
            currentLobbyName.text = LobbyManager.instance.GetCurrentLobbyName();
        }
        if (currentLobbyOccupancy) {
            currentLobbyOccupancy.text = LobbyManager.instance.GetCurrentLobbyOccupancy();
        }
        if (currentLobbyPrivacy) {
            currentLobbyPrivacy.text = LobbyManager.instance.GetCurrentLobbyPrivacy();
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
                        colorButton.transform.Find("Image PlayerColor").GetComponent<Image>().color = ColorUtility.colorDictionary[nextColorKey];
                        colorButton.interactable = false;
                        resetSelectionOnNextUpdate = true;
                        colorButton.transform.Find("Image PlayerColor/Image PlayerColorLoader").gameObject.SetActive(true);
                        HttpReturnCode httpReturnCode = await LobbyManager.instance.UpdatePlayerColor(nextColorKey);
                        httpReturnCode.Log();
                    });
                }
            }
            panelPlayerName.transform.Find("Image HostIndicator").gameObject.SetActive(LobbyManager.instance.IsHost(player.Id));
            panelPlayerName.transform.Find("Button PlayerColor/Image PlayerColor").GetComponent<Image>().color = ColorUtility.colorDictionary[player.Data["PlayerColor"].Value];
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

public class LobbyComparer : IEqualityComparer<Lobby> {
    public bool Equals(Lobby x, Lobby y) {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(Lobby obj) {
        return obj.Id.GetHashCode();
    }
}