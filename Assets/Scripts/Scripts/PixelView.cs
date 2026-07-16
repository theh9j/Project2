using UnityEngine;

public class PixelView : MonoBehaviour
{
    [SerializeField] private GameObject pixel;
    [SerializeField] private Renderer render;

    [SerializeField] private ColorData colorData;
    private MaterialPropertyBlock material;
    public ColorType Color { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsReserved { get; private set; }
    public bool IsPickedUp { get; private set; }

    public bool IsAvailableFor(ColorType color) {
        return Color == color &&
               Color != ColorType.None &&
               Color != ColorType.Invalid &&
               !IsReserved &&
               !IsPickedUp;
    }

    void Awake() {
        if (render == null) {
            render = GetComponentInChildren<Renderer>();
        }

        material = new MaterialPropertyBlock();
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }

        ChangeColor(ColorType.Beige);

    }

    public void ChangeColor(ColorType color) {
        if (material == null || render == null || colorData == null) return;
        this.Color = color;
        if (ColorType.None == color) {
            pixel.SetActive(false);
            return;
        }
        if (!pixel.activeSelf) pixel.SetActive(true);

        render.GetPropertyBlock(material);
        material.SetColor("_BaseColor", colorData.GetColor(color));
        render.SetPropertyBlock(material);
    }

    public void SetGridPosition(int x, int y) {
        GridPosition = new Vector2Int(x, y);
    }

    public bool TryReserve() {
        if (IsReserved || IsPickedUp) return false;

        IsReserved = true;
        return true;
    }

    public void ReleaseReservation() {
        if (!IsPickedUp) {
            IsReserved = false;
        }
    }

    public void MarkPickedUp() {
        IsPickedUp = true;
        IsReserved = false;
    }

}
