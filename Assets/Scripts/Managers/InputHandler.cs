using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }
    [SerializeField] private AdministrationHandler administration;

    private readonly List<RaycastResult> uiRaycastResults = new();

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update() {
        switch (administration.State) {
                case EditorState.Basic:
                    if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
                        InputProcess();
                    break;  
                case EditorState.Drawing:
                    if ((Mouse.current != null && Mouse.current.leftButton.isPressed))
                        Drawing();
                    break;
            }
    }

    private bool IsPointerOverUI(Vector2 screenPos) {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new(EventSystem.current) {
            position = screenPos
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        foreach (RaycastResult result in uiRaycastResults) {
            if (result.gameObject.GetComponentInParent<Selectable>() != null) {
                return true;
            }
        }

        return false;
    }

    private bool TryGetPointerPosition(out Vector2 screenPos) {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null) {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
    }

    private Vector3 PointerDirection() {
        if (!TryGetPointerPosition(out Vector2 screenPos)) return Vector3.zero;
        if (IsPointerOverUI(screenPos)) return Vector3.zero;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;
        
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    private void InputProcess() {
        RaycastHit2D hit = Physics2D.Raycast(PointerDirection(), Vector3.zero);

#if UNITY_EDITOR
        if (administration != null && administration.gameObject.activeSelf) {
            if (hit.collider == null) { 
                administration.ResetConfig();
                return;
            }

            AdministrationInput(hit);
            return;
        }
#endif

        if (hit.collider == null) {
            return;
        }




        //INPUT PROCESSING
    }


    private void AdministrationInput(RaycastHit2D hit) {

        BoxConfiguration box = hit.collider.GetComponent<BoxConfiguration>();
        if (box != null) {
            administration.ResetConfig();
            administration.SetBox(box);
        }
    }

    private void Drawing() {
#if !UNITY_EDITOR
        administration.ChangeMode(EditorState.Basic);
        Debug.Log("User not authorized");
        return;
#endif
        if (administration == null) return;

        Vector2 worldPosition = PointerDirection();

        Collider2D hitCollider = Physics2D.OverlapPoint(worldPosition);

        if (hitCollider == null) {
            return;
        }

        if (!hitCollider.TryGetComponent(out PixelView pixel)) {
            return;
        }

        administration.Draw(pixel);
    }

}
