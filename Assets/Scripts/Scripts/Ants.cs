using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ants : MonoBehaviour
{
    public static Ants Instance;

    [SerializeField] private SpaceNavigation navigation;
    private List<Ant> listOfAnts = new();

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (navigation == null) {
            navigation = FindAnyObjectByType<SpaceNavigation>();
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
}
