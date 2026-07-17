using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
    public event Action LinkState;
    [HideInInspector] public Box selectedBox;

    void Awake() {
        listOfColorTypes = Enum.GetValues(typeof(ColorType)).Cast<ColorType>().ToList();

        dropdown.AddOptions(listOfColorTypes.ConvertAll(i => i.ToString()));
        dropdown.onValueChanged.AddListener(OnDropdownChange);

        amountInput.onEndEdit.AddListener((value) => {
            if (selectedBox == null) return;
            if (int.TryParse(value, out int amount)) selectedBox.SetAmount(amount);
        });

        deleteButton.onClick.AddListener(() => {
            if (selectedBox == null) return;
            boxMana.Remove(selectedBox);
            handler.Log();
        });

        mysterize.onClick.AddListener(() => {
            if (selectedBox == null) return;
            selectedBox.SetMysterize(true);
        });

        addToPlate.onClick.AddListener(() => {
            if (selectedBox == null) return;
            if (waitingSlots.AddBoxToAvailablePlate(selectedBox))
                boxMana.RemoveBox(selectedBox);
        });

        linkBox.onClick.AddListener(() => {
            if (selectedBox == null) return;
            LinkState.Invoke();
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

    public void Init(Box box) {
        if (selectedBox == box) return;
        selectedBox = box;
        selectedBox.SetOutline(Color.aquamarine);

        for (int i = 0; i < dropdown.options.Count; i++) 
            if (dropdown.options[i].ToString() == box.Color.ToString()) dropdown.value = i;
        
        amountInput.text = box.Amount.ToString();

    }

    public void SetLink(Box box) {
        if (selectedBox == box || selectedBox.Link != null || box.Link != null) return;

        selectedBox.Link = box;
        box.Link = selectedBox;

        selectedBox.CreateLinkLine();
    }

    public void Deselection() {
        if (selectedBox == null) return;
        selectedBox.DisableOutline();
        selectedBox = null;
    }

    private void OnDropdownChange(int colorType) {
        if (selectedBox == null) return;

        ColorType newColor = ColorType.Invalid; 
        for (int i = 0; i < listOfColorTypes.Count; i++) {
            if (listOfColorTypes[i].ToString() == dropdown.options[colorType].text) {
                newColor = listOfColorTypes[i];
            }
        }

        selectedBox.ChangeColor(newColor, colorData.GetColor(newColor));
        handler.Log();
    }
}
