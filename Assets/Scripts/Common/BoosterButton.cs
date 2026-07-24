using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = UnityEngine.Events.UnityAction;

public class BoosterButton : MonoBehaviour
{
    private static readonly CultureInfo CoinCulture = new("de-DE");
    [SerializeField] private Button button;

    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text price;
    [SerializeField] private TMP_Text amount;

    [SerializeField] private string basePriceText = "<sprite name=\"coin\"> ";
    [HideInInspector] private GameObject priceText;
    [HideInInspector] private GameObject amountText;
    private bool priority;

    void Awake() {
        priceText = price.transform.parent.gameObject;
        amountText = amount.transform.parent.gameObject;
    }

    public void SetIcon(Sprite icon) {
        this.icon.sprite = icon;
    }

    public void Init(Action boosterAction) {
        button.onClick.AddListener(boosterAction);
    }

    public void SetPrice(int price) {
        this.price.text = basePriceText + price.ToString("N0", CoinCulture);
    }

    public void SetAmount(int amount) {
        this.amount.text = amount >= 100 ? "-" : amount.ToString();
    }

    public bool Swap() {
        priceText.SetActive(priority);
        amountText.SetActive(!priority);
        priority = !priority;
        return priority;
    }

}
