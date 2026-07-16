using System;
using System.Collections.Generic;
using UnityEngine;

public class Ant : MonoBehaviour
{
    [Header("Ant's Body Parts")]
    [SerializeField] private Transform pickUpPoint;
    [SerializeField] private Transform mainBody;

    [SerializeField] private Transform upperLeftLeg;
    [SerializeField] private Transform upperRightLeg;

    [SerializeField] private Transform middleLeftLeg;
    [SerializeField] private Transform middleRightLeg;

    [SerializeField] private Transform bottomLeftLeg;
    [SerializeField] private Transform bottomRightLeg;

    [SerializeField] private Animator animator;

    [Header("Movement")]
    [Min(0.01f)]
    [SerializeField] private float moveSpeed = 2f;

    [Min(0.01f)]
    [SerializeField] private float stoppingDistance = 0.03f;

    [Min(1f)]
    [SerializeField] private float turnSpeed = 720f;

    [Min(0.01f)]
    [SerializeField] private float pickupDistance = 0.12f;

    private List<Transform> bodyParts;
    private List<Renderer> renderers = new();
    private readonly Queue<Vector3> route = new();
    private bool completing;
    public event Action<Ant> Finished;
    public ColorType Color { get; private set; } = ColorType.White;
    public PixelView TargetPixel { get; private set; }
    public PixelView Pixel { get; private set; }

    void Awake() {
        animator.SetLayerWeight(1, 0f);
    }

    void Update() {
        FollowRoute();
        TryPickUpTarget();

        if (Pixel != null) {
            Pixel.transform.position = pickUpPoint.position;

            if (route.Count == 0 && !completing) {
                completing = true;
                OnComplete();
            }
        }
    }

    private void Init() {
        bodyParts = new() {
            mainBody,

            upperLeftLeg,
            upperRightLeg,

            middleLeftLeg,
            middleRightLeg,

            bottomLeftLeg,
            bottomRightLeg
        };

        foreach (Transform t in bodyParts) {
            renderers.Add(t.GetComponent<Renderer>());
        }
    }

    public void SetAntColor(ColorType colorType, Color color) {
        if (bodyParts == null) Init();

        foreach (Renderer render in renderers) {
            MaterialPropertyBlock material = new();
            render.GetPropertyBlock(material);
            material.SetColor("_BaseColor", color);
            render.SetPropertyBlock(material);
        }
        Color = colorType;
    }

    public void AssignTarget(PixelView pixel, IEnumerable<Vector3> waypoints) {
        TargetPixel = pixel;
        route.Clear();

        if (waypoints == null) return;
        foreach (Vector3 waypoint in waypoints) {
            route.Enqueue(waypoint);
        }
    }

    public void Carry(PixelView pixel) {
        if (pixel == null) return;

        if (TargetPixel != null && TargetPixel != pixel) {
            TargetPixel.ReleaseReservation();
        }

        TargetPixel = pixel;
        pixel.MarkPickedUp();
        animator.SetLayerWeight(1, 1f);
        animator.SetTrigger("Carry");
        this.Pixel = pixel;
    }

    public void OnComplete() {
        Destroy(Pixel?.gameObject);
        Finished?.Invoke(this);
    }

    void OnDestroy() {
        if (TargetPixel != null && !TargetPixel.IsPickedUp) {
            TargetPixel.ReleaseReservation();
        }
    }

    private void FollowRoute() {
        if (route.Count == 0) return;

        Vector3 destination = route.Peek();
        Vector3 offset = destination - transform.position;
        offset.z = 0f;

        if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance) {
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);
            route.Dequeue();
            return;
        }

        Vector3 direction = offset.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    private void TryPickUpTarget() {
        if (Pixel != null || TargetPixel == null || TargetPixel.IsPickedUp) return;

        Vector2 offset = TargetPixel.transform.position - transform.position;
        if (offset.sqrMagnitude <= pickupDistance * pickupDistance) {
            Carry(TargetPixel);
        }
    }

}
