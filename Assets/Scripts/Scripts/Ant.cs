using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Ant : MonoBehaviour
{
    [Header("Ant's Body Parts")]
    [SerializeField] private Transform mainBody;

    [SerializeField] private Transform upperLeftLeg;
    [SerializeField] private Transform upperRightLeg;

    [SerializeField] private Transform middleLeftLeg;
    [SerializeField] private Transform middleRightLeg;

    [SerializeField] private Transform bottomLeftLeg;
    [SerializeField] private Transform bottomRightLeg;

    private List<Transform> bodyParts = new();
    private List<Renderer> renderers = new();

    void Awake() {
        bodyParts.Add(mainBody);

        bodyParts.Add(upperLeftLeg);
        bodyParts.Add(upperRightLeg);

        bodyParts.Add(middleLeftLeg);
        bodyParts.Add(middleRightLeg);

        bodyParts.Add(bottomLeftLeg);
        bodyParts.Add(bottomRightLeg);

        foreach (Transform t in bodyParts) {
            renderers.Add(t.GetComponent<Renderer>());
        }
    }

    public void SetAntColor(Color color) {
        foreach (Renderer render in renderers) {
            MaterialPropertyBlock material = new();
            render.GetPropertyBlock(material);
            material.SetColor("_BaseColor", color);
            render.SetPropertyBlock(material);
        }
    }

}
