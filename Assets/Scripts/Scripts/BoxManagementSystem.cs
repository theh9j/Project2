using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BoxManagementSystem : MonoBehaviour
{
    private const int MAX = 4;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 0.01f;

    [Header("Layout")]
    [SerializeField] private Transform boxParent;
    [SerializeField] private float xSpacing = 2f;
    [SerializeField] private float ySpacing = 1.5f;

    private readonly List<Box>[] boxes = new List<Box>[MAX];
    [SerializeField] private GameObject prefab;

    [Min(1)]
    [SerializeField] private float scrollRowsPerStep = 1f;
    private int rowScrollOffset;

    void Awake() {
        for (int i = 0; i < MAX; i++) {
            boxes[i] = new();
        }
    }

    public void Remove(Box box) {
        if (box == null) return;

        RemoveBox(box);
        Destroy(box.gameObject);
    }

    public void Add() {
        int colIndex = 0;

        if (prefab == null) return;
        GameObject box = Instantiate(prefab);

        for (int i = 1; i < boxes.Length - 1; i++) {
            colIndex = boxes[i].Count < boxes[colIndex].Count ? i : colIndex;
        }
        Add(box.GetComponent<Box>(), colIndex);
    }

    public void Add(Box box, int colIndex) {
        if (box == null) return;

        if (!IsValidColumn(colIndex)) return;

        boxes[colIndex].Add(box);

        box.transform.SetParent(boxParent, false);
        box.SetCol(colIndex);

        RefreshLayout();
    }

    private void RemoveBox(Box box) {
        if (box == null) return;

        for (int i = 0; i < MAX; i++) {
            if (!boxes[i].Remove(box)) continue;

            RefreshLayout();
            return;
        }
    }

    public void InsertBox(Box box,
            int colIndex,
            int rowIndex) {

        if (box == null) return;
        if (!IsValidColumn(colIndex)) return;

        List<Box> col = boxes[colIndex];

        rowIndex = Mathf.Clamp(rowIndex, 0, col.Count);

        col.Insert(rowIndex, box);

        box.transform.SetParent(boxParent, false);
        box.SetCol(colIndex);
    }

    private void RefreshLayout() {
        List<int> activeCols = GetActiveColsIndicies();

        int acvColCount = activeCols.Count;

        if (acvColCount == 0) return;

        for (int i = 0; i < activeCols.Count; i++) {
            int logCol = activeCols[i];

            float xPos = GetCenteredXPosition(i, acvColCount);

            PositionCol(logCol, xPos);

        }
    }

    private void PositionCol(int logCol, float xPos) {
        List<Box> col = boxes[logCol];

        for (int i = 0; i < col.Count; i++) {
            Box box = col[i];

            if (box == null) continue;

            float yPos = -(i - rowScrollOffset) * ySpacing;

            box.transform.DOLocalMove(
                new Vector3(xPos, yPos, 0f),
                moveSpeed
                )
                .SetEase(Ease.InOutBack, 1f);

            box.SetGridPosition(logCol, i);
        }
    }

    private float GetCenteredXPosition(int col, int acvCols) {
        float totalWidth = (acvCols - 1) * xSpacing;

        float leftPos = -totalWidth * .5f;

        return leftPos + (col * xSpacing);
    }

    private List<int> GetActiveColsIndicies() {
        List<int> activeCols = new();

        for (int i = 0; i < boxes.Length; i++) {
            RemoveNulls(boxes[i]);
            if (boxes[i].Count == 0) continue;
            activeCols.Add(i);
        }
        return activeCols;
    }

    private void RemoveNulls(List<Box> col) {
        col.RemoveAll(box => box == null);
    }

    private bool IsValidColumn(int colIndex) {
        return colIndex >= 0 && colIndex < MAX;
    }

    public List<Box> BoxList {
        get {
            List<Box> b = new();
            HashSet<Box> added = new();

            foreach (List<Box> boxCol in boxes)
                foreach (Box box in boxCol) {
                    if (box == null || !added.Add(box)) continue;
                    b.Add(box);
                }

            if (boxParent == null) return b;

            foreach (Box box in boxParent.GetComponentsInChildren<Box>(false)) {
                if (box == null || !added.Add(box)) continue;
                b.Add(box);
            }

            return b;
        }
    }

}
