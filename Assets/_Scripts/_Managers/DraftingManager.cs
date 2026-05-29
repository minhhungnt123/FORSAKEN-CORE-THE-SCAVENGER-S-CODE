using System;
using System.Collections.Generic;
using UnityEngine;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.Managers
{
    /// <summary>
    /// Quản lý logic nghiệp vụ cho tiến trình vẽ bản thiết kế (Drafting).
    /// Đảm nhận vai trò kiểm tra tài nguyên và thực hiện giao dịch (đổi nguyên liệu lấy bản vẽ).
    /// </summary>
    public class DraftingManager : MonoBehaviour
    {
        public static DraftingManager Instance { get; private set; }

        [Header("Danh sách công thức")]
        [Tooltip("Danh sách toàn bộ các bản thiết kế có thể vẽ được trong game")]
        [SerializeField] private List<DraftingRecipeSO> draftingRecipes = new List<DraftingRecipeSO>();

        // Sự kiện thông báo khi vẽ thành công để UI tự động làm mới dữ liệu
        public event Action OnDraftingSuccess;

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
        /// Lấy danh sách tất cả các công thức vẽ bản thiết kế.
        /// </summary>
        public List<DraftingRecipeSO> GetRecipes()
        {
            return draftingRecipes;
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ điều kiện để vẽ một công thức cụ thể hay không.
        /// </summary>
        /// <param name="recipe">Công thức cần kiểm tra</param>
        /// <returns>True nếu đủ điều kiện, ngược lại False</returns>
        public bool CanDraft(DraftingRecipeSO recipe)
        {
            if (recipe == null) return false;
            if (InventoryManager.Instance == null) return false;

            // 1. Kiểm tra túi đồ còn chỗ chứa bản vẽ đầu ra hay không
            if (!InventoryManager.Instance.CanAddItem(recipe.resultBlueprint, 1))
            {
                return false;
            }

            // 2. Kiểm tra xem có đủ từng loại nguyên liệu trong danh sách yêu cầu không
            foreach (var requirement in recipe.requirements)
            {
                if (requirement == null || requirement.item == null) continue;

                if (!InventoryManager.Instance.HasItem(requirement.item, requirement.quantity))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Thực hiện vẽ bản vẽ thiết kế (tiêu hao nguyên liệu và thêm bản vẽ vào Inventory).
        /// </summary>
        /// <param name="recipe">Công thức vẽ mong muốn</param>
        /// <returns>True nếu thực hiện vẽ thành công</returns>
        public bool DraftBlueprint(DraftingRecipeSO recipe)
        {
            if (recipe == null) return false;

            // Kiểm tra điều kiện một lần nữa trước khi thực hiện giao dịch (Transaction Integrity)
            if (!CanDraft(recipe))
            {
                Debug.LogWarning($"[DraftingManager] Không đủ điều kiện để vẽ bản thiết kế: {recipe.recipeName}");
                return false;
            }

            // 1. Khấu trừ các nguyên liệu khỏi kho đồ của người chơi
            foreach (var requirement in recipe.requirements)
            {
                if (requirement == null || requirement.item == null) continue;
                
                InventoryManager.Instance.RemoveItem(requirement.item, requirement.quantity);
            }

            // 2. Thêm bản vẽ kết quả vào kho đồ
            bool addSuccess = InventoryManager.Instance.AddItem(recipe.resultBlueprint, 1);

            if (addSuccess)
            {
                Debug.Log($"[DraftingManager] Vẽ thành công bản thiết kế: {recipe.resultBlueprint.itemName}");
                OnDraftingSuccess?.Invoke();
                return true;
            }
            else
            {
                Debug.LogError($"[DraftingManager] Lỗi xảy ra khi thêm bản thiết kế {recipe.resultBlueprint.itemName} vào kho đồ!");
                return false;
            }
        }
    }
}
