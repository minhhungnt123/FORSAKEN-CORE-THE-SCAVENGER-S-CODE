using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class RobotModule : MonoBehaviour, IConnectable, IAssemblable
{
    [Header("Module Stats")]
    public float moduleMass = 5f;
    public string moduleName = "Unknown Module";
    public string moduleDescription = "";

    [Tooltip("Offset dịch chuyển vị trí cục bộ khi lắp ráp (dùng để sửa lỗi lệch khớp/lệch tâm của một số prefab đặc biệt)")]
    public Vector3 localPositionOffset = Vector3.zero;

    [Header("Module UI")]
    public Sprite moduleIcon;

    [Header("Connection Points")]
    public Transform[] availableSockets;

    protected Rigidbody rb;
    protected Joint connectionJoint;
    protected Transform currentSocket;

    // Moi subclass bat buoc khai bao socket tag cua minh
    public abstract string RequiredSocketTag { get; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = moduleMass;
        rb.isKinematic = true; // Mac dinh khoa vat ly, chi mo khi dang keo
    }

    public virtual void Connect(Transform parentSocket)
    {
        currentSocket = parentSocket;
        
        // GIỮ KINEMATIC TRUE: Tắt hoàn toàn trọng lực và vật lý rớt rơi
        rb.isKinematic = true; 

        transform.SetParent(parentSocket);
        
        // Đưa tâm của linh kiện về trùng với tâm của thân robot (Chassis)
        Transform chassisTransform = parentSocket.GetComponentInParent<ChassisModule>().transform;
        transform.position = chassisTransform.position;

        // Áp dụng offset sửa lệch tâm/khớp vai thủ công (nếu có)
        transform.localPosition += localPositionOffset;
        
        // LƯU Ý: KHÔNG ghi đè góc xoay (Rotation) ở đây. 

        // Đã xóa FixedJoint vì nó gây lỗi rơi rớt khi di chuyển Chassis bằng Transform

        // BỎ QUA VA CHẠM VẬT LÝ GIỮA CÁC MẢNH ĐỂ TRÁNH LỖI (Mặc dù kinematic không cần lắm nhưng cứ giữ cho chắc)
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        chassisTransform = parentSocket.GetComponentInParent<ChassisModule>().transform;
        Collider[] chassisColliders = chassisTransform.GetComponentsInChildren<Collider>();
        foreach (var myCol in myColliders)
        {
            foreach (var chassisCol in chassisColliders)
            {
                Physics.IgnoreCollision(myCol, chassisCol, true);
            }
        }

        OnAssembled(parentSocket);
        Debug.Log($"{moduleName} da ket noi vao {parentSocket.name}");
    }

    public virtual void Disconnect()
    {
        transform.SetParent(null);
        rb.isKinematic = true; // Luôn giữ kinematic để nó lơ lửng, không rớt
        currentSocket = null;

        OnDetached();
        Debug.Log($"{moduleName} da thao roi.");
    }

    // Virtual: subclass override neu can them logic rieng
    public virtual void OnAssembled(Transform socket) { }
    public virtual void OnDetached() { }

    public bool IsEquipped => currentSocket != null;

    public float GetTotalMassWithChildren()
    {
        float total = moduleMass;
        foreach (var socket in availableSockets)
        {
            if (socket == null) continue;
            var child = socket.GetComponentInChildren<RobotModule>();
            if (child != null && child != this)
                total += child.moduleMass;
        }
        return total;
    }
}