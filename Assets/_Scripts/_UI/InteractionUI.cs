using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

namespace RoboticsProject.UI
{
    /// <summary>
    /// Quản lý giao diện tương tác (Interaction UI) lấy cảm hứng từ Genshin Impact.
    /// Tuân thủ nguyên lý Single Responsibility (SOLID) - chỉ tập trung vào hiển thị và hiệu ứng UI.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionUI : MonoBehaviour
    {
        [Header("UI Elements Reference")]
        [Tooltip("TextMeshPro hiển thị dòng chữ mô tả hành động (ví dụ: 'Mở cửa', 'Ngủ qua ngày')")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Tooltip("TextMeshPro hiển thị phím bấm tương tác (ví dụ: 'E', 'F')")]
        [SerializeField] private TextMeshProUGUI keyText;

        [Tooltip("Khung chứa phím tương tác để có thể ẩn/hiện linh hoạt")]
        [SerializeField] private GameObject keyIndicatorPanel;

        [Tooltip("RectTransform của Panel chính để thực hiện hiệu ứng dịch chuyển")]
        [SerializeField] private RectTransform mainPanelRect;

        [Header("Animation Settings")]
        [Tooltip("Tốc độ Fade In/Fade Out của giao diện")]
        [SerializeField] private float fadeSpeed = 10f;

        [Tooltip("Tốc độ dịch chuyển (Slide) khi xuất hiện")]
        [SerializeField] private float slideSpeed = 12f;

        [Tooltip("Khoảng cách dịch chuyển (Offset) theo trục X khi ẩn UI (tạo hiệu ứng lướt từ phải sang)")]
        [SerializeField] private float slideOffset = 30f;

        private CanvasGroup canvasGroup;
        private float targetAlpha = 0f;
        private Vector2 defaultAnchoredPosition;
        private Vector2 hiddenAnchoredPosition;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            
            // Nếu không kéo thả RectTransform của Panel chính, lấy luôn RectTransform của vật thể này
            if (mainPanelRect == null)
            {
                mainPanelRect = GetComponent<RectTransform>();
            }

            // Lưu lại vị trí ban đầu và tính toán vị trí ẩn (lệch sang phải một khoảng slideOffset)
            if (mainPanelRect != null)
            {
                defaultAnchoredPosition = mainPanelRect.anchoredPosition;
                hiddenAnchoredPosition = defaultAnchoredPosition + new Vector2(slideOffset, 0f);
                mainPanelRect.anchoredPosition = hiddenAnchoredPosition;
            }

            // Đảm bảo ban đầu UI ẩn hoàn toàn
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            targetAlpha = 0f;
        }

        private void Update()
        {
            // Tối ưu hiệu năng: Nếu alpha đã đạt mục tiêu và bằng 0, không cần chạy Lerp vị trí nữa
            if (Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                if (targetAlpha == 0f && canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                return;
            }

            // Cập nhật Alpha (Fade)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            // Cập nhật Vị trí (Slide)
            if (mainPanelRect != null)
            {
                Vector2 targetPos = targetAlpha > 0f ? defaultAnchoredPosition : hiddenAnchoredPosition;
                mainPanelRect.anchoredPosition = Vector2.Lerp(mainPanelRect.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
            }
        }

        /// <summary>
        /// Hiển thị UI tương tác với mô tả và phím tắt tương ứng.
        /// </summary>
        /// <param name="rawPrompt">Dòng chữ mô tả hành động</param>
        /// <param name="keyName">Tên phím bấm (ví dụ: 'E', 'F')</param>
        public void Show(string rawPrompt, string keyName = "E")
        {
            if (promptText != null)
            {
                // Làm sạch chuỗi prompt (loại bỏ phím bấm cứng dạng [E] nếu có)
                promptText.text = CleanPromptMessage(rawPrompt);
            }

            if (keyText != null)
            {
                keyText.text = keyName;
            }

            if (keyIndicatorPanel != null)
            {
                keyIndicatorPanel.SetActive(!string.IsNullOrEmpty(keyName));
            }

            targetAlpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        /// <summary>
        /// Ẩn UI tương tác đi.
        /// </summary>
        public void Hide()
        {
            targetAlpha = 0f;
        }

        /// <summary>
        /// Loại bỏ các ký tự phím bấm cứng nằm trong ngoặc vuông (ví dụ: 'Nhặt đồ [E]' -> 'Nhặt đồ')
        /// để tránh bị lặp phím khi hiển thị trên giao diện động kiểu Genshin.
        /// </summary>
        private string CleanPromptMessage(string rawMessage)
        {
            if (string.IsNullOrEmpty(rawMessage)) return string.Empty;

            // Regex loại bỏ khoảng trắng cùng các ký tự dạng [E], [e], [Space], [F]...
            string cleaned = Regex.Replace(rawMessage, @"\s*\[\w+\]\s*", "");
            return cleaned.Trim();
        }
    }
}
