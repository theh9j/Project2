using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SingularColorWheel : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TMP_Text text;

    public Button SetButton(Color color, string text) {
        buttonImage.color = color;
        this.text.text = text;
        return button;
    }

}
