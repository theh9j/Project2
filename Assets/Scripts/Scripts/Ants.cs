using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ants : MonoBehaviour
{
    public static Ants Instance;

    [SerializeField] private SpaceNavigation navigation;
    [SerializeField] private BoxManagementSystem boxManager;

    [Header("Fast Forward")]
    [Min(1f)]
    [SerializeField] private float fastForwardAntSpeedMultiplier = 2f;
    [Range(0.01f, 1f)]
    [SerializeField] private float fastForwardSpawnIntervalMultiplier = .5f;
    public event Action NoAnts;
    private List<Ant> listOfAnts = new();
    public bool FastForwardActive => boxManager != null && boxManager.FastForward;
    public float AntSpeedMultiplier => FastForwardActive ? fastForwardAntSpeedMultiplier : 1f;
    public float SpawnIntervalMultiplier => FastForwardActive ? fastForwardSpawnIntervalMultiplier : 1f;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (navigation == null) {
            navigation = FindAnyObjectByType<SpaceNavigation>();
        }

        if (boxManager == null) {
            boxManager = FindAnyObjectByType<BoxManagementSystem>();
        }
    }

    public bool TryReserveTarget(
        ColorType color,
        Vector3 spawnPosition,
        out PixelView pixel,
        out List<Vector3> route) {
        pixel = null;
        route = null;

        return navigation != null &&
               navigation.TryReserveTarget(color, spawnPosition, out pixel, out route);
    }

    public void Add(Ant ant) {
        listOfAnts ??= new();

        listOfAnts.Add(ant);
        ant.Finished += (a) => {
            listOfAnts.Remove(a);
            Destroy(a.gameObject);

            if (listOfAnts.Count == 0) NoAnts.Invoke();
        };
    }

    public void KillAnts(Func<Ant, bool> condition) {
        List<Ant> antsToKill = listOfAnts
            .Where(ant => ant != null && condition(ant))
            .ToList();

        foreach (Ant ant in antsToKill) {
            ant.OnComplete();
        }
    }

    public int GetAntCount => listOfAnts.Count;
}
