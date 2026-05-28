using UnityEngine;
using UnityEngine.InputSystem;

public class AssemblyManager : MonoBehaviour
{
    [Header("Cai dat Lap rap")]
    public float snapDistance = 1.5f;
    public float dragSpeed = 15f;

    [Header("Tham chieu den Robot Root")]
    public ChassisModule chassis;

    // Public de CameraController kiem tra
    public bool IsDragging => selectedModule != null;

    private Camera mainCamera;
    private RobotModule selectedModule;
    private float zDistanceToCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (chassis == null)
            Debug.LogError("[AssemblyManager] Chua gan ChassisModule!");
    }

    void Update()
    {
        HandleSelection();
        HandleDragging();
        HandleRelease();
    }

    private void HandleSelection()
    {
        // Chi cho phep chon module khi chuot dang mo khoa (visible)
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Cursor.lockState == CursorLockMode.Locked) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        RobotModule module = hit.collider.GetComponentInParent<RobotModule>();
        if (module == null || module is ChassisModule) return;

        selectedModule = module;
        selectedModule.Disconnect();
        zDistanceToCamera = mainCamera.WorldToScreenPoint(selectedModule.transform.position).z;
        selectedModule.GetComponent<Rigidbody>().isKinematic = true;

        Debug.Log($"[Assembly] Dang keo: {selectedModule.moduleName}");
    }

    private void HandleDragging()
    {
        if (selectedModule == null || !Mouse.current.leftButton.isPressed) return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = zDistanceToCamera;
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        selectedModule.transform.position = Vector3.Lerp(
            selectedModule.transform.position,
            targetPos,
            Time.deltaTime * dragSpeed
        );
    }

    private void HandleRelease()
    {
        if (selectedModule == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        float distToChassis = Vector3.Distance(
            selectedModule.transform.position,
            chassis.transform.position
        );

        bool snapped = false;
        if (distToChassis <= snapDistance * 3f)
            snapped = chassis.TryEquip(selectedModule);

        if (!snapped)
        {
            selectedModule.GetComponent<Rigidbody>().isKinematic = false;
            Debug.Log($"[Assembly] {selectedModule.moduleName} khong snap duoc.");
        }

        selectedModule = null;
    }
}