using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour {
    public static AuthenticationManager instance { get; private set; }

    [Tooltip("If true, the player will be asked to chose a username. If false, the player will authenticate automatically without a username.")]
    [SerializeField]
    private bool manualAuthentication;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start() {
        SetupEvents();
        if (!manualAuthentication) {
            StartCoroutine(AutomaticAuthenticationAfterWaitForUnityServiceManagerInitialization());
        }
    }

    // Update is called once per frame
    void Update() {

    }

    private IEnumerator AutomaticAuthenticationAfterWaitForUnityServiceManagerInitialization() {
        while (!UnityServicesManager.instance.IsInitialized()) {
            yield return false;
        }
        AutomaticAuthentication();
    }

    private async void AutomaticAuthentication() {
        HttpReturnCode httpReturnCode = await Authenticate("");
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Lobby");
        }
    }

    //Authenticates the player (returns true in case of a success)
    public async Task<HttpReturnCode> Authenticate(string playerName) {
        HttpReturnCode httpReturnCode = await UnityServicesManager.instance.InitializeUnityServices();
        if (httpReturnCode.IsSuccess()) {
            if (playerName.Length > 0) {
                AuthenticationService.Instance.SwitchProfile(playerName);
            }
            try {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                httpReturnCode = new HttpReturnCode(200, "Authenticated successfully");
            } catch (Exception e) {
                httpReturnCode = new HttpReturnCode(e);
            }
        }
        return httpReturnCode;
    }

    public void Logout() {
        try {
            AuthenticationService.Instance.SignOut();
            CanvasCoordinator.instance.SwitchPanel("Panel Authentication");
        } catch (Exception e) {
            Debug.Log("An error occured while signing out: " + e.ToString());
        }
    }

    private void SetupEvents() {
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Player " + AuthenticationService.Instance.PlayerId + " signed in successfully");
        };

        AuthenticationService.Instance.SignInFailed += (e) => {
            Debug.LogWarning("An error occured while authenticating:\n" + e.ToString());
        };

        AuthenticationService.Instance.SignedOut += () => {
            Debug.Log("Player " + AuthenticationService.Instance.PlayerId + " signed out successfully");
        };

        AuthenticationService.Instance.Expired += () => {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }

    public bool IsManualAuthenticationOn() {
        return manualAuthentication;
    }
}
