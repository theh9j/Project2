using System.Collections.Generic;
using UnityEngine;

public class SpaceNavigation : MonoBehaviour
{
    [SerializeField] private MapCoordination map;
    [SerializeField] private ParticleSystem holeEFX;

    [Header("Route spacing")]
    [Min(0.01f)]
    [SerializeField] private float trayEdgeOffset = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float holeClearance = 0.2f;

    void Awake() {
        if (map == null) {
            map = GetComponent<MapCoordination>();
        }

        if (!map.GetPositionOfHole(out Vector3 holePosition)) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            holePosition
            );

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
            new(screenPoint.x, screenPoint.y)
            );

        holeEFX.transform.position = worldPoint;
    }

    public bool TryReserveTarget(
        ColorType color,
        Vector3 start,
        out PixelView target,
        out List<Vector3> route) {
        target = null;
        route = null;

        if (map == null ||
            !map.TryGetGridWorldBounds(out Bounds trayBounds)) {
            return false;
        }

        List<MapCoordination.ExposedTarget> candidates =
            map.GetAvailableExposedTargets(color);
        if (candidates.Count == 0) return false;

        MapCoordination.ExposedTarget chosen = default;
        float bestScore = float.MaxValue;
        foreach (MapCoordination.ExposedTarget candidate in candidates) {
            float score = EstimateApproachDistance(
                start,
                candidate,
                trayBounds);
            if (score >= bestScore) continue;

            chosen = candidate;
            bestScore = score;
        }

        if (chosen.Pixel == null || !chosen.Pixel.TryReserve()) return false;

        List<Vector3> chosenRoute = BuildRoute(
            start,
            chosen.Opening,
            chosen.OpeningSide,
            trayBounds);

        if (!AppendPixelCollectionSimulation(
                chosenRoute,
                chosen.Pixel,
                chosen.Opening,
                chosen.OpeningSide,
                trayBounds,
                start.z)) {
            chosen.Pixel.ReleaseReservation();
            return false;
        }

        target = chosen.Pixel;
        route = chosenRoute;
        return true;
    }

    private float EstimateApproachDistance(
        Vector3 start,
        MapCoordination.ExposedTarget candidate,
        Bounds trayBounds) {
        float bottomY = trayBounds.min.y - trayEdgeOffset;
        float leftX = trayBounds.min.x - trayEdgeOffset;
        float rightX = trayBounds.max.x + trayEdgeOffset;
        float topY = trayBounds.max.y + trayEdgeOffset;
        float distance = Mathf.Abs(start.y - bottomY);

        switch (candidate.OpeningSide) {
            case TraySide.Bottom:
                distance += Mathf.Abs(start.x - candidate.Opening.x);
                break;
            case TraySide.Left:
                distance += Mathf.Abs(start.x - leftX) +
                            Mathf.Abs(candidate.Opening.y - bottomY);
                break;
            case TraySide.Right:
                distance += Mathf.Abs(start.x - rightX) +
                            Mathf.Abs(candidate.Opening.y - bottomY);
                break;
            case TraySide.Top:
                distance += Mathf.Abs(start.x - leftX) +
                            Mathf.Abs(topY - bottomY) +
                            Mathf.Abs(candidate.Opening.x - leftX);
                break;
        }

        return distance + candidate.GridDistance * map.CellStep;
    }

    private List<Vector3> BuildRoute(
        Vector3 start,
        Vector3 opening,
        TraySide openingSide,
        Bounds trayBounds) {
        List<Vector3> route = new();

        float bottomY = trayBounds.min.y - trayEdgeOffset;
        float leftX = trayBounds.min.x - trayEdgeOffset;
        float rightX = trayBounds.max.x + trayEdgeOffset;
        float topY = trayBounds.max.y + trayEdgeOffset;

        Vector3 bottomStaging = new(start.x, bottomY, start.z);
        AddHoleDetour(start, bottomStaging, route);
        AddDistinct(route, bottomStaging);

        switch (openingSide) {
            case TraySide.Bottom:
                AddDistinct(route, new Vector3(opening.x, bottomY, start.z));
                break;

            case TraySide.Left:
                AddDistinct(route, new Vector3(leftX, bottomY, start.z));
                AddDistinct(route, new Vector3(leftX, opening.y, start.z));
                break;

            case TraySide.Right:
                AddDistinct(route, new Vector3(rightX, bottomY, start.z));
                AddDistinct(route, new Vector3(rightX, opening.y, start.z));
                break;

            case TraySide.Top:
                AddDistinct(route, new Vector3(leftX, bottomY, start.z));
                AddDistinct(route, new Vector3(leftX, topY, start.z));
                AddDistinct(route, new Vector3(opening.x, topY, start.z));
                break;
        }

        return route;
    }

    private void AddHoleDetour(
        Vector3 start,
        Vector3 destination,
        List<Vector3> route) {
        if (!map.TryGetHoleWorldBounds(out Bounds holeBounds)) return;

        holeBounds.Expand(new Vector3(
            holeClearance * 2f,
            holeClearance * 2f,
            0f));

        if (!SegmentIntersectsBounds2D(start, destination, holeBounds)) return;

        float leftX = holeBounds.min.x;
        float rightX = holeBounds.max.x;
        float leftCost = Mathf.Abs(start.x - leftX) + Mathf.Abs(destination.x - leftX);
        float rightCost = Mathf.Abs(start.x - rightX) + Mathf.Abs(destination.x - rightX);
        float detourX = leftCost <= rightCost ? leftX : rightX;

        float lowerY = holeBounds.min.y;
        float upperY = Mathf.Max(destination.y, holeBounds.max.y);

        if (start.y < lowerY) {
            AddDistinct(route, new Vector3(start.x, lowerY, start.z));
        }

        AddDistinct(route, new Vector3(detourX, lowerY, start.z));
        AddDistinct(route, new Vector3(detourX, upperY, start.z));
    }

    private static bool SegmentIntersectsBounds2D(
        Vector3 start,
        Vector3 end,
        Bounds bounds) {
        Vector2 direction = end - start;
        float enter = 0f;
        float exit = 1f;

        return Clip(-direction.x, start.x - bounds.min.x, ref enter, ref exit) &&
               Clip(direction.x, bounds.max.x - start.x, ref enter, ref exit) &&
               Clip(-direction.y, start.y - bounds.min.y, ref enter, ref exit) &&
               Clip(direction.y, bounds.max.y - start.y, ref enter, ref exit);
    }

    private static bool Clip(
        float denominator,
        float numerator,
        ref float enter,
        ref float exit) {
        if (Mathf.Approximately(denominator, 0f)) return numerator >= 0f;

        float value = numerator / denominator;
        if (denominator < 0f) {
            if (value > exit) return false;
            enter = Mathf.Max(enter, value);
        } else {
            if (value < enter) return false;
            exit = Mathf.Min(exit, value);
        }

        return true;
    }

    private static void AddDistinct(List<Vector3> route, Vector3 point) {
        if (route.Count == 0 ||
            (route[route.Count - 1] - point).sqrMagnitude > 0.0001f) {
            route.Add(point);
        }
    }

    private bool AppendPixelCollectionSimulation(
        List<Vector3> route,
        PixelView target,
        Vector3 opening,
        TraySide openingSide,
        Bounds trayBounds,
        float antZ) {
        if (route == null || target == null || map == null) return false;

        if (!TryFindGridRoute(target, opening, out List<Vector2Int> gridPath)) {
            Debug.LogWarning($"No temporary grid route was found to {target.name}.", target);
            return false;
        }

        foreach (Vector2Int cell in gridPath) {
            AddDistinct(route, AtAntDepth(
                map.GetCellWorldPosition(cell),
                antZ));
        }

        AddDistinct(route, AtAntDepth(
            map.GetCellWorldPosition(target.GridPosition),
            antZ));

        List<Vector3> entryReturn = new();
        for (int i = gridPath.Count - 1; i >= 0; i--) {
            AddDistinct(entryReturn, AtAntDepth(
                map.GetCellWorldPosition(gridPath[i]),
                antZ));
        }
        AppendOpeningToHole(
            entryReturn,
            opening,
            openingSide,
            trayBounds,
            antZ);

        List<Vector3> bottomReturn = null;
        if (TryFindBottomExitRoute(target.GridPosition, out List<Vector2Int> bottomPath)) {
            bottomReturn = new List<Vector3>();
            foreach (Vector2Int cell in bottomPath) {
                AddDistinct(bottomReturn, AtAntDepth(
                    map.GetCellWorldPosition(cell),
                    antZ));
            }

            Vector3 bottomOpening = map.GetCellWorldPosition(
                bottomPath[bottomPath.Count - 1]);
            AppendOpeningToHole(
                bottomReturn,
                bottomOpening,
                TraySide.Bottom,
                trayBounds,
                antZ);
        }

        List<Vector3> selectedReturn = bottomReturn != null &&
            RouteLengthFrom(route[route.Count - 1], bottomReturn) <
            RouteLengthFrom(route[route.Count - 1], entryReturn)
                ? bottomReturn
                : entryReturn;
        foreach (Vector3 point in selectedReturn) {
            AddDistinct(route, point);
        }
        return true;
    }

    private bool TryFindBottomExitRoute(
        Vector2Int start,
        out List<Vector2Int> path) {
        path = null;
        Queue<Vector2Int> search = new();
        Dictionary<Vector2Int, Vector2Int> previous = new();
        HashSet<Vector2Int> visited = new();

        search.Enqueue(start);
        visited.Add(start);

        while (search.Count > 0) {
            Vector2Int current = search.Dequeue();
            if (current.y == map.Rows - 1) {
                path = ReconstructGridPath(current, start, previous);
                return true;
            }

            foreach (Vector2Int next in GridNeighbours(current)) {
                if (!IsWalkableCell(next) || !visited.Add(next)) continue;

                previous[next] = current;
                search.Enqueue(next);
            }
        }

        return false;
    }

    private bool TryFindGridRoute(
        PixelView target,
        Vector3 opening,
        out List<Vector2Int> path) {
        path = new List<Vector2Int>();
        Vector2Int targetGrid = target.GridPosition;

        if (targetGrid.x == 0 || targetGrid.x == map.Columns - 1 ||
            targetGrid.y == 0 || targetGrid.y == map.Rows - 1) {
            return true;
        }

        if (!TryFindOpeningCell(opening, out Vector2Int startGrid)) return false;
        Queue<Vector2Int> search = new();
        Dictionary<Vector2Int, Vector2Int> previous = new();
        HashSet<Vector2Int> visited = new();

        search.Enqueue(startGrid);
        visited.Add(startGrid);

        while (search.Count > 0) {
            Vector2Int current = search.Dequeue();
            if (IsAdjacent(current, targetGrid)) {
                path = ReconstructGridPath(current, startGrid, previous);
                return true;
            }

            foreach (Vector2Int next in GridNeighbours(current)) {
                if (!IsWalkableCell(next) || !visited.Add(next)) {
                    continue;
                }

                previous[next] = current;
                search.Enqueue(next);
            }
        }

        return false;
    }

    private bool TryFindOpeningCell(Vector3 opening, out Vector2Int result) {
        result = default;
        bool found = false;
        float closestDistance = float.MaxValue;

        for (int y = 0; y < map.Rows; y++) {
            for (int x = 0; x < map.Columns; x++) {
                Vector2Int cell = new(x, y);
                if (!IsWalkableCell(cell)) continue;

                float distance = (
                    map.GetCellWorldPosition(cell) - opening
                ).sqrMagnitude;
                if (distance >= closestDistance) continue;

                result = cell;
                found = true;
                closestDistance = distance;
            }
        }

        return found;
    }

    private List<Vector2Int> ReconstructGridPath(
        Vector2Int end,
        Vector2Int start,
        Dictionary<Vector2Int, Vector2Int> previous) {
        List<Vector2Int> result = new();
        Vector2Int current = end;

        while (true) {
            result.Add(current);
            if (current == start) break;
            current = previous[current];
        }

        result.Reverse();
        return result;
    }
    private void AppendOpeningToHole(
        List<Vector3> route,
        Vector3 opening,
        TraySide side,
        Bounds trayBounds,
        float antZ) {
        bool hasHole = map.TryGetHoleWorldBounds(out Bounds holeBounds);
        float bottomY = trayBounds.min.y - trayEdgeOffset;
        float leftX = trayBounds.min.x - trayEdgeOffset;
        float rightX = trayBounds.max.x + trayEdgeOffset;
        float topY = trayBounds.max.y + trayEdgeOffset;

        bool useLeftCorner = side == TraySide.Left ||
            (side != TraySide.Right && opening.x <= trayBounds.center.x);

        switch (side) {
            case TraySide.Bottom:
                AddDistinct(route, new Vector3(opening.x, bottomY, antZ));
                break;
            case TraySide.Left:
                AddDistinct(route, new Vector3(leftX, opening.y, antZ));
                AddDistinct(route, new Vector3(leftX, bottomY, antZ));
                break;
            case TraySide.Right:
                AddDistinct(route, new Vector3(rightX, opening.y, antZ));
                AddDistinct(route, new Vector3(rightX, bottomY, antZ));
                break;
            case TraySide.Top:
                AddDistinct(route, new Vector3(opening.x, topY, antZ));
                AddDistinct(route, new Vector3(
                    useLeftCorner ? leftX : rightX,
                    topY,
                    antZ));
                AddDistinct(route, new Vector3(
                    useLeftCorner ? leftX : rightX,
                    bottomY,
                    antZ));
                break;
        }

        if (!hasHole) return;

        Vector3 hole = AtAntDepth(holeBounds.center, antZ);
        AddDistinct(route, hole);
    }

    private static float RouteLengthFrom(Vector3 start, List<Vector3> route) {
        float length = 0f;
        Vector3 previous = start;
        foreach (Vector3 point in route) {
            length += Vector3.Distance(previous, point);
            previous = point;
        }

        return length;
    }

    private static IEnumerable<Vector2Int> GridNeighbours(Vector2Int point) {
        yield return new Vector2Int(point.x, point.y - 1);
        yield return new Vector2Int(point.x + 1, point.y);
        yield return new Vector2Int(point.x, point.y + 1);
        yield return new Vector2Int(point.x - 1, point.y);
    }

    private static bool IsAdjacent(Vector2Int a, Vector2Int b) {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private bool IsWalkableCell(Vector2Int grid) {
        return map != null && map.IsCellWalkable(grid);
    }

    private static Vector3 AtAntDepth(Vector3 point, float antZ) {
        return new Vector3(point.x, point.y, antZ);
    }
}
