using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoboticsProject.Managers;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.UI
{
    /// <summary>
    /// Điều khiển giao diện người dùng cho Bàn Thiết Kế (Drafting Table UI).
    /// Tuân thủ nguyên lý Single Responsibility (SRP) - Chỉ chịu trách nhiệm quản lý hiển thị và tương tác của giao diện vẽ bản thiết kế.
    /// </summary>
    public class DraftingTableUI : MonoBehaviour
    {
        public static DraftingTableUI Instance { get; private set; }

        [Header("UI Panels")]
        [Tooltip("Khung panel chính chứa toàn bộ giao diện vẽ")]
        [SerializeField] private GameObject mainPanel;

        [Header("Left Panel (1/2 Menu) - Blueprint List")]
        [Tooltip("Container chứa danh sách các nút chọn công thức vẽ")]
        [SerializeField] private Transform recipeListContainer;

        [Tooltip("Prefab cho nút chọn công thức vẽ")]
        [SerializeField] private GameObject recipeButtonPrefab;

        [Header("Top-Right Panel (1/4 Menu) - Requirements")]
        [Tooltip("Container chứa danh sách các nguyên liệu yêu cầu để vẽ")]
        [SerializeField] private Transform requirementsContainer;

        [Tooltip("Prefab hiển thị thông tin từng nguyên liệu yêu cầu (gồm Icon, tên, số lượng)")]
        [SerializeField] private GameObject requirementItemPrefab;

        [Header("Bottom-Right Panel (1/4 Menu) - Description & Actions")]
        [Tooltip("Văn bản hiển thị mô tả bản vẽ")]
        [SerializeField] private TextMeshProUGUI recipeDescriptionText;

        [Tooltip("Nút bấm để thực hiện hành động vẽ bản thiết kế")]
        [SerializeField] private Button draftButton;

        [Tooltip("Nút đóng giao diện")]
        [SerializeField] private Button closeButton;

        // Lưu trữ công thức đang được chọn
        private DraftingRecipeSO selectedRecipe;

        // Lưu trữ cache các đối tượng nút công thức đã sinh ra
        private List<GameObject> activeRecipeButtons = new List<GameObject>();
        // Lưu trữ cache các phần tử hiển thị nguyên liệu đã sinh ra
        private List<GameObject> activeRequirementItems = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Ẩn panel UI khi bắt đầu game
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            // Gắn các lắng nghe sự kiện
            if (draftButton != null)
            {
                draftButton.onClick.AddListener(OnDraftButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseUI);
            }

            // Lắng nghe sự kiện vẽ thành công để làm mới giao diện
            if (DraftingManager.Instance != null)
            {
                DraftingManager.Instance.OnDraftingSuccess += RefreshDetailsPanel;
            }
        }

        private void OnDestroy()
        {
            if (DraftingManager.Instance != null)
            {
                DraftingManager.Instance.OnDraftingSuccess -= RefreshDetailsPanel;
            }
        }

        private void Update()
        {
            // Nếu bảng vẽ đang mở, cho phép nhấn phím ESC để đóng nhanh
            if (mainPanel != null && mainPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseUI();
                }
            }
        }

        /// <summary>
        /// Mở giao diện Bàn Thiết Kế.
        /// </summary>
        public void OpenUI()
        {
            if (mainPanel == null) return;

            mainPanel.SetActive(true);

            // Khóa di chuyển của nhân vật và giải phóng con trỏ chuột
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetCursorState(false);
                InputManager.Instance.IsGameplayInputBlocked = true;
            }

            // Khởi tạo danh sách bản vẽ ở panel bên trái
            PopulateRecipeList();

            // Mặc định chọn công thức đầu tiên nếu có
            var recipes = DraftingManager.Instance != null ? DraftingManager.Instance.GetRecipes() : null;
            if (recipes != null && recipes.Count > 0)
            {
                SelectRecipe(recipes[0]);
            }
            else
            {
                ClearDetailsPanel();
            }
        }

        /// <summary>
        /// Đóng giao diện Bàn Thiết Kế.
        /// </summary>
        public void CloseUI()
        {
            if (mainPanel == null) return;

            mainPanel.SetActive(false);

            // Mở khóa chuột và cho phép di chuyển nhân vật bình thường
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetCursorState(true);
                InputManager.Instance.IsGameplayInputBlocked = false;
            }
        }

        /// <summary>
        /// Sinh danh sách công thức vẽ bản thiết kế ở panel bên trái.
        /// </summary>
        private void PopulateRecipeList()
        {
            // 1. Dọn dẹp danh sách nút cũ
            foreach (var btn in activeRecipeButtons)
            {
                if (btn != null) Destroy(btn);
            }
            activeRecipeButtons.Clear();

            if (DraftingManager.Instance == null || recipeListContainer == null || recipeButtonPrefab == null)
                return;

            // 2. Tạo các nút mới dựa trên cơ sở dữ liệu công thức trong DraftingManager
            List<DraftingRecipeSO> recipes = DraftingManager.Instance.GetRecipes();
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;

                GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
                activeRecipeButtons.Add(btnObj);

                // Thiết lập hiển thị thông tin công thức trên nút
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = recipe.recipeName;
                }

                // Gắn sự kiện click
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => SelectRecipe(recipe));
                }
            }
        }

        /// <summary>
        /// Chọn một công thức cụ thể để hiển thị chi tiết nguyên liệu và mô tả.
        /// </summary>
        /// <param name="recipe">Công thức thiết kế được chọn</param>
        public void SelectRecipe(DraftingRecipeSO recipe)
        {
            selectedRecipe = recipe;
            RefreshDetailsPanel();
        }

        /// <summary>
        /// Cập nhật hiển thị panel nguyên liệu và mô tả dựa trên công thức đang chọn.
        /// </summary>
        private void RefreshDetailsPanel()
        {
            if (selectedRecipe == null)
            {
                ClearDetailsPanel();
                return;
            }

            // 1. Cập nhật mô tả kỹ thuật
            if (recipeDescriptionText != null)
            {
                recipeDescriptionText.text = selectedRecipe.recipeDescription;
            }

            // 2. Dọn dẹp danh sách nguyên liệu yêu cầu cũ
            foreach (var item in activeRequirementItems)
            {
                if (item != null) Destroy(item);
            }
            activeRequirementItems.Clear();

            // 3. Sinh danh sách nguyên liệu yêu cầu mới (Top-Right)
            if (requirementsContainer != null && requirementItemPrefab != null)
            {
                foreach (var req in selectedRecipe.requirements)
                {
                    if (req == null || req.item == null) continue;

                    GameObject reqObj = Instantiate(requirementItemPrefab, requirementsContainer);
                    activeRequirementItems.Add(reqObj);

                    // Thiết lập thông tin vật phẩm yêu cầu
                    // Thử tìm các component Image và Text trong prefab nguyên liệu
                    Image iconImage = reqObj.transform.Find("Icon")?.GetComponent<Image>();
                    TextMeshProUGUI nameText = reqObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI quantityText = reqObj.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

                    // Gán icon
                    if (iconImage != null)
                    {
                        iconImage.sprite = req.item.itemIcon;
                        iconImage.enabled = req.item.itemIcon != null;
                    }

                    // Gán tên
                    if (nameText != null)
                    {
                        nameText.text = req.item.itemName;
                    }

                    // Gán số lượng (Định dạng: Đang có / Yêu cầu)
                    if (quantityText != null)
                    {
                        int currentQty = 0;
                        if (InventoryManager.Instance != null)
                        {
                            // Tìm tổng số lượng nguyên liệu này đang có trong kho đồ
                            foreach (var slot in InventoryManager.Instance.slots)
                            {
                                if (slot != null && slot.item == req.item)
                                {
                                    currentQty += slot.quantity;
                                }
                            }
                        }

                        // Nếu đủ nguyên liệu, hiển thị màu trắng, thiếu hiển thị màu đỏ
                        string qtyColor = (currentQty >= req.quantity) ? "#FFFFFF" : "#FF5555";
                        quantityText.text = $"<color={qtyColor}>{currentQty}</color> / {req.quantity}";
                    }
                }
            }

            // 4. Cập nhật trạng thái của nút Vẽ thiết kế
            if (draftButton != null && DraftingManager.Instance != null)
            {
                bool canDraft = DraftingManager.Instance.CanDraft(selectedRecipe);
                draftButton.interactable = canDraft;
                
                // Thay đổi hiển thị chữ trên nút để phản hồi trực quan
                TextMeshProUGUI btnText = draftButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "VẼ";
                }
            }
        }

        /// <summary>
        /// Xóa sạch các thông tin trên panel chi tiết khi không có công thức nào được chọn.
        /// </summary>
        private void ClearDetailsPanel()
        {
            if (recipeDescriptionText != null)
            {
                recipeDescriptionText.text = "Chọn một bản thiết kế ở danh sách bên trái để bắt đầu thiết kế...";
            }

            foreach (var item in activeRequirementItems)
            {
                if (item != null) Destroy(item);
            }
            activeRequirementItems.Clear();

            if (draftButton != null)
            {
                draftButton.interactable = false;
                TextMeshProUGUI btnText = draftButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "VẼ";
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi bấm nút vẽ bản vẽ.
        /// </summary>
        private void OnDraftButtonClicked()
        {
            if (selectedRecipe == null) return;

            if (DraftingManager.Instance != null && DraftingManager.Instance.DraftBlueprint(selectedRecipe))
            {
                Debug.Log($"[DraftingTableUI] Đã hoàn thành bản vẽ: {selectedRecipe.resultBlueprint.itemName}");
                // UI tự động làm mới qua sự kiện OnDraftingSuccess
            }
        }
    }
}
