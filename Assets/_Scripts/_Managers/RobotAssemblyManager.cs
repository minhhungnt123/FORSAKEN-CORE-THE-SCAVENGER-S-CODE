using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// RobotAssemblyManager: Quản lý toàn bộ scene lắp ráp robot
/// - Quản lý camera và lighting
/// - Kiểm soát UI
/// - Xử lý hoàn thành lắp ráp
/// - Hỗ trợ xóa/reset module
/// </summary>
public class RobotAssemblyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button exitButton;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;     // Điểm nhìn vào robot
    [SerializeField] private float cameraDistance = 15f; // Zoom ra xa
    [SerializeField] private float cameraHeight = 4f;    // Nâng cao camera lên

    [Header("Lighting")]
    [SerializeField] private Light mainLight;
    [SerializeField] private Color assemblyRoomAmbient = new Color(0.6f, 0.6f, 0.7f);

    private ModuleSelector moduleSelector;
    private RobotModule selectedForDeletion;
    private bool isAssemblyComplete = false;
    private bool isFreeMode = false;
    private float freeModeYaw = 0f;
    private float freeModePitch = 20f;

    private void Start()
    {
        moduleSelector = GetComponent<ModuleSelector>();

        // Thiết lập UI
        if (titleText != null)
            titleText.text = "🤖 PHÒNG CHẾ TẠO ROBOT";

        if (instructionText != null)
            instructionText.text = "1. Chọn THÂN robot\n2. Chọn các bộ phận khác\n3. Nhấn XÁC NHẬN để hoàn thành";

        // Gắn các button callback
        if (resetButton != null)
            resetButton.onClick.AddListener(OnReset);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);

        // Thiết lập ánh sáng
        SetupLighting();

        // Ép khoảng cách mặc định xa ra (nếu Unity đang lưu giá trị cũ quá nhỏ)
        if (cameraDistance < 15f) cameraDistance = 25f;
        if (cameraHeight < 10f) cameraHeight = 15f;

        // Thiết lập camera
        SetupCamera();

        Debug.Log("[RobotAssemblyManager] Scene lắp ráp robot đã sẵn sàng!");
    }

    private void Update()
    {
        // Luôn bám tâm camera vào đúng vị trí của thân (Chassis) nếu nó đã được tạo ra
        if (moduleSelector != null && moduleSelector.GetCurrentChassis() != null && cameraTarget != null)
        {
            cameraTarget.position = moduleSelector.GetCurrentChassis().transform.position;
        }

        if (isFreeMode)
        {
            HandleFreeModeCamera();
            return;
        }

        // Cho phép xóa module bằng phím Delete
        if (Input.GetKeyDown(KeyCode.Delete) && selectedForDeletion != null)
        {
            DeleteModule(selectedForDeletion);
            selectedForDeletion = null;
        }

        // Cho phép xoay camera bằng chuột phải + di chuyển
        if (Input.GetMouseButton(1))
        {
            RotateCamera();
        }

        // Cho phép zoom bằng con lăn chuột
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ZoomCamera(scroll);
        }
    }

    private void ZoomCamera(float scrollAmount)
    {
        if (Camera.main == null || cameraTarget == null) return;
        
        // Tính toán khoảng cách mới
        float zoomSpeed = 3f;
        cameraDistance -= scrollAmount * zoomSpeed;
        
        // Giới hạn zoom (không cho zoom sát quá 5f, và xa nhất là 60f)
        cameraDistance = Mathf.Clamp(cameraDistance, 5f, 60f);
        
        // Giữ nguyên góc nhìn hiện tại, chỉ dịch chuyển dọc theo hướng nhìn
        Vector3 direction = (Camera.main.transform.position - cameraTarget.position).normalized;
        Camera.main.transform.position = cameraTarget.position + direction * cameraDistance;
    }

    private void SetupLighting()
    {
        if (mainLight == null)
            mainLight = FindObjectOfType<Light>();

        if (mainLight != null)
        {
            mainLight.intensity = 1.2f;
            mainLight.type = LightType.Directional;
            mainLight.transform.rotation = Quaternion.Euler(45, 45, 0);
        }

        RenderSettings.ambientLight = assemblyRoomAmbient;
    }

    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (cameraTarget == null)
        {
            // Tạo target nếu chưa có
            GameObject targetObj = new GameObject("CameraTarget");
            cameraTarget = targetObj.transform;
        }

        // Đặt tâm xoay đúng bằng vị trí spawn của model (để xoay tròn luôn xoay quanh chính giữa robot)
        if (moduleSelector != null)
        {
            cameraTarget.position = moduleSelector.GetSpawnPosition();
        }

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (cameraTarget == null || Camera.main == null) return;

        Camera mainCamera = Camera.main;
        Vector3 targetPos = cameraTarget.position;

        // Đặt camera ở phía TRƯỚC mặt robot (cộng thêm Vector3.forward)
        Vector3 cameraPos = targetPos + Vector3.forward * cameraDistance;
        cameraPos.y = targetPos.y + cameraHeight;

        mainCamera.transform.position = cameraPos;
        mainCamera.transform.LookAt(targetPos + Vector3.up * 2.5f);
    }

    private void RotateCamera()
    {
        if (Camera.main == null || cameraTarget == null) return;

        float mouseX = Input.GetAxis("Mouse X") * 2f;

        // Xoay quanh target (chỉ xoay ngang theo trục Y)
        Camera.main.transform.RotateAround(cameraTarget.position, Vector3.up, mouseX);
        
        // Luôn nhìn thẳng vào target để tránh bị lệch
        Camera.main.transform.LookAt(cameraTarget.position + Vector3.up * 2.5f);
    }

    /// <summary>
    /// Gọi khi lắp ráp hoàn thành
    /// </summary>
    public void OnAssemblyComplete(ChassisModule chassis)
    {
        isAssemblyComplete = true;
        Debug.Log("[RobotAssemblyManager] Lắp ráp hoàn tất! Chuyển sang chế độ đi tự do...");

        // --- Tự động đẩy robot lên khỏi mặt đất (tránh lún chân) ---
        if (chassis != null)
        {
            float lowestY = float.MaxValue;
            Renderer[] renderers = chassis.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r.bounds.min.y < lowestY)
                {
                    lowestY = r.bounds.min.y;
                }
            }

            // Giả định mặt đất ở quanh mức Y = 0 (hoặc vị trí spawn)
            float floorY = 0f; 
            if (lowestY < floorY && lowestY != float.MaxValue)
            {
                float offset = floorY - lowestY;
                chassis.transform.position += new Vector3(0, offset + 0.05f, 0); // Cộng dư 0.05f để chân chạm hờ trên mặt đất
                Debug.Log($"[RobotAssemblyManager] Tự động đẩy robot lên mặt đất. Offset: {offset}");
            }
        }

        isFreeMode = true;

        // Ẩn UI
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (resetButton != null) resetButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);

        // Đồng bộ góc camera hiện tại
        if (Camera.main != null)
        {
            freeModeYaw = Camera.main.transform.eulerAngles.y;
            freeModePitch = Camera.main.transform.eulerAngles.x;
        }

        // Khóa chuột
        var camController = FindFirstObjectByType<CameraController>();
        if (camController != null)
            camController.SetCursorState(true);
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Ẩn luôn UI của ModuleSelector (danh sách bên trái và nút xác nhận)
        var moduleSelectorUI = FindFirstObjectByType<ModuleSelector>();
        if (moduleSelectorUI != null)
        {
            // Tắt script ModuleSelector để ngừng nhận click
            moduleSelectorUI.enabled = false;
            
            // Tìm Canvas cha của confirmButton để tắt toàn bộ UI lắp ráp
            // Lưu ý: Tùy theo cấu trúc Canvas của bạn, nếu bạn có Canvas riêng cho Assembly thì tắt nó đi.
            // Để an toàn, tôi sẽ chỉ tắt gameObject của ModuleSelector nếu nó chứa Canvas, 
            // hoặc bạn tự nhóm UI vào 1 panel rồi tắt.
        }
    }

    /// <summary>
    /// Xóa một module khỏi robot
    /// </summary>
    private void DeleteModule(RobotModule module)
    {
        if (module is ChassisModule)
        {
            Debug.LogWarning("[RobotAssemblyManager] Không thể xóa thân robot!");
            return;
        }

        Debug.Log($"[RobotAssemblyManager] Xóa module: {module.moduleName}");
        Destroy(module.gameObject);
    }

    /// <summary>
    /// Reset toàn bộ scene - tạo lại từ đầu
    /// </summary>
    private void OnReset()
    {
        Debug.Log("[RobotAssemblyManager] Reset scene...");

        // Xóa tất cả module
        RobotModule[] allModules = FindObjectsOfType<RobotModule>();
        foreach (var module in allModules)
        {
            Destroy(module.gameObject);
        }

        // Reset trạng thái
        isAssemblyComplete = false;
        selectedForDeletion = null;

        if (instructionText != null)
            instructionText.text = "1. Chọn THÂN robot\n2. Chọn các bộ phận khác\n3. Nhấn XÁC NHẬN để hoàn thành";

        if (moduleSelector != null)
            moduleSelector.ResetSelector();

        Debug.Log("[RobotAssemblyManager] Reset hoàn tất!");
    }

    /// <summary>
    /// Thoát khỏi scene lắp ráp
    /// </summary>
    private void OnExit()
    {
        Debug.Log("[RobotAssemblyManager] Thoát scene lắp ráp...");
        SceneManager.LoadScene("MainMenu");
    }

    private void HandleFreeModeCamera()
    {
        if (Camera.main == null || cameraTarget == null) return;

        var inputManager = Object.FindFirstObjectByType<InputManager>();
        float lookX = 0f;
        float lookY = 0f;
        
        if (inputManager != null)
        {
            lookX = inputManager.LookInput.x;
            lookY = inputManager.LookInput.y;
        }
        else
        {
            lookX = Input.GetAxis("Mouse X");
            lookY = Input.GetAxis("Mouse Y");
        }

        float sensitivity = 2f;
        freeModeYaw += lookX * sensitivity;
        freeModePitch -= lookY * sensitivity;
        freeModePitch = Mathf.Clamp(freeModePitch, -20f, 60f);

        Vector3 dir = new Vector3(0, 0, -cameraDistance);
        Quaternion rotation = Quaternion.Euler(freeModePitch, freeModeYaw, 0);
        
        // Đặt camera sau lưng robot
        Vector3 finalPos = cameraTarget.position + Vector3.up * (cameraHeight * 0.5f) + rotation * dir;

        Camera.main.transform.position = finalPos;
        Camera.main.transform.LookAt(cameraTarget.position + Vector3.up * (cameraHeight * 0.5f));
    }

    /// <summary>
    /// Lấy module được chọn để xóa
    /// </summary>
    public void SelectModuleForDeletion(RobotModule module)
    {
        selectedForDeletion = module;
        Debug.Log($"[RobotAssemblyManager] Module được chọn để xóa: {module.moduleName} (nhấn Delete để xóa)");
    }

    /// <summary>
    /// Kiểm tra lắp ráp đã hoàn thành chưa
    /// </summary>
    public bool IsAssemblyComplete() => isAssemblyComplete;

    /// <summary>
    /// Public version của reset
    /// </summary>
    public void ResetAssembly()
    {
        OnReset();
    }

    /// <summary>
    /// Public version của exit
    /// </summary>
    public void ExitAssembly()
    {
        OnExit();
    }
}
