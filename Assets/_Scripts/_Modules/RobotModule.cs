using UnityEngine;

// Ép buộc Unity tự động thêm Rigidbody khi gắn script này vào GameObject
[RequireComponent(typeof(Rigidbody))]
public abstract class RobotModule : MonoBehaviour, IConnectable
{
    [Header("Module Stats")]
    public float moduleMass = 5f;
    public string moduleName = "Unknown Module";

    [Header("Connection Points")]
    // Danh sách các điểm nối (Socket) trên module này để module khác gắn vào
    public Transform[] availableSockets;

    protected Rigidbody rb;
    protected Joint connectionJoint; // Khớp nối vật lý

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = moduleMass;
    }

    // Các class con có thể ghi đè (override) logic kết nối này
    public virtual void Connect(Transform parentSocket)
    {
        // 1. Vô hiệu hóa vật lý tạm thời để snap vị trí
        rb.isKinematic = true;

        // 2. Dịch chuyển module này khớp với vị trí và góc xoay của parentSocket
        transform.position = parentSocket.position;
        transform.rotation = parentSocket.rotation;

        // 3. Mở lại vật lý và tạo Joint (ví dụ FixedJoint) để dính chặt vào parent
        rb.isKinematic = false;
        connectionJoint = gameObject.AddComponent<FixedJoint>();
        connectionJoint.connectedBody = parentSocket.GetComponentInParent<Rigidbody>();

        Debug.Log($"{moduleName} đã kết nối thành công!");
    }

    // Các class con có thể ghi đè (override) logic tháo rời này
    public virtual void Disconnect()
    {
        // Phá hủy khớp nối vật lý
        if (connectionJoint != null)
        {
            Destroy(connectionJoint); 
        }
        Debug.Log($"{moduleName} đã tháo rời.");
    }
}