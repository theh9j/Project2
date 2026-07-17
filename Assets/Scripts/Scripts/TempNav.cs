using System.Collections.Generic;
using UnityEngine;

public partial class SpaceNavigation : MonoBehaviour
{
    // Temporary collection simulation. The permanent route selector still
    // lives in SpaceNavigation.cs; this file extends the chosen route into
    // the tray, to the pixel, and back to the hole.
    private bool AppendPixelCollectionSimulation(
        List<Vector3> route,
        PixelView target,
        Vector3 opening,
        TraySide openingSide,
        Bounds trayBounds,
        float antZ)
    {
        if (route == null || target == null || map == null) return false;

        if (!TryFindGridRoute(target, opening, out List<Vector2Int> gridPath)) {
            Debug.LogWarning($"No temporary grid route was found to {target.name}.", target);
            return false;
        }

        // Step through the opening and across reachable empty pixels.
        foreach (Vector2Int cell in gridPath) {
            AddDistinct(route, AtAntDepth(
                map.GetCellWorldPosition(cell),
                antZ));
        }

        // Touch the target so Ant can pick it up. Once carried, its former
        // cell becomes traversable and the same path can safely be reversed.
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

        // Picking up the target frees its cell. If that creates a shorter
        // route to an empty bottom opening, the ant may leave there instead
        // of retracing the side it used to enter the tray.
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
        out List<Vector2Int> path)
    {
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
                // The starting cell is still occupied while this route is
                // planned, but it becomes walkable as soon as pickup occurs.
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
        out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();
        Vector2Int targetGrid = target.GridPosition;

        // A boundary pixel can be collected directly from outside the tray.
        // It does not need an empty cell between the opening and the target.
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

    private bool TryFindOpeningCell(Vector3 opening, out Vector2Int result)
    {
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
        Dictionary<Vector2Int, Vector2Int> previous)
    {
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
        float antZ)
    {
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
        // A bottom exit already has a clear line away from the tray, so it can
        // head diagonally to the hole without first visiting a tray corner.
        AddDistinct(route, hole);
    }

    private static float RouteLengthFrom(Vector3 start, List<Vector3> route)
    {
        float length = 0f;
        Vector3 previous = start;
        foreach (Vector3 point in route) {
            length += Vector3.Distance(previous, point);
            previous = point;
        }

        return length;
    }

    private static IEnumerable<Vector2Int> GridNeighbours(Vector2Int point)
    {
        yield return new Vector2Int(point.x, point.y - 1);
        yield return new Vector2Int(point.x + 1, point.y);
        yield return new Vector2Int(point.x, point.y + 1);
        yield return new Vector2Int(point.x - 1, point.y);
    }

    private static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private bool IsWalkableCell(Vector2Int grid)
    {
        return map != null && map.IsCellWalkable(grid);
    }

    private static Vector3 AtAntDepth(Vector3 point, float antZ)
    {
        return new Vector3(point.x, point.y, antZ);
    }
}
