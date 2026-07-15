using UnityEngine;

public class PixelView : MonoBehaviour
{
    [SerializeField] private GameObject pixel;
    [SerializeField] private Renderer render;

    [SerializeField] private ColorData colorData;
    private MaterialPropertyBlock material;
    public ColorType Color { get; private set; }
    public bool wait = false;
    public bool pickedUp = false;

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

}
