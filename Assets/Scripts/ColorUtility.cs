using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ColorUtility {

    public static Dictionary<string, Color> colorDictionary = new Dictionary<string, Color>(){
        {"blue", Color.blue},
        {"cyan", Color.cyan},
        {"green", Color.green},
        {"grey", Color.grey},
        {"magenta", Color.magenta},
        {"red", Color.red},
        {"yellow", Color.yellow},
        {"pink", new Color(1.0f, 0.753f, 0.796f, 1.0f)},
        {"orange", new Color(1.0f, 0.64f, 0.0f, 1.0f)},
        {"purple", new Color(0.627f, 0.125f, 0.941f, 1.0f)},
        {"brown", new Color(0.39f, 0.1961f, 0.1255f, 1.0f)}
    };

    public static string GetRandomColorKey() {
        return colorDictionary.Keys.ElementAtOrDefault(Random.Range(0, colorDictionary.Count - 1));
    }

    public static string GetNextColorKey(string currentColorKey) {
        List<string> listOfColorKeys = colorDictionary.Keys.ToList();
        int index = listOfColorKeys.IndexOf(currentColorKey);
        if (index == -1) {
            return GetRandomColorKey();
        }
        if (index < (listOfColorKeys.Count - 1)) {
            return colorDictionary.Keys.ElementAtOrDefault(index + 1);
        } else {
            return colorDictionary.Keys.ElementAtOrDefault(0);
        }
    }
}
