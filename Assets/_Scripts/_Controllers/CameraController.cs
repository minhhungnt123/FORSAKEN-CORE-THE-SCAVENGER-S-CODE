using UnityEngine;

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

    private void Update()
    {
        // ESC: mo khoa chuot de keo module
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleCursorState();

        // Click chuot trai chi khoa lai khi KHONG co module nao dang duoc keo
        if (Input.GetMouseButtonDown(0) && !isCursorLocked)
        {
            // Kiem tra AssemblyManager co dang keo module khong
            var asm = FindFirstObjectByType<AssemblyManager>();
            bool isDragging = asm != null && asm.IsDragging;

            if (!isDragging)
                SetCursorState(true);
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