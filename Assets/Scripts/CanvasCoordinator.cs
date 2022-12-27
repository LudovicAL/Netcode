using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CanvasCoordinator : MonoBehaviour {

    public static CanvasCoordinator instance { get; private set; }

    [Tooltip("List of all panels direct children of the Canvas")]
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
        ResetSelection();
    }

    private void ResetSelection() {
        foreach (GameObject panel in panelList) {
            if (panel.activeSelf) {
                foreach (Transform child in panel.transform) {
                    if (child.gameObject.activeSelf) {
                        Selectable selectable = child.GetComponentInChildren<Selectable>();
                        if (selectable) {
                            selectable.Select();
                            break;
                        }
                    }
                }
                break;
            }
        }
    }
}
