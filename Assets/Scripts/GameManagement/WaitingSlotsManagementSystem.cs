using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WaitingSlotsManagementSystem : MonoBehaviour
{
    [SerializeField] private GameObject platePrefab;
    [SerializeField] private Camera cam;
    [SerializeField] private MapCoordination map;

    [Header("Settings")]
    [UnityEngine.Range(0, 2)]
    [SerializeField] private float xOffset = 1.5f;
    [UnityEngine.Range(0.4f, 1f)]
    [SerializeField] private float maxLayoutAspect = 9f / 20f;
    [Min(.01f)]
    [SerializeField] private float columnSpacing = 1f;
    [Min(1)]
    [SerializeField] private int maxSize = 7;

    [SerializeField] private List<GameObject> existingPlates = new();
    private readonly List<WaitingLine> plates = new();
    private List<Box> boxes = new();

    private void Awake() {
        if (cam == null) {
            cam = Camera.main;
        }

        SyncPlateList();
    }

    public void PlateGenerate(int amount = 1) {
        if (amount <= 0 || platePrefab == null) return;
        int maxPlateCount = GetMaxPlateCount();
        if (existingPlates.Count >= maxPlateCount) {
            WarningMessage.Instance.Warn("Out of slots to open");
            return;
        }

        int amountToGenerate = Mathf.Min(amount, maxPlateCount - existingPlates.Count);
        while (amountToGenerate > 0) {
            GameObject newPlate = Instantiate(
                platePrefab,
                transform
                );

            existingPlates.Add(newPlate);
            AddPlate(newPlate);
            amountToGenerate--;
        }

        Rearrange();
    }

    private void Rearrange() {
        if (existingPlates.Count == 0) return;
        if (cam == null) return;

        float centerX = GetCameraCenterX();
        float startX = -(existingPlates.Count - 1) * columnSpacing * .5f;

        for (int i = 0; i < existingPlates.Count; i++) {

            existingPlates[i].transform.DOMove(
                new Vector3(
                    centerX + startX + i * columnSpacing,
                    transform.position.y,
                    transform.position.z),
                .35f
                ).SetEase(Ease.InOutBack, 1f);
        }
    }

    private int GetMaxPlateCount() {
        if (cam == null) return maxSize;

        int columnsThatFit = Mathf.FloorToInt(GetUsableWidth() / columnSpacing) + 1;
        return Mathf.Clamp(columnsThatFit, 1, maxSize);
    }

    private float GetUsableWidth() {
        float totalHeight = cam.orthographicSize * 2f;
        float screenWidth = cam.aspect * totalHeight;
        float portraitSafeWidth = totalHeight * maxLayoutAspect;
        return Mathf.Max(0f, Mathf.Min(screenWidth, portraitSafeWidth) - xOffset);
    }

    private float GetCameraCenterX() {
        float distanceFromCamera = Mathf.Abs(transform.position.z - cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(.5f, .5f, distanceFromCamera)).x;
    }

    public void ClearPlate(int amount = 1) {
        if (amount <= 0 || existingPlates.Count == 0) return;

        amount = Mathf.Min(amount, existingPlates.Count);
        for (int i = 0; i < amount; i++) {
            int lastIndex = existingPlates.Count - 1;
            GameObject plate = existingPlates[lastIndex];
            existingPlates.RemoveAt(lastIndex);
            RemovePlate(plate);
            Destroy(plate);
        }

        Rearrange();
    }

    public void RemoveBoxesOfCertainColor(ColorType color) {
        List<Box> boxOfColor = new();
        foreach (Box box in boxes) {
            if (box.Color == color) {
                boxOfColor.Add(box);
            }
        }

        foreach (Box box in boxOfColor) {
            box.Animation(BoxAnimationState.Killed);
        }
    }

    public void AddBoxToAvailablePlate(Box box) {
        if (box == null || !box.Interactable) return;

        SyncPlateList();

        if (box.Link != null) {
            AddLinkedBoxToAvailablePlates(box);
            return;
        }

        WaitingLine availablePlate = FindFirstAvailablePlate();
        if (availablePlate == null) return;

        box.Finished += (value) => {
            boxes.Remove(value);
        };
        boxes.Add(box);
        availablePlate.Add(box);
        box.OnElevate();
    }

    private void AddLinkedBoxToAvailablePlates(Box box) {
        Box linkedBox = box.Link;
        if (linkedBox == null || !linkedBox.Interactable) return;

        if (!TryFindAdjacentAvailablePlates(
                out WaitingLine firstPlate,
                out WaitingLine secondPlate)) {
            int u = 0;
            for (int i = 0; i < plates.Count; i++) {
                if (plates[i].Available) {
                    u++;
                }
                if (u == 2) {
                    Reorder();
                    break;
                }
            }
            TryFindAdjacentAvailablePlates(
                out firstPlate,
                out secondPlate);
        }

        if (firstPlate == null || secondPlate == null) return;

        box.Finished += (value) => {
            boxes.Remove(value);
        };
        boxes.Add(box);
        firstPlate.Add(box);
        box.OnElevate();

        linkedBox.Finished += (value) => {
            boxes.Remove(value);
        };
        boxes.Add(linkedBox);
        secondPlate.Add(linkedBox);
        linkedBox.OnElevate();
    }

    private WaitingLine FindFirstAvailablePlate() {
        foreach (WaitingLine plate in plates) {
            if (plate != null && plate.Available) {
                return plate;
            }
        }

        return null;
    }

    private bool TryFindAdjacentAvailablePlates(
        out WaitingLine firstPlate,
        out WaitingLine secondPlate) {
        firstPlate = null;
        secondPlate = null;

        for (int i = 0; i < plates.Count - 1; i++) {
            WaitingLine current = plates[i];
            WaitingLine next = plates[i + 1];

            if (current == null || next == null) continue;
            if (!current.Available || !next.Available) continue;

            firstPlate = current;
            secondPlate = next;
            return true;
        }

        return false;
    }

    private void Reorder() {
        SyncPlateList();

        List<Box> occupiedBoxes = new();
        foreach (WaitingLine plate in plates) {
            if (plate == null || plate.CurrentBox == null) continue;
            occupiedBoxes.Add(plate.RemoveBox());
        }

        for (int i = 0; i < occupiedBoxes.Count && i < plates.Count; i++) {
            plates[i].Add(occupiedBoxes[i]);
        }
    }

    private void SyncPlateList() {
        plates.Clear();

        for (int i = existingPlates.Count - 1; i >= 0; i--) {
            if (existingPlates[i] == null) {
                existingPlates.RemoveAt(i);
            }
        }

        for (int i = 0; i < existingPlates.Count; i++) {
            AddPlate(existingPlates[i]);
        }
    }

    private void AddPlate(GameObject plateObject) {
        if (plateObject == null) return;
        if (!plateObject.TryGetComponent(out WaitingLine plate)) {
            plate = plateObject.AddComponent<WaitingLine>();
        }

        if (plates.Contains(plate)) return;

        plates.Add(plate);
    }

    private void RemovePlate(GameObject plateObject) {
        if (plateObject == null) return;
        if (!plateObject.TryGetComponent(out WaitingLine plate)) return;

        plates.Remove(plate);
    }
}
