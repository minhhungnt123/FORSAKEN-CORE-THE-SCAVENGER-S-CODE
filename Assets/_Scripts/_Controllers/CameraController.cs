using UnityEngine;
using UnityEngine.InputSystem;
using RoboticsProject.Managers;

public class CameraController : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursorOnStart = true;

    private bool isCursorLocked;

    private void Start()
    {
        if (lockCursorOnStart)
            SetCursorState(true);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryToggle += HandleInventoryToggle;
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryToggle -= HandleInventoryToggle;
        }
    }

    private void HandleInventoryToggle(bool isOpen)
    {
        SetCursorState(!isOpen);
    }

    private void Update()
    {
        // Ví dụ: Bấm phím ESC để Mở/Khóa chuột tạm thời
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleCursorState();

        // Bấm chuột trái để khóa lại (chuẩn thao tác game bắn súng)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
        {
            // Chỉ khóa lại khi túi đồ không mở
            if (InventoryManager.Instance == null || !InventoryManager.Instance.IsOpen)
            {
                SetCursorState(true);
            }
        }
    }

    public void SetCursorState(bool isLocked)
    {
        isCursorLocked = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void ToggleCursorState()
    {
        SetCursorState(!isCursorLocked);
    }
}