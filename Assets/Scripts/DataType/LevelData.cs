using System.Collections.Generic;

[System.Serializable]
public class LevelSaveData {
    public string levelId;
    public RewardSaveData rewards = new();

    public int boxColumns = 1;
    public int waitingSlotCount;
    public List<BoxSaveData> boxes = new();
    public List<LevelKeySaveData> keys = new();

    public List<PixelSaveData> pixels = new();
}

[System.Serializable]
public class RewardSaveData {
    public int coins;

    public int bAdd;
    public int bCherry;
    public int bClearer;
}

[System.Serializable]
public class PixelSaveData {
    public int x;
    public int y;
    public ColorType color;
}

[System.Serializable]
public class BoxSaveData {
    public int id;
    public ColorType color;
    public int amount;
    public int column;
    public int row;

    public bool mysterious;
    public int linkId = -1; //default if no links
    public int lockId = -1;
}

[System.Serializable]
public class LevelKeySaveData {
    public int id;
    public int x;
    public int y;
    public int lockId = -1;
}