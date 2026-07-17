using UnityEngine;

public enum EditorState {
    Basic,
    Drawing,
}

public enum BoxAnimationState {
    Enable,
    Open,
    Close,
    Killed
}

public enum ColorType {
    None,
    Invalid,
    Unknown,

    Red,
    Green,
    Blue,

    Yellow,
    Orange,
    Brown,
    Emerald,
    LightBlue,

    Pink,
    Magenta,
    Purple,

    White,
    Beige,
    Gray,
    Black,

}

public enum InputType {
    Computer,
    Mobile
}

public enum WaitingSlotState {
    Open,
    Occupied,
    Locked
}

public enum TraySide {
    Bottom,
    Left,
    Right,
    Top
}