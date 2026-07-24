using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class CoinVisualiser : MonoBehaviour
{
    private static readonly CultureInfo CoinCulture = new("de-DE");

    [SerializeField] private RectTransform coinBackground;
    [SerializeField] private TMP_Text coin;

    [Header("Coin Background Extension")]
    [SerializeField] private int widthOffset;
    [SerializeField] private float initialWidth = 200f;

    [Header("Coin Add Settings")]
    [SerializeField] private float coinAddDuration = 5f;
    [SerializeField] private int maxCoinAddSteps = 120;

    void Awake() {
        coinBackground?.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialWidth);
    }

    public IEnumerator CoinAdd(int amount) {
        if (amount <= 0) {
            yield break;
        }

        int startCoins = (SaveManager.Instance?.coins ?? 0) - amount;
        int targetCoins = startCoins + amount;
        int steps = Mathf.Clamp(amount, 1, maxCoinAddSteps);
        float stepDuration = coinAddDuration / steps;

        for (int step = 1; step <= steps; step++) {
            float progress = (float)step / steps;
            float easedProgress = EaseOutCubic(progress);
            int displayedCoins = Mathf.RoundToInt(Mathf.Lerp(startCoins, targetCoins, easedProgress));

            SetCoin(displayedCoins);
            yield return new WaitForSeconds(stepDuration);
        }

        SetCoin(targetCoins);
    }

    private float EaseOutCubic(float value) {
        value = Mathf.Clamp01(value);
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    public void SetCoin(int? amount) {
        int value = amount ?? 0;

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
