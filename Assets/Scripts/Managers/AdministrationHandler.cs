using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AdministrationHandler : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PixelColorWheel colorWheel;
    [SerializeField] private BoxManagementSystem boxManager;
    [SerializeField] private WaitingSlotsManagementSystem waitManager;
    [SerializeField] private ColorMissingLog colorLog;
    [SerializeField] private MapCoordination map;
    [SerializeField] private ColorData colorData;
    [SerializeField] private TMP_Text activeMode;
    [SerializeField] private LevelManager levelMana;

    [Header("Modes")]
    [SerializeField] private Button drawMode;

    [Header("Panels")]
    [SerializeField] private GameObject artistPanel;
    [SerializeField] private AdminBoxConfig boxConfig;

    [Header("Buttons")]
    [SerializeField] private Button addBoxButton;
    [SerializeField] private Button deleteColorBoxButton; //testing only

    [Header("Input Field")]
    [SerializeField] private TMP_InputField columnInput;

    [Header("Level Management")]
    [SerializeField] private Button save;
    [SerializeField] private Button load;
    [SerializeField] private TMP_InputField levelIndex;

    [SerializeField] private TMP_InputField coins;
    [SerializeField] private TMP_InputField bAdd;
    [SerializeField] private TMP_InputField bCherry;
    [SerializeField] private TMP_InputField bClearer;


    public EditorState State { get; private set; } = EditorState.Basic;
    [HideInInspector] public bool link;

    void Awake() {
        //MODES
        drawMode.onClick.AddListener(() => ChangeMode(EditorState.Drawing));

        boxConfig.LinkState += (sourceBox) => {
            link = sourceBox != null;
        };

        deleteColorBoxButton.onClick.AddListener(() => {
            boxManager.RemoveBoxesOfCertainColor(ColorType.White);
            waitManager.RemoveBoxesOfCertainColor(ColorType.White);
            Ants.Instance?.KillAnts(a => a.Color == ColorType.White && a.Pixel == null);
        });

        //FUNCTIONS
        addBoxButton.onClick.AddListener(() => {
            boxManager.Add();
            Log();
        });

        columnInput.onEndEdit.AddListener((value) => {
            if (!int.TryParse(value, out var amount)) return;
            boxManager.SetColumns(amount);
        });


        save.onClick.AddListener(() => {
            if (!int.TryParse(levelIndex.text, out int level)) return;
            levelMana.SaveLevel(level,
                int.TryParse(coins.text, out int coin) ? coin : null,
                int.TryParse(bAdd.text, out int add) ? add : null,
                int.TryParse(bCherry.text, out int cherry) ? cherry : null,
                int.TryParse(bClearer.text, out int clearer) ? clearer : null
                );
        });

        load.onClick.AddListener(() => {
            if (!int.TryParse(levelIndex.text, out int level)) return;
            levelMana.LoadLevel(level);

            coins.text = levelMana.Data.rewards.coins.ToString();
            bAdd.text = levelMana.Data.rewards.bAdd.ToString();
            bCherry.text = levelMana.Data.rewards.bCherry.ToString();
            bClearer.text = levelMana.Data.rewards.bClearer.ToString();

            Log();

        });

    }

    //MODE CONFIGS
    public void ChangeMode(EditorState state) {
        ResetConfig();

        if (state == State) {
            State = EditorState.Basic;
            activeMode.text = $"Current Mode: {State}";
            return;
        }
        State = state;
        activeMode.text = $"Current Mode: {State}";

        switch (state) {
            case EditorState.Drawing:
                artistPanel.SetActive(true);
                break;
        }
    }

    //Drawing mode

    public void Draw(PixelView pixel) {
        if (colorWheel.Brush == ColorType.Invalid) return;
        if (colorWheel.Brush == pixel.Color) return;

        pixel.ChangeColor(colorWheel.Brush, colorData.GetColor(colorWheel.Brush));

        Log();
    }

    public void Log() {
        colorLog.LogProcess(map.GetPixelColorCount(), boxManager.BoxList);
    }


    //BOX CONFIGURATION
    public void SetBox(Box box) {
        if (box == null) return;
        if (link) {
            boxConfig.SetLink(box);
            link = false;
            return;
        }
        boxConfig.Deselection();
        link = false;

        boxConfig.gameObject.SetActive(true);
        boxConfig.Init(box);
    }

    //COMMONS

    public void ResetConfig() {

        ResetConfigPanel();
    }

    private void ResetConfigPanel() {
        //MODE PANELS
        artistPanel.SetActive(false);


        boxConfig.gameObject.SetActive(false);
    }
}
