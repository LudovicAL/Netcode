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

    private bool eventSetupCompleted = false;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    void Start() {
        StartCoroutine(WaitForUnityServiceManagerInitialization());
    }

    void Update() {

    }

    //Waits for the UnityServiceManager to initialize before launching other methods
    private IEnumerator WaitForUnityServiceManagerInitialization() {
        while (!UnityServicesManager.instance.IsInitialized()) {
            yield return null;
        }
        SetupEvents();
        if (!manualAuthentication) {
            AutomaticAuthentication();
        }
    }

    //Authenticates the player without a username
    private async void AutomaticAuthentication() {
        HttpReturnCode httpReturnCode = await Authenticate("");
        httpReturnCode.Log();
        if (httpReturnCode.IsSuccess()) {
            CanvasCoordinator.instance.SwitchPanel("Panel Lobby");
        }
    }

    //Authenticates the player with a username
    public async Task<HttpReturnCode> Authenticate(string playerName) {
        HttpReturnCode httpReturnCode = await UnityServicesManager.instance.InitializeUnityServices();
        if (!httpReturnCode.IsSuccess()) {
            return httpReturnCode;
        }
        SetupEvents();
        try {
            if (!string.IsNullOrWhiteSpace(playerName)) {
                AuthenticationService.Instance.SwitchProfile(playerName);
            }
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        } catch (Exception e) {
            return new HttpReturnCode(e);
        }
        return new HttpReturnCode(200, "Authenticated successfully");
    }

    //Logs out, returns true in case of a success
    public bool Logout() {
        try {
            AuthenticationService.Instance.SignOut();
            return true;
        } catch (Exception e) {
            Debug.Log("An error occured while signing out: " + e.ToString());
            return false;
        }
    }

    //Setups the AuthenticationService's events
    private void SetupEvents() {
        if (!eventSetupCompleted) {
            try {
                AuthenticationService.Instance.SignedIn += () => {
                    Debug.Log("Player signed in successfully");
                };

                AuthenticationService.Instance.SignInFailed += (e) => {
                    Debug.LogWarning("An error occured while authenticating:\n" + e.ToString());
                };

                AuthenticationService.Instance.SignedOut += () => {
                    Debug.Log("Player signed out successfully");
                };

                AuthenticationService.Instance.Expired += () => {
                    Debug.Log("Player session could not be refreshed and expired.");
                };
            } catch {
                Debug.LogWarning("An error occured during the events setup phase.");
                return;
            }
            Debug.Log("Events setup complete");
            eventSetupCompleted = true;
        }
    }

    //Returns the value of variable manualAuthentication
    public bool IsManualAuthenticationOn() {
        return manualAuthentication;
    }
}
