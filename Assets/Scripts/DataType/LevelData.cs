using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public List<PixelData> pixels;
    public List<BoxData> boxes;
}

[System.Serializable]
public class BoxData {
    public int id;

    public ColorType color;
    public int amount;

    public bool mysterious;

    public int linkId = -1;
    public bool IsLink => linkId > 0;

}

[System.Serializable]
public class PixelData {
    public int id;

    public ColorType color;

    public bool mysterious;
}