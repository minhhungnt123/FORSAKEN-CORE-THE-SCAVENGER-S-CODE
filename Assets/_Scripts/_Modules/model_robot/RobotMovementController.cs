// FILE NÀY ĐÃ ĐƯỢC THAY THẾ BỞI RobotController.cs
// Giữ lại file rỗng để tránh lỗi reference trong scene.
// Không xóa file này — xóa thủ công từ Unity Editor nếu muốn dọn dẹp.
using UnityEngine;

/// <summary>DEPRECATED – Dùng RobotController.cs thay thế.</summary>
public class RobotMovementController : MonoBehaviour
{
    [System.NonSerialized] public float  Speed             = 0f;
    [System.NonSerialized] public bool   IsMoving          = false;
    [System.NonSerialized] public bool   IsSprinting       = false;
    [System.NonSerialized] public Vector3 MoveDirectionLocal = Vector3.zero;
}
