using UnityEngine;

public class Link : MonoBehaviour
{
    [SerializeField] private Renderer render;
    private MaterialPropertyBlock material;

    private ColorData colorData;
    private Box start;
    private Box end;

    void Awake() {
        if (colorData == null) {
            colorData = ColorData.LoadDefault();
        }
    }

    void Update() {
        if (start == null || end == null) return;

        float dist = Vector3.Distance(start.linkPoint.position, end.linkPoint.position);
        Vector3 direction = end.linkPoint.position - start.linkPoint.position;

        Vector3 newScale = transform.localScale;
        newScale.y = dist;
        transform.localScale = newScale;

        transform.position = (start.linkPoint.position + end.linkPoint.position) / 2;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Init(Box start, Box end) {
        if (start == null || end == null) return;

        this.start = start;
        this.end = end;

        Color startColor = start.Mysterious ? colorData.GetColor(ColorType.Unknown) : colorData.GetColor(start.Color);
        Color endColor = end.Mysterious ? colorData.GetColor(ColorType.Unknown) : colorData.GetColor(end.Color);

        material = new();
        render.GetPropertyBlock(material);
        material.SetColor("_ColorStart", startColor);
        material.SetColor("_ColorEnd", endColor);
        render.SetPropertyBlock(material);
    }

    public void SetNewColor(Box box, Color color) {
        if (material == null) return;

        if (box == start) {
            material.SetColor("_ColorStart", color);
            render.SetPropertyBlock(material);

        } else if (box == end) {
            material.SetColor("_ColorEnd", color);
            render.SetPropertyBlock (material);
        } else return;
    }
}
