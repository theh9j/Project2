using UnityEngine;

public class AdministrationHandler : MonoBehaviour
{
    [SerializeField] private AdminBoxConfig boxConfig;

    public void SetBox(BoxConfiguration box) {
        if (box == null) return;
        boxConfig.gameObject.SetActive(true);
        boxConfig.Init(box);
    }

    public void ResetConfig() {
        boxConfig.Deselection();

        ResetConfigPanel();
    }

    private void ResetConfigPanel() {
        boxConfig.gameObject.SetActive(false);
    }
}
