using TMPro;
using UnityEngine;

public class AuthenticationCanvas : MonoBehaviour {

    [Header("AUTHENTICATE")]
    [SerializeField]
    private TMP_InputField playerNameInputField;
    [SerializeField]
    private ExtendedButton authenticateButton;

    void Start() {
        if (authenticateButton) {
            authenticateButton.onClick.AddListener(() => {
                Authenticate();
            });
        }
    }

    void Update() {

    }

    void OnEnable() {
        playerNameInputField.text = "";
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
                } else {
                    authenticateButton.ShakeButtonSideways();
                }
            } else {
                Debug.LogWarning("Enter a player name first");
                authenticateButton.HighlightLinkedInputField();
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
        }
    }
}
