using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [SerializeField] private bool reset;
    
    //Main
    [HideInInspector] public int level;
    [HideInInspector] public int coins;

    //Boosters
    [HideInInspector] public int bAdd;
    [HideInInspector] public int bCherry;
    [HideInInspector] public int bClearer;

    //Settings
    [HideInInspector] public bool sFX;
    [HideInInspector] public bool music;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(Instance);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
        LoadPlayerData();
    }

    private void LoadPlayerData() {
        if (!PlayerPrefs.HasKey("Played") || reset) {
            LoadInitialData();
            return;
        }

        level = PlayerPrefs.GetInt("Level");
        coins = PlayerPrefs.GetInt("Coins");

        bAdd = PlayerPrefs.GetInt("Booster Plate Add");
        bCherry = PlayerPrefs.GetInt("Booster Cherry Pick");
        bClearer = PlayerPrefs.GetInt("Booster Color Clearer");

        sFX = PlayerPrefs.GetInt("SFX") == 1;
        music = PlayerPrefs.GetInt("Music") == 1;
    }

    private void LoadInitialData() {
        level = 1;
        coins = 200;

        sFX = true;
        music = true;

        PlayerPrefs.SetInt("Played", 1);
    }

    private void OnApplicationPause(bool pause) {
        if (pause) SaveData();
    }

    private void OnApplicationQuit() => SaveData();

    private void SaveData() {
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("Coins", coins);

        PlayerPrefs.SetInt("Booster Plate Add", bAdd);
        PlayerPrefs.SetInt("Booster Cherry Pick", bCherry);
        PlayerPrefs.SetInt("Booster Color Clearer", bClearer);

        PlayerPrefs.SetInt("SFX", sFX ? 1 : 0);
        PlayerPrefs.SetInt("Music", music ? 1 : 0);
    }
}
