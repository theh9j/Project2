using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AdminBoxConfig : MonoBehaviour
{
    [SerializeField] private AdministrationHandler handler;
    [SerializeField] private BoxManagementSystem boxMana;
    [SerializeField] private WaitingSlotsManagementSystem waitingSlots;

    [Header("References")]
    [SerializeField] private ColorData colorData;

    //INPUT
    [SerializeField] private TMP_InputField amountInput;

    //BUTTONS
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button addToPlate;
    [SerializeField] private Button linkBox;
    [SerializeField] private Button mysterize;

    //DROPDOWN
    [SerializeField] private TMP_Dropdown dropdown;

    [Header("Aniimation")]
    [SerializeField] private Button enable;
    [SerializeField] private Button open;
    [SerializeField] private Button close;
    [SerializeField] private Button kill;

    //VARIABLES
    List<ColorType> listOfColorTypes = new();
    public event Action<Box> LinkState;
    [HideInInspector] public Box selectedBox;
    private Box linkSource;

    void Awake() {
        listOfColorTypes = Enum
            .GetValues(typeof(ColorType))
            .Cast<ColorType>()
            .Where(c => c != ColorType.None && c != ColorType.Unknown && c != ColorType.Invalid)
            .ToList();
        
        dropdown.AddOptions(
            listOfColorTypes
            .ToList()
            .ConvertAll(i => i.ToString())
            );

        dropdown.onValueChanged.AddListener(OnDropdownChange);

        amountInput.onEndEdit.AddListener((value) => {
            if (selectedBox == null) return;
            if (int.TryParse(value, out int amount)) selectedBox.SetAmount(amount);
            handler.Log();
        });

        deleteButton.onClick.AddListener(() => {
            if (selectedBox == null) return;
            boxMana.Remove(selectedBox);
            handler.Log();
        });

        mysterize.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.SetMysterize(!selectedBox.Mysterious);
        });

        addToPlate.onClick.AddListener(() => {
            if (selectedBox == null) return;
            waitingSlots.AddBoxToAvailablePlate(selectedBox);
        });

        linkBox.onClick.AddListener(() => {
            if (selectedBox == null) return;
            linkSource = selectedBox;
            LinkState?.Invoke(linkSource);
        });


        //ANIMATIONS

        enable.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.Animation(BoxAnimationState.Enable);
        });

        open.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.Animation(BoxAnimationState.Open);
        });

        close.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.Animation(BoxAnimationState.Close);
        });

        kill.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.Animation(BoxAnimationState.Killed);
        });
    }

    void Update() {
        if (selectedBox == null) return;

        if (Keyboard.current != null) {
            if (Keyboard.current[Key.S].wasPressedThisFrame) selectedBox.SetMysterize(!selectedBox.Mysterious);
            if (Keyboard.current[Key.E].wasPressedThisFrame) {
                linkSource = selectedBox;
                LinkState?.Invoke(linkSource);
            }
            if (Keyboard.current[Key.A].wasPressedThisFrame) waitingSlots.AddBoxToAvailablePlate(selectedBox);
        }
    }

    public void Init(Box box) {
        if (selectedBox == box) {
            RefreshConfigFields();
            return;
        }

        selectedBox = box;
        selectedBox.SetOutline(Color.aquamarine);

        RefreshConfigFields();
    }

    public bool SetLink(Box box) {
        Box sourceBox = linkSource ?? selectedBox;
        linkSource = null;

        if (sourceBox == null || box == null) return false;
        if (sourceBox == box) return false;
        if (sourceBox.Link != null || box.Link != null) return false;

        sourceBox.Link = box;
        box.Link = sourceBox;

        box.linkLine = sourceBox.CreateLinkLine();
        if (box.linkLine == null || sourceBox.linkLine == null) {
            WarningMessage.Instance?.Warn("ERR | Can't create link line");
            return false;
        }
        return true;
    }

    public void Deselection() {
        if (selectedBox == null) return;
        selectedBox.DisableOutline();
        selectedBox = null;
    }

    private void OnDropdownChange(int colorType) {
        if (selectedBox == null) return;
        if (colorType < 0 || colorType >= listOfColorTypes.Count) return;

        ColorType newColor = listOfColorTypes[colorType];
        selectedBox.ChangeColor(newColor, colorData.GetColor(newColor));
        handler.Log();
    }

    private void RefreshConfigFields() {
        int colorIndex = listOfColorTypes.IndexOf(selectedBox.Color);
        if (colorIndex >= 0) {
            dropdown.SetValueWithoutNotify(colorIndex);
            dropdown.RefreshShownValue();
        }

        amountInput.SetTextWithoutNotify(selectedBox.Amount.ToString());
    }
}
