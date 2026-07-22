using System.Collections.Generic;
using UnityEngine;

public class MapCoordination : MonoBehaviour {
    [SerializeField] private ColorType baseColor;

    [Header("UI Frame")]
    [SerializeField] private RectTransform frame;
    [SerializeField] private RectTransform hole;
    [SerializeField] private Canvas frameCanvas;

    [Header("World")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("Optional flat transform used only for measuring the frame. Leave empty to use this transform without its local X/Y tilt.")]
    [SerializeField] private Transform layoutReference;
    [SerializeField] private ColorData colorData;

    [Header("Grid")]
    [SerializeField] private GameObject pixelPrefab;
    [SerializeField] private Transform pixelParent;

    [Min(0.001f)]
    [SerializeField] private float pixelSize = 0.2f;

    [Min(0.001f)]
    [SerializeField] private float pixelDepth = 0.05f;

    [SerializeField] private float pixelGap = 0f;

    [Tooltip("World-space margin inside the frame.")]
    [Min(0f)]
    [SerializeField] private float padding = 0.1f;

    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private bool fitVisualToPixelSize = true;

    private readonly List<GameObject> pixels = new();
    private readonly List<PixelView> pixelArt = new();
    private readonly Vector3[] frameCorners = new Vector3[4];
    private PixelView[,] pixelGrid;
    private Vector3[,] cellWorldPositions;
    private Bounds gridWorldBounds;
    private bool hasGridWorldBounds;
    private readonly List<ExposedTarget> exposedTargetCache = new();
    private bool navigationDirty = true;

    private int columns;
    private int rows;

    public int Columns => columns;
    public int Rows => rows;
    public float CellStep => Mathf.Abs(pixelSize + pixelGap);

    private void Start() {
        if (rebuildOnStart) {
            RebuildGrid();
        }
    }

    public List<PixelView> GetMapLayout() {
        return pixelArt;
    }

    public void RebuildGrid() {
        pixelParent ??= transform;

        if (!TryGetFrameLocalBounds(
                out Vector3 localCenter,
                out float width,
                out float height)) {
            return;
        }

        float usableWidth = Mathf.Max(0f, width - padding * 2f);
        float usableHeight = Mathf.Max(0f, height - padding * 2f);

        float cellSize = pixelSize + pixelGap;
        columns = Mathf.FloorToInt((usableWidth + pixelGap) / cellSize);
        rows = Mathf.FloorToInt((usableHeight + pixelGap) / cellSize);

        if (columns < 1 || rows < 1) {
            WarningMessage.Instance?.Warn("ERR | The frame is too small for the current pixel size.");
            return;
        }

        pixelParent.localPosition = localCenter;
        pixelParent.localRotation = Quaternion.identity;
        pixelParent.localScale = Vector3.one;

        GenerateGrid();
    }

    private bool TryGetFrameLocalBounds(
        out Vector3 localCenter,
        out float width,
        out float height) {
        localCenter = Vector3.zero;
        width = 0f;
        height = 0f;

        if (frame == null || frameCanvas == null || worldCamera == null) {
            WarningMessage.Instance?.Warn("ERR | Frame, Canvas, or World Camera is missing.");
            return false;
        }

        frame.GetWorldCorners(frameCorners);

        Camera canvasCamera =
            frameCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : frameCanvas.worldCamera;

        Quaternion layoutRotation = GetLayoutRotation();
        Vector3 layoutNormal = layoutRotation * Vector3.forward;
        Plane mapPlane = new(layoutNormal, transform.position);
        Vector3[] localCorners = new Vector3[4];

        for (int i = 0; i < frameCorners.Length; i++) {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    canvasCamera,
                    frameCorners[i]);

            Ray ray = worldCamera.ScreenPointToRay(screenPoint);
            if (!mapPlane.Raycast(ray, out float distance)) {
                WarningMessage.Instance?.Warn("ERR | Could not project frame corner onto map plane.");
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(distance);
            localCorners[i] = WorldToLayoutLocal(worldPoint, layoutRotation);
        }

        float minX = localCorners[0].x;
        float maxX = localCorners[0].x;
        float minY = localCorners[0].y;
        float maxY = localCorners[0].y;

        for (int i = 1; i < localCorners.Length; i++) {
            minX = Mathf.Min(minX, localCorners[i].x);
            maxX = Mathf.Max(maxX, localCorners[i].x);
            minY = Mathf.Min(minY, localCorners[i].y);
            maxY = Mathf.Max(maxY, localCorners[i].y);
        }

        localCenter = new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            0f);

        width = maxX - minX;
        height = maxY - minY;

        return true;
    }

    private Quaternion GetLayoutRotation() {
        if (layoutReference != null) {
            return layoutReference.rotation;
        }

        Vector3 euler = transform.rotation.eulerAngles;
        return Quaternion.Euler(0f, euler.y, euler.z);
    }

    private Vector3 WorldToLayoutLocal(Vector3 worldPoint, Quaternion layoutRotation) {
        return Quaternion.Inverse(layoutRotation) * (worldPoint - transform.position);
    }

    private void GenerateGrid() {
        ClearGrid();

        if (pixelPrefab == null) {
            WarningMessage.Instance?.Warn("ERR | Pixel prefab is not assigned.");
            return;
        }

        if (pixelPrefab.scene.IsValid()) {
            pixelPrefab.SetActive(false);
        }

        float cellSize = pixelSize + pixelGap;
        float gridWidth = columns * pixelSize + (columns - 1) * pixelGap;
        float gridHeight = rows * pixelSize + (rows - 1) * pixelGap;

        float startX =
            -gridWidth * 0.5f +
            pixelSize * 0.5f;

        float startY =
            gridHeight * 0.5f -
            pixelSize * 0.5f;

        pixelGrid = new PixelView[columns, rows];
        cellWorldPositions = new Vector3[columns, rows];
        hasGridWorldBounds = false;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                GameObject pixel =
                    Instantiate(pixelPrefab, pixelParent);

                pixel.SetActive(true);
                pixel.transform.localPosition =
                    new Vector3(
                        startX + x * cellSize,
                        startY - y * cellSize,
                        0f);

                pixel.transform.localRotation =
                    Quaternion.identity;

                pixel.transform.localScale =
                    Vector3.one;

                if (fitVisualToPixelSize) {
                    FitPixelVisual(pixel);
                } else {
                    pixel.transform.localScale =
                        new Vector3(
                            pixelSize,
                            pixelSize,
                            pixelDepth);
                }

                if (pixel.GetComponent<PixelView>() == null) {
                    pixel.AddComponent<PixelView>();
                }

                PixelView pixelView = pixel.GetComponent<PixelView>();
                pixelView.ChangeColor(baseColor, colorData.GetColor(baseColor));
                pixelView.SetGridPosition(x, y, this);
                pixelArt.Add(pixelView);

                pixelGrid[x, y] = pixelView;
                cellWorldPositions[x, y] = pixel.transform.position;

                if (!hasGridWorldBounds) {
                    gridWorldBounds = new Bounds(pixel.transform.position, Vector3.zero);
                    hasGridWorldBounds = true;
                } else {
                    gridWorldBounds.Encapsulate(pixel.transform.position);
                }

                pixels.Add(pixel);
            }
        }

        if (hasGridWorldBounds) {
            gridWorldBounds.Expand(new Vector3(pixelSize, pixelSize, 0f));
        }
    }

    private void FitPixelVisual(GameObject pixel) {
        Renderer pixelRenderer = pixel.GetComponentInChildren<Renderer>();
        if (pixelRenderer == null) return;

        Vector3 visualSize = pixelRenderer.bounds.size;
        float largestVisibleAxis = Mathf.Max(visualSize.x, visualSize.y);
        if (largestVisibleAxis <= 0f) return;

        float scale = pixelSize / largestVisibleAxis;
        float depthScale = visualSize.z > 0f ? pixelDepth / visualSize.z : scale;
        pixel.transform.localScale = new Vector3(scale, scale, depthScale);
    }

    public void ClearGrid() {
        foreach (GameObject pixel in pixels) {
            if (pixel != null)
                Destroy(pixel);
        }

        pixels.Clear();
        pixelArt.Clear();
        pixelGrid = null;
        cellWorldPositions = null;
        hasGridWorldBounds = false;
        exposedTargetCache.Clear();
        navigationDirty = true;
    }

    public void SetPixelColor(int x, int y, ColorType color) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;

        PixelView pixelView = GetPixel(x, y);
        pixelView?.ChangeColor(color, colorData.GetColor(color));
        navigationDirty = true;
    }

    public void ApplySavedMap(List<PixelSaveData> map) {
        if (map == null) return;

        RebuildGrid();
        SetAllPixels(ColorType.Invalid);

        foreach (PixelSaveData pixel in map) {
            SetPixelColor(pixel.x, pixel.y, pixel.color);
        }
    }

    private void SetAllPixels(ColorType color) {
        if (pixelGrid == null) return;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                PixelView pixelView = GetPixel(x, y);
                pixelView?.ChangeColor(color, colorData.GetColor(color));
            }
        }

        navigationDirty = true;
    }

    public Dictionary<ColorType, int> GetPixelColorCount() {
        if (pixels == null) return new Dictionary<ColorType, int>();

        Dictionary<ColorType, int> listOfPixel = new();
        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                PixelView pixel = GetPixel(x, y);
                if (pixel == null || !ShouldCountPixelColor(pixel.Color)) continue;

                listOfPixel.TryGetValue(pixel.Color, out int amount);
                listOfPixel[pixel.Color] = amount + 1;
            }
        }

        return listOfPixel;
    }

    private bool ShouldCountPixelColor(ColorType color) {
        return color != ColorType.None &&
               color != ColorType.Invalid &&
               color != ColorType.Unknown;
    }

    public List<PixelView> ExposedPixels {
        get {
            List<PixelView> exposedPixels = new();
            if (pixelGrid == null) return exposedPixels;

            RebuildNavigationCacheIfNeeded();
            foreach (ExposedTarget target in exposedTargetCache) {
                if (target.Pixel != null) exposedPixels.Add(target.Pixel);
            }

            return exposedPixels;
        }
    }

    public PixelView GetPixel(int x, int y) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return null;
        if (pixelGrid == null) return null;
        return pixelGrid[x, y];
    }

    public bool IsCellWalkable(Vector2Int grid) {
        if (grid.x < 0 || grid.x >= columns ||
            grid.y < 0 || grid.y >= rows ||
            pixelGrid == null) return false;

        PixelView pixel = pixelGrid[grid.x, grid.y];
        return pixel == null ||
               pixel.Color == ColorType.None ||
               pixel.IsPickedUp;
    }

    public Vector3 GetCellWorldPosition(int x, int y) {
        if (x < 0 || x >= columns || y < 0 || y >= rows ||
            cellWorldPositions == null) return Vector3.zero;
        return cellWorldPositions[x, y];
    }

    public Vector3 GetCellWorldPosition(Vector2Int grid) {
        return GetCellWorldPosition(grid.x, grid.y);
    }

    public List<ExposedTarget> GetAvailableExposedTargets(ColorType color) {
        List<ExposedTarget> results = new();
        if (pixelGrid == null || columns <= 0 || rows <= 0) return results;

        RebuildNavigationCacheIfNeeded();
        foreach (ExposedTarget target in exposedTargetCache) {
            if (target.Pixel != null && target.Pixel.IsAvailableFor(color)) {
                results.Add(target);
            }
        }

        return results;
    }

    public void NotifyNavigationChanged() {
        navigationDirty = true;
    }

    public void VacateCell(PixelView pixel) {
        if (pixel == null || pixelGrid == null) return;

        Vector2Int grid = pixel.GridPosition;
        if (grid.x < 0 || grid.x >= columns ||
            grid.y < 0 || grid.y >= rows) return;

        if (pixelGrid[grid.x, grid.y] != pixel) return;

        pixelGrid[grid.x, grid.y] = null;
        navigationDirty = true;
    }

    private void RebuildNavigationCacheIfNeeded() {
        if (!navigationDirty) return;

        navigationDirty = false;
        exposedTargetCache.Clear();

        BuildOpeningMap(
            out int[,] distances,
            out TraySide[,] sides,
            out Vector2Int[,] openings);

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                PixelView pixel = pixelGrid[x, y];
                if (pixel == null ||
                    pixel.Color == ColorType.None ||
                    pixel.IsPickedUp) continue;

                if (TryResolveOpening(
                        pixel.GridPosition,
                        distances,
                        sides,
                        openings,
                        out TraySide side,
                        out Vector3 opening,
                        out int gridDistance)) {
                    exposedTargetCache.Add(new ExposedTarget(
                        pixel,
                        side,
                        opening,
                        gridDistance));
                }
            }
        }
    }

    private bool IsExposed(int x, int y, bool[,] reachableEmpty) {
        return IsOutsideReachableEmpty(x, y - 1, reachableEmpty) ||
               IsOutsideReachableEmpty(x + 1, y, reachableEmpty) ||
               IsOutsideReachableEmpty(x, y + 1, reachableEmpty) ||
               IsOutsideReachableEmpty(x - 1, y, reachableEmpty);
    }

    private bool IsOutsideReachableEmpty(int x, int y, bool[,] reachableEmpty) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return true;
        return reachableEmpty[x, y];
    }

    private bool[,] GetOutsideReachableEmptyPixels() {
        bool[,] reachableEmpty = new bool[columns, rows];
        Queue<Vector2Int> search = new();

        for (int x = 0; x < columns; x++) {
            TryAddEmptyPixel(x, 0, reachableEmpty, search);
            TryAddEmptyPixel(x, rows - 1, reachableEmpty, search);
        }

        for (int y = 0; y < rows; y++) {
            TryAddEmptyPixel(0, y, reachableEmpty, search);
            TryAddEmptyPixel(columns - 1, y, reachableEmpty, search);
        }

        while (search.Count > 0) {
            Vector2Int current = search.Dequeue();

            TryAddEmptyPixel(current.x, current.y - 1, reachableEmpty, search);
            TryAddEmptyPixel(current.x + 1, current.y, reachableEmpty, search);
            TryAddEmptyPixel(current.x, current.y + 1, reachableEmpty, search);
            TryAddEmptyPixel(current.x - 1, current.y, reachableEmpty, search);
        }

        return reachableEmpty;
    }

    private void TryAddEmptyPixel(
        int x,
        int y,
        bool[,] reachableEmpty,
        Queue<Vector2Int> search) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;
        if (reachableEmpty[x, y]) return;
        if (HasVisiblePixel(x, y)) return;

        reachableEmpty[x, y] = true;
        search.Enqueue(new Vector2Int(x, y));
    }

    private bool HasVisiblePixel(int x, int y) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return false;
        return !IsCellWalkable(new Vector2Int(x, y));
    }

    public bool TryGetOpening(
        PixelView target,
        out TraySide side,
        out Vector3 openingPosition) {
        side = TraySide.Bottom;
        openingPosition = Vector3.zero;

        if (target == null || columns <= 0 || rows <= 0) return false;

        BuildOpeningMap(
            out int[,] distances,
            out TraySide[,] sides,
            out Vector2Int[,] openings);

        return TryResolveOpening(
            target.GridPosition,
            distances,
            sides,
            openings,
            out side,
            out openingPosition,
            out _);
    }

    private void BuildOpeningMap(
        out int[,] distances,
        out TraySide[,] sides,
        out Vector2Int[,] openings) {
        distances = new int[columns, rows];
        sides = new TraySide[columns, rows];
        openings = new Vector2Int[columns, rows];
        Queue<Vector2Int> search = new();

        for (int x = 0; x < columns; x++) {
            TrySeedOpening(x, rows - 1, TraySide.Bottom, distances, sides, openings, search);
        }
        for (int y = rows - 1; y >= 0; y--) {
            TrySeedOpening(0, y, TraySide.Left, distances, sides, openings, search);
            TrySeedOpening(columns - 1, y, TraySide.Right, distances, sides, openings, search);
        }
        for (int x = 0; x < columns; x++) {
            TrySeedOpening(x, 0, TraySide.Top, distances, sides, openings, search);
        }

        while (search.Count > 0) {
            Vector2Int current = search.Dequeue();
            TrySpreadOpening(current, current.x, current.y + 1, distances, sides, openings, search);
            TrySpreadOpening(current, current.x - 1, current.y, distances, sides, openings, search);
            TrySpreadOpening(current, current.x + 1, current.y, distances, sides, openings, search);
            TrySpreadOpening(current, current.x, current.y - 1, distances, sides, openings, search);
        }
    }

    private bool TryResolveOpening(
        Vector2Int targetGrid,
        int[,] distances,
        TraySide[,] sides,
        Vector2Int[,] openings,
        out TraySide side,
        out Vector3 openingPosition,
        out int gridDistance) {
        side = TraySide.Bottom;
        openingPosition = Vector3.zero;
        gridDistance = 0;

        if (TryGetDirectBoundaryOpening(targetGrid, out side, out Vector2Int directOpening)) {
            PixelView directPixel = GetPixel(directOpening.x, directOpening.y);
            if (directPixel == null) return false;
            openingPosition = GetCellWorldPosition(directOpening);
            return true;
        }

        int bestDistance = int.MaxValue;
        Vector2Int bestOpening = targetGrid;
        Vector2Int[] neighbours = {
            new(targetGrid.x, targetGrid.y + 1), // bottom first
            new(targetGrid.x - 1, targetGrid.y),
            new(targetGrid.x + 1, targetGrid.y),
            new(targetGrid.x, targetGrid.y - 1)
        };

        foreach (Vector2Int neighbour in neighbours) {
            if (neighbour.x < 0 || neighbour.x >= columns ||
                neighbour.y < 0 || neighbour.y >= rows) continue;

            int distance = distances[neighbour.x, neighbour.y];
            if (distance <= 0 || distance >= bestDistance) continue;

            bestDistance = distance;
            side = sides[neighbour.x, neighbour.y];
            bestOpening = openings[neighbour.x, neighbour.y];
        }

        if (bestDistance == int.MaxValue) return false;
        openingPosition = GetCellWorldPosition(bestOpening);
        gridDistance = bestDistance;
        return true;
    }

    public bool TryGetGridWorldBounds(out Bounds bounds) {
        bounds = gridWorldBounds;
        return hasGridWorldBounds;
    }

    public bool TryGetHoleWorldBounds(out Bounds bounds) {
        bounds = new Bounds();
        if (hole == null) return false;

        return TryGetRectWorldBounds(hole, out bounds);
    }

    public bool GetPositionOfHole(out Vector3 holePosition) {
        holePosition = new Vector3();
        if (hole == null) return false;

        holePosition = hole.position;
        return holePosition != Vector3.zero;
    }

    private void TrySeedOpening(
        int x,
        int y,
        TraySide side,
        int[,] distances,
        TraySide[,] sides,
        Vector2Int[,] openings,
        Queue<Vector2Int> search) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;
        if (distances[x, y] != 0 || HasVisiblePixel(x, y)) return;

        distances[x, y] = 1;
        sides[x, y] = side;
        openings[x, y] = new Vector2Int(x, y);
        search.Enqueue(new Vector2Int(x, y));
    }

    private void TrySpreadOpening(
        Vector2Int source,
        int x,
        int y,
        int[,] distances,
        TraySide[,] sides,
        Vector2Int[,] openings,
        Queue<Vector2Int> search) {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;
        if (distances[x, y] != 0 || HasVisiblePixel(x, y)) return;

        distances[x, y] = distances[source.x, source.y] + 1;
        sides[x, y] = sides[source.x, source.y];
        openings[x, y] = openings[source.x, source.y];
        search.Enqueue(new Vector2Int(x, y));
    }

    private bool TryGetDirectBoundaryOpening(
        Vector2Int grid,
        out TraySide side,
        out Vector2Int opening) {
        opening = grid;

        if (grid.y == rows - 1) {
            side = TraySide.Bottom;
            return true;
        }
        if (grid.x == 0) {
            side = TraySide.Left;
            return true;
        }
        if (grid.x == columns - 1) {
            side = TraySide.Right;
            return true;
        }
        if (grid.y == 0) {
            side = TraySide.Top;
            return true;
        }

        side = TraySide.Bottom;
        return false;
    }

    private bool TryGetRectWorldBounds(RectTransform rect, out Bounds bounds) {
        bounds = new Bounds();
        rect.GetWorldCorners(frameCorners);

        Camera canvasCamera =
            frameCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : frameCanvas.worldCamera;

        Quaternion layoutRotation = GetLayoutRotation();
        Vector3 layoutNormal = layoutRotation * Vector3.forward;
        Plane mapPlane = new(layoutNormal, transform.position);
        bool hasCorner = false;

        foreach (Vector3 corner in frameCorners) {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, corner);
            Ray ray = worldCamera.ScreenPointToRay(screenPoint);
            if (!mapPlane.Raycast(ray, out float distance)) return false;

            Vector3 worldPoint = ray.GetPoint(distance);
            if (!hasCorner) {
                bounds = new Bounds(worldPoint, Vector3.zero);
                hasCorner = true;
            } else {
                bounds.Encapsulate(worldPoint);
            }
        }

        return hasCorner;
    }

    public readonly struct ExposedTarget {
        public PixelView Pixel { get; }
        public TraySide OpeningSide { get; }
        public Vector3 Opening { get; }
        public int GridDistance { get; }

        public ExposedTarget(
            PixelView pixel,
            TraySide openingSide,
            Vector3 opening,
            int gridDistance) {
            Pixel = pixel;
            OpeningSide = openingSide;
            Opening = opening;
            GridDistance = gridDistance;
        }
    }
}
