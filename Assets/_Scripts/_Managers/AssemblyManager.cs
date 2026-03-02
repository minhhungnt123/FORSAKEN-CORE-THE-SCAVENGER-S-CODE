using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    [Header("Cài đặt Lắp ráp")]
    public float snapDistance = 1.5f; // Khoảng cách tối đa để hít vào socket
    public float dragSpeed = 15f;     // Tốc độ lướt đi của module khi kéo

    private Camera mainCamera;
    private RobotModule selectedModule;
    private float zDistanceToCamera;

    void Start()
    {
        // Lấy camera chính để phục vụ việc bắn Raycast
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleSelection();
        HandleDragging();
        HandleRelease();
    }

    // 1. Logic chọn vật thể
    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0)) // Bấm chuột trái
        {
            // Bắn một tia từ Camera xuyên qua vị trí trỏ chuột trên màn hình
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Kiểm tra xem vật thể bị bắn trúng có script RobotModule không
                RobotModule module = hit.collider.GetComponent<RobotModule>();

                if (module != null)
                {
                    selectedModule = module;

                    // Tháo rời module này ra nếu nó đang gắn vào đâu đó
                    selectedModule.Disconnect();

                    // Tính toán khoảng cách chiều sâu (trục Z) từ Camera đến vật thể để giữ nguyên độ sâu khi kéo
                    zDistanceToCamera = mainCamera.WorldToScreenPoint(selectedModule.transform.position).z;

                    // Tắt vật lý tạm thời để kéo thả không bị rung lắc
                    Rigidbody rb = selectedModule.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }
    }

    // 2. Logic kéo vật thể đi theo chuột
    private void HandleDragging()
    {
        if (Input.GetMouseButton(0) && selectedModule != null) // Đang giữ chuột trái
        {
            // Chuyển tọa độ chuột 2D thành tọa độ 3D
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = zDistanceToCamera;
            Vector3 targetPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

            // Di chuyển mượt mà (Lerp) vật thể tới vị trí chuột
            selectedModule.transform.position = Vector3.Lerp(selectedModule.transform.position, targetPos, Time.deltaTime * dragSpeed);
        }
    }

    // 3. Logic nhả chuột và tìm Socket
    private void HandleRelease()
    {
        if (Input.GetMouseButtonUp(0) && selectedModule != null) // Nhả chuột trái
        {
            Transform closestSocket = FindClosestSocket(selectedModule.transform.position);

            if (closestSocket != null)
            {
                // Nếu tìm thấy socket ở gần, ra lệnh hít vào!
                selectedModule.Connect(closestSocket);
            }
            else
            {
                // Nếu không có socket nào, bật lại vật lý để khối linh kiện rơi xuống đất
                Rigidbody rb = selectedModule.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }

            // Xóa bộ nhớ đệm, chờ lần gắp tiếp theo
            selectedModule = null;
        }
    }

    // Hàm phụ trợ: Quét tìm Socket gần nhất
    private Transform FindClosestSocket(Vector3 currentPosition)
    {
        Transform bestSocket = null;
        float minDistance = snapDistance;

        // Lấy tất cả các module đang có trong Scene (Lưu ý: Cách này tiện cho Prototype, sau này tối ưu có thể dùng OverlapSphere)
        RobotModule[] allModules = FindObjectsByType<RobotModule>(FindObjectsSortMode.None);

        foreach (var module in allModules)
        {
            if (module == selectedModule) continue; // Không tự hít vào chính mình

            // Quét qua danh sách các điểm Socket của module đó
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