using DG.Tweening;
using TMPro;
using UnityEngine;

public class WarningMessage : MonoBehaviour
{
    public static WarningMessage Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text text;
    [SerializeField] private float totalTime = 2f;
    [SerializeField] private float moveHeight = 1.5f;
    [SerializeField] private float fadeDuration = 0.35f;

    private Sequence seq;
    private Vector3 initialLocalPosition;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        initialLocalPosition = transform.localPosition;

        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDisable() {
        KillSequence();
    }

    public void Warn(string message) {
        if (canvasGroup == null || text == null) {
            Debug.LogWarning("WarningMessage is missing CanvasGroup or TMP_Text.");
            return;
        }

        KillSequence();

        canvasGroup.gameObject.SetActive(true);
        text.text = message;
        transform.localPosition = initialLocalPosition;
        seq = DOTween.Sequence();

        seq.Join(canvasGroup.DOFade(1f, fadeDuration));
        seq.Join(transform.DOLocalMoveY(
            initialLocalPosition.y + moveHeight,
            fadeDuration))
            .SetEase(Ease.OutBack, 3f);

        seq.AppendInterval(totalTime);
        seq.Append(canvasGroup.DOFade(0f, fadeDuration));

        seq.OnComplete(() => {
            seq = null;
            canvasGroup.gameObject.SetActive(false);
        });
    }

    private void KillSequence() {
        if (seq == null) return;

        seq.Kill();
        seq = null;
    }
}
