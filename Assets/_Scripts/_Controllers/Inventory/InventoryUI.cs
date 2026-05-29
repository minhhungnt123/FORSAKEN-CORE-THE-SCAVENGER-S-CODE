using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RoboticsProject.Managers;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.Controllers.Inventory
{
    /// <summary>
    /// Quản lý giao diện chính của túi đồ (Inventory Canvas).
    /// Tự động tìm kiếm và đồng bộ hóa 20 ô vật phẩm dưới slotsParent, hiển thị mô tả chi tiết và trạng thái chuột.
    /// Tuân thủ nguyên lý SOLID và tối ưu hóa hiệu năng.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("Panel cha chứa toàn bộ giao diện túi đồ để đóng/mở")]
        [SerializeField] private GameObject inventoryPanel;

        [Tooltip("GameObject cha chứa danh sách các slot tĩnh (InventorySlot trong Unity Editor)")]
        [SerializeField] private Transform slotsParent;

        [Header("Detail Description Panel")]
        [Tooltip("Ảnh hiển thị của vật phẩm được chọn trong phần mô tả")]
        [SerializeField] private Image detailImage;

        [Tooltip("Text hiển thị Tên của vật phẩm")]
        [SerializeField] private TextMeshProUGUI detailNameText;

        [Tooltip("Text hiển thị Mô tả chi tiết của vật phẩm")]
        [SerializeField] private TextMeshProUGUI detailDescriptionText;

        // Danh sách cache các component slot UI
        private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

        // Lưu trữ index của slot đang được chọn để xem mô tả
        private int currentlySelectedSlotIndex = -1;

        // Cache reference của InventoryManager để hủy đăng ký sự kiện an toàn trong OnDestroy
        private InventoryManager cachedInventoryManager;

        private void Awake()
        {
            InitializeSlots();
        }

        private void Start()
        {
            // Mặc định ẩn túi đồ khi bắt đầu game
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            ClearDescription();

            // Tìm và đăng ký sự kiện từ InventoryManager (hỗ trợ load trễ do thứ tự Awake)
            cachedInventoryManager = InventoryManager.Instance;
            if (cachedInventoryManager == null)
            {
                cachedInventoryManager = FindFirstObjectByType<InventoryManager>();
            }

            if (cachedInventoryManager != null)
            {
                cachedInventoryManager.OnInventoryChanged += RefreshUI;
                cachedInventoryManager.OnInventoryToggle += HandleInventoryToggle;
            }
            else
            {
                Debug.LogWarning("[InventoryUI] Không tìm thấy InventoryManager để đăng ký sự kiện dữ liệu!");
            }
        }

        private void OnDestroy()
        {
            // Hủy đăng ký sự kiện an toàn qua biến đã cache để tránh Memory Leak và lỗi FindFirstObjectByType khi thoát Scene
            if (cachedInventoryManager != null)
            {
                cachedInventoryManager.OnInventoryChanged -= RefreshUI;
                cachedInventoryManager.OnInventoryToggle -= HandleInventoryToggle;
            }
        }

        /// <summary>
        /// Tự động quét và cache các component slot UI dưới slotsParent
        /// </summary>
        private void InitializeSlots()
        {
            if (slotsParent == null)
            {
                Debug.LogError("[InventoryUI] Vui lòng kéo thả GameObject 'InventorySlot' (cha của các slot) vào slotsParent!");
                return;
            }

            slotUIs.Clear();
            int childCount = slotsParent.childCount;
            Debug.Log($"[InventoryUI] Đang khởi tạo {childCount} slot dưới slotsParent: {slotsParent.name}");

            for (int i = 0; i < childCount; i++)
            {
                Transform child = slotsParent.GetChild(i);
                
                InventorySlotUI slotUI = child.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                {
                    slotUI = child.gameObject.AddComponent<InventorySlotUI>();
                }

                slotUI.SlotIndex = i;
                slotUIs.Add(slotUI);
            }
        }

        /// <summary>
        /// Bật/tắt giao diện túi đồ và cập nhật con trỏ chuột
        /// </summary>
        private void HandleInventoryToggle(bool isOpen)
        {
            if (inventoryPanel == null) return;

            inventoryPanel.SetActive(isOpen);

            if (isOpen)
            {
                RefreshUI();
                
                // Hiển thị chuột và chặn input di chuyển khi mở túi đồ
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.SetCursorState(false);
                    InputManager.Instance.IsGameplayInputBlocked = true;
                }
            }
            else
            {
                ClearDescription();
                currentlySelectedSlotIndex = -1;

                // Khóa chuột lại và khôi phục di chuyển khi đóng túi đồ
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.SetCursorState(true);
                    InputManager.Instance.IsGameplayInputBlocked = false;
                }
            }
        }

        /// <summary>
        /// Cập nhật hiển thị toàn bộ ô vật phẩm dựa trên dữ liệu thực tế từ InventoryManager
        /// </summary>
        private void RefreshUI()
        {
            if (inventoryPanel == null || !inventoryPanel.activeSelf) return;
            if (InventoryManager.Instance == null || slotUIs == null) return;

            var slotsData = InventoryManager.Instance.slots;

            for (int i = 0; i < slotUIs.Count; i++)
            {
                if (i < slotsData.Count)
                {
                    slotUIs[i].SetSlotData(slotsData[i]);
                }
                else
                {
                    slotUIs[i].SetSlotData(null);
                }
            }

            // Cập nhật lại Panel mô tả nếu slot đang chọn thay đổi hoặc trống
            if (currentlySelectedSlotIndex >= 0 && currentlySelectedSlotIndex < slotUIs.Count)
            {
                var selectedSlot = slotUIs[currentlySelectedSlotIndex];
                if (selectedSlot.SlotData == null || selectedSlot.SlotData.item == null)
                {
                    ClearDescription();
                    currentlySelectedSlotIndex = -1;
                }
                else
                {
                    UpdateDescription(selectedSlot.SlotData.item);
                }
            }
        }

        /// <summary>
        /// Phương thức công khai để InventorySlotUI gọi khi người dùng click vào slot.
        /// </summary>
        public void HandleSlotClicked(int index)
        {
            if (index < 0 || index >= slotUIs.Count) return;

            var clickedSlot = slotUIs[index];
            if (clickedSlot.SlotData == null || clickedSlot.SlotData.item == null)
            {
                ClearDescription();
                currentlySelectedSlotIndex = -1;
                return;
            }

            currentlySelectedSlotIndex = index;
            UpdateDescription(clickedSlot.SlotData.item);
        }

        /// <summary>
        /// Hiển thị thông tin mô tả chi tiết vật phẩm
        /// </summary>
        private void UpdateDescription(ItemData item)
        {
            if (item == null)
            {
                ClearDescription();
                return;
            }

            if (detailImage != null)
            {
                detailImage.sprite = item.itemIcon;
                detailImage.enabled = true;
            }

            if (detailNameText != null)
            {
                detailNameText.text = $"<color=#FFA500><b>{item.itemName}</b></color>";
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = $"<color=#DDDDDD>{item.description}</color>";
            }
        }

        /// <summary>
        /// Đưa phần mô tả chi tiết về trạng thái mặc định
        /// </summary>
        private void ClearDescription()
        {
            if (detailImage != null)
            {
                detailImage.sprite = null;
                detailImage.enabled = false;
            }

            if (detailNameText != null)
            {
                detailNameText.text = string.Empty;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = "<color=#888888><i>Chọn một vật phẩm để xem chi tiết...</i></color>";
            }
        }
    }
}
