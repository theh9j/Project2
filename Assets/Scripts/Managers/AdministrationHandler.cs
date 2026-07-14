using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdministrationHandler : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PixelColorWheel colorWheel;
    [SerializeField] private ColorMissingLog colorLog;
    [SerializeField] private MapCoordination map;
    [SerializeField] private TMP_Text activeMode;

    [Header("Modes")]
    [SerializeField] private Button drawMode;

    [Header("Panels")]
    [SerializeField] private GameObject artistPanel;
    [SerializeField] private AdminBoxConfig boxConfig;


    public EditorState State { get; private set; } = EditorState.Basic;


    void Awake() {
        drawMode.onClick.AddListener(() => ChangeMode(EditorState.Drawing));




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

        pixel.ChangeColor(colorWheel.Brush);


        //WIP
        colorLog.LogColors(map.GetPixelColorCount());
    }



    //BOX CONFIGURATION
    public void SetBox(BoxConfiguration box) {
        if (box == null) return;
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
