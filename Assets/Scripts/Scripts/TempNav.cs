using System.Collections.Generic;
using UnityEngine;

public partial class SpaceNavigation : MonoBehaviour
{
    // Temporary collection simulation. The permanent route selector still
    // lives in SpaceNavigation.cs; this file extends the chosen route into
    // the tray, to the pixel, and back to the hole.
    private void AppendPixelCollectionSimulation(
        List<Vector3> route,
        PixelView target,
        Vector3 opening,
        TraySide openingSide,
        Bounds trayBounds,
        float antZ)
    {
        if (route == null || target == null || map == null) return;

        if (!TryFindGridRoute(target, opening, out List<PixelView> gridPath)) {
            Debug.LogWarning($"No temporary grid route was found to {target.name}.", target);
            return;
        }

        // Step through the opening and across reachable empty pixels.
        foreach (PixelView cell in gridPath) {
            AddDistinct(route, AtAntDepth(cell.transform.position, antZ));
        }

        // Touch the target so Ant can pick it up. Once carried, its former
        // cell becomes traversable and the same path can safely be reversed.
        AddDistinct(route, AtAntDepth(target.transform.position, antZ));

        for (int i = gridPath.Count - 1; i >= 0; i--) {
            AddDistinct(route, AtAntDepth(gridPath[i].transform.position, antZ));
        }

        AppendOpeningToHole(route, opening, openingSide, trayBounds, antZ);
    }

    private bool TryFindGridRoute(
        PixelView target,
        Vector3 opening,
        out List<PixelView> path)
    {
        path = new List<PixelView>();
        Vector2Int targetGrid = target.GridPosition;

        // A boundary pixel can be collected directly from outside the tray.
        // It does not need an empty cell between the opening and the target.
        if (targetGrid.x == 0 || targetGrid.x == map.Columns - 1 ||
            targetGrid.y == 0 || targetGrid.y == map.Rows - 1) {
            return true;
        }

        PixelView start = FindOpeningCell(opening);
        if (start == null) return false;

        Vector2Int startGrid = start.GridPosition;
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
                PixelView cell = map.GetPixel(next.x, next.y);
                if (cell == null || cell.Color != ColorType.None || !visited.Add(next)) {
                    continue;
                }

                previous[next] = current;
                search.Enqueue(next);
            }
        }

        return false;
    }

    private PixelView FindOpeningCell(Vector3 opening)
    {
        PixelView closest = null;
        float closestDistance = float.MaxValue;

        for (int y = 0; y < map.Rows; y++) {
            for (int x = 0; x < map.Columns; x++) {
                PixelView cell = map.GetPixel(x, y);
                if (cell == null || cell.Color != ColorType.None) continue;

                float distance = (cell.transform.position - opening).sqrMagnitude;
                if (distance >= closestDistance) continue;

                closest = cell;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private List<PixelView> ReconstructGridPath(
        Vector2Int end,
        Vector2Int start,
        Dictionary<Vector2Int, Vector2Int> previous)
    {
        List<PixelView> result = new();
        Vector2Int current = end;

        while (true) {
            PixelView cell = map.GetPixel(current.x, current.y);
            if (cell != null) result.Add(cell);
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
        float bottomY = trayBounds.min.y - trayEdgeOffset;
        float leftX = trayBounds.min.x - trayEdgeOffset;
        float rightX = trayBounds.max.x + trayEdgeOffset;
        float topY = trayBounds.max.y + trayEdgeOffset;

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
                AddDistinct(route, new Vector3(leftX, topY, antZ));
                AddDistinct(route, new Vector3(leftX, bottomY, antZ));
                break;
        }

        if (!map.TryGetHoleWorldBounds(out Bounds holeBounds)) return;

        Vector3 hole = AtAntDepth(holeBounds.center, antZ);
        // First line up above the hole, then enter it vertically. This avoids
        // cutting diagonally across neighbouring boxes or the tray edge.
        AddDistinct(route, new Vector3(hole.x, bottomY, antZ));
        AddDistinct(route, hole);
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

    private static Vector3 AtAntDepth(Vector3 point, float antZ)
    {
        return new Vector3(point.x, point.y, antZ);
    }
}
