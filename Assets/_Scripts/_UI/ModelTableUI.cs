using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RoboticsProject.Managers;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.UI
{
    /// <summary>
    /// Điều khiển giao diện người dùng cho Bàn Chế Tạo (Model Table UI).
    /// Tuân thủ nguyên lý Single Responsibility (SRP) - Chỉ chịu trách nhiệm quản lý hiển thị, xem trước mô hình thành phẩm và hiển thị mô tả qua Tooltip khi di chuột.
    /// </summary>
    public class ModelTableUI : MonoBehaviour
    {
        public static ModelTableUI Instance { get; private set; }

        [Header("UI Panels")]
        [Tooltip("Khung panel chính chứa toàn bộ giao diện chế tạo")]
        [SerializeField] private GameObject mainPanel;

        [Header("Left Panel (1/2 Menu) - Model Recipe List")]
        [Tooltip("Container chứa danh sách các nút chọn công thức chế tạo")]
        [SerializeField] private Transform recipeListContainer;

        [Tooltip("Prefab cho nút chọn công thức chế tạo")]
        [SerializeField] private GameObject recipeButtonPrefab;

        [Header("Top-Right Panel (1/4 Menu) - Requirements")]
        [Tooltip("Container chứa danh sách các nguyên liệu yêu cầu để chế tạo")]
        [SerializeField] private Transform requirementsContainer;

        [Tooltip("Prefab hiển thị thông tin từng nguyên liệu yêu cầu (gồm Icon, tên, số lượng)")]
        [SerializeField] private GameObject requirementItemPrefab;

        [Header("Bottom-Right Panel (1/4 Menu) - Preview & Actions")]
        [Tooltip("Ảnh hiển thị mô hình thành phẩm (Result Model Preview)")]
        [SerializeField] private Image resultPreviewImage;

        [Tooltip("Nút bấm để thực hiện hành động chế tạo mô hình")]
        [SerializeField] private Button craftButton;

        [Tooltip("Nút đóng giao diện")]
        [SerializeField] private Button closeButton;

        [Header("Hover Tooltip System")]
        [Tooltip("Panel chứa hộp mô tả chi tiết khi hover chuột")]
        [SerializeField] private GameObject tooltipPanel;

        [Tooltip("Text hiển thị mô tả kỹ thuật trong Tooltip")]
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Tooltip("Khoảng cách lệch từ con trỏ chuột đến bảng Tooltip")]
        [SerializeField] private Vector2 tooltipOffset = new Vector2(15f, 15f);

        // Lưu trữ công thức đang được chọn
        private ModelRecipeSO selectedRecipe;

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
            // Ẩn panel UI và Tooltip khi bắt đầu game
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            // Gắn các lắng nghe sự kiện
            if (craftButton != null)
            {
                craftButton.onClick.AddListener(OnCraftButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseUI);
            }

            // Đăng ký sự kiện di chuột vào hình ảnh preview
            SetupPreviewHoverEvents();

            // Cấu hình bố cục tự động cho danh sách nguyên liệu yêu cầu
            ConfigureRequirementsLayout();

            // Lắng nghe sự kiện chế tạo thành công để làm mới giao diện
            if (ModelCraftingManager.Instance != null)
            {
                ModelCraftingManager.Instance.OnCraftingSuccess += RefreshDetailsPanel;
            }
        }

        /// <summary>
        /// Tự động cấu hình VerticalLayoutGroup và ContentSizeFitter trên container nguyên liệu.
        /// Đảm bảo các dòng nguyên liệu giãn cách đẹp mắt, không đè chồng chéo hoặc bị co dẹt.
        /// </summary>
        private void ConfigureRequirementsLayout()
        {
            if (requirementsContainer == null) return;

            VerticalLayoutGroup layoutGroup = requirementsContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = requirementsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.spacing = 10f; // Khoảng cách giữa các dòng nguyên liệu
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            ContentSizeFitter fitter = requirementsContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = requirementsContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private void OnDestroy()
        {
            if (ModelCraftingManager.Instance != null)
            {
                ModelCraftingManager.Instance.OnCraftingSuccess -= RefreshDetailsPanel;
            }
        }

        private void Update()
        {
            if (mainPanel != null && mainPanel.activeSelf)
            {
                // Cho phép nhấn phím ESC để đóng nhanh UI
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseUI();
                }

                // Cập nhật vị trí Tooltip bám theo chuột nếu Tooltip đang hoạt động
                if (tooltipPanel != null && tooltipPanel.activeSelf)
                {
                    UpdateTooltipPosition();
                }
            }
        }

        /// <summary>
        /// Mở giao diện Bàn Chế Tạo.
        /// </summary>
        public void OpenUI()
        {
            if (mainPanel == null)
            {
                Debug.LogError("[ModelTableUI] Trường 'Main Panel' chưa được kéo thả gán trong Inspector!");
                return;
            }

            mainPanel.SetActive(true);

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            // Khóa di chuyển của nhân vật và giải phóng con trỏ chuột
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetCursorState(false);
                InputManager.Instance.IsGameplayInputBlocked = true;
            }

            // Khởi tạo danh sách công thức ở panel bên trái
            PopulateRecipeList();

            // Mặc định chọn công thức đầu tiên nếu có
            var recipes = ModelCraftingManager.Instance != null ? ModelCraftingManager.Instance.GetRecipes() : null;
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
        /// Đóng giao diện Bàn Chế Tạo.
        /// </summary>
        public void CloseUI()
        {
            if (mainPanel == null) return;

            mainPanel.SetActive(false);

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            // Mở khóa chuột và khôi phục di chuyển nhân vật bình thường
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetCursorState(true);
                InputManager.Instance.IsGameplayInputBlocked = false;
            }
        }

        /// <summary>
        /// Thiết lập sự kiện hover (PointerEnter, PointerExit) động trên Preview Image.
        /// </summary>
        private void SetupPreviewHoverEvents()
        {
            if (resultPreviewImage == null) return;

            // Đảm bảo Image có tùy chọn Raycast Target được tích để nhận diện sự kiện chuột
            resultPreviewImage.raycastTarget = true;

            EventTrigger trigger = resultPreviewImage.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = resultPreviewImage.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();

            // Sự kiện chuột đi vào
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => ShowTooltip());
            trigger.triggers.Add(entryEnter);

            // Sự kiện chuột đi ra
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(entryExit);
        }

        /// <summary>
        /// Hiển thị bảng mô tả chi tiết (Tooltip).
        /// </summary>
        private void ShowTooltip()
        {
            if (selectedRecipe == null || tooltipPanel == null || tooltipText == null) return;

            tooltipText.text = selectedRecipe.recipeDescription;
            tooltipPanel.SetActive(true);
            UpdateTooltipPosition();
        }

        /// <summary>
        /// Ẩn bảng mô tả chi tiết (Tooltip).
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Cập nhật vị trí của Tooltip bám theo con trỏ chuột.
        /// </summary>
        private void UpdateTooltipPosition()
        {
            if (tooltipPanel == null) return;

            Vector2 mousePos = Input.mousePosition;
            tooltipPanel.transform.position = mousePos + tooltipOffset;
        }

        /// <summary>
        /// Sinh danh sách công thức chế tạo ở panel bên trái.
        /// </summary>
        private void PopulateRecipeList()
        {
            foreach (var btn in activeRecipeButtons)
            {
                if (btn != null) Destroy(btn);
            }
            activeRecipeButtons.Clear();

            if (ModelCraftingManager.Instance == null || recipeListContainer == null || recipeButtonPrefab == null)
                return;

            List<ModelRecipeSO> recipes = ModelCraftingManager.Instance.GetRecipes();
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;

                GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
                activeRecipeButtons.Add(btnObj);

                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = recipe.recipeName;
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => SelectRecipe(recipe));
                }
            }
        }

        /// <summary>
        /// Chọn một công thức cụ thể để hiển thị chi tiết.
        /// </summary>
        public void SelectRecipe(ModelRecipeSO recipe)
        {
            selectedRecipe = recipe;
            HideTooltip(); // Ẩn tooltip cũ khi đổi công thức
            RefreshDetailsPanel();
        }

        /// <summary>
        /// Cập nhật hiển thị panel nguyên liệu và ảnh preview dựa trên công thức đang chọn.
        /// </summary>
        private void RefreshDetailsPanel()
        {
            if (selectedRecipe == null)
            {
                ClearDetailsPanel();
                return;
            }

            // 1. Cập nhật ảnh Preview thành phẩm
            if (resultPreviewImage != null)
            {
                resultPreviewImage.preserveAspect = true;
                if (selectedRecipe.resultModel != null && selectedRecipe.resultModel.itemIcon != null)
                {
                    resultPreviewImage.sprite = selectedRecipe.resultModel.itemIcon;
                    resultPreviewImage.enabled = true;
                }
                else
                {
                    resultPreviewImage.sprite = null;
                    resultPreviewImage.enabled = false;
                }
            }

            // 2. Dọn dẹp danh sách nguyên liệu yêu cầu cũ
            foreach (var item in activeRequirementItems)
            {
                if (item != null) Destroy(item);
            }
            activeRequirementItems.Clear();

            if (requirementsContainer == null || requirementItemPrefab == null)
                return;

            // 3. Sinh dòng yêu cầu Bản vẽ thiết kế (Blueprint) trước (không tiêu hao)
            if (selectedRecipe.requiredBlueprint != null)
            {
                GameObject bluePrintObj = Instantiate(requirementItemPrefab, requirementsContainer);
                activeRequirementItems.Add(bluePrintObj);

                // Cố định chiều cao dòng để tránh bị co dẹt và lệch chữ
                LayoutElement layoutElement = bluePrintObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = bluePrintObj.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = 45f;
                layoutElement.minHeight = 40f;

                Image iconImage = bluePrintObj.transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI nameText = bluePrintObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI quantityText = bluePrintObj.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

                if (iconImage != null)
                {
                    iconImage.sprite = selectedRecipe.requiredBlueprint.itemIcon;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = selectedRecipe.requiredBlueprint.itemIcon != null;
                }

                if (nameText != null)
                {
                    nameText.text = $"{selectedRecipe.requiredBlueprint.itemName} <color=#55AAFF>(Bản Vẽ)</color>";
                }

                if (quantityText != null)
                {
                    bool hasBlueprint = false;
                    if (InventoryManager.Instance != null)
                    {
                        hasBlueprint = InventoryManager.Instance.HasItem(selectedRecipe.requiredBlueprint, 1);
                    }

                    string qtyText = hasBlueprint ? "<color=#FFFFFF>1</color> / 1" : "<color=#FF5555>0</color> / 1";
                    quantityText.text = qtyText;
                }
            }

            // 4. Sinh danh sách nguyên liệu yêu cầu tiêu hao (Top-Right)
            foreach (var req in selectedRecipe.ingredients)
            {
                if (req == null || req.item == null) continue;

                GameObject reqObj = Instantiate(requirementItemPrefab, requirementsContainer);
                activeRequirementItems.Add(reqObj);

                // Cố định chiều cao dòng để tránh bị co dẹt và lệch chữ
                LayoutElement layoutElement = reqObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = reqObj.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = 45f;
                layoutElement.minHeight = 40f;

                Image iconImage = reqObj.transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI nameText = reqObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI quantityText = reqObj.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

                if (iconImage != null)
                {
                    iconImage.sprite = req.item.itemIcon;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = req.item.itemIcon != null;
                }

                if (nameText != null)
                {
                    nameText.text = req.item.itemName;
                }

                if (quantityText != null)
                {
                    int currentQty = 0;
                    if (InventoryManager.Instance != null)
                    {
                        foreach (var slot in InventoryManager.Instance.slots)
                        {
                            if (slot != null && slot.item == req.item)
                            {
                                currentQty += slot.quantity;
                            }
                        }
                    }

                    string qtyColor = (currentQty >= req.quantity) ? "#FFFFFF" : "#FF5555";
                    quantityText.text = $"<color={qtyColor}>{currentQty}</color> / {req.quantity}";
                }
            }

            // 5. Cập nhật trạng thái của nút Chế tạo
            if (craftButton != null && ModelCraftingManager.Instance != null)
            {
                bool canCraft = ModelCraftingManager.Instance.CanCraft(selectedRecipe);
                craftButton.interactable = canCraft;
                
                TextMeshProUGUI btnText = craftButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "CHẾ TẠO";
                }
            }
        }

        /// <summary>
        /// Xóa sạch thông tin hiển thị khi không có công thức nào được chọn.
        /// </summary>
        private void ClearDetailsPanel()
        {
            if (resultPreviewImage != null)
            {
                resultPreviewImage.sprite = null;
                resultPreviewImage.enabled = false;
            }

            foreach (var item in activeRequirementItems)
            {
                if (item != null) Destroy(item);
            }
            activeRequirementItems.Clear();

            if (craftButton != null)
            {
                craftButton.interactable = false;
                TextMeshProUGUI btnText = craftButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "CHẾ TẠO";
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi bấm nút chế tạo mô hình.
        /// </summary>
        private void OnCraftButtonClicked()
        {
            if (selectedRecipe == null) return;

            if (ModelCraftingManager.Instance != null && ModelCraftingManager.Instance.CraftModel(selectedRecipe))
            {
                Debug.Log($"[ModelTableUI] Đã hoàn thành chế tạo: {selectedRecipe.resultModel.itemName}");
            }
        }
    }
}
