using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PixelColorWheel : MonoBehaviour
{
    [SerializeField] private GameObject singularColorWheelPrefab;
    [SerializeField] private Transform colorWheelParent;
    [SerializeField] private ColorData data;

    private List<ColorType> listOfColors = new();
    public ColorType Brush { get; private set; } = ColorType.None;

    void Awake() {
        listOfColors = Enum.GetValues(typeof(ColorType)).Cast<ColorType>().ToList();
        listOfColors.Remove(ColorType.Invalid);

        BuildColorButtons();
    }

    void OnEnable() {
        Brush = ColorType.None;
    }

    private void BuildColorButtons() {
        
        foreach (ColorType type in listOfColors) {
            GameObject colorWheel = Instantiate(
                singularColorWheelPrefab,
                colorWheelParent
                );

            SingularColorWheel thisColorWheel = colorWheel.GetComponent<SingularColorWheel>();

            thisColorWheel.SetButton(data.GetColor(type), type.ToString()).onClick.AddListener(
                () => {
                    Brush = type;
                });

        }

    }

}
