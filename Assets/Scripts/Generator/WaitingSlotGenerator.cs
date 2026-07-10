using System.Collections.Generic;
using UnityEngine;

public class WaitingSlotGenerator : MonoBehaviour
{
    [SerializeField] private GameObject platePrefab;
    [SerializeField] private Camera cam;

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

    private void Awake() {
        if (cam == null) {
            cam = Camera.main;
        }

    }

    public void PlateGenerate(int amount = 1) {
        if (amount <= 0 || platePrefab == null) return;
        int maxPlateCount = GetMaxPlateCount();
        if (existingPlates.Count >= maxPlateCount) return; //Feedback max sized

        int amountToGenerate = Mathf.Min(amount, maxPlateCount - existingPlates.Count);
        while (amountToGenerate > 0) {
            GameObject newPlate = Instantiate(
                platePrefab,
                transform
                );

            existingPlates.Add(newPlate);
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
            existingPlates[i].transform.position = new Vector3(
                centerX + startX + i * columnSpacing,
                transform.position.y,
                transform.position.z
                );
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
            Destroy(plate);
        }

        Rearrange();
    }
}
