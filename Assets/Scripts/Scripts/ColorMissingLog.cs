using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorMissingLog : MonoBehaviour
{
    [SerializeField] private TMP_Text log;

    public void LogProcess(Dictionary<ColorType, int> pixels, List<Box> boxes) {
        pixels ??= new Dictionary<ColorType, int>();
        boxes ??= new List<Box>();

        Dictionary<ColorType, int> boxAmounts = new();

        foreach (Box box in boxes) {
            if (box == null) continue;

            boxAmounts.TryGetValue(box.Color, out int currentAmount);
            boxAmounts[box.Color] = currentAmount + box.Amount;
        }

        HashSet<ColorType> colors = new(pixels.Keys);
        colors.UnionWith(boxAmounts.Keys);

        Dictionary<ColorType, int> colorLog = new();
        foreach (ColorType color in colors) {
            pixels.TryGetValue(color, out int pixelAmount);
            boxAmounts.TryGetValue(color, out int boxAmount);

            int difference = pixelAmount - boxAmount;
            if (difference != 0) {
                colorLog[color] = difference;
            }
        }


        LogColors(colorLog);
    }

    private void LogColors(Dictionary<ColorType, int> log) {
        this.log.text = "";
        foreach (var (key, value) in log) {
            string amount = value < 0 ? $"-x{Mathf.Abs(value)}" : $"x{value}";
            this.log.text += $"\n{amount}  -  {key}";
        }
    }
}
