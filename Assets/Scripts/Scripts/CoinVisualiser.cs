using System.Globalization;
using TMPro;
using UnityEngine;

public class CoinVisualiser : MonoBehaviour
{
    private static readonly CultureInfo CoinCulture = new("de-DE");

    [SerializeField] private RectTransform coinBackground;
    [SerializeField] private TMP_Text coin;

    [SerializeField] private int widthOffset;
    [SerializeField] private float initialWidth = 200f;
    public int CurrentCoins { get; private set; }

    void Awake() {
        coinBackground?.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialWidth);
    }

    public void AddCoin(int amount) {
        SetCoin(CurrentCoins + amount);
    }

    public void SetCoin(int? amount) {
        int value = amount ?? 0;
        CurrentCoins = value;

        if (coin == null) return;

        string text = value.ToString("N0", CoinCulture);
        if (value.ToString().Length >= 7) text = "-";

        coin.text = text;
        Resize(coin.text.Length);
    }

    private void Resize(int len) {
        if (coinBackground == null) return;

        int extraDigits = Mathf.Max(0, len - 4);
        float width = initialWidth + widthOffset * extraDigits;

        coinBackground.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width);
    }
}
