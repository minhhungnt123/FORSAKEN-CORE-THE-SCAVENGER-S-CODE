using UnityEngine;

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
    }
    // Cập nhật mỗi frame để kiểm tra input thay đổi trạng thái chuột
    private void Update()
    {
        // Ví dụ: Bấm phím ESC để Mở/Khóa chuột tạm thời
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorState();
        }

        // Bấm chuột trái để khóa lại (chuẩn thao tác game bắn súng)
        if (Input.GetMouseButtonDown(0) && !isCursorLocked)
        {
            SetCursorState(true);
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