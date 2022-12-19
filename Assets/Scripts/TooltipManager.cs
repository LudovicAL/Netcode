using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour {

    public static TooltipManager instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI tooltipText;
    [SerializeField]
    private RectTransform backgroundRectTransform;
    [SerializeField]
    private RectTransform canvasRectTransform;
    private Vector2 textPadding;
    private RectTransform parentRectTransform;


    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
        HideTooltip();
    }

    void Start() {
        parentRectTransform = GetComponent<RectTransform>();
        textPadding = tooltipText.GetComponent<RectTransform>().offsetMin * 2;
    }

    void Update() {
        //Adjust background size
        Vector2 textSize = tooltipText.GetRenderedValues(false);
        backgroundRectTransform.sizeDelta = textSize + textPadding;
        //Set tooltip position
        Vector2 anchoredPosition = Input.mousePosition / canvasRectTransform.localScale.x;
        if (anchoredPosition.x + backgroundRectTransform.rect.width > canvasRectTransform.rect.width) {
            anchoredPosition.x = canvasRectTransform.rect.width - backgroundRectTransform.rect.width;
        }
        if (anchoredPosition.y + backgroundRectTransform.rect.height > canvasRectTransform.rect.height) {
            anchoredPosition.y = canvasRectTransform.rect.height - backgroundRectTransform.rect.height;
        }
        parentRectTransform.anchoredPosition = anchoredPosition;
    }

    private void ShowToolTipPrivate(string text) {
        gameObject.SetActive(true);        
        tooltipText.SetText(text);
        tooltipText.ForceMeshUpdate();
    }

    private void HideTooltipPrivate() {
        gameObject.SetActive(false);
    }

    public static void ShowTooltip(string text) {
        instance.ShowToolTipPrivate(text);
    }

    public static void HideTooltip() {
        instance.HideTooltipPrivate();
    }
}
