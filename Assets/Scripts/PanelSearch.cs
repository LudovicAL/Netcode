using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelSearch : MonoBehaviour {

    private float refreshTimer;
    private bool resetSelectionOnNextUpdate = true;
    private static LobbyComparer lobbyComparer = new LobbyComparer();
    private Dictionary<Lobby, GameObject> canvasLobbyDictionnary = new Dictionary<Lobby, GameObject>(lobbyComparer);

    [Header("PANEL PARAMETERS")]
    [Tooltip("The default maximum number of players in a lobby")]
    [SerializeField]
    private int defaultMaxNumberOfPlayers;
    [Tooltip("The maximum number of results to a query to get available lobbies")]
    [SerializeField]
    private int maxNumberOfResults;
    [Tooltip("The delay in seconds between automatic updates of the available lobbies list")]
    [SerializeField]
    private float delayBetweenLobbiesListAutomaticRefresh;

    [Header("CREATE LOBBY")]
    [SerializeField]
    [IfNullTryFetch("InputField LobbyName")]
    private TMP_InputField lobbyNameInputField;
    [SerializeField]
    [IfNullTryFetch("Toggle PrivateLobby")]
    private Toggle privateLobbyToggle;
    [SerializeField]
    [IfNullTryFetch("Button CreateLobby")]
    private ExtendedButton createLobbyButton;

    [Header("JOIN LOBBY BY CODE")]
    [SerializeField]
    [IfNullTryFetch("Button JoinByCode")]
    private TMP_InputField lobbyCodeInputField;
    [SerializeField]
    [IfNullTryFetch("InputField LobbyCode")]
    private ExtendedButton joinLobbyByCodeButton;

    [Header("JOIN AVAILABLE LOBBY")]
    [SerializeField]
    [IfNullTryFetch("Panel ListOfAvailableLobbies")]
    private Transform availableLobbiesPanel;
    [SerializeField]
    private GameObject panelAvailableLobbyPrefab;
    [SerializeField]
    [IfNullTryFetch("Button RefreshAvailableLobbies")]
    private Button refreshAvailableLobbiesButton;

    [Header("LOGOUT")]
    [SerializeField]
    [IfNullTryFetch("Button Logout")]
    private ExtendedButton logoutButton;


    // Start is called before the first frame update
    void Start() {
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
    }

    // Update is called once per frame
    void Update() {
        if (AuthenticationService.Instance.IsSignedIn) {
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
        lobbyNameInputField.text = "";
        lobbyCodeInputField.text = "";
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
        HttpReturnCode httpReturnCode = await LobbyManager.instance.CreateLobby(lobbyName, Mathf.Max(defaultMaxNumberOfPlayers, 1), createLobbyOptions);
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Joined");
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
            CanvasCoordinator.instance.SwitchPanel("Panel Joined");
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
            CanvasCoordinator.instance.SwitchPanel("Panel Joined");
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
            GameObject panelAvailableLobbyButton = null;
            //UPDATE LOBBIES THAT REMAINED
            if (canvasLobbyList.Contains(lobby, lobbyComparer)) {
                panelAvailableLobby = canvasLobbyDictionnary[lobby];
                panelAvailableLobbyButton = panelAvailableLobby.transform.Find("Button JoinAvailableLobby").gameObject;
                //ADD LOBBIES THAT APPEARED
            } else {
                panelAvailableLobby = Instantiate(panelAvailableLobbyPrefab, availableLobbiesPanel);
                panelAvailableLobby.name = lobby.Name;
                canvasLobbyDictionnary.Add(lobby, panelAvailableLobby);
                panelAvailableLobbyButton = panelAvailableLobby.transform.Find("Button JoinAvailableLobby").gameObject;
                string lobbyId = lobby.Id;
                panelAvailableLobbyButton.GetComponentInChildren<ExtendedButton>().onClick.AddListener(delegate {
                    JoinLobbyById(lobbyId, panelAvailableLobbyButton.GetComponentInChildren<ExtendedButton>());
                });
            }
            panelAvailableLobbyButton.GetComponentInChildren<ExtendedButton>().interactable = (lobby.AvailableSlots > 0);
            panelAvailableLobbyButton.GetComponent<UITooltip>().enabled = (lobby.AvailableSlots == 0);
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
}

public class LobbyComparer : IEqualityComparer<Lobby> {
    public bool Equals(Lobby x, Lobby y) {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(Lobby obj) {
        return obj.Id.GetHashCode();
    }
}
