using System.Collections.Generic;
using UnityEngine;

namespace RoboticsProject.Modules.Inventory
{
    /// <summary>
    /// Định nghĩa công thức chế tạo mô hình (Recipe) cho Bàn Chế Tạo (Model Table).
    /// Tuân thủ nguyên lý Single Responsibility (SRP) - Chỉ lưu trữ cấu trúc dữ liệu của công thức.
    /// </summary>
    [CreateAssetMenu(fileName = "NewModelRecipe", menuName = "Robotics/Model Recipe", order = 3)]
    public class ModelRecipeSO : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("Tên của công thức chế tạo (Ví dụ: Chế tạo mô hình Đầu)")]
        public string recipeName = "Tên công thức";

        [Tooltip("Mô hình 3D đầu ra (Vật phẩm loại Model)")]
        public ItemData resultModel;

        [Header("Yêu cầu bản vẽ")]
        [Tooltip("Bản vẽ thiết kế bắt buộc phải có trong túi đồ (Không bị tiêu hao)")]
        public ItemData requiredBlueprint;

        [Header("Nguyên liệu tiêu hao")]
        [Tooltip("Danh sách nguyên liệu chế tạo sẽ bị tiêu hao (Ví dụ: Sắt, Đồng, Lõi Năng Lượng...)")]
        public List<ItemSlot> ingredients = new List<ItemSlot>();

        [Header("Mô tả kỹ thuật")]
        [TextArea(3, 5)]
        [Tooltip("Mô tả chi tiết về mô hình để hiển thị trên UI")]
        public string recipeDescription = "Mô tả chi tiết linh kiện...";

        private void OnValidate()
        {
            // 1. Kiểm tra mô hình thành phẩm đầu ra phải là loại Model
            if (resultModel != null && resultModel.itemType != ItemType.Model)
            {
                Debug.LogWarning($"[ModelRecipeSO] Vật phẩm '{resultModel.itemName}' ở mục Result Model phải có Item Type là 'Model'. Tự động reset ô này.");
                resultModel = null;
            }

            // 2. Kiểm tra bản vẽ yêu cầu phải là loại Blueprint
            if (requiredBlueprint != null && requiredBlueprint.itemType != ItemType.Blueprint)
            {
                Debug.LogWarning($"[ModelRecipeSO] Vật phẩm '{requiredBlueprint.itemName}' ở mục Required Blueprint phải có Item Type là 'Blueprint'. Tự động reset ô này.");
                requiredBlueprint = null;
            }

            // 3. Kiểm tra các nguyên liệu tiêu hao phải là loại Resource
            if (ingredients != null)
            {
                for (int i = 0; i < ingredients.Count; i++)
                {
                    var slot = ingredients[i];
                    if (slot != null && slot.item != null && slot.item.itemType != ItemType.Resource)
                    {
                        Debug.LogWarning($"[ModelRecipeSO] Nguyên liệu '{slot.item.itemName}' tại Element {i} phải có Item Type là 'Resource'. Tự động reset ô này.");
                        slot.item = null;
                    }
                }
            }
        }
    }
}
