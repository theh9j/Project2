using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CoinVisualiser : MonoBehaviour
{
    [SerializeField] private RectTransform coinBackground;
    [SerializeField] private TMP_Text coin;

    [SerializeField] private int widthOffset;
    [SerializeField] private float initialWidth = 200f;

    void Awake() {
        coinBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialWidth);
    }

    public void SetCoin(int? amount) {
        int value = amount ?? 0;
        string text = value.ToString("N0", new CultureInfo("de-DE"));
        if (value.ToString().Length >= 7) text = "-";

        coin.text = text;
        Resize(coin.text.Length);
    }

    private void Resize(int len) {
        int extraDigits = Mathf.Max(0, len - 4);
        float width = initialWidth + widthOffset * extraDigits;

        coinBackground.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width);
    }
}
