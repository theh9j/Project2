using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class SpaceNavigation : MonoBehaviour
{
    [SerializeField] private MapCoordination map;

    [Header("Route spacing")]
    [Min(0.01f)]
    [SerializeField] private float trayEdgeOffset = 0.25f;

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

        List<RouteCandidate> candidates = map.ExposedPixels
            .Where(pixel => pixel != null && pixel.IsAvailableFor(color))
            .Select(pixel => CreateCandidate(pixel, start, trayBounds))
            .Where(candidate => candidate.IsValid)
            .ToList();

        if (candidates.Count == 0) return false;

        List<RouteCandidate> bottomCandidates = candidates
            .Where(candidate => candidate.OpeningSide == TraySide.Bottom)
            .ToList();

        RouteCandidate chosen = (bottomCandidates.Count > 0
                ? bottomCandidates
                : candidates)
            .OrderBy(candidate => candidate.Distance)
            .First();

        if (!chosen.Pixel.TryReserve()) return false;

        target = chosen.Pixel;
        route = chosen.Route;
        return true;
    }

    private RouteCandidate CreateCandidate(
        PixelView pixel,
        Vector3 start,
        Bounds trayBounds) {
        if (!map.TryGetOpening(pixel, out TraySide openingSide, out Vector3 opening)) {
            return default;
        }

        List<Vector3> route = BuildRoute(start, opening, openingSide, trayBounds);
        AppendPixelCollectionSimulation(
            route,
            pixel,
            opening,
            openingSide,
            trayBounds,
            start.z);
        float distance = GetRouteDistance(start, route);
        return new RouteCandidate(pixel, openingSide, route, distance);
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

    private static float GetRouteDistance(Vector3 start, List<Vector3> route) {
        float distance = 0f;
        Vector3 previous = start;

        foreach (Vector3 point in route) {
            distance += Vector3.Distance(previous, point);
            previous = point;
        }

        return distance;
    }

    private readonly struct RouteCandidate {
        public PixelView Pixel { get; }
        public TraySide OpeningSide { get; }
        public List<Vector3> Route { get; }
        public float Distance { get; }
        public bool IsValid => Pixel != null && Route != null;

        public RouteCandidate(
            PixelView pixel,
            TraySide openingSide,
            List<Vector3> route,
            float distance) {
            Pixel = pixel;
            OpeningSide = openingSide;
            Route = route;
            Distance = distance;
        }
    }

}
