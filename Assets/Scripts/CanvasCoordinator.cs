using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    // Start is called before the first frame update
    void Start() {
        SwitchPanel(panelList.First<GameObject>().name);
    }

    // Update is called once per frame
    void Update() {

    }

    //Switches to the specified UI Panel
    public void SwitchPanel(string nameOfPanelToDisplay) {
        foreach (GameObject panel in panelList) {
            panel.SetActive(panel.name == nameOfPanelToDisplay);
        }
    }
}
