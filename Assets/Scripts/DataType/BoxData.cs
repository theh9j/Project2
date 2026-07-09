using UnityEngine;

[System.Serializable]
public class BoxData
{
    public ColorType color;
    public int amount;

    public int row;
    public bool gate;
    private bool hidden;
    public BoxData link;

    //Properties
    public bool FrontRow => row == 0;
    public bool HiddenProp {
        get => hidden;
        set => hidden = !FrontRow && value;
    }

}
