using UnityEngine;

namespace RoboticsProject.Modules.Inventory
{
    public enum ItemType
    {
        Resource,   // Nguyên liệu thô (Sắt, Đồng...)
        Blueprint,  // Bản vẽ thiết kế
        Model       // Mô hình 3D (Đầu, Tay, Chân...)
    }

    /// <summary>
    /// ScriptableObject chứa dữ liệu cố định của một vật phẩm.
    /// Cho phép tạo dữ liệu trực tiếp từ menu chuột phải trong Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Robotics/Item Data", order = 1)]
    public class ItemData : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        public string id = "item_id_unique";
        public string itemName = "Tên Vật Phẩm";
        [TextArea(2, 4)]
        public string description = "Mô tả vật phẩm...";
        public ItemType itemType = ItemType.Resource;

        [Header("Hiển thị (UI)")]
        [Tooltip("Ảnh hiển thị của vật phẩm trong túi đồ")]
        public Sprite itemIcon; // ĐÂY LÀ CHỖ CHỨA ẢNH RESOURCE!

        [Header("Thông số phụ")]
        public bool isStackable = true; // Có thể xếp chồng lên nhau không (VD: Sắt x99)
        public int maxStackSize = 99;
    }
}
