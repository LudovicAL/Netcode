using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetcodeCanvas : MonoBehaviour {

    [SerializeField]
    private Button hostButton;
    [SerializeField]
    private Button serverButton;
    [SerializeField]
    private Button clientButton;
    [SerializeField]
    private Button stopButton;

    // Start is called before the first frame update
    void Start() {
        if (hostButton) {
            hostButton.onClick.AddListener(() => {
                NetworkManager.Singleton.StartHost();
            });
        }
        if (serverButton) {
            serverButton.onClick.AddListener(() => {
                NetworkManager.Singleton.StartServer();
            });
        }
        if (clientButton) {
            clientButton.onClick.AddListener(() => {
                NetworkManager.Singleton.StartClient();
            });
        }
    }

    // Update is called once per frame
    void Update() {

    }
}
