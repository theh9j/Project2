using DG.Tweening;
using UnityEngine;

public class WaitingLine : MonoBehaviour
{
    [SerializeField] private Transform boxPoint;
    private Box box;
    public Box CurrentBox => box;
    public bool Available => box == null;
    private bool settled;

    private void Awake() {
        if (boxPoint != null) return;

        Transform childPoint = transform.Find("BoxPoint");
        boxPoint = childPoint ?? transform;
    }

    void Update() {
        if (!Available && settled && boxPoint != null) {
            box.transform.position = boxPoint.position;
        }
    }

    public void Add(Box box) {
        if (!Available) return;
        if (box == null) return;

        this.box = box;
        box.Finished -= OnBoxFinished;
        box.Finished += OnBoxFinished;

        box.transform.SetParent(transform, true);
        if (boxPoint == null) return;

        box.inColMovement?.Kill();
        box.transform.DOMove(
            boxPoint.position,
            .2f
            ).SetEase(Ease.InOutBack, 1f)
            .OnComplete(OnBoxSettle);
    }

    private void OnBoxSettle() {
        if (box == null) return;

        settled = true;
        box.Animation(BoxAnimationState.Open);
    }

    public Box RemoveBox() {
        Box removedBox = box;
        box = null;
        settled = false;
        return removedBox;
    }

    private void OnBoxFinished(Box finishedBox) {
        if (box != finishedBox) return;

        box.Finished -= OnBoxFinished;
        box = null;
        settled = false;
    }
}
