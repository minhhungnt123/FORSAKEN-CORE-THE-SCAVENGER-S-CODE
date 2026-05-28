using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.UI
{
    /// <summary>
    /// Quản lý giao diện và hiệu ứng của một dòng thông báo nhặt vật phẩm đơn lẻ.
    /// Tuân thủ nguyên lý Single Responsibility (SOLID) - chỉ tập trung vào hiển thị và hiệu ứng của chính nó.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PickupNotificationItem : MonoBehaviour
    {
        [Header("UI Elements Reference")]
        [Tooltip("Ảnh đại diện cho icon vật phẩm")]
        [SerializeField] private Image itemIconImage;

        [Tooltip("Text hiển thị nội dung thông báo (Tên vật phẩm x Số lượng)")]
        [SerializeField] private TextMeshProUGUI notificationText;

        [Tooltip("Panel con chứa hình ảnh hiển thị để chạy hiệu ứng trượt (tránh xung đột với Vertical Layout Group)")]
        [SerializeField] private RectTransform visualPanel;

        [Tooltip("CanvasGroup của Panel con để thực hiện hiệu ứng mờ dần (Fade)")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("Thời gian hiển thị của thông báo trước khi biến mất (giây)")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("Tốc độ Fade In và Fade Out")]
        [SerializeField] private float fadeSpeed = 5f;

        [Tooltip("Tốc độ trượt (Slide)")]
        [SerializeField] private float slideSpeed = 8f;

        [Tooltip("Khoảng cách lệch vị trí X khi bắt đầu xuất hiện (tạo hiệu ứng trượt từ trái qua)")]
        [SerializeField] private float slideStartXOffset = -150f;

        [Header("Layout & Size Settings")]
        [Tooltip("Chiều rộng thiết kế của khung thông báo")]
        [SerializeField] private float notificationWidth = 250f;

        [Tooltip("Chiều cao thiết kế của khung thông báo (sẽ tự động đồng bộ vào Layout Group)")]
        [SerializeField] private float notificationHeight = 45f;

        // Sự kiện báo hiệu khi thông báo bắt đầu biến mất (Fade/Slide Out)
        public event System.Action<PickupNotificationItem> OnStartDismissing;

        // Các thuộc tính phục vụ cơ chế gộp thông báo trùng loại
        public ItemData DisplayedItem { get; private set; }
        public bool IsDisposing => isDisposing;

        private LayoutElement layoutElement;
        private float initialHeight;
        private float targetAlpha = 0f;
        private Vector2 targetVisualPos;
        private Vector2 startVisualPos;
        private bool isDisposing = false;

        // Lưu giữ dữ liệu để cập nhật số lượng
        private string currentItemName;
        private int currentQuantity;

        private void Awake()
        {
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            // Gán kích thước thiết kế cho LayoutElement để Vertical Layout Group sử dụng làm căn lề
            layoutElement.preferredHeight = notificationHeight;
            layoutElement.preferredWidth = notificationWidth;
            initialHeight = notificationHeight;

            // Đồng bộ trực tiếp kích thước của Root RectTransform
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, notificationHeight);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, notificationWidth);
            }

            // Tự động căn chỉnh Anchor, Pivot và Vị trí của visualPanel con để trùng khít hoàn hảo với Root cha
            if (visualPanel != null && rectTransform != null)
            {
                // Đồng bộ Pivot của con theo cha để tránh lệch tâm khi hoạt ảnh co giãn
                visualPanel.pivot = rectTransform.pivot;

                // Thiết lập anchor stretch-stretch để visualPanel tự động nhận kích thước từ cha
                visualPanel.anchorMin = new Vector2(0f, 0f);
                visualPanel.anchorMax = new Vector2(1f, 1f);

                // sizeDelta bằng Vector2.zero nghĩa là kích thước của visualPanel luôn khít 100% với cha
                visualPanel.sizeDelta = Vector2.zero;

                // Vị trí đích đến khi trượt xong luôn là trùng khít với cha (Vector2.zero)
                targetVisualPos = Vector2.zero;

                // Vị trí bắt đầu chỉ bị lệch trục X (slideStartXOffset), trục Y giữ nguyên bằng 0 so với cha
                startVisualPos = new Vector2(slideStartXOffset, 0f);
                visualPanel.anchoredPosition = startVisualPos;
            }
            else
            {
                Debug.LogWarning($"[PickupNotificationItem] visualPanel chưa được gán trên GameObject: {gameObject.name}. Vui lòng kiểm tra lại Inspector!");
            }

            // Tự động tìm kiếm CanvasGroup nếu chưa được gán trên Inspector để tránh lỗi NullReference
            if (canvasGroup == null)
            {
                if (visualPanel != null)
                {
                    canvasGroup = visualPanel.GetComponent<CanvasGroup>();
                }
                if (canvasGroup == null)
                {
                    canvasGroup = GetComponentInChildren<CanvasGroup>();
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            // Phòng thủ: Nếu đang trong quá trình hủy mà thiếu component quan trọng, hủy ngay lập tức tránh treo UI
            if (isDisposing && (canvasGroup == null || visualPanel == null))
            {
                Destroy(gameObject);
                return;
            }

            if (visualPanel == null || canvasGroup == null) return;

            Vector2 currentTargetPos = targetAlpha > 0f ? targetVisualPos : startVisualPos;

            // Tối ưu CPU: Chỉ thực hiện Lerp/MoveTowards khi các giá trị chưa đạt trạng thái đích
            bool isAlphaChanging = !Mathf.Approximately(canvasGroup.alpha, targetAlpha);
            bool isPositionChanging = Vector2.Distance(visualPanel.anchoredPosition, currentTargetPos) > 0.05f;

            if (isAlphaChanging)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }
            else if (!isDisposing && targetAlpha > 0.9f)
            {
                canvasGroup.alpha = 1f; // Khóa chặt giá trị khi hoàn thành
            }

            if (isPositionChanging)
            {
                visualPanel.anchoredPosition = Vector2.Lerp(visualPanel.anchoredPosition, currentTargetPos, Time.deltaTime * slideSpeed);
            }
            else if (!isDisposing && targetAlpha > 0.9f)
            {
                visualPanel.anchoredPosition = targetVisualPos; // Khóa chặt vị trí khi hoàn thành
            }

            // Co chiều cao của Layout Element về 0 khi bắt đầu hủy để các dòng khác dồn hàng mượt mà
            if (isDisposing && layoutElement != null)
            {
                layoutElement.preferredHeight = Mathf.MoveTowards(layoutElement.preferredHeight, 0f, Time.deltaTime * fadeSpeed * initialHeight);
            }

            // Nếu đang trong quá trình biến mất (Fade Out) và alpha đã về 0, tiến hành hủy đối tượng
            if (isDisposing && Mathf.Approximately(canvasGroup.alpha, 0f))
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Thiết lập dữ liệu hiển thị và bắt đầu chu trình hoạt ảnh.
        /// </summary>
        /// <param name="item">Dữ liệu vật phẩm nhặt được</param>
        /// <param name="quantity">Số lượng vật phẩm nhặt được</param>
        public void Setup(ItemData item, int quantity)
        {
            if (item == null) return;

            DisplayedItem = item;
            currentItemName = item.itemName;
            currentQuantity = quantity;

            if (notificationText != null)
            {
                // Định dạng theo cấu trúc yêu cầu: [Tên_vật_phẩm xSố_lượng]
                notificationText.text = $"{currentItemName} x{currentQuantity}";
            }

            if (itemIconImage != null)
            {
                if (item.itemIcon != null)
                {
                    itemIconImage.sprite = item.itemIcon;
                    itemIconImage.gameObject.SetActive(true);
                }
                else
                {
                    itemIconImage.gameObject.SetActive(false);
                }
            }

            // Bắt đầu chu trình hiển thị
            StartCoroutine(NotificationLifecycleCoroutine());
        }

        /// <summary>
        /// Cộng dồn số lượng và reset lại thời gian tự hủy khi nhặt vật phẩm trùng loại trong thời gian ngắn.
        /// </summary>
        /// <param name="additionalQuantity">Số lượng nhặt thêm</param>
        public void UpdateQuantity(int additionalQuantity)
        {
            if (isDisposing) return;

            currentQuantity += additionalQuantity;
            if (notificationText != null)
            {
                notificationText.text = $"{currentItemName} x{currentQuantity}";
            }

            // Reset đếm ngược thời gian tự hủy
            StopAllCoroutines();
            StartCoroutine(NotificationLifecycleCoroutine());
        }

        /// <summary>
        /// Buộc thông báo phải biến mất sớm hơn (khi bị đẩy ra bởi thông báo mới)
        /// </summary>
        public void DismissEarly()
        {
            if (isDisposing) return;

            StopAllCoroutines();
            isDisposing = true;
            targetAlpha = 0f;
            OnStartDismissing?.Invoke(this);
        }

        /// <summary>
        /// Coroutine quản lý vòng đời hiển thị của thông báo
        /// </summary>
        private IEnumerator NotificationLifecycleCoroutine()
        {
            // 1. Fade In & Slide In
            targetAlpha = 1f;

            // 2. Chờ hết thời gian hiển thị
            yield return new WaitForSeconds(displayDuration);

            // 3. Fade Out & Slide Out
            targetAlpha = 0f;
            isDisposing = true;
            OnStartDismissing?.Invoke(this);
        }
    }
}
