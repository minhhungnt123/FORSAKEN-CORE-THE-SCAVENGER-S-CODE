using UnityEngine;
using RoboticsProject.Managers;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.UI
{
    /// <summary>
    /// Điều phối và quản lý toàn bộ hệ thống thông báo nhặt vật phẩm trên màn hình (Notification Manager).
    /// Lắng nghe sự kiện từ InventoryManager và sinh ra các dòng thông báo tương ứng.
    /// </summary>
    public class PickupNotificationUI : MonoBehaviour
    {
        [Header("Prefab Configuration")]
        [Tooltip("Prefab của dòng thông báo nhặt vật phẩm (chứa component PickupNotificationItem)")]
        [SerializeField] private GameObject notificationPrefab;

        [Tooltip("Container chứa danh sách các thông báo (thường có Vertical Layout Group, neo ở góc trái màn hình)")]
        [SerializeField] private Transform notificationContainer;

        [Header("Limit Settings")]
        [Tooltip("Số lượng thông báo tối đa hiển thị cùng lúc trên màn hình")]
        [SerializeField] private int maxNotifications = 5;

        // Danh sách lưu trữ các thông báo đang hoạt động
        private System.Collections.Generic.List<PickupNotificationItem> activeNotifications = new System.Collections.Generic.List<PickupNotificationItem>();
        
        // Lưu trữ tham chiếu Coroutine để hủy khi Disable tránh Memory Leak
        private Coroutine registerEventsCoroutine;

        private void Start()
        {
            if (notificationPrefab == null)
            {
                Debug.LogError("[PickupNotificationUI] Chưa kéo thả notificationPrefab vào Script!");
                return;
            }

            if (notificationContainer == null)
            {
                notificationContainer = transform;
            }
        }

        private void OnEnable()
        {
            // Đăng ký sự kiện từ InventoryManager khi UI được kích hoạt để tránh rò rỉ bộ nhớ
            RegisterEvents();
        }

        private void OnDisable()
        {
            // Hủy đăng ký sự kiện từ InventoryManager
            UnregisterEvents();

            // Hủy đăng ký sự kiện của các item đang hoạt động
            foreach (var item in activeNotifications)
            {
                if (item != null)
                {
                    item.OnStartDismissing -= HandleItemDismissed;
                }
            }
            activeNotifications.Clear();
        }

        private void RegisterEvents()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAdded += HandleItemAdded;
            }
            else
            {
                // Dừng Coroutine cũ nếu đang chạy trước khi bắt đầu cái mới
                if (registerEventsCoroutine != null)
                {
                    StopCoroutine(registerEventsCoroutine);
                }
                // Phòng trường hợp InventoryManager khởi tạo trễ so với UI
                registerEventsCoroutine = StartCoroutine(RegisterEventsDeferredCoroutine());
            }
        }

        private System.Collections.IEnumerator RegisterEventsDeferredCoroutine()
        {
            while (InventoryManager.Instance == null)
            {
                yield return null;
            }
            InventoryManager.Instance.OnItemAdded += HandleItemAdded;
            registerEventsCoroutine = null; // Reset sau khi đã đăng ký thành công
        }

        private void UnregisterEvents()
        {
            // Dừng Coroutine đăng ký trễ nếu nó vẫn đang chờ
            if (registerEventsCoroutine != null)
            {
                StopCoroutine(registerEventsCoroutine);
                registerEventsCoroutine = null;
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
            }
        }

        /// <summary>
        /// Xử lý khi nhận được sự kiện thêm vật phẩm mới vào kho đồ
        /// </summary>
        /// <param name="item">Dữ liệu vật phẩm nhặt được</param>
        /// <param name="quantity">Số lượng</param>
        private void HandleItemAdded(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            if (notificationPrefab == null || notificationContainer == null) return;

            // 1. Dọn dẹp các phần tử null trong danh sách để tránh rò rỉ bộ nhớ hoặc tắc nghẽn hàng đợi
            activeNotifications.RemoveAll(x => x == null);

            // 2. Tìm kiếm xem có dòng thông báo trùng loại đang hiển thị hay không (Cơ chế gộp - Stacking)
            PickupNotificationItem existingNotification = activeNotifications.Find(x => x != null && x.DisplayedItem == item && !x.IsDisposing);

            if (existingNotification != null)
            {
                // Nếu tìm thấy dòng trùng loại, cộng dồn số lượng hiển thị và reset đếm ngược
                existingNotification.UpdateQuantity(quantity);
                return;
            }

            // 3. Nếu không trùng loại, tạo mới một thông báo từ Prefab
            GameObject newNotificationObj = Instantiate(notificationPrefab, notificationContainer);
            
            // Thiết lập dữ liệu và kích hoạt hiệu ứng hiển thị
            PickupNotificationItem notificationItem = newNotificationObj.GetComponent<PickupNotificationItem>();
            if (notificationItem != null)
            {
                notificationItem.Setup(item, quantity);
                
                // Đăng ký sự kiện để biết khi nào item bắt đầu biến mất
                notificationItem.OnStartDismissing += HandleItemDismissed;
                
                // Thêm vào danh sách quản lý
                activeNotifications.Add(notificationItem);
                
                // Kiểm tra giới hạn số lượng hiển thị
                CheckNotificationLimit();
            }
            else
            {
                Debug.LogWarning("[PickupNotificationUI] Prefab được gán không có component PickupNotificationItem!");
            }
        }

        /// <summary>
        /// Kiểm tra và đẩy bớt thông báo cũ nhất đi nếu vượt quá giới hạn hiển thị tối đa
        /// </summary>
        private void CheckNotificationLimit()
        {
            // Dọn dẹp phần tử null
            activeNotifications.RemoveAll(x => x == null);

            if (activeNotifications.Count > maxNotifications)
            {
                // Thông báo cũ nhất nằm ở vị trí đầu tiên
                PickupNotificationItem oldestNotification = activeNotifications[0];
                if (oldestNotification != null)
                {
                    // Buộc thông báo cũ nhất biến mất sớm hơn
                    oldestNotification.DismissEarly();
                }
            }
        }

        /// <summary>
        /// Callback được gọi khi một dòng thông báo bắt đầu quá trình biến mất (Fade/Slide Out)
        /// </summary>
        private void HandleItemDismissed(PickupNotificationItem item)
        {
            if (item != null)
            {
                item.OnStartDismissing -= HandleItemDismissed;
                activeNotifications.Remove(item);
            }
        }
    }
}
