using System.Collections.Generic;
using UnityEngine;

namespace RoboticsProject.Modules.Inventory
{
    /// <summary>
    /// Định nghĩa công thức vẽ bản thiết kế (Recipe) cho Bàn Thiết Kế (Drafting Table).
    /// Tuân thủ nguyên lý Single Responsibility (SRP) - Chỉ lưu trữ cấu trúc dữ liệu của công thức.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDraftingRecipe", menuName = "Robotics/Drafting Recipe", order = 2)]
    public class DraftingRecipeSO : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("Tên của công thức vẽ (Ví dụ: Vẽ Bản Thiết Kế Khung Gầm)")]
        public string recipeName = "Tên công thức";

        [Tooltip("Bản thiết kế đầu ra (Vật phẩm loại Blueprint)")]
        public ItemData resultBlueprint;

        [Header("Nguyên liệu yêu cầu")]
        [Tooltip("Danh sách nguyên liệu vẽ (Ví dụ: Giấy vẽ, Ghi chép phế liệu...)")]
        public List<ItemSlot> requirements = new List<ItemSlot>();

        [Header("Mô tả kỹ thuật")]
        [TextArea(3, 5)]
        [Tooltip("Mô tả chi tiết về bộ phận thiết kế để hiển thị trên UI")]
        public string recipeDescription = "Mô tả chi tiết cấu trúc bộ phận...";

        private void OnValidate()
        {
            // 1. Kiểm tra bản thiết kế đầu ra phải là loại Blueprint
            if (resultBlueprint != null && resultBlueprint.itemType != ItemType.Blueprint)
            {
                Debug.LogWarning($"[DraftingRecipeSO] Vật phẩm '{resultBlueprint.itemName}' ở mục Result Blueprint phải có Item Type là 'Blueprint'. Tự động reset ô này.");
                resultBlueprint = null;
            }

            // 2. Kiểm tra các nguyên liệu vẽ phải là loại Resource
            if (requirements != null)
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    var slot = requirements[i];
                    if (slot != null && slot.item != null && slot.item.itemType != ItemType.Resource)
                    {
                        Debug.LogWarning($"[DraftingRecipeSO] Nguyên liệu '{slot.item.itemName}' tại Element {i} phải có Item Type là 'Resource'. Tự động reset ô này.");
                        slot.item = null;
                    }
                }
            }
        }
    }
}
