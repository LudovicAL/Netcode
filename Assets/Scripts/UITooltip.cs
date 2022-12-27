using UnityEngine;
using UnityEngine.EventSystems;
using WebSocketSharp;

public class UITooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    [Multiline]
    public string tooltipText;

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        if (!tooltipText.IsNullOrEmpty()) {
            TooltipManager.ShowTooltip(tooltipText);
        }
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        TooltipManager.HideTooltip();
    }
}
