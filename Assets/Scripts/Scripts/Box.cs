using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public partial class Box : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Outline outline;

    [Header("Box's components")]
    [SerializeField] private Transform boxVisual;
    [SerializeField] private Renderer boxRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private ParticleSystem particle;
    private MaterialPropertyBlock boxMaterial;

    [Header("Box's Mouth")]
    [SerializeField] private Transform mouth;
    [SerializeField] private Renderer mouthRenderer;
    private MaterialPropertyBlock mouthMaterial;

    [Header("Box position")]
    public int ColIndex { get; private set; } = 1000;
    public int RowIndex { get; private set; } = 1000;
    public bool Interactable => RowIndex == 0;
    public event Action<Box> Finished;

    [Header("Box's properties")]
    [SerializeField] private ColorType baseColor;
    public ColorType Color { get; private set; } = ColorType.White;
    [field: SerializeField, Min(0)] public int Amount { get; private set; } = 100;
    private UnityEngine.Color boxVisualColor = UnityEngine.Color.white;
    private ColorData colorData;

    [Header("Special Properties")]
    public bool Mysterious { get; private set; }
    public Box Link {
        get {
            return Link;
        } 
        
        set {
            if (value == this) return;
            Link = value;
        }
    }


    void Awake() {
        antNest = GameObject.FindWithTag("Nest").transform;

        boxMaterial = new MaterialPropertyBlock();
        mouthMaterial = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
        outline.enabled = false;
    }

    void Start() {
        ChangeColor(baseColor, colorData.GetColor(baseColor));
        SetAmount(Amount);
    }

    public void SetAmount(int amount) {
        if (amount < 0) return;
        if (amount == 0)
            Animation(BoxAnimationState.Close);
        

        Amount = Mathf.Min(amount, 100);
        if (text == null) return;
        text.text = Mysterious ? "?" :
            (Amount > 0 ? Amount.ToString() : "");
    }

    public void Decrease(int amount) {
        SetAmount(Mathf.Max(0, Amount - amount));
    }

    public void ChangeColor(
        ColorType colorType, 
        UnityEngine.Color color) 
        {

        Color = colorType;
        boxVisualColor = color;
        if (Mysterious) color = colorData.GetColor(ColorType.Unknown);

        boxRenderer.GetPropertyBlock(boxMaterial);
        boxMaterial.SetColor("_BaseColor", color);
        boxRenderer.SetPropertyBlock(boxMaterial);

        mouthRenderer.GetPropertyBlock(mouthMaterial);
        mouthMaterial.SetColor("_BaseColor", color);
        mouthRenderer.SetPropertyBlock(mouthMaterial);
    }

    public void CreateLinkLine() {
        if (Link == null) return;


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
        if (!Interactable && y == 0) {
            SetMysterize(false);
            Animation(BoxAnimationState.Enable);
        };
        ColIndex = x;
        RowIndex = y;
    }

    public void OnPress(bool press) {
        if (boxVisual == null) return;

        boxVisual.DOKill();
        if (press) {
            boxVisual.DOScale(
                new Vector3(1.1f, 1.1f, 1f),
                .1f
                );
        } else {
            boxVisual.DOScale(
                new Vector3(1f, 1f, 1f),
                .1f
                );
        }
    }

    public void SetMysterize(bool mys) {
        if (Mysterious == mys || Interactable) return;

        Mysterious = mys;
        if (!mys) 
            particle.Play();
        ChangeColor(Color, boxVisualColor);
        SetAmount(Amount);
    }

    public void OnComplete() {
        Finished?.Invoke(this);
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
                release = false;
                anim.SetTrigger("Complete");
                break;
            case BoxAnimationState.Killed:
                DisableOutline();
                release = false;
                anim.SetTrigger("Killed");
                break;
        }
    }
}
