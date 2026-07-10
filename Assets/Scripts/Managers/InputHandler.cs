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
        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)) 
            InputProcess();
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

    private void InputProcess() {
        if (!TryGetPointerPosition(out Vector2 screenPos)) return;
        if (IsPointerOverUI(screenPos)) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

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
}
