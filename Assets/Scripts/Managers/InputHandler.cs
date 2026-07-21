using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }
    [SerializeField] private AdministrationHandler administration;
    [SerializeField] private BoxManagementSystem boxManager;
    [SerializeField] private WaitingSlotsManagementSystem waitManager;

    [Min(0f)]
    [SerializeField] private float inputDelay = 0f;

    private readonly List<RaycastResult> uiRaycastResults = new();
    private float t = 0f;
    private Box clickedBox;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update() {
#if UNITY_EDITOR
        float scroll = Mouse.current != null && 
            (Keyboard.current != null && Keyboard.current[Key.LeftCtrl].isPressed) ? 
            Mouse.current.scroll.ReadValue().y : 0;

        if (scroll > 0f) boxManager.Scroll(true);
        else if (scroll < 0f) boxManager.Scroll(false);
#endif

        t += Time.deltaTime;

        if (t < inputDelay) return;

        switch (administration.State) {
                case EditorState.Basic:
                    if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)) {
                        t = 0f;
                        InputPressProcess();
                    }

                    if ((Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) ||
                        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)) {
                        t = 0f;
                        InputReleaseProcess();
                    }
                
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

    private void InputReleaseProcess() {
        if (!TryGetPointerPosition(out Vector2 screenPos)) return;
        if (IsPointerOverUI(screenPos)) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector3.zero);
        clickedBox?.OnPress(false);
        clickedBox = null;

#if UNITY_EDITOR
        if (administration != null && administration.gameObject.activeSelf) return;
#endif


        if (hit.collider == null) {
            return;
        }

        Box box = hit.collider.GetComponent<Box>();
        if (box != null && box.Interactable) {
            waitManager.AddBoxToAvailablePlate(box);
        }
    }

    private void InputPressProcess() {
        if (!TryGetPointerPosition(out Vector2 screenPos)) return;
        if (IsPointerOverUI(screenPos)) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector3.zero);


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

        Box box = hit.collider.GetComponent<Box>();
        if (box != null && box.Interactable) {
            clickedBox = box;
            clickedBox.OnPress(true);
        }

    }

    private void AdministrationInput(RaycastHit2D hit) {

        Box box = hit.collider.GetComponent<Box>();
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
