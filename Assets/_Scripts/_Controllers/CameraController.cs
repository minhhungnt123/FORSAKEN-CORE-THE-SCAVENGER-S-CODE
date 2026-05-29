using UnityEngine;
using UnityEngine.InputSystem;
using RoboticsProject.Managers;

public class CameraController : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursorOnStart = true;

    // Đọc trực tiếp từ trạng thái hệ thống để tránh lỗi bất đồng bộ giữa các UI khác nhau
    private bool isCursorLocked => Cursor.lockState == CursorLockMode.Locked;

    private void Start()
    {
        if (lockCursorOnStart)
            SetCursorState(true);

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
        }

        // Bấm chuột trái để khóa lại (chuẩn thao tác game bắn súng)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
        {
            // Chỉ khóa lại khi không có giao diện UI nào đang mở
            if (InputManager.Instance == null || !InputManager.Instance.IsGameplayInputBlocked)
            {
                SetCursorState(true);
            }
        }
    }

    public void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void ToggleCursorState()
    {
        SetCursorState(!isCursorLocked);
    }
}