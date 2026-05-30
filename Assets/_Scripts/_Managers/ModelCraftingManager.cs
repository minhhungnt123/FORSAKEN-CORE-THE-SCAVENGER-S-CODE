using System;
using System.Collections.Generic;
using UnityEngine;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.Managers
{
    /// <summary>
    /// Quản lý logic nghiệp vụ cho tiến trình chế tạo mô hình (Model Crafting).
    /// Đảm nhận vai trò kiểm tra bản vẽ thiết kế, tài nguyên tiêu hao và thực hiện giao dịch chế tạo mô hình.
    /// </summary>
    public class ModelCraftingManager : MonoBehaviour
    {
        public static ModelCraftingManager Instance { get; private set; }

        [Header("Danh sách công thức")]
        [Tooltip("Danh sách toàn bộ các mô hình có thể chế tạo được trong game")]
        [SerializeField] private List<ModelRecipeSO> modelRecipes = new List<ModelRecipeSO>();

        // Sự kiện thông báo khi chế tạo thành công để UI tự động làm mới dữ liệu
        public event Action OnCraftingSuccess;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Lấy danh sách tất cả các công thức chế tạo mô hình.
        /// </summary>
        public List<ModelRecipeSO> GetRecipes()
        {
            return modelRecipes;
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ điều kiện để chế tạo một mô hình cụ thể hay không.
        /// </summary>
        /// <param name="recipe">Công thức chế tạo cần kiểm tra</param>
        /// <returns>True nếu đủ điều kiện, ngược lại False</returns>
        public bool CanCraft(ModelRecipeSO recipe)
        {
            if (recipe == null) return false;
            if (InventoryManager.Instance == null) return false;

            // 1. Kiểm tra xem người chơi có sở hữu bản vẽ thiết kế bắt buộc không (không bị tiêu hao)
            if (recipe.requiredBlueprint != null)
            {
                if (!InventoryManager.Instance.HasItem(recipe.requiredBlueprint, 1))
                {
                    return false;
                }
            }

            // 2. Kiểm tra túi đồ còn chỗ chứa mô hình đầu ra hay không
            if (!InventoryManager.Instance.CanAddItem(recipe.resultModel, 1))
            {
                return false;
            }

            // 3. Kiểm tra xem có đủ từng loại nguyên liệu trong danh sách yêu cầu không
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null) continue;

                if (!InventoryManager.Instance.HasItem(ingredient.item, ingredient.quantity))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Thực hiện chế tạo mô hình (tiêu hao nguyên liệu thô và thêm mô hình vào Inventory).
        /// </summary>
        /// <param name="recipe">Công thức chế tạo mong muốn</param>
        /// <returns>True nếu thực hiện chế tạo thành công</returns>
        public bool CraftModel(ModelRecipeSO recipe)
        {
            if (recipe == null) return false;

            // Kiểm tra điều kiện một lần nữa trước khi thực hiện giao dịch (Transaction Integrity)
            if (!CanCraft(recipe))
            {
                Debug.LogWarning($"[ModelCraftingManager] Không đủ điều kiện để chế tạo mô hình: {recipe.recipeName}");
                return false;
            }

            // 1. Khấu trừ các nguyên liệu khỏi kho đồ của người chơi
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null) continue;
                
                InventoryManager.Instance.RemoveItem(ingredient.item, ingredient.quantity);
            }

            // 2. Thêm mô hình kết quả vào kho đồ
            bool addSuccess = InventoryManager.Instance.AddItem(recipe.resultModel, 1);

            if (addSuccess)
            {
                Debug.Log($"[ModelCraftingManager] Chế tạo thành công mô hình: {recipe.resultModel.itemName}");
                OnCraftingSuccess?.Invoke();
                return true;
            }
            else
            {
                Debug.LogError($"[ModelCraftingManager] Lỗi xảy ra khi thêm mô hình {recipe.resultModel.itemName} vào kho đồ!");
                return false;
            }
        }
    }
}
