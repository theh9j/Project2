using UnityEngine;
using UnityEngine.UI;

public class AdminPanelActivity : MonoBehaviour
{
    [SerializeField] private Button adminButton;
    [SerializeField] private GameObject canvas;
    [HideInInspector] public bool activity;

    void Awake() {
        activity = canvas.activeSelf;

        adminButton.onClick.AddListener(OpenAdminPanel);
    }

    private void OpenAdminPanel() {
#if UNITY_EDITOR
        activity = !activity;
        canvas.SetActive(activity);
#endif
    }
}
