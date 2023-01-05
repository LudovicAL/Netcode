using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelAuthentication : MonoBehaviour {

    [Header("AUTHENTICATE")]
    [SerializeField]
    [IfNullTryFetch("InputField")]
    private TMP_InputField playerNameInputField;
    [SerializeField]
    [IfNullTryFetch("ExtendedButton")]
    private ExtendedButton authenticateButton;
    private bool resetSelectionOnNextUpdate = true;

    private const string LAST_USER_NAME = "LastUserName";

    private void Awake() {
        IfNullTryFetchAttribute.Init(this);
    }

    void Start() {
        if (playerNameInputField) {
            playerNameInputField.text = PlayerPrefs.GetString(LAST_USER_NAME, "");
        }
        if (authenticateButton) {
            authenticateButton.onClick.AddListener(() => {
                Authenticate();
            });
        }
    }

    void Update() {
        if (resetSelectionOnNextUpdate) {
            ResetSelection();
        }
    }

    void OnEnable() {
        playerNameInputField.text = "";
        resetSelectionOnNextUpdate = true;
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

    //Requests the player's authentication
    private async void Authenticate() {
        if (!playerNameInputField || !authenticateButton) {
            Debug.LogWarning("Missing a GameObject reference");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        string playerName = playerNameInputField.text;
        if (string.IsNullOrWhiteSpace(playerName)) {
            Debug.LogWarning("Enter a player name first");
            authenticateButton.HighlightLinkedInputField();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            return;
        }
        PlayerPrefs.SetString(LAST_USER_NAME, playerName);
        PlayerPrefs.Save();
        HttpReturnCode httpReturnCode = await AuthenticationManager.instance.Authenticate(playerName);
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Search");
            AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
        } else {
            authenticateButton.ShakeButtonSideways();
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }
}
