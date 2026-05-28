using UnityEngine;
using UnityEngine.InputSystem;
using RoboticsProject.Managers;

public class CameraController : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("Tự động khóa chuột khi bắt đầu game")]
    [SerializeField] private bool lockCursorOnStart = true;

    // Biến lưu trữ trạng thái hiện tại của chuột
    private bool isCursorLocked;

    // Khởi tạo trạng thái chuột khi bắt đầu
    private void Start()
    {
        if (lockCursorOnStart)
        {
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
        }

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

    /// <summary>
    /// Hàm điều khiển trạng thái chuột
    /// </summary>
    /// <param name="isLocked">true: Ẩn và khóa, false: Hiện và mở khóa</param>
    public void SetCursorState(bool isLocked)
    {
        isCursorLocked = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    /// <summary>
    /// Đảo ngược trạng thái chuột hiện tại
    /// </summary>
    private void ToggleCursorState()
    {
        SetCursorState(!isCursorLocked);
    }
}