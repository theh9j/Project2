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
    [SerializeField] private float maxLayoutAspect = 9f / 16f;
    [Min(1)]
    [SerializeField] private int maxSizeUntilOrthographic = 7;
    [Min(0f)]
    [SerializeField] private float orthographicIncrement = 1.5f;
    [SerializeField] private List<GameObject> existingPlates = new();

    private float baseOrthographicSize;
    private bool hasBaseOrthographicSize;

    private void Awake() {
        if (cam == null) {
            cam = Camera.main;
        }

        CacheBaseOrthographicSize();
    }

    public void PlateGenerate(int amount = 1) {
        if (amount <= 0 || platePrefab == null) return;

        while (amount > 0) {
            GameObject newPlate = Instantiate(
                platePrefab,
                transform
                );

            existingPlates.Add(newPlate);
            amount--;
        }

        CameraReAdjust();
        Rearrange();
    }

    private void Rearrange() {
        if (existingPlates.Count == 0) return;
        if (cam == null) return;

        float centerX = GetCameraCenterX();

        if (existingPlates.Count == 1) {
            existingPlates[0].transform.position = new Vector3(
                centerX,
                transform.position.y,
                transform.position.z
                );
            return;
        }

        float totalHeight = cam.orthographicSize * 2f;
        float screenWidth = cam.aspect * totalHeight;
        float portraitSafeWidth = totalHeight * maxLayoutAspect;
        float totalWidth = Mathf.Max(0f, Mathf.Min(screenWidth, portraitSafeWidth) - xOffset);

        float spacing = totalWidth / (existingPlates.Count - 1);
        float startX = -totalWidth * .5f;

        for (int i = 0; i < existingPlates.Count; i++) {
            existingPlates[i].transform.position = new Vector3(
                centerX + startX + i * spacing,
                transform.position.y,
                transform.position.z
                );
        }
    }

    private float GetCameraCenterX() {
        float distanceFromCamera = Mathf.Abs(transform.position.z - cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(.5f, .5f, distanceFromCamera)).x;
    }

    private void CameraReAdjust() {
        if (cam == null) return;

        CacheBaseOrthographicSize();

        int resizeThreshold = Mathf.Max(1, maxSizeUntilOrthographic);
        int extraPlateCount = Mathf.Max(0, existingPlates.Count - resizeThreshold + 1);
        cam.orthographicSize = baseOrthographicSize + extraPlateCount * orthographicIncrement;
    }

    private void CacheBaseOrthographicSize() {
        if (cam == null || hasBaseOrthographicSize) return;

        baseOrthographicSize = cam.orthographicSize;
        hasBaseOrthographicSize = true;
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

        CameraReAdjust();
        Rearrange();
    }
}
