using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("READ DESCRIPTION BEFORE TICK"), Tooltip("In order for this to work, Rebuild on start must be disabled on Map")]
    [SerializeField] private bool load;
    [SerializeField, Min(0f)] private float stateCheckDelay = .35f;

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
    private Coroutine stateCheckRoutine;
    private bool gameEnded;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(Instance);
            return;
        }

        Instance = this;

        if (ants == null) ants = FindAnyObjectByType<Ants>();
    }

    void Start() {
        if (ants != null) ants.NoAnts += QueueGameStateCheck;
        if (load) levelManager.LoadLevel(SaveManager.Instance?.level);
        else map.RebuildGrid();
        coinVisualiser.SetCoin(SaveManager.Instance?.coins);
        levelText.SetText($"Level {SaveManager.Instance?.level.ToString()}");

        gameEndVisualiser.LoadLevel += () => {
            gameEnded = false;
            levelManager.LoadLevel(SaveManager.Instance?.level);
            levelText.SetText($"Level {SaveManager.Instance?.level.ToString()}");
        };
    }

    private void OnDestroy() {
        if (ants != null) ants.NoAnts -= QueueGameStateCheck;
    }

    private void QueueGameStateCheck() {
        if (stateCheckRoutine != null) {
            StopCoroutine(stateCheckRoutine);
        }

        stateCheckRoutine = StartCoroutine(CheckGameStateAfterDelay());
    }

    private IEnumerator CheckGameStateAfterDelay() {
        yield return new WaitForSeconds(stateCheckDelay);

        stateCheckRoutine = null;
        CheckForGameCompletion();
    }

    private void CheckForGameCompletion() {
        if (gameEnded) return;

        if (HasRemainingPixels()) {
            CheckForGameFail();
            return;
        }

        gameEnded = true;
        if (SaveManager.Instance == null) levelManager.LoadLevel();

        SaveManager.Instance.level += 1;

        SaveManager.Instance.coins += levelManager.Data.rewards.coins;

        SaveManager.Instance.bAdd += levelManager.Data.rewards.bAdd;
        SaveManager.Instance.bCherry += levelManager.Data.rewards.bCherry;
        SaveManager.Instance.bClearer += levelManager.Data.rewards.bClearer;

        gameEndVisualiser.Win(levelManager.Data);
    }

    private void CheckForGameFail() {
        if (ants != null && ants.GetAntCount > 0) return;
        if (HasReservedPixels()) return;
        if (HasWaitingBoxThatCanSpawn()) return;
        if (HasMoveableBox()) return;

        gameEnded = true;
        gameEndVisualiser.Lose();
    }

    //HELPERS

    private bool HasRemainingPixels() {
        if (map == null) return false;

        return map.GetMapLayout()
            .Any(pixel => pixel != null &&
                map.IsVisiblePixelColor(pixel.Color));
    }

    private bool HasReservedPixels() {
        if (map == null) return false;

        return map.GetMapLayout()
            .Any(pixel => pixel != null && pixel.IsReserved);
    }

    private bool HasWaitingBoxThatCanSpawn() {
        if (waitMana == null || map == null) return false;

        foreach (Box box in waitMana.WaitingBoxes) {
            if (box == null || !box.HasAntsToRelease) continue;
            if (!map.IsVisiblePixelColor(box.Color)) continue;
            if (map.GetAvailableExposedTargets(box.Color).Count == 0) continue;

            return true;
        }

        return false;
    }

    private bool HasMoveableBox() {
        if (boxMana == null || waitMana == null) return false;

        int freeSlots = waitMana.PlateCount - waitMana.ActivePlateCount;

        return boxMana.BoxList.Any(box =>
            box != null &&
            box.Interactable &&
            (box.Link == null ? freeSlots >= 1 : freeSlots >= 2));
    }

}
