using UnityEngine;
using TMPro;

public class PlayerSelector : MonoBehaviour
{
    [Header("Proximity Settings")]
    [Tooltip("Bán kính vùng quét xung quanh người chơi")]
    [SerializeField] private float interactRadius = 2f;
    [Tooltip("Lọc layer để chỉ quét những vật thể Selectable")]
    [SerializeField] private LayerMask interactLayerMask;

    [Header("UI Setup")]
    [SerializeField] private TextMeshProUGUI promptTextUI;
    [SerializeField] private GameObject promptPanel;

    [Header("References")]
    [SerializeField] private InputManager inputManager;

    private IInteractable currentInteractable;

    // TỐI ƯU 1: Tạo sẵn một mảng cố định để chứa kết quả quét vật lý (tránh tạo mảng mới mỗi frame)
    // Con số 10 là số lượng vật thể tối đa có thể nằm trong bán kính cùng lúc. 
    // Bạn có thể tăng lên nếu game có mật độ vật thể dày đặc hơn.
    private Collider[] hitColliders = new Collider[10];

    private void Start()
    {
        HideUI();
    }

    private void Update()
    {
        FindClosestInteractable();
        HandleInteraction();
    }

    private void FindClosestInteractable()
    {
        // TỐI ƯU 1: Dùng NonAlloc. Hàm này trả về số lượng vật thể chạm phải và nhét kết quả vào mảng hitColliders có sẵn.
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, hitColliders, interactLayerMask);

        IInteractable closestInteractable = null;
        float minDistance = float.MaxValue;

        // Chỉ lặp qua đúng số lượng vật thể tìm thấy (hitCount)
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitColliders[i];
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        // TỐI ƯU 2: Chỉ cập nhật UI nếu mục tiêu GẦN NHẤT thực sự THAY ĐỔI
        if (closestInteractable != currentInteractable)
        {
            currentInteractable = closestInteractable;

            if (currentInteractable != null)
            {
                ShowUI(currentInteractable.GetInteractPrompt());
            }
            else
            {
                HideUI();
            }
        }
    }

    private void HandleInteraction()
    {
        // Kiểm tra nút bấm từ InputManager
        if (inputManager.InteractTriggered && currentInteractable != null)
        {
            currentInteractable.Interact();

            // Ép UI cập nhật ngay lập tức sau khi tương tác (VD: Đổi ngay chữ "Gieo hạt" thành "Tưới nước")
            ShowUI(currentInteractable.GetInteractPrompt());
        }
    }

    private void ShowUI(string promptMessage)
    {
        if (promptPanel != null && !promptPanel.activeSelf)
        {
            promptPanel.SetActive(true);
        }
        if (promptTextUI != null)
        {
            promptTextUI.text = promptMessage;
        }
    }

    private void HideUI()
    {
        if (promptPanel != null && promptPanel.activeSelf)
        {
            promptPanel.SetActive(false);
        }
        if (promptTextUI != null)
        {
            promptTextUI.text = "";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, interactRadius);
    }
}