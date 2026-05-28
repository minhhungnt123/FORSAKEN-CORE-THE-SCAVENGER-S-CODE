using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RoboticsProject.Modules.Inventory;
using RoboticsProject.Managers;

namespace RoboticsProject.Controllers.Inventory
{
    /// <summary>
    /// Quản lý tương tác và giao diện của một ô vật phẩm đơn lẻ.
    /// Triển khai các Interface của EventSystem để xử lý Drag & Drop.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;

        public int SlotIndex { get; set; } = -1;
        public ItemSlot SlotData { get; private set; }

        // Biến static lưu slot đang được kéo trên toàn bộ game
        public static InventorySlotUI DraggedSlot { get; private set; }

        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        
        // Thực thể visual tạm thời bay theo chuột khi kéo thả (gồm cả icon và text số lượng)
        private GameObject dragVisualInstance;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            // Tìm Canvas cha để quản lý layer hiển thị khi drag
            parentCanvas = GetComponentInParent<Canvas>();

            // Tự động tìm kiếm các component UI nếu chưa được gán
            if (iconImage == null)
            {
                Image[] childImages = GetComponentsInChildren<Image>(true);
                foreach (var img in childImages)
                {
                    if (img.gameObject != this.gameObject)
                    {
                        iconImage = img;
                        break;
                    }
                }
            }

            if (quantityText == null)
            {
                quantityText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        /// <summary>
        /// Đồng bộ dữ liệu của slot này với dữ liệu thực tế
        /// </summary>
        public void SetSlotData(ItemSlot data)
        {
            SlotData = data;

            if (data != null && data.item != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = data.item.itemIcon;
                    iconImage.gameObject.SetActive(true);
                    iconImage.enabled = true;
                }

                if (quantityText != null)
                {
                    if (data.item.isStackable && data.quantity > 1)
                    {
                        quantityText.text = data.quantity.ToString();
                        quantityText.gameObject.SetActive(true);
                    }
                    else
                    {
                        quantityText.text = string.Empty;
                        quantityText.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Ô trống
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.gameObject.SetActive(false);
                    iconImage.enabled = false;
                }

                if (quantityText != null)
                {
                    quantityText.text = string.Empty;
                    quantityText.gameObject.SetActive(false);
                }
            }
        }

        #region EventSystem Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Chỉ cho phép drag khi ô này có chứa vật phẩm
            if (SlotData == null || SlotData.item == null || iconImage == null || !iconImage.gameObject.activeSelf)
            {
                eventData.pointerDrag = null; // Hủy drag
                return;
            }

            DraggedSlot = this;

            // Tạo Drag Visual Instance tạm thời
            dragVisualInstance = new GameObject("DraggingItemVisual", typeof(RectTransform), typeof(CanvasGroup));
            dragVisualInstance.transform.SetParent(parentCanvas.transform, false);
            dragVisualInstance.transform.SetAsLastSibling();

            CanvasGroup dragGroup = dragVisualInstance.GetComponent<CanvasGroup>();
            if (dragGroup != null)
            {
                dragGroup.blocksRaycasts = false;
            }

            // Đồng bộ kích thước của Drag Visual giống với Slot gốc
            RectTransform rect = dragVisualInstance.transform as RectTransform;
            RectTransform slotRect = transform as RectTransform;
            if (rect != null && slotRect != null)
            {
                rect.sizeDelta = slotRect.sizeDelta;
            }

            // Tạo Image con cho Icon bay theo chuột
            GameObject dragIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            dragIconObj.transform.SetParent(dragVisualInstance.transform, false);
            
            RectTransform iconRect = dragIconObj.transform as RectTransform;
            if (iconRect != null)
            {
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = Vector2.zero; // Stretch full
            }

            Image dragIconImage = dragIconObj.GetComponent<Image>();
            if (dragIconImage != null)
            {
                dragIconImage.sprite = iconImage.sprite;
                dragIconImage.raycastTarget = false;
            }

            // Tạo Text con cho số lượng bay theo chuột (nếu có)
            if (quantityText != null && quantityText.gameObject.activeSelf && !string.IsNullOrEmpty(quantityText.text))
            {
                GameObject dragTextObj = new GameObject("QuantityText", typeof(RectTransform), typeof(TextMeshProUGUI));
                dragTextObj.transform.SetParent(dragVisualInstance.transform, false);

                RectTransform textRect = dragTextObj.transform as RectTransform;
                RectTransform originalTextRect = quantityText.transform as RectTransform;
                if (textRect != null && originalTextRect != null)
                {
                    textRect.anchorMin = originalTextRect.anchorMin;
                    textRect.anchorMax = originalTextRect.anchorMax;
                    textRect.pivot = originalTextRect.pivot;
                    textRect.anchoredPosition = originalTextRect.anchoredPosition;
                    textRect.sizeDelta = originalTextRect.sizeDelta;
                }

                TextMeshProUGUI dragTextMesh = dragTextObj.GetComponent<TextMeshProUGUI>();
                if (dragTextMesh != null)
                {
                    dragTextMesh.text = quantityText.text;
                    dragTextMesh.font = quantityText.font;
                    dragTextMesh.fontSize = quantityText.fontSize;
                    dragTextMesh.alignment = quantityText.alignment;
                    dragTextMesh.color = quantityText.color;
                    dragTextMesh.raycastTarget = false;
                }
            }

            // Làm mờ slot gốc và tắt raycast để có thể thả vào slot đích bên dưới
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
                canvasGroup.blocksRaycasts = false;
            }

            // Đồng bộ vị trí chuột tức thời ở frame đầu tiên
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragVisualInstance != null && parentCanvas != null)
            {
                RectTransform canvasRect = parentCanvas.transform as RectTransform;
                // Sử dụng RectTransformUtility để chuyển đổi tọa độ màn hình sang tọa độ local trên Canvas (hỗ trợ Camera Space/World Space)
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, parentCanvas.worldCamera, out Vector2 localPoint))
                {
                    (dragVisualInstance.transform as RectTransform).anchoredPosition = localPoint;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Hủy thực thể kéo thả tạm thời
            if (dragVisualInstance != null)
            {
                Destroy(dragVisualInstance);
                dragVisualInstance = null;
            }

            // Khôi phục hiển thị và raycast cho slot gốc
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            DraggedSlot = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Sự kiện này kích hoạt trên slot ĐÍCH khi thả chuột
            if (DraggedSlot != null && DraggedSlot != this)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.SwapSlots(DraggedSlot.SlotIndex, this.SlotIndex);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Gửi sự kiện click lên InventoryUI để xem mô tả vật phẩm
            InventoryUI parentUI = GetComponentInParent<InventoryUI>();
            if (parentUI != null)
            {
                parentUI.HandleSlotClicked(SlotIndex);
            }
        }

        #endregion
    }
}
