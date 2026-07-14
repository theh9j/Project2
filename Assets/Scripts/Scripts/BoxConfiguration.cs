using System;
using TMPro;
using UnityEngine;

public class BoxConfiguration : MonoBehaviour
{
    [SerializeField] private ColorData colorData;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Outline outline;

    [Header("Box's components")]
    [SerializeField] private Renderer boxRenderer;
    private MaterialPropertyBlock boxMaterial;

    [Header("Box's Mouth")]
    [SerializeField] private Transform mouth;
    [SerializeField] private Renderer mouthRenderer;
    private MaterialPropertyBlock mouthMaterial;

    void Awake() {
        boxMaterial = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
    }

    public void ChangeText(string text) {
        this.text.text = text;
    }

    public void ChangeColor(ColorType color) {
        if (colorData == null) return;

        boxRenderer.GetPropertyBlock(boxMaterial);
        boxMaterial.SetColor("_BaseColor", colorData.GetColor(color));
        boxRenderer.SetPropertyBlock(boxMaterial);

        mouthRenderer.GetPropertyBlock(mouthMaterial);
        mouthMaterial.SetColor("_BaseColor", colorData.GetColor(color));
        mouthRenderer.SetPropertyBlock(mouthMaterial);
    }
    public void DisableOutline() {
        outline.enabled = false;
    }

    public void SetOutline(Color color) {
        outline.OutlineColor = color;
        outline.enabled = true;
    }

    public void SetOutline() {
        SetOutline(Color.white);
    }

    public void OpenMouth() {

    }

}
