using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {
    private const int CurrentVersion = 1;
    private const string ResourceFolder = "Levels";

    [SerializeField] private MapCoordination map;
    [SerializeField] private BoxManagementSystem boxMana;
    [Min(0)]
    [SerializeField] private int currentLevel;
    [Min(0)]
    [SerializeField] private int fallBackLevel = 0;
    private LevelSaveData levelData = new();
    public LevelSaveData Data => levelData;

    private string GetLevelId(int level) {
        return $"level_{level:D2}";
    }

    private string GetLevelResourcePath(int level) {
        return $"{ResourceFolder}/{GetLevelId(level)}";
    }

    public void LoadLevel(int? levelInput = null) {
        int level = levelInput ?? fallBackLevel;

        if (map == null || boxMana == null) {
            WarningMessage.Instance?.Warn("CRIT | LevelManager references are missing.");
            return;
        }

        TextAsset levelJson = Resources.Load<TextAsset>(GetLevelResourcePath(level));
        if (levelJson == null) {
            WarningMessage.Instance?.Warn($"ERR | Could not load {GetLevelResourcePath(level)}.");
            return;
        }

        levelData = JsonUtility.FromJson<LevelSaveData>(levelJson.text);
        if (levelData == null) {
            WarningMessage.Instance?.Warn("ERR | Level JSON is invalid.");
            return;
        }

        ApplyLevel(levelData);
        currentLevel = level;
    }

    public void SaveLevel() {
        SaveLevel(currentLevel);
    }

    public void SaveLevel(int level, 
        int? coins = null, 
        int? bAdd = null,
        int? bCherry = null,
        int? bClearer = null) {
#if !UNITY_EDITOR
        return;
#else
        if (map == null || boxMana == null) {
            WarningMessage.Instance?.Warn("CRIT | LevelManager references are missing.");
            return;
        }

        if (Ants.Instance?.GetAntCount > 0 || 
            WaitingSlotsManagementSystem.Instance?.ActivePlateCount > 0) {
            WarningMessage.Instance?.Warn($"ERR | Cannot save active level, clear boxes & ants");
            return;
        }

        levelData = BuildSaveData(level,
            coins ?? 0,
            bAdd ?? 0,
            bCherry ?? 0,
            bClearer ?? 0);

        string json = JsonUtility.ToJson(levelData, true);

        string directory = Path.Combine(Application.dataPath, "Resources", ResourceFolder);
        Directory.CreateDirectory(directory);

        string filePath = Path.Combine(directory, $"{GetLevelId(level)}.json");
        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();

        currentLevel = level;
        WarningMessage.Instance?.Warn($"Saved {GetLevelId(level)}.");
#endif
    }

    private LevelSaveData BuildSaveData(int level,
        int coins,
        int bAdd,
        int bCherry,
        int bClearer) {

        List<PixelView> pixelArt = map.GetMapLayout();
        List<Box> boxes = boxMana.BoxList;
        Dictionary<Box, int> boxIds = BuildBoxIds(boxes);

        levelData = new() {
            levelId = GetLevelId(level),
            boxColumns = boxMana.Columns,
            waitingSlotCount = WaitingSlotsManagementSystem.Instance == null ?
                5 : WaitingSlotsManagementSystem.Instance.PlateCount,

            rewards = new() {
                coins = coins,
                bAdd = bAdd,
                bCherry = bCherry,
                bClearer = bClearer
            },
        };


        foreach (PixelView pixel in pixelArt) {
            if (pixel == null) continue;

            Vector2Int grid = pixel.GridPosition;
            levelData.pixels.Add(new PixelSaveData {
                x = grid.x,
                y = grid.y,
                color = pixel.Color
            });
        }

        foreach (Box box in boxes) {
            if (box == null) continue;

            BoxSaveData boxData = new() {
                id = boxIds[box],
                color = box.Color,
                amount = box.Amount,
                mysterious = box.Mysterious,
                column = box.ColIndex,
                row = box.RowIndex,
                linkId = TryGetBoxId(boxIds, box.Link)
            };

            levelData.boxes.Add(boxData);
        }

        return levelData;
    }

    private void ApplyLevel(LevelSaveData levelData) {
        Ants.Instance?.KillAnts(a => a);
        boxMana.ClearBoxes();
        map.ApplySavedMap(levelData.pixels);

        if (WaitingSlotsManagementSystem.Instance == null) return;

        WaitingSlotsManagementSystem.Instance.ClearAllPlates();
        WaitingSlotsManagementSystem.Instance.PlateGenerate(levelData.waitingSlotCount);

        int neededColumns = Mathf.Max(1, levelData.boxColumns);
        foreach (BoxSaveData boxData in levelData.boxes) {
            neededColumns = Mathf.Max(neededColumns, boxData.column + 1);
        }

        boxMana.SetColumns(neededColumns);

        Dictionary<int, Box> loadedBoxes = new();
        foreach (BoxSaveData boxData in levelData.boxes) {
            Box box = boxMana.CreateBox(boxData.column, boxData.row);
            if (box == null) continue;

            box.ApplySavedState(
                boxData.color,
                boxData.amount,
                boxData.mysterious);

            loadedBoxes[boxData.id] = box;
        }

        RestoreLinks(levelData.boxes, loadedBoxes);
    }

    private Dictionary<Box, int> BuildBoxIds(List<Box> boxes) {
        Dictionary<Box, int> ids = new();

        for (int i = 0; i < boxes.Count; i++) {
            Box box = boxes[i];
            if (box == null || ids.ContainsKey(box)) continue;

            ids[box] = ids.Count;
        }

        return ids;
    }

    private int TryGetBoxId(Dictionary<Box, int> boxIds, Box box) {
        if (box == null) return -1;
        return boxIds.TryGetValue(box, out int id) ? id : -1;
    }

    private void RestoreLinks(
        List<BoxSaveData> boxData,
        Dictionary<int, Box> loadedBoxes) {
        HashSet<int> linkedBoxes = new();

        foreach (BoxSaveData data in boxData) {
            if (data.linkId < 0) continue;
            if (linkedBoxes.Contains(data.id)) continue;
            if (!loadedBoxes.TryGetValue(data.id, out Box box)) continue;
            if (!loadedBoxes.TryGetValue(data.linkId, out Box linkedBox)) continue;

            box.Link = linkedBox;
            linkedBox.Link = box;

            Link linkLine = box.CreateLinkLine();
            box.linkLine = linkLine;
            linkedBox.linkLine = linkLine;

            linkedBoxes.Add(data.id);
            linkedBoxes.Add(data.linkId);
        }
    }
}
