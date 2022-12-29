using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesManager : MonoBehaviour {
    public static UnityServicesManager instance { get; private set; }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    async void Start() {
        (await InitializeUnityServices()).Log();
    }

    void Update() {

    }

    //Initializes UnityServices
    public async Task<HttpReturnCode> InitializeUnityServices() {
        switch (UnityServices.State) {
            case ServicesInitializationState.Initialized:
                return new HttpReturnCode(200, "UnityServices already initialized.");
            case ServicesInitializationState.Initializing:
                return new HttpReturnCode(400, "UnityServices are initializing.");
            default:
                try {
                    await UnityServices.InitializeAsync();
                    return new HttpReturnCode(200, "UnityServices initialized successfully.");
                } catch (Exception e) {
                    return new HttpReturnCode(e);
                }
        }
    }

    //Returns true if UnityServices are initialized
    public bool IsInitialized() {
        return (UnityServices.State == ServicesInitializationState.Initialized);
    }
}
