using System;

namespace RoboticsProject.Modules.Inventory
{
    /// <summary>
    /// Lưu trữ thông tin của một ô vật phẩm trong kho đồ (Data Model).
    /// </summary>
    [Serializable]
    public class ItemSlot
    {
        public ItemData item;
        public int quantity;

        public ItemSlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }
}
