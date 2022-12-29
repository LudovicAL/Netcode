using System.Text.RegularExpressions;
using UnityEngine;

public class IfNullTryFetchAttribute : PropertyAttribute {

    private string targetName;

    public IfNullTryFetchAttribute(string targetName = "") {
        this.targetName = targetName;
    }

    public static void Init(MonoBehaviour monoBehavior) {
        System.Reflection.FieldInfo[] fieldInfos = monoBehavior.GetType().GetFields(
            System.Reflection.BindingFlags.DeclaredOnly
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);
        foreach (System.Reflection.FieldInfo fieldInfo in fieldInfos) {
            object fieldInfoValue = fieldInfo.GetValue(monoBehavior);
            if (fieldInfoValue != null && fieldInfoValue.ToString() != "null") {
                continue;
            }
            IfNullTryFetchAttribute[] attributes = fieldInfo.GetCustomAttributes(typeof(IfNullTryFetchAttribute), false) as IfNullTryFetchAttribute[];
            if (attributes == null || attributes.Length == 0) {
                continue;
            }
            IfNullTryFetchAttribute attribute = attributes[0];
            GameObject targetGameObject = null;
            string finalizedTargetName = BuildTargetName(attribute, fieldInfo.Name);
            //Search first with the name as a path from the passed MonoBehavior
            Transform targetTransform = monoBehavior.transform.Find(finalizedTargetName);
            if (targetTransform) {
                targetGameObject = targetTransform.gameObject;
            }
            //Search second in the children of the passed MonoBehavior
            if (!targetGameObject) {
                targetGameObject = GetChildWithName(monoBehavior.transform, finalizedTargetName);
            }
            //Search third everywhere in the scene
            if (!targetGameObject) {
                targetGameObject = GameObject.Find(finalizedTargetName);
            }
            if (!targetGameObject) {
                continue;
            }
            object value;
            if (typeof(GameObject).IsAssignableFrom(fieldInfo.FieldType)) {
                value = targetGameObject;
            } else {
                value = targetGameObject.GetComponentInChildren(fieldInfo.FieldType);
            }
            if (value != null) {
                fieldInfo.SetValue(monoBehavior, value);
                Debug.LogWarning("Assigning a value at runtime to variable '" + fieldInfo.Name + "' in class '" + fieldInfo.ReflectedType.Name + "'.");
            }
        }
    }

    private static string BuildTargetName(IfNullTryFetchAttribute attribute, string fieldName) {
        if (string.IsNullOrEmpty(attribute.targetName)) {
            return Regex.Replace(fieldName, "^m_", "");
        } else {
            return attribute.targetName;
        }
    }

    private static GameObject GetChildWithName(Transform parentTransform, string name) {
        for (int i = 0, max = parentTransform.childCount; i < max; i++) {
            Transform childTransform = parentTransform.GetChild(i);
            if (childTransform.name == name) {
                return childTransform.gameObject;
            } else {
                GameObject GrandChildGameObject = GetChildWithName(childTransform, name);
                if (GrandChildGameObject) {
                    return GrandChildGameObject;
                }
            }
        }
        return null;
    }
}