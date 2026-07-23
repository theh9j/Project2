using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("READ DESCRIPTION BEFORE TICK"), Tooltip("In order for this to work, Rebuild on start must be disabled on Map")]
    [SerializeField] private bool load;

    [Header("References")]
    [SerializeField] private MapCoordination map;
    [SerializeField] private BoxManagementSystem boxMana;
    [SerializeField] private WaitingSlotsManagementSystem waitMana;

    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Ants ants;

    //VISUAL
    [Header("UI References"), Tooltip("This section includes coins, boosters visualisation")]
    [SerializeField] private TextCommon levelText;
    [SerializeField] private CoinVisualiser coinVisualiser;
    [SerializeField] private GameEndVisualiser gameEndVisualiser;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(Instance);
            return;
        }

        Instance = this;

        if (ants == null) ants = FindAnyObjectByType<Ants>();
    }

    void Start() {
        ants.NoAnts += CheckForGameCompletion;
        if (load) levelManager.LoadLevel(SaveManager.Instance?.level);
        else map.RebuildGrid();
        coinVisualiser.SetCoin(SaveManager.Instance?.coins);
        levelText.SetText($"Level {SaveManager.Instance?.level.ToString()}");

        gameEndVisualiser.Advance += () => {
            levelManager.LoadLevel(SaveManager.Instance?.level);
            levelText.SetText($"Level {SaveManager.Instance?.level.ToString()}");
        };
    }

    private void CheckForGameCompletion() {
        if (map.GetMapLayout()
            .Any(p => p != null 
            && p.Color != ColorType.None 
            && p.Color != ColorType.Invalid
            && p.Color != ColorType.Unknown)) {
            CheckForGameFail();
            return;
        }

        if (SaveManager.Instance == null) levelManager.LoadLevel();

        SaveManager.Instance.level += 1;

        SaveManager.Instance.coins += levelManager.Data.rewards.coins;

        SaveManager.Instance.bAdd += levelManager.Data.rewards.bAdd;
        SaveManager.Instance.bCherry += levelManager.Data.rewards.bCherry;
        SaveManager.Instance.bClearer += levelManager.Data.rewards.bClearer;

        gameEndVisualiser.Win(levelManager.Data);
    }

    private void CheckForGameFail() {
        int freeSlots = waitMana.PlateCount - waitMana.ActivePlateCount;

        bool hasMoveableBox = boxMana.BoxList.Any(box =>
            box != null &&
            box.Interactable &&
            (box.Link == null ? freeSlots >= 1 : freeSlots >= 2));

        if (hasMoveableBox) return;

        bool hasReservedPixels = map.GetMapLayout().Any(pixel =>
            pixel != null && pixel.IsReserved);

        if (hasReservedPixels) return;

        Debug.Log("Lost");
    }

}
