using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoosterData", menuName = "Scriptable Objects/BoosterData")]
public class BoosterData : ScriptableObject
{
    public List<BoosterSingleData> boosters;
}

[System.Serializable]
public class BoosterSingleData {
    public BoosterType boost;
    public Sprite image;
    public int price;
}
