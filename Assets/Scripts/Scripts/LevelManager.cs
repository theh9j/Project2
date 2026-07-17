using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{

    [SerializeField] private MapCoordination map;
    [SerializeField] private BoxManagementSystem boxMana;
    private const string path = "Levels/";

    private string GetLevelSave(int level) {
        return path + $"level_{level.ToString("D2")}";
    }


    public void LoadLevel() {

    }

    public void SaveLevel() {
#if !UNITY_EDITOR
        return;
#endif

        LevelData levelData = new();
        List<PixelView> pixelArt = map.GetMapLayout();
        List<Box> boxes = boxMana.BoxList;

        if (pixelArt == null) {
            WarningMessage.Instance?.Warn("CRIT | Map layout doesn't exist");
            return;
        }

        if (boxes == null) {
            WarningMessage.Instance?.Warn("CRIT | No box on the map");
            return;
        }

        levelData.pixels = new();
        for (int i = 0; i < pixelArt.Count; i++) {

            levelData.pixels[i].id = i;
            levelData.pixels[i].color = pixelArt[i].Color;
            levelData.pixels[i].mysterious = pixelArt[i].Mysterious;

        }

        levelData.boxes = new();
        for (int i = 0; i < boxes.Count; i++) {
            levelData.boxes[i].id = i;
            levelData.boxes[i].color = boxes[i].Color;
            levelData.boxes[i].amount = boxes[i].Amount;
            levelData.boxes[i].mysterious = boxes[i].Mysterious;

            if (boxes[i].Link != null) {

            }
        }
    }

}
