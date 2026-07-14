using System.Collections;
using TMPro;
using UnityEngine;

public class Box : MonoBehaviour
{
    [SerializeField] private ColorData colorData;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Outline outline;

    [Header("Box's components")]
    [SerializeField] private Renderer boxRenderer;
    [SerializeField] private Animator anim;
    private MaterialPropertyBlock boxMaterial;

    [Header("Box's Mouth")]
    [SerializeField] private Transform mouth;
    [SerializeField] private Renderer mouthRenderer;
    private MaterialPropertyBlock mouthMaterial;

    [Header("Box position")]
    public int ColIndex { get; private set; }
    public int RowIndex { get; private set; }


    //BOX DETAILS
    public ColorType Color { get; private set; } = ColorType.White;
    [field: SerializeField, Min(0)] public int Amount { get; private set; } = 20;

    void Awake() {
        boxMaterial = new MaterialPropertyBlock();
        mouthMaterial = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
        outline.enabled = false;
    }

    void Start() {
        ChangeColor(ColorType.White);
        SetAmount(Amount);
    }

    public void SetAmount(int amount) {
        if (amount < 0) return;

        Amount = Mathf.Min(amount, 100);
        if (text != null) {
            text.text = Amount.ToString();
        }
    }

    public void Decrease(int amount) {
        SetAmount(Mathf.Max(0, Amount - amount));
    }

    public void Animation(BoxAnimationState state) {
        switch (state) {
            case BoxAnimationState.Enable:
                SetOutline();
                anim.SetTrigger("TopRow");
                break;
            case BoxAnimationState.Open:
                DisableOutline();
                anim.SetTrigger("OnWait");
                break;
            case BoxAnimationState.Close:
                DisableOutline();
                anim.SetTrigger("Complete");
                break;
            case BoxAnimationState.Killed:
                DisableOutline();
                anim.SetTrigger("Killed");
                break;
        }
    }

    public void ChangeColor(ColorType color) {
        if (colorData == null) return;

        Color = color;

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
        SetOutline(UnityEngine.Color.white);
    }

    public void SetCol(int col) {
        ColIndex = col;
    }

    public void SetGridPosition(int x, int y) {
        ColIndex = x;
        RowIndex = y;
    }

}
