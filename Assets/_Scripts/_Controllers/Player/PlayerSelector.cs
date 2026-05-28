using UnityEngine;
using RoboticsProject.Interfaces;
using RoboticsProject.Managers;
using RoboticsProject.UI;

namespace RoboticsProject.Controllers.Player
{
    /// <summary>
    /// Chịu trách nhiệm quét vật lý phát hiện vật thể tương tác gần nhất và kích hoạt hành động tương tác.
    /// Tuân thủ nguyên lý Single Responsibility Principle (SRP) - chỉ tập trung vào logic tương tác, bàn giao phần hiển thị cho InteractionUI.
    /// </summary>
    public class PlayerSelector : MonoBehaviour
    {
        [Header("Proximity Settings")]
        [Tooltip("Bán kính vùng quét xung quanh người chơi")]
        [SerializeField] private float interactRadius = 2f;
        
        [Tooltip("Lọc layer để chỉ quét những vật thể Selectable")]
        [SerializeField] private LayerMask interactLayerMask;

        [Header("UI Setup")]
        [Tooltip("Giao diện tương tác cao cấp phong cách Genshin")]
        [SerializeField] private InteractionUI interactionUI;

        [Header("References")]
        [SerializeField] private InputManager inputManager;

        private IInteractable currentInteractable;

        // TỐI ƯU 1: Tạo sẵn một mảng cố định để chứa kết quả quét vật lý (tránh tạo mảng mới mỗi frame)
        private Collider[] hitColliders = new Collider[10];

        private void Start()
        {
            // Tự động tìm kiếm InteractionUI trong Scene nếu chưa được gán tay
            if (interactionUI == null)
            {
                interactionUI = FindFirstObjectByType<InteractionUI>();
                if (interactionUI == null)
                {
                    Debug.LogWarning($"[PlayerSelector] Không tìm thấy Component InteractionUI nào trong Scene trên GameObject: {gameObject.name}");
                }
            }

            HideUI();

            // Tự động gán reference cho InputManager
            if (inputManager == null)
            {
                inputManager = InputManager.Instance;
                if (inputManager == null)
                {
                    inputManager = FindFirstObjectByType<InputManager>();
                }
            }
        }

        private void Update()
        {
            // Nếu túi đồ đang mở, ẩn UI tương tác và không thực hiện quét vật lý
            if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen)
            {
                if (currentInteractable != null)
                {
                    currentInteractable = null;
                    HideUI();
                }
                return;
            }

            FindClosestInteractable();
            HandleInteraction();
        }

        private void FindClosestInteractable()
        {
            // TỐI ƯU 1: Dùng NonAlloc để tránh phân bổ bộ nhớ rác (GC.Alloc) trong vòng lặp Update
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

            // TỐI ƯU 2: Chỉ cập nhật trạng thái UI khi mục tiêu thực sự thay đổi giữa các frame
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
            // Kiểm tra nút bấm tương tác từ InputManager
            if (inputManager != null && inputManager.InteractTriggered && currentInteractable != null)
            {
                currentInteractable.OnInteract();

                // Cập nhật UI ngay lập tức sau khi tương tác (ví dụ: đổi text từ "Mở cửa" sang "Đóng cửa")
                ShowUI(currentInteractable.GetInteractPrompt());
            }
        }

        private void ShowUI(string promptMessage)
        {
            if (interactionUI == null) return;

            // Lấy phím bấm tương tác động từ InputManager
            string interactKey = "E";
            if (inputManager != null)
            {
                interactKey = inputManager.GetInteractBindingName();
            }

            interactionUI.Show(promptMessage, interactKey);
        }

        private void HideUI()
        {
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, interactRadius);
        }
    }
}