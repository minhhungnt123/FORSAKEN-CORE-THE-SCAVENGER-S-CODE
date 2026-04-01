using UnityEngine;

public class CameraController : MonoBehaviour
{
    void Start()
    {
        // Khóa chuột vào giữa màn hình và ẩn đi (chuẩn thao tác game TPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}