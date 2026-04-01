using UnityEngine;
using UnityEngine.InputSystem; // Thêm thư viện này

public class AssemblyManager : MonoBehaviour
{
    [Header("Cài đặt Lắp ráp")]
    public float snapDistance = 1.5f;
    public float dragSpeed = 15f;

    private Camera mainCamera;
    private RobotModule selectedModule;
    private float zDistanceToCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Kiểm tra xem có chuột không để tránh lỗi Null
        if (Mouse.current == null) return;

        HandleSelection();
        HandleDragging();
        HandleRelease();
    }

    private void HandleSelection()
    {
        // Thay thế Input.GetMouseButtonDown(0)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Thay thế Input.mousePosition
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RobotModule module = hit.collider.GetComponent<RobotModule>();

                if (module != null)
                {
                    selectedModule = module;
                    selectedModule.Disconnect();

                    zDistanceToCamera = mainCamera.WorldToScreenPoint(selectedModule.transform.position).z;

                    Rigidbody rb = selectedModule.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }
    }

    private void HandleDragging()
    {
        // Thay thế Input.GetMouseButton(0)
        if (Mouse.current.leftButton.isPressed && selectedModule != null)
        {
            // Lấy vị trí chuột hiện tại
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = zDistanceToCamera;
            Vector3 targetPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

            selectedModule.transform.position = Vector3.Lerp(selectedModule.transform.position, targetPos, Time.deltaTime * dragSpeed);
        }
    }

    private void HandleRelease()
    {
        // Thay thế Input.GetMouseButtonUp(0)
        if (Mouse.current.leftButton.wasReleasedThisFrame && selectedModule != null)
        {
            Transform closestSocket = FindClosestSocket(selectedModule.transform.position);

            if (closestSocket != null)
            {
                selectedModule.Connect(closestSocket);
            }
            else
            {
                Rigidbody rb = selectedModule.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }

            selectedModule = null;
        }
    }

    private Transform FindClosestSocket(Vector3 currentPosition)
    {
        Transform bestSocket = null;
        float minDistance = snapDistance;

        RobotModule[] allModules = FindObjectsByType<RobotModule>(FindObjectsSortMode.None);

        foreach (var module in allModules)
        {
            if (module == selectedModule) continue;

            foreach (var socket in module.availableSockets)
            {
                if (socket == null) continue;

                float dist = Vector3.Distance(currentPosition, socket.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSocket = socket;
                }
            }
        }

        return bestSocket;
    }
}