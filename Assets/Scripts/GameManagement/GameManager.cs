using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private MapCoordination map;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Ants ants;

    //VISUAL
    [Header("UI References"), Tooltip("This section includes coins, boosters visualisation")]
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
        levelManager.LoadLevel(SaveManager.Instance?.level);


        coinVisualiser.SetCoin(SaveManager.Instance?.coins);
    }

    private void CheckForGameCompletion() {
        if (map.GetMapLayout()
            .Any(p => p != null 
            && p.Color != ColorType.None 
            && p.Color != ColorType.Invalid
            && p.Color != ColorType.Unknown))
            return;

        Debug.Log("Check Comp");
        if (SaveManager.Instance == null) levelManager.LoadLevel();
        Debug.Log("Check Comp2");

        SaveManager.Instance.level += 1;

        SaveManager.Instance.coins += levelManager.Data.rewards.coins;

        SaveManager.Instance.bAdd += levelManager.Data.rewards.bAdd;
        SaveManager.Instance.bCherry += levelManager.Data.rewards.bCherry;
        SaveManager.Instance.bClearer += levelManager.Data.rewards.bClearer;

        gameEndVisualiser.Win(levelManager.Data);
    }

}
