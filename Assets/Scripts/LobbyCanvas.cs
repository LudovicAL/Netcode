using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
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
        if (availableLobbiesPanel.gameObject.activeInHierarchy) {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer < 0f) {
                refreshTimer = delayBetweenLobbiesListAutomaticRefresh;
                RefreshAvailableLobbies();
            }
        }
    }

    void OnEnable() {
        SwitchPanel(panelList.First<GameObject>().name);
    }

    //Initializes the panel's input fields
    private void InitializeInputFields() {
        lobbyNameInputField.text = "";
        lobbyCodeInputField.text = "";
    }

    //Switches to the specified UI Panel
    private void SwitchPanel(string nameOfPanelToDisplay) {
        foreach (GameObject panel in panelList) {
            panel.SetActive(panel.name == nameOfPanelToDisplay);
        }
        InitializeInputFields();
    }

    //Requests a lobby creation
    private async void CreateLobby() {
        if (lobbyNameInputField) {
            string lobbyName = lobbyNameInputField.text;
            if (lobbyName.Length > 0) {
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
            } else {
                Debug.LogWarning("Enter a lobby name first");
                createLobbyButton.HighlightLinkedInputField();
                AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
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
        if (lobbyCode.Length > 0) {
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
        } else {
            Debug.LogWarning("Enter a lobby code first");
            joinLobbyByCodeButton.HighlightLinkedInputField();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Requests to join a lobby by Id
    private async void JoinLobbyById(string lobbyId, ExtendedButton button) {
        if (lobbyId.Length > 0) {
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
        } else {
            Debug.LogWarning("Enter a lobby id first");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }

    //Request an update of the available lobbies
    private async void RefreshAvailableLobbies() {
        if (availableLobbiesPanel) {
            foreach (Transform child in availableLobbiesPanel) {
                GameObject.Destroy(child.gameObject);
            }
            QueryLobbiesOptions queryLobbiesOptions = LobbyManager.instance.DefineQueryLobbiesOptions(Mathf.Max(maxNumberOfResults, 5));
            HttpReturnCode httpReturnCode = await LobbyManager.instance.SearchForLobby(queryLobbiesOptions);
            httpReturnCode.Log();
            if (httpReturnCode.IsSuccess() && httpReturnCode.queryResponse != null) {
                foreach (Lobby lobby in httpReturnCode.queryResponse.Results) {
                    GameObject newPanelAvailableLobby = Instantiate(panelAvailableLobbyPrefab, availableLobbiesPanel);
                    newPanelAvailableLobby.transform.Find("Text AvailableLobbyName").GetComponent<TextMeshProUGUI>().text = lobby.Name;
                    newPanelAvailableLobby.transform.Find("Text AvailableLobbyOccupancy").GetComponent<TextMeshProUGUI>().text = lobby.Players.Count + "/" + lobby.MaxPlayers;
                    ExtendedButton newPanelAvailableLobbyButton = newPanelAvailableLobby.transform.Find("Button JoinAvailableLobby").GetComponentInChildren<ExtendedButton>();
                    if (lobby.AvailableSlots > 0) {
                        string lobbyId = lobby.Id;
                        newPanelAvailableLobbyButton.onClick.AddListener(delegate {
                            JoinLobbyById(lobbyId, newPanelAvailableLobbyButton);
                        });
                    } else {
                        newPanelAvailableLobbyButton.enabled = false;
                    }
                }
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
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
        if (leaveLobbyButton) {
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
        } else {
            Debug.LogWarning("Missing a GameObject reference");
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
        if (panelPlayers) {
            foreach (Transform child in panelPlayers) {
                GameObject.Destroy(child.gameObject);
            }
            if (panelPlayerNamePrefab) {
                List<Player> oldLobbyPlayerlist = currentLobbyPlayerList;
                currentLobbyPlayerList = LobbyManager.instance.GetCurrentLobbyPlayerList();
                foreach (Player player in currentLobbyPlayerList) {
                    GameObject newPlayerNamePanel = Instantiate(panelPlayerNamePrefab, panelPlayers);
                    newPlayerNamePanel.GetComponentInChildren<TextMeshProUGUI>().text = player.Data["PlayerName"].Value;
                }
                PlayAudioFeedBackForPlayersJoiningAndLeavingLobby(oldLobbyPlayerlist, currentLobbyPlayerList);
            }
        }
        if (currentLobbyCode) {
            currentLobbyCode.text = LobbyManager.instance.GetCurrentLobbyCode();
        }
    }

    private void PlayAudioFeedBackForPlayersJoiningAndLeavingLobby(List<Player> oldLobbyPlayerList, List<Player> currentLobbyPlayerList) {
        if (oldLobbyPlayerList.Count > 0) {
            //Checks if at least one player joined
            foreach (Player player in currentLobbyPlayerList) {
                if (!oldLobbyPlayerList.Exists(x => x.Id == player.Id)) {
                    AudioManager.Instance.PlayClip(AudioManager.Instance.playerJoiningClip);
                    break;
                }
            }
            //Checks if at least one player left
            foreach (Player player in oldLobbyPlayerList) {
                if (!currentLobbyPlayerList.Exists(x => x.Id == player.Id)) {
                    AudioManager.Instance.PlayClip(AudioManager.Instance.playerLeavingClip);
                    break;
                }
            }
        }
    }
}
