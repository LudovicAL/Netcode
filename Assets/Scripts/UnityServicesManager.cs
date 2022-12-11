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

    // Start is called before the first frame update
    async void Start() {
        (await InitializeUnityServices()).Log();
    }

    // Update is called once per frame
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
        }
        return new HttpReturnCode(200, "Initialized UnityServices successfully.");
    }

    public bool IsInitialized() {
        return (UnityServices.State == ServicesInitializationState.Initialized);
    }
}
