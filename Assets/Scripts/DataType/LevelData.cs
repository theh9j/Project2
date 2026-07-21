using System.Collections.Generic;

[System.Serializable]
public class LevelSaveData {
    public int version = 1;
    public string levelId;
    public MapSaveData map = new();
    public int boxColumns = 1;
    public int waitingSlotCount;
    public List<BoxSaveData> boxes = new();
    public List<LevelKeySaveData> keys = new();
    public List<LevelLockSaveData> locks = new();
}

[System.Serializable]
public class MapSaveData {
    public int columns;
    public int rows;
    public ColorType defaultColor = ColorType.None;
    public List<PixelSaveData> pixels = new();
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
    public bool mysterious;
    public int column;
    public int row;

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

[System.Serializable]
public class LevelLockSaveData {
    public int id;
    public string type;
}
