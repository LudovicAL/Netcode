using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AuthenticationCanvas : MonoBehaviour {

    [Header("AUTHENTICATE")]
    [SerializeField]
    private TMP_InputField playerNameInputField;
    [SerializeField]
    private ExtendedButton authenticateButton;
    private bool resetSelectionOnNextUpdate = true;

    void Start() {
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
        if (playerNameInputField && authenticateButton) {
            string playerName = playerNameInputField.text;
            if (playerName.Length > 0) {
                HttpReturnCode httpReturnCode = await AuthenticationManager.instance.Authenticate(playerName);
                httpReturnCode.Log();
                if (httpReturnCode.IsSuccess()) {
                    CanvasCoordinator.instance.SwitchPanel("Panel Lobby");
                    AudioManager.Instance.PlayClip(AudioManager.Instance.menuClickClip);
                } else {
                    authenticateButton.ShakeButtonSideways();
                    AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
                }
            } else {
                Debug.LogWarning("Enter a player name first");
                authenticateButton.HighlightLinkedInputField();
                AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
            AudioManager.Instance.PlayClip(AudioManager.Instance.warningClip);
        }
    }
}
