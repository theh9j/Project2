using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorMissingLog : MonoBehaviour
{
    [SerializeField] private TMP_Text log;

    public void LogColors(Dictionary<ColorType, int> log) {
        this.log.text = "";
        foreach (var (key, value) in log) {
            this.log.text += $"\nx{value}  -  {key}";
        }
    }
}
