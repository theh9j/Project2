using UnityEngine;

public class PixelView : MonoBehaviour
{
    [SerializeField] private GameObject pixel;
    [SerializeField] private Renderer render;

    private MaterialPropertyBlock material;
    private MapCoordination map;
    public ColorType Color { get; private set; }
    public bool Mysterious { get; private set; }

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


    }

    public void ChangeColor(ColorType colorType, Color color) {
        if (material == null || render == null) return;
        this.Color = colorType;
        map?.NotifyNavigationChanged();
        if (ColorType.None == colorType) {
            pixel.SetActive(false);
            return;
        }
        if (!pixel.activeSelf) pixel.SetActive(true);

        render.GetPropertyBlock(material);
        material.SetColor("_BaseColor", color);
        render.SetPropertyBlock(material);
    }

    public void SetGridPosition(int x, int y, MapCoordination owner = null) {
        GridPosition = new Vector2Int(x, y);
        map = owner;
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
        map?.VacateCell(this);
    }

    private void OnDestroy() {
        map?.VacateCell(this);
    }

}
