using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private float pressedScale = .9f;
    [SerializeField] private float animationDuration = .1f;

    private Button button;
    private Vector3 initialScale;

    void Awake() {
        button = GetComponent<Button>();
        initialScale = transform.localScale;
    }


    public void OnPointerDown(PointerEventData eventData) {
        if (button != null && !button.interactable) return;

        transform.DOKill();
        transform.DOScale(initialScale * pressedScale, animationDuration);
    }

    public void OnPointerUp(PointerEventData eventData) {
        ResetScale();
    }

    private void OnDisable() {
        transform.DOKill();
        transform.localScale = initialScale;
    }

    private void ResetScale() {
        transform.DOKill();
        transform.DOScale(initialScale, animationDuration);

    }
}
