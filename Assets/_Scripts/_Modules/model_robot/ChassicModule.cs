using UnityEngine;
using System.Collections.Generic;

// ChassisModule: Root của toàn bộ robot
// Quan ly 4 slot (Dau, Tay Trai, Tay Phai, Chan)
// Nhan lenh lap rap / thao do tu AssemblyManager
public class ChassisModule : RobotModule
{
    // Chassis la root, khong can gan vao socket nao ca
    public override string RequiredSocketTag => "";

    [Header("--- KE O 4 SOCKET TU HIERARCHY VAO DAY ---")]
    [Tooltip("Empty GameObject dat tai khop co (dau Armature)")]
    public Transform headSocket;

    [Tooltip("Empty GameObject dat tai khop vai trai")]
    public Transform armLeftSocket;

    [Tooltip("Empty GameObject dat tai khop vai phai")]
    public Transform armRightSocket;

    [Tooltip("Empty GameObject dat tai khop hang")]
    public Transform legSocket;

    // Bo nho trang thai: socketTag -> module dang duoc lap
    // Vi du: "HEAD_SOCKET" -> HeadModule instance
    private Dictionary<string, RobotModule> _equippedModules = new();



    protected override void Awake()
    {
        base.Awake();
        moduleName = "Khung gam trung tam";

        // Tự động tìm socket nếu chưa được gán trong Inspector
        AutoDiscoverSockets();

        // Khóa di chuyển cho đến khi lắp đủ bộ phận
        SetMovementEnabled(false);
    }

    /// <summary>
    /// Tự động tìm các socket Transform theo tên nếu chưa được gán trong Inspector.
    /// Tên quy ước: Head_Socket, Arm_L_Socket, Arm_R_Socket, Leg_Socket
    /// </summary>
    private void AutoDiscoverSockets()
    {
        if (headSocket == null)
        {
            headSocket = FindSocketInChildren("Head_Socket");
            if (headSocket != null) Debug.Log("[Chassis] Auto-discovered: Head_Socket");
        }

        if (armLeftSocket == null)
        {
            armLeftSocket = FindSocketInChildren("Arm_L_Socket");
            if (armLeftSocket != null) Debug.Log("[Chassis] Auto-discovered: Arm_L_Socket");
        }

        if (armRightSocket == null)
        {
            armRightSocket = FindSocketInChildren("Arm_R_Socket");
            if (armRightSocket != null) Debug.Log("[Chassis] Auto-discovered: Arm_R_Socket");
        }

        if (legSocket == null)
        {
            legSocket = FindSocketInChildren("Leg_Socket");
            if (legSocket != null) Debug.Log("[Chassis] Auto-discovered: Leg_Socket");
        }
    }

    /// <summary>
    /// Tìm child transform theo tên (đệ quy toàn bộ hierarchy)
    /// </summary>
    private Transform FindSocketInChildren(string socketName)
    {
        // Tìm chính xác theo tên
        Transform found = transform.Find(socketName);
        if (found != null) return found;

        // Tìm đệ quy nếu socket nằm sâu hơn (vd: bên trong Armature)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == socketName) return child;
        }

        Debug.LogWarning($"[Chassis] Không tìm thấy socket '{socketName}' trong hierarchy của {gameObject.name}!");
        return null;
    }

    private void SetMovementEnabled(bool enabled)
    {
        // Tắt/Bật PlayerMovement nếu có (gắn trên root)
        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) pm.enabled = enabled;

        // Tắt/Bật RobotController nếu có
        var rc = GetComponent<RobotController>();
        if (rc != null) rc.enabled = enabled;
    }

    // --- API CHINH: AssemblyManager goi ham nay khi thu tha module ---
    // Tra ve true neu lap thanh cong, false neu khong tim duoc socket hop le
    public bool TryEquip(RobotModule module, string forcedSocketTag = null)
    {
        string targetTag = !string.IsNullOrEmpty(forcedSocketTag) ? forcedSocketTag : module.RequiredSocketTag;
        Transform socket = GetSocketByTag(targetTag);

        if (socket == null)
        {
            Debug.LogWarning($"[Chassis] Khong tim thay socket cho tag: {targetTag}");
            return false;
        }

        // Nếu slot đã có bộ phận → từ chối, không cho ghi đè
        if (_equippedModules.ContainsKey(targetTag))
        {
            RobotModule existing = _equippedModules[targetTag];
            Debug.LogWarning($"[Chassis] Slot '{targetTag}' đã có '{existing.moduleName}'. Gỡ bộ phận cũ trước khi lắp mới!");
            return false;
        }

        // Lắp module mới vào socket
        module.Connect(socket);
        _equippedModules[targetTag] = module;

        // Nếu đã lắp đủ 4 bộ phận → kích hoạt animation ăn mừng và mở khóa di chuyển
        if (IsFullyAssembled())
        {
            SetMovementEnabled(true);

            var animCtrl = GetComponent<RobotAnimatorController>();
            animCtrl?.TriggerFullAssembledCelebration();

            var proceduralAnim = GetComponent<RobotProceduralAnimator>();
            proceduralAnim?.AutoDiscoverPartsFromChassis();

            var robotCtrl = GetComponent<RobotController>();
            robotCtrl?.NotifyFullyAssembled();

            Debug.Log("[ChassisModule] Robot đã lắp ráp đầy đủ! Di chuyển được mở khóa.");
        }

        return true;
    }

    // Thao module tai mot slot cu the
    public void Detach(string socketTag)
    {
        if (_equippedModules.TryGetValue(socketTag, out var module))
        {
            module.Disconnect();
            _equippedModules.Remove(socketTag);
        }
    }

    // Tra ve module dang duoc lap tai slot, null neu trong
    public RobotModule GetEquipped(string socketTag) =>
        _equippedModules.TryGetValue(socketTag, out var m) ? m : null;

    // Kiem tra robot da lap day du 4 phan chua (dung cho UI nut "Xac nhan")
    public bool IsFullyAssembled() =>
        _equippedModules.ContainsKey("HEAD_SOCKET") &&
        _equippedModules.ContainsKey("ARM_L_SOCKET") &&
        _equippedModules.ContainsKey("ARM_R_SOCKET") &&
        _equippedModules.ContainsKey("LEG_SOCKET");

    // Tinh tong mass cua toan bo robot (than + tat ca module)
    public float GetTotalMass()
    {
        float total = moduleMass;
        foreach (var mod in _equippedModules.Values)
            if (mod != null) total += mod.moduleMass;
        return total;
    }

    // --- Private: map tag -> Transform socket tuong ung ---
    private Transform GetSocketByTag(string tag) => tag switch
    {
        "HEAD_SOCKET" => headSocket,
        "ARM_L_SOCKET" => armLeftSocket,
        "ARM_R_SOCKET" => armRightSocket,
        "LEG_SOCKET" => legSocket,
        _ => null
    };

    // Gizmos: ve cau voi cac socket de de nhin trong Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        DrawSocketGizmo(headSocket, "HEAD");
        DrawSocketGizmo(armLeftSocket, "ARM_L");
        DrawSocketGizmo(armRightSocket, "ARM_R");
        DrawSocketGizmo(legSocket, "LEG");
    }

    private void DrawSocketGizmo(Transform socket, string label)
    {
        if (socket == null) return;
        Gizmos.DrawWireSphere(socket.position, 0.1f);
        Gizmos.DrawLine(transform.position, socket.position);
    }

    // Di chuyển đã được tách sang RobotMovementController.
    // ChassisModule chỉ quản lý slot lắp ráp.
}