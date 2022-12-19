using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using WebSocketSharp;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

public class ExtendedButton : UnityEngine.UI.Button {
    [SerializeField]
    private TMP_InputField linkedInputField;
    //[SerializeField]
    //private float floatValue;
    [Multiline]
    public string tooltipText;

    //Highlights the linked InputField object
    public void HighlightLinkedInputField() {
        if (linkedInputField != null) {
            linkedInputField.Select();
            if (TweenManager.instance != null) {
                TweenManager.instance.TweenScale(linkedInputField.transform.parent, TweenManager.instance.tweenScaleDefault, null);
            } else {
                Debug.LogWarning("Your scene is missing a TweenManager");
            }
        } else {
            Debug.LogWarning("Missing a GameObject reference");
        }
    }

    //Shakes the current buttons sideways
    public void ShakeButtonSideways() {
        if (TweenManager.instance != null) {
            TweenManager.instance.TweenPosition(transform.parent, TweenManager.instance.tweenHorizontalPositionDefault, TweenManager.instance.tweenVerticalPositionDefault, null);
        } else {
            Debug.LogWarning("Your scene is missing a TweenManager");
        }
    }

    //Detects when the Cursor starts to pass over the GameObject
    public override void OnPointerEnter(PointerEventData pointerEventData) {
        base.OnPointerEnter(pointerEventData);
        if (!tooltipText.IsNullOrEmpty()) {
            TooltipManager.ShowTooltip(tooltipText);
        }
    }

    //Detects when Cursor leaves the GameObject
    public override void OnPointerExit(PointerEventData pointerEventData) {
        base.OnPointerExit(pointerEventData);
        TooltipManager.HideTooltip();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ExtendedButton))]
public class ExtendedButtonEditor : ButtonEditor {
    private SerializedProperty linkedInputFieldProperty;
    private SerializedProperty tooltipTextProperty;
    //private SerializedProperty floatValueProperty;

    protected override void OnEnable() {
        base.OnEnable();

        linkedInputFieldProperty = serializedObject.FindProperty("linkedInputField");
        tooltipTextProperty = serializedObject.FindProperty("tooltipText");
        //floatValueProperty = serializedObject.FindProperty("floatValue");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.ObjectField(linkedInputFieldProperty);
        EditorGUILayout.PropertyField(tooltipTextProperty);
        //EditorGUILayout.PropertyField(floatValueProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
