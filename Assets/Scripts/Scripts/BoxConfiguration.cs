using TMPro;
using UnityEngine;

public class BoxConfiguration : MonoBehaviour
{
    [SerializeField] private ColorData colorData;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Transform mouth;
    [SerializeField] private Outline outline;

    [SerializeField] private Renderer boxRenderer;
    private MaterialPropertyBlock boxMaterial;

    void Awake() {
        boxMaterial = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
        if (outline != null) outline.enabled = false;
    }

    public void ChangeText(string text) {
        this.text.text = text;
    }

    public void ChangeColor(ColorType color) {
        if (colorData == null) return;

        boxRenderer.GetPropertyBlock(boxMaterial);
        boxMaterial.SetColor("_BaseColor", colorData.GetColor(color));
        boxRenderer.SetPropertyBlock(boxMaterial);
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
