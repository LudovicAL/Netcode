using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticationCanvas : MonoBehaviour {

    [Header("AUTHENTICATE")]
    [SerializeField]
    private TMP_InputField playerNameInputField;
    [SerializeField]
    private Button authenticateButton;

    // Start is called before the first frame update
    void Start() {
        if (authenticateButton) {
            authenticateButton.onClick.AddListener(() => {
                Authenticate();
            });
        }
    }

    // Update is called once per frame
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
                }
            } else {
                Debug.LogWarning("Enter a player name first");
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
        }
    }
}
