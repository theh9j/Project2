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

    //BUTTONS
    [SerializeField] private Button deleteButton;


    //DROPDOWN
    [SerializeField] private TMP_Dropdown dropdown;

    //VARIABLES
    List<ColorType> listOfColorTypes = new();
    [HideInInspector] public BoxConfiguration selectedBox;

    void Awake() {
        listOfColorTypes = Enum.GetValues(typeof(ColorType)).Cast<ColorType>().ToList();

        dropdown.AddOptions(listOfColorTypes.ConvertAll(i => i.ToString()));
        dropdown.onValueChanged.AddListener(OnDropdownChange);

        deleteButton.onClick.AddListener(Delete);
    }

    public void Init(BoxConfiguration box) {
        if (selectedBox == box) return;
        selectedBox = box;
        selectedBox.SetOutline(Color.aquamarine);
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

        selectedBox.ChangeColor(newColor);
    }

    private void Delete() {
        if (selectedBox == null) return;
        Destroy(selectedBox.gameObject);
        gameObject.SetActive(false);
    }
}
