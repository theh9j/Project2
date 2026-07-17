using System.Collections.Generic;
using UnityEngine;

public partial class SpaceNavigation : MonoBehaviour
{
    [SerializeField] private MapCoordination map;

    [Header("Route spacing")]
    [Min(0.01f)]
    [SerializeField] private float trayEdgeOffset = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float holeClearance = 0.2f;

    void Awake() {
        if (map == null) {
            map = GetComponent<MapCoordination>();
        }
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

        // All ants first reach the staging line beneath the tray. They only
        // choose a horizontal direction after arriving at this bottom section.
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
                // Top openings deliberately approach from the left side.
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

        // Stay in the box's lane until the ant actually reaches the hole.
        // Without this waypoint, the ant starts moving sideways immediately
        // after spawning, even when the obstacle is still far away.
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

}
