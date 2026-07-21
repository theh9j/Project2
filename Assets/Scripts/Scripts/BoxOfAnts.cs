using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class Box : MonoBehaviour
{
    [Header("Ants")]
    [SerializeField] private Transform antsSpawner;
    [SerializeField] private GameObject antPrefab;

    [Min(2), Tooltip("Higher number means higher chances for spawning")]
    [SerializeField] private int change = 5;
    [SerializeField] private float timeTilAnt = .2f;
    private float t;
    private List<Ant> antsList = new();
    private Transform antNest;
    private bool release;

    void Update() {
        if (!release) return;
        if (Amount <= 0) return;
        t += Time.deltaTime;

        float spawnIntervalMultiplier = Ants.Instance != null ?
            Ants.Instance.SpawnIntervalMultiplier :
            1f;

        float spawnInterval = Mathf.Max(.01f, timeTilAnt * spawnIntervalMultiplier);
        if (t > spawnInterval) {
            if (Random.Range(0, change) != 1) {
                if (OnAntSpawn()) {
                    Decrease(1);
                }
            }
            t = 0;
        }
    }

    public void OnRelease() {
        release = true;
    }

    private bool OnAntSpawn() {
        if (Ants.Instance == null || antPrefab == null || antsSpawner == null) {
            return false;
        }

        if (!Ants.Instance.TryReserveTarget(
                Color,
                antsSpawner.position,
                out PixelView target,
                out List<Vector3> route)) {
            return false;
        }

        GameObject ant = Instantiate(antPrefab, antsSpawner.position, Quaternion.identity, antNest);
        Ant antSelf = ant.GetComponent<Ant>();
        if (antSelf == null) {
            target.ReleaseReservation();
            Destroy(ant);
            return false;
        }

        antSelf.SetAntColor(Color, boxVisualColor);
        antSelf.AssignTarget(target, route);

        Ants.Instance.Add(antSelf);
        return true;
    }

    public void CheckForPossibleDeath() {
        if (Amount > 0) return;
        if (Link != null && Link.Amount > 0) return;

        Link?.Animation(BoxAnimationState.Killed);
        Animation(BoxAnimationState.Killed);
    }

}
