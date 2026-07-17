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

    [Header("Modes")]
    [SerializeField] private Button drawMode;

    [Header("Panels")]
    [SerializeField] private GameObject artistPanel;
    [SerializeField] private AdminBoxConfig boxConfig;

    [Header("Buttons")]
    [SerializeField] private Button addBoxButton;
    [SerializeField] private Button deleteColorBoxButton; //testing only


    public EditorState State { get; private set; } = EditorState.Basic;
    public bool link;

    void Awake() {
        //MODES
        drawMode.onClick.AddListener(() => ChangeMode(EditorState.Drawing));

        boxConfig.LinkState += () => {
            link = true;
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

        boxConfig.gameObject.SetActive(true);
        boxConfig.Init(box);
    }




    //COMMONS

    public void ResetConfig() {
        boxConfig.Deselection();

        ResetConfigPanel();
    }

    private void ResetConfigPanel() {
        //MODE PANELS
        artistPanel.SetActive(false);


        boxConfig.gameObject.SetActive(false);
    }
}
