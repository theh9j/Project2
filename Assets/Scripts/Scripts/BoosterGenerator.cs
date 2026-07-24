using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BoosterGenerator : MonoBehaviour
{
    [SerializeField] private GameManager gameMana;
    [SerializeField] private BoosterData data;
    [SerializeField] private GameObject boosterPrefab;
    private Dictionary<BoosterType, BoosterButton> boosterFuncs = new();

    void Awake() {
        BuildBoosters();
    }

    private void BuildBoosters() {
        boosterFuncs.Clear();

        foreach (BoosterSingleData boosterInfo in data.boosters) {
            GameObject boosterGO = Instantiate(boosterPrefab, transform);
            if (!boosterGO.TryGetComponent<BoosterButton>(out BoosterButton booster)) return;
            booster.SetPrice(boosterInfo.price);
            booster.SetIcon(boosterInfo.image);
            boosterFuncs.Add(boosterInfo.boost, booster);
        }

        BuildBoosterFunctions();
        Canvas.ForceUpdateCanvases();
    }

    private void BuildBoosterFunctions() {
        BuildAddBooster();
    }   

    private void BuildAddBooster() {
        BoosterButton booster = boosterFuncs[BoosterType.Add];

        booster.Init(
            () => {
                Debug.Log("Say hello");
            });
    }
}
