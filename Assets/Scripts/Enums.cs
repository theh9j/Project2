using UnityEngine;

public enum EditorState {
    Basic,
    Drawing
}

public enum BoxAnimationState {
    Enable,
    Open,
    Close,
    Killed
}

public enum ColorType {
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

    None,
    Invalid
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
