using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxManagementSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 0.01f;
    [Range(1, 4)]
    [SerializeField] private int columns = 4;

    [Header("Layout")]
    [SerializeField] private Transform boxParent;
    [SerializeField] private float xSpacing = 2f;
    [SerializeField] private float ySpacing = 1.5f;

    private readonly List<List<Box>> boxes = new();
    [SerializeField] private GameObject prefab;

    [Min(1)]
    [SerializeField] private float scrollRowsPerStep = 1f;
    private float rowScrollOffset;
    private bool refreshQueued;
    public bool FastForward => BoxList.Count == 0;
    public int Columns => columns;

    void Awake() {
        SetColumns(columns);
    }

    public void SetColumns(int amounts) {
        int newColumnCount = Mathf.Max(1, amounts);
        if (newColumnCount == boxes.Count) return;

        while (boxes.Count < newColumnCount) {
            boxes.Add(new());
        }

        while (boxes.Count > newColumnCount) {
            int removedColumnIndex = boxes.Count - 1;
            List<Box> removedColumn = boxes[removedColumnIndex];
            boxes.RemoveAt(removedColumnIndex);

            foreach (Box box in removedColumn) {
                GetShortestColumn().Add(box);
            }
        }

        columns = newColumnCount;
        RefreshLayout();

    }

    public void Remove(Box box) {
        if (box == null) return;

        RemoveBox(box);
        box.transform.DOKill();
        Destroy(box.gameObject);
    }

    public void Add() {
        int colIndex = 0;

        if (prefab == null) return;
        GameObject box = Instantiate(prefab);

        for (int i = 1; i < boxes.Count; i++) {
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
        RegisterBoxEvents(box);

        RefreshLayout();
    }

    public void RemoveBox(Box box) {
        if (box == null) return;
        box.SetGridPosition(1000, 1000); // fall back

        for (int i = 0; i < boxes.Count; i++) {
            if (!boxes[i].Remove(box)) continue;

            QueueRefreshLayout();
            return;
        }
    }

    public void RemoveBoxesOfCertainColor(ColorType color) {
        List<Box> boxOfColor = new();
        foreach (Box box in BoxList) {
            if (box.Color == color)
                boxOfColor.Add(box);
        }

        foreach (Box box in boxOfColor) {
            box.Animation(BoxAnimationState.Killed);
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
        RegisterBoxEvents(box);
        RefreshLayout();
    }

    public Box CreateBox(int colIndex, int rowIndex) {
        if (prefab == null) return null;
        if (!IsValidColumn(colIndex)) return null;

        GameObject boxObject = Instantiate(prefab);
        if (!boxObject.TryGetComponent(out Box box)) {
            Destroy(boxObject);
            return null;
        }

        InsertBox(box, colIndex, rowIndex);
        return box;
    }

    public void ClearBoxes() {
        foreach (Box box in BoxList) {
            if (box == null) continue;

            box.transform.DOKill();
            Destroy(box.gameObject);
        }

        foreach (List<Box> boxColumn in boxes) {
            boxColumn.Clear();
        }

        rowScrollOffset = 0f;
        refreshQueued = false;
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

            box.transform.DOKill();

            box.inColMovement = box.transform.DOLocalMove(
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

        for (int i = 0; i < boxes.Count; i++) {
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
        return colIndex >= 0 && colIndex < boxes.Count;
    }

    private void RegisterBoxEvents(Box box) {
        box.Finished += (value) => {
            Remove(value);
        };
        box.Elevated += () => {
            RemoveBox(box);
        };
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

    public void Scroll(bool up) {
        int upward = up ? 1 : -1;
        rowScrollOffset += scrollRowsPerStep * upward;
        ClampScrollOffset();

        RefreshLayout();
    }

    public void SetScrollRow(int row) {
        rowScrollOffset = Mathf.Max(0, row);
        ClampScrollOffset();

        RefreshLayout();
    }

    public void ResetScroll() {
        rowScrollOffset = 0;
        RefreshLayout();
    }

    private void ClampScrollOffset() {
        int largestRowCount = 0;
        for (int i = 0; i < boxes.Count; i++) {
            largestRowCount = Mathf.Max(
                largestRowCount,
                boxes[i].Count
                );
        }

        int maxOffset = Mathf.Max(
            0,
            largestRowCount - 1);

        rowScrollOffset = Mathf.Clamp(
            rowScrollOffset,
            0,
            maxOffset);
    }

    private void QueueRefreshLayout() {
        if (refreshQueued) return;

        refreshQueued = true;
        StartCoroutine(RefreshLayoutAtEndOfFrame());
    }

    private IEnumerator RefreshLayoutAtEndOfFrame() {
        yield return null;

        refreshQueued = false;
        RefreshLayout();
    }

    private List<Box> GetShortestColumn() {
        List<Box> shortestColumn = boxes[0];

        for (int i = 1; i < boxes.Count; i++) {
            if (boxes[i].Count >= shortestColumn.Count) continue;

            shortestColumn = boxes[i];
        }

        return shortestColumn;
    }
}
