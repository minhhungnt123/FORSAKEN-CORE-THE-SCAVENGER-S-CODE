using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ModuleSelector: Quản lý việc chọn linh kiện từ bên trái màn hình
/// - Hiển thị danh sách các module có sẵn
/// - Cho phép chọn module để thêm vào robot
/// - Ưu tiên thân (Chassis) trước
/// </summary>
public class ModuleSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform modulesListContent;    // Content của Scroll View (bên trái)
    [SerializeField] private Button confirmButton;             // Nút xác nhận lắp ráp
    [SerializeField] private Text selectedModulesText;         // Hiển thị modules đã chọn

    [Header("Module Prefabs")]
    [SerializeField] private List<ChassisModule> chassisPrefabs = new();
    [SerializeField] private List<HeadModule> headPrefabs = new();
    [SerializeField] private List<RobotModule> armLeftPrefabs = new();
    [SerializeField] private List<RobotModule> armRightPrefabs = new();
    [SerializeField] private List<LegModule> legPrefabs = new();

    [Header("Settings")]
    [SerializeField] private Vector3 moduleSpawnPosition = new Vector3(0, 0, 0);  // Vị trí spawn module
    [SerializeField] private float selectionPanelWidth = 300f;

    private Dictionary<string, RobotModule> selectedModules = new();
    private RobotAssemblyManager assemblyManager;
    private ChassisModule currentChassis;

    private void Start()
    {
        assemblyManager = GetComponent<RobotAssemblyManager>();

        if (assemblyManager == null)
        {
            Debug.LogError("[ModuleSelector] Không tìm thấy RobotAssemblyManager trên GameObject này!");
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmAssembly);
        }

        Debug.Log("[ModuleSelector] Hệ thống lắp ráp đã sẵn sàng. Hãy chọn các linh kiện!");
    }

    /// <summary>
    /// Gọi khi người chơi click chọn một module từ danh sách
    /// </summary>
    public void SelectModule(string moduleTag, int index = 0)
    {
        // Ưu tiên Chassis trước
        if (moduleTag == "CHASSIS")
        {
            if (currentChassis != null)
            {
                Destroy(currentChassis.gameObject);
            }
            SpawnChassis(index);
            selectedModules["CHASSIS"] = currentChassis;
            UpdateSelectedModulesDisplay();
            return;
        }

        // Nếu chưa có Chassis thì phải chọn Chassis trước
        if (currentChassis == null)
        {
            Debug.LogWarning("[ModuleSelector] Phải chọn THÂN trước!");
            return;
        }

        // Hủy module cũ nếu đã chọn trước đó
        if (selectedModules.TryGetValue(moduleTag, out RobotModule oldModule) && oldModule != null)
        {
            Destroy(oldModule.gameObject);
        }

        // Spawn module mới (Head, Arm, Leg)
        RobotModule module = SpawnModule(moduleTag, index);
        if (module != null)
        {
            selectedModules[moduleTag] = module;
            UpdateSelectedModulesDisplay();
            Debug.Log($"[ModuleSelector] Đã đổi sang: {module.moduleName}");
        }
    }

    private void SpawnChassis(int index = 0)
    {
        if (chassisPrefabs == null || chassisPrefabs.Count <= index) return;
        var prefab = chassisPrefabs[index];
        if (prefab == null) return;

        currentChassis = Instantiate(prefab, moduleSpawnPosition, prefab.transform.rotation);
        currentChassis.gameObject.name = "Chassis_Assembly";
        Debug.Log("[ModuleSelector] Thân robot đã được tạo!");
    }

    private RobotModule SpawnModule(string moduleTag, int index = 0)
    {
        if (currentChassis == null)
            return null;

        RobotModule module = null;
        Vector3 spawnPos = moduleSpawnPosition;

        switch (moduleTag)
        {
            case "HEAD":
                if (headPrefabs.Count > index && headPrefabs[index] != null) {
                    module = Instantiate(headPrefabs[index], spawnPos, headPrefabs[index].transform.rotation);
                    module.gameObject.name = "Head_Assembly";
                }
                break;

            case "ARM_LEFT":
                if (armLeftPrefabs.Count > index && armLeftPrefabs[index] != null) {
                    module = Instantiate(armLeftPrefabs[index], spawnPos, armLeftPrefabs[index].transform.rotation);
                    module.gameObject.name = "ArmLeft_Assembly";
                }
                break;

            case "ARM_RIGHT":
                if (armRightPrefabs.Count > index && armRightPrefabs[index] != null) {
                    module = Instantiate(armRightPrefabs[index], spawnPos, armRightPrefabs[index].transform.rotation);
                    module.gameObject.name = "ArmRight_Assembly";
                }
                break;

            case "LEG":
                if (legPrefabs.Count > index && legPrefabs[index] != null) {
                    module = Instantiate(legPrefabs[index], spawnPos, legPrefabs[index].transform.rotation);
                    module.gameObject.name = "Leg_Assembly";
                }
                break;

            default:
                Debug.LogWarning($"[ModuleSelector] Loại module không xác định: {moduleTag}");
                return null;
        }

        return module;
    }

    /// <summary>
    /// Cập nhật hiển thị các modules đã chọn
    /// </summary>
    private void UpdateSelectedModulesDisplay()
    {
        string displayText = "Linh kiện đã chọn:\n";

        foreach (var kvp in selectedModules)
        {
            if (kvp.Value != null)
                displayText += $"✓ {kvp.Value.moduleName}\n";
        }

        if (selectedModulesText != null)
            selectedModulesText.text = displayText;
    }

    private string GetSocketTagFromModuleKey(string key) => key switch
    {
        "HEAD" => "HEAD_SOCKET",
        "ARM_LEFT" => "ARM_L_SOCKET",
        "ARM_RIGHT" => "ARM_R_SOCKET",
        "LEG" => "LEG_SOCKET",
        _ => ""
    };

    /// <summary>
    /// Gọi khi người chơi nhấn nút "Xác nhận"
    /// Tiến hành kết nối tất cả các module với nhau
    /// </summary>
    private void OnConfirmAssembly()
    {
        if (currentChassis == null)
        {
            Debug.LogWarning("[ModuleSelector] Chưa chọn THÂN!");
            return;
        }

        Debug.Log("[ModuleSelector] Bắt đầu lắp ráp robot...");

        // Kết nối tất cả module với Chassis
        foreach (var kvp in selectedModules)
        {
            if (kvp.Key != "CHASSIS" && kvp.Value != null)
            {
                // Tự động ép socket chuẩn xác dựa theo nút người dùng đã bấm trên UI
                // Bỏ qua cài đặt Left/Right bên trong Prefab để tránh rủi ro người dùng set nhầm
                string targetSocket = GetSocketTagFromModuleKey(kvp.Key);
                bool success = currentChassis.TryEquip(kvp.Value, targetSocket);
                if (success)
                {
                    Debug.Log($"✓ Đã kết nối: {kvp.Value.moduleName}");
                }
                else
                {
                    Debug.LogWarning($"✗ Lỗi kết nối: {kvp.Value.moduleName}");
                }
            }
        }

        // Kiểm tra robot đã đầy đủ 4 phần chưa
        if (currentChassis.IsFullyAssembled())
        {
            Debug.Log("🎉 Robot đã được lắp ráp hoàn chỉnh!");
            // Vô hiệu hóa nút xác nhận nếu có
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            // TODO: Có thể chuyển sang scene tiếp theo hoặc hiển thị thông báo
            if (assemblyManager != null)
                assemblyManager.OnAssemblyComplete(currentChassis);
        }
        else
        {
            Debug.LogWarning("⚠️ Robot chưa đầy đủ các bộ phận!");
        }
    }

    /// <summary>
    /// Lấy Chassis hiện tại
    /// </summary>
    public ChassisModule GetCurrentChassis() => currentChassis;

    /// <summary>
    /// Lấy vị trí tâm spawn
    /// </summary>
    public Vector3 GetSpawnPosition() => moduleSpawnPosition;

    /// <summary>
    /// Lấy danh sách module đã chọn
    /// </summary>
    public Dictionary<string, RobotModule> GetSelectedModules() => selectedModules;

    /// <summary>
    /// Reset selector về trạng thái ban đầu
    /// </summary>
    public void ResetSelector()
    {
        selectedModules.Clear();
        currentChassis = null;
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        Debug.Log("[ModuleSelector] Selector đã được reset!");
    }

    /// <summary>
    /// Xác nhận lắp ráp - public version
    /// </summary>
    public void ConfirmAssembly()
    {
        OnConfirmAssembly();
    }

    // --- API cho AssemblyUIBuilder đọc danh sách ---

    public int GetModuleCount(string legacyTag)
    {
        return legacyTag switch
        {
            "CHASSIS" => chassisPrefabs.Count,
            "HEAD" => headPrefabs.Count,
            "ARM_LEFT" => armLeftPrefabs.Count,
            "ARM_RIGHT" => armRightPrefabs.Count,
            "LEG" => legPrefabs.Count,
            _ => 0
        };
    }

    public string GetModulePrefabName(string legacyTag, int index)
    {
        RobotModule prefab = legacyTag switch
        {
            "CHASSIS" => (chassisPrefabs.Count > index) ? chassisPrefabs[index] : null,
            "HEAD" => (headPrefabs.Count > index) ? headPrefabs[index] : null,
            "ARM_LEFT" => (armLeftPrefabs.Count > index) ? armLeftPrefabs[index] : null,
            "ARM_RIGHT" => (armRightPrefabs.Count > index) ? armRightPrefabs[index] : null,
            "LEG" => (legPrefabs.Count > index) ? legPrefabs[index] : null,
            _ => null
        };

        if (prefab != null && !string.IsNullOrEmpty(prefab.moduleName))
        {
            return prefab.moduleName;
        }
        return $"Module {index + 1}";
    }
}
