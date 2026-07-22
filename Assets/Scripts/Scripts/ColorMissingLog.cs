using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class ColorMissingLog : MonoBehaviour
{
    [SerializeField] private GameObject log;
    [SerializeField] private Transform parent;
    private List<GameObject> listOfText = new();

    public void LogProcess(Dictionary<ColorType, int> pixels, List<Box> boxes) {
        pixels ??= new Dictionary<ColorType, int>();
        boxes ??= new List<Box>();

        Dictionary<ColorType, int> boxAmounts = new();

        foreach (Box box in boxes) {
            if (box == null) continue;
            if (!ShouldLogColor(box.Color)) continue;
            if (box.Amount <= 0) continue;

            boxAmounts.TryGetValue(box.Color, out int currentAmount);
            boxAmounts[box.Color] = currentAmount + box.Amount;
        }

        HashSet<ColorType> colors = new(
            pixels.Keys.Where(ShouldLogColor));
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
        if (this.log == null) return;

        foreach (GameObject text in listOfText) Destroy(text);

        foreach (var (key, value) in log.OrderBy(entry => entry.Key.ToString())) {
            GameObject newLog = Instantiate(this.log, parent);
            listOfText.Add(newLog);

            if (!newLog.TryGetComponent<TMP_Text>(out TMP_Text text)) {
                WarningMessage.Instance?.Warn("ERR | Can't output diff");
                continue;
            }

            if (value > 0) text.color = Color.limeGreen;
            else if (value < 0) text.color = Color.red;

            string amount = value < 0 ? $"-x{Mathf.Abs(value)}" : $"x{value}";
            text.text = $"{amount}  -  {key}";
        }

    }

    private bool ShouldLogColor(ColorType color) {
        return color != ColorType.None &&
               color != ColorType.Invalid &&
               color != ColorType.Unknown;
    }
}
