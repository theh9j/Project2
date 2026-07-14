using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Scriptable Objects/ColorData")]
public class ColorData : ScriptableObject
{
    public const string DefaultResourcePath = "MainColorData";

    [SerializeField] private List<ColorSingleData> data = new();

    public static ColorData LoadDefault() {
        return Resources.Load<ColorData>(DefaultResourcePath);
    }

    public Color GetColor(ColorType type) {
        if (data == null) return Color.black;

        foreach (ColorSingleData colorData in data) {
            if (colorData.colorID == type) return colorData.color;
        }
        return Color.black;
    }
}

[System.Serializable]
public class ColorSingleData {
    public ColorType colorID;
    public Color color;
}
