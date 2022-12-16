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
        if (UnityServices.State == ServicesInitializationState.Uninitialized) {
            try {
                await UnityServices.InitializeAsync();
            } catch (Exception e) {
                return new HttpReturnCode(e);
            }
        } else if (UnityServices.State == ServicesInitializationState.Initializing) {
            return new HttpReturnCode(400, "UnityServices are initializing.");
        }
        return new HttpReturnCode(200, "Initialized UnityServices successfully.");
    }

    //Returns true if UnityServices are initialized
    public bool IsInitialized() {
        return (UnityServices.State == ServicesInitializationState.Initialized);
    }
}
