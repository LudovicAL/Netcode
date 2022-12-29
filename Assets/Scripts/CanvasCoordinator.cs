using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CanvasCoordinator : MonoBehaviour {

    public static CanvasCoordinator instance { get; private set; }

    [SerializeField]
    private List<GameObject> panelList;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    void Start() {
        SwitchPanel(panelList.First<GameObject>().name);
    }

    void Update() {

    }

    //Switches to the specified UI Panel
    public void SwitchPanel(string nameOfPanelToDisplay) {
        foreach (GameObject panel in panelList) {
            panel.SetActive(panel.name == nameOfPanelToDisplay);
        }
    }
}
