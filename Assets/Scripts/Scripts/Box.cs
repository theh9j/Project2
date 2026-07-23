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
    [SerializeField] private GameObject linkPrefab;
    public Transform linkPoint;
    private MaterialPropertyBlock boxMaterial;

    [Header("Box's Mouth")]
    [SerializeField] private Transform mouth;
    [SerializeField] private Renderer mouthRenderer;
    private MaterialPropertyBlock mouthMaterial;

    [Header("Box position")]
    public int ColIndex { get; private set; } = 1000;
    public int RowIndex { get; private set; } = 1000;
    public bool Interactable {
        get {
            if (Link == null)
                return RowIndex == 0;

            bool bothOnTopOfDifferentColumns =
                RowIndex == 0 &&
                Link.RowIndex == 0 &&
                ColIndex != Link.ColIndex;

            bool verticalPairOnFirstTwoRows =
                ColIndex == Link.ColIndex &&
                (
                    (RowIndex == 0 && Link.RowIndex == 1) ||
                    (RowIndex == 1 && Link.RowIndex == 0)
                );

            return bothOnTopOfDifferentColumns ||
                   verticalPairOnFirstTwoRows;
        }
    }

    public event Action<Box> Finished;
    public event Action Elevated;

    [Header("Box's properties")]
    [SerializeField] private ColorType baseColor;
    public ColorType Color { get; private set; } = ColorType.White;
    [field: SerializeField, Min(0)] public int Amount { get; private set; } = 100;
    private UnityEngine.Color boxVisualColor = UnityEngine.Color.white;
    private ColorData colorData;

    [Header("Special Properties")]
    public bool Mysterious { get; private set; }
    public Box Link { get; set; }
    [HideInInspector] public Link linkLine;
    public Tween inColMovement;
    private bool finishing;

    void Awake() {
        antNest = GameObject.FindWithTag("Nest").transform;

        boxMaterial = new MaterialPropertyBlock();
        mouthMaterial = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
        outline.enabled = false;

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
        linkLine?.SetNewColor(this, color);

        boxRenderer.GetPropertyBlock(boxMaterial);
        boxMaterial.SetColor("_BaseColor", color);
        boxRenderer.SetPropertyBlock(boxMaterial);

        mouthRenderer.GetPropertyBlock(mouthMaterial);
        mouthMaterial.SetColor("_BaseColor", color);
        mouthRenderer.SetPropertyBlock(mouthMaterial);
    }

    public void ApplySavedState(
        ColorType colorType,
        int amount,
        bool mysterious) {
        baseColor = colorType;
        Mysterious = mysterious;

        ChangeColor(colorType, colorData.GetColor(colorType));
        SetAmount(amount);
    }

    public Link CreateLinkLine() {
        if (Link == null || linkPrefab == null) return null;

        GameObject linkGO = Instantiate(linkPrefab, transform);
        linkLine = linkGO.GetComponent<Link>();
        linkLine.Init(this, Link);

        Finished += (box) => {
            Destroy(linkLine);
        };

        Link.Finished += (box) => {
            Destroy(linkLine);
        };

        return linkLine;
    }

    public void RemoveLink() {
        if (linkLine != null) Destroy(linkLine.gameObject);
        Link = null;
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
        if (y == 0 || y == 1000) SetMysterize(false);
        if (RowIndex != 0 && y == 0) {
            Animation(BoxAnimationState.Enable);
        }
        ColIndex = x;
        RowIndex = y;
        if (!Interactable && RowIndex != 0 && RowIndex != 1000) {
            DisableOutline();
            anim.Play("Idle", 0, 0f);
        }
    }

    public void OnPress(bool press) {
        if (press) {
            transform.DOScale(
                new Vector3(1.1f, 1.1f, 1f),
                .1f
                );
        } else {
            transform.DOScale(
                new Vector3(1f, 1f, 1f),
                .1f
                );
        }
    }

    public void SetMysterize(bool mys) {
        if (Mysterious == mys || Interactable) return;

        Mysterious = mys;
        if (!mys) {
            particle.Play();
        }
        ChangeColor(Color, boxVisualColor);
        SetAmount(Amount);
    }

    private void ClearConnections() {
        RemoveLink();
    }

    public void OnComplete() {
        Finished?.Invoke(this);
    }

    public void OnElevate() {
        Elevated?.Invoke();
    }

    public void Animation(BoxAnimationState state) {
        if (finishing) return;
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
                ClearConnections();
                anim.SetTrigger("Killed");
                finishing = true;
                break;
        }
    }
}
