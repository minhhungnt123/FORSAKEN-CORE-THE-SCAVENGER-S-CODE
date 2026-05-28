using UnityEngine;

/// <summary>
/// EXAMPLE: Cách sử dụng Assembly System trong game
/// 
/// File này là tài liệu + ví dụ code, KHÔNG cần gắn vào scene
/// Chỉ để tham khảo cách implement
/// </summary>
public class AssemblySystemExample : MonoBehaviour
{
    // EXAMPLE 1: Lắp ráp robot từ code
    // ================================================
    public void Example_ProgrammaticAssembly()
    {
        // Lấy ModuleSelector
        ModuleSelector selector = FindObjectOfType<ModuleSelector>();

        // 1. Chọn Chassis trước
        selector.SelectModule("CHASSIS");
        Debug.Log("✓ Chassis được chọn");

        // 2. Chọn các module khác
        selector.SelectModule("HEAD");
        selector.SelectModule("ARM_LEFT");
        selector.SelectModule("ARM_RIGHT");
        selector.SelectModule("LEG");

        // 3. Xác nhận lắp ráp
        selector.ConfirmAssembly();
    }

    // EXAMPLE 2: Kiểm tra trạng thái robot
    // ================================================
    public void Example_CheckRobotStatus()
    {
        ModuleSelector selector = FindObjectOfType<ModuleSelector>();
        ChassisModule chassis = selector.GetCurrentChassis();

        if (chassis == null)
        {
            Debug.Log("Robot chưa được tạo");
            return;
        }

        // Kiểm tra xem robot đã hoàn chỉnh chưa
        if (chassis.IsFullyAssembled())
        {
            Debug.Log("🎉 Robot đã hoàn chỉnh!");
            Debug.Log($"Tổng khối lượng: {chassis.GetTotalMass()} kg");
        }
        else
        {
            Debug.Log("⚠️ Robot chưa hoàn chỉnh!");

            // Kiểm tra từng bộ phận
            var head = chassis.GetEquipped("HEAD_SOCKET");
            var armL = chassis.GetEquipped("ARM_L_SOCKET");
            var armR = chassis.GetEquipped("ARM_R_SOCKET");
            var leg = chassis.GetEquipped("LEG_SOCKET");

            if (head == null) Debug.Log("  ✗ Thiếu: ĐẦU");
            if (armL == null) Debug.Log("  ✗ Thiếu: TAY TRÁI");
            if (armR == null) Debug.Log("  ✗ Thiếu: TAY PHẢI");
            if (leg == null) Debug.Log("  ✗ Thiếu: CHÂN");
        }
    }

    // EXAMPLE 3: Thay thế module
    // ================================================
    public void Example_ReplaceModule()
    {
        ModuleSelector selector = FindObjectOfType<ModuleSelector>();
        ChassisModule chassis = selector.GetCurrentChassis();

        if (chassis == null) return;

        // Lấy đầu hiện tại
        RobotModule oldHead = chassis.GetEquipped("HEAD_SOCKET");

        if (oldHead != null)
        {
            Debug.Log($"Tháo: {oldHead.moduleName}");
            chassis.Detach("HEAD_SOCKET");
        }

        // Lắp đầu mới
        selector.SelectModule("HEAD");
        // Xác nhận để kết nối
        selector.ConfirmAssembly();
    }

    // EXAMPLE 4: Callback khi hoàn thành lắp ráp
    // ================================================
    private void Start()
    {
        // Có thể subscribe vào event hoàn thành
        RobotAssemblyManager manager = FindObjectOfType<RobotAssemblyManager>();

        if (manager != null)
        {
            // Note: Có thể thêm event vào RobotAssemblyManager
            // manager.OnAssemblyCompleted += HandleAssemblyComplete;
        }
    }

    private void HandleAssemblyComplete(ChassisModule chassis)
    {
        Debug.Log($"Robot lắp ráp xong! Tổng mass: {chassis.GetTotalMass()}kg");

        // Ở đây có thể:
        // - Chuyển scene sang gameplay
        // - Khởi tạo AI điều khiển robot
        // - Unlock tính năng trong game
        // - v.v...
    }

    // EXAMPLE 5: Debug scene
    // ================================================
    public void Example_DebugAssembly()
    {
        ModuleSelector selector = FindObjectOfType<ModuleSelector>();

        var selectedModules = selector.GetSelectedModules();
        Debug.Log("=== SELECTED MODULES ===");
        foreach (var kvp in selectedModules)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value?.moduleName ?? "NULL"}");
        }

        ChassisModule chassis = selector.GetCurrentChassis();
        if (chassis != null)
        {
            Debug.Log($"\n=== CHASSIS INFO ===");
            Debug.Log($"Chassis Mass: {chassis.moduleMass}");
            Debug.Log($"Total Mass: {chassis.GetTotalMass()}");
            Debug.Log($"Fully Assembled: {chassis.IsFullyAssembled()}");
        }
    }

    // EXAMPLE 6: Custom Module Creation
    // ================================================
    public void Example_CustomModuleSpawn()
    {
        // Nếu muốn spawn module không qua UI
        // Có thể tạo prefab custom:

        // ChassisModule chassisModule = 
        //     Instantiate(chassisPrefab, spawnPos, Quaternion.identity);
        // 
        // HeadModule headModule = 
        //     Instantiate(headPrefab, spawnPos + Vector3.up, Quaternion.identity);
        // 
        // // Kết nối trực tiếp
        // bool success = chassisModule.TryEquip(headModule);
        // Debug.Log($"Kết nối: {(success ? "Thành công" : "Thất bại")}");
    }

    // EXAMPLE 7: Module Physics
    // ================================================
    public void Example_PhysicsSettings()
    {
        ModuleSelector selector = FindObjectOfType<ModuleSelector>();
        ChassisModule chassis = selector.GetCurrentChassis();

        if (chassis != null)
        {
            Rigidbody rb = chassis.GetComponent<Rigidbody>();

            // Kiểm tra vật lý
            Debug.Log($"Rigidbody Active: {!rb.isKinematic}");
            Debug.Log($"Rigidbody Mass: {rb.mass}");
            Debug.Log($"Gravity Enabled: {rb.useGravity}");

            // Có thể sửa settings
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;
        }
    }

    // EXAMPLE 8: UI Integration
    // ================================================
    public void Example_UIIntegration()
    {
        // Trong UI script của bạn, có thể gọi:

        ModuleSelector selector = FindObjectOfType<ModuleSelector>();

        // Khi nhấn nút "Start Assembly"
        // selector.ResetSelector();

        // Khi nhấn nút "Cancel"
        // RobotAssemblyManager manager = FindObjectOfType<RobotAssemblyManager>();
        // manager.ResetAssembly();
    }
}

/*
 * ============================================================================
 * HƯỚNG DẪN TÍCH HỢP VÀO GAME
 * ============================================================================
 * 
 * 1. GỌITION CUSTOM ASSEMBLY FLOW
 *    - Nếu muốn custom flow, tạo script riêng
 *    - Subscribe vào các public methods của selector/manager
 *    
 * 2. LƯỚI TRẠNG THÁI ROBOT
 *    - Lưu trạng thái robot vào SaveData
 *    - Khi load game, rebuild robot từ trạng thái đã lưu
 *    
 * 3. PROGRESSION SYSTEM
 *    - Unlock module mới khi tiến độ
 *    - Disable button nếu chưa unlock
 *    - Thêm UI hiển thị module requirements
 *    
 * 4. ACHIEVEMENT / QUEST
 *    - "Lắp ráp robot hoàn chỉnh" → Achievement
 *    - "Lắp ráp robot trong thời gian X" → Quest
 *    
 * 5. MULTIPLAYER (Nếu có)
 *    - Sync trạng thái robot qua network
 *    - Send RPC: SelectModule, ConfirmAssembly, etc.
 */
