using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [SerializeField] private bool reset;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(Instance);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }

    private void LoadPlayerData() {
        if (!PlayerPrefs.HasKey("Played") || reset) {
            LoadInitialData();
            return;
        }

        //Get user data


    }

    private void LoadInitialData() {

    }


}
