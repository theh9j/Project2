using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Scriptable Objects/ColorData")]
public class ColorData : ScriptableObject
{
    public List<ColorSingleData> data;
}

[System.Serializable]
public class ColorSingleData {
    public ColorType colorID;
    public Color color;
}