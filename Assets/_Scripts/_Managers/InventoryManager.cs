using System;
using System.Collections.Generic;
using UnityEngine;
using RoboticsProject.Modules.Inventory;

namespace RoboticsProject.Managers
{
    /// <summary>
    /// Quản lý Logic toàn bộ kho đồ. Sử dụng Singleton để dễ gọi từ các Bàn.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Data")]
        public List<ItemSlot> slots = new List<ItemSlot>();
        public int maxSlots = 20;

        // Event báo hiệu cho UI biết khi túi đồ thay đổi dữ liệu bên trong
        public event Action OnInventoryChanged;

        // Event báo hiệu khi có vật phẩm được thêm vào túi đồ (phục vụ UI thông báo)
        public event Action<ItemData, int> OnItemAdded;

        // Event báo hiệu khi trạng thái mở/đóng túi đồ thay đổi
        public event Action<bool> OnInventoryToggle;

        // Trạng thái mở/đóng hiện tại của túi đồ
        public bool IsOpen { get; private set; } = false;

        private void Awake()
        {
            // Thiết lập Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ túi đồ không bị mất khi qua Scene khác

            InitializeInventory();
        }

        private void Start()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            // Khởi tạo các ô trống cho đủ số lượng maxSlots
            while (slots.Count < maxSlots)
            {
                slots.Add(new ItemSlot(null, 0));
            }

            // Cắt ngắn nếu vượt quá maxSlots
            if (slots.Count > maxSlots)
            {
                slots.RemoveRange(maxSlots, slots.Count - maxSlots);
            }
        }

        private void Update()
        {
            // Sử dụng InputManager để kiểm tra phím bấm mở hòm đồ
            if (InputManager.Instance != null && InputManager.Instance.InventoryTriggered)
            {
                ToggleInventory();
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái mở của kho đồ và kích hoạt sự kiện cho UI cập nhật
        /// </summary>
        public void ToggleInventory()
        {
            IsOpen = !IsOpen;
            OnInventoryToggle?.Invoke(IsOpen);
        }

        /// <summary>
        /// Kiểm tra xem có thể thêm một lượng vật phẩm vào túi đồ hay không (Dry-run check).
        /// </summary>
        public bool CanAddItem(ItemData itemToAdd, int amount)
        {
            if (itemToAdd == null || amount <= 0) return false;

            int freeSpace = 0;
            if (itemToAdd.isStackable)
            {
                // Tính dung lượng còn trống ở các ô hiện có
                foreach (var slot in slots)
                {
                    if (slot != null && slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
                    {
                        freeSpace += (itemToAdd.maxStackSize - slot.quantity);
                    }
                }
            }

            // Tính số lượng ô trống hoàn toàn
            foreach (var slot in slots)
            {
                if (slot == null || slot.item == null)
                {
                    freeSpace += (itemToAdd.isStackable ? itemToAdd.maxStackSize : 1);
                }
            }

            return freeSpace >= amount;
        }

        /// <summary>
        /// Kiểm tra xem có thể thêm một danh sách vật phẩm vào túi đồ hay không (Batch dry-run check).
        /// Đảm bảo tính toàn vẹn giao dịch khi nhặt nhiều vật phẩm.
        /// </summary>
        public bool CanAddItems(List<ItemSlot> itemsToAdd)
        {
            if (itemsToAdd == null || itemsToAdd.Count == 0) return true;

            // Mô phỏng danh sách các ô đồ để kiểm tra dung lượng thực tế
            List<ItemSlot> tempSlots = new List<ItemSlot>();
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    tempSlots.Add(new ItemSlot(slot.item, slot.quantity));
                }
                else
                {
                    tempSlots.Add(new ItemSlot(null, 0));
                }
            }

            foreach (var toAdd in itemsToAdd)
            {
                if (toAdd == null || toAdd.item == null || toAdd.quantity <= 0) continue;

                int amount = toAdd.quantity;
                ItemData itemData = toAdd.item;

                if (itemData.isStackable)
                {
                    // Mô phỏng cộng dồn vào các ô hiện có
                    foreach (var slot in tempSlots)
                    {
                        if (slot.item == itemData && slot.quantity < itemData.maxStackSize)
                        {
                            int spaceLeft = itemData.maxStackSize - slot.quantity;
                            if (amount <= spaceLeft)
                            {
                                slot.quantity += amount;
                                amount = 0;
                                break;
                            }
                            else
                            {
                                slot.quantity += spaceLeft;
                                amount -= spaceLeft;
                            }
                        }
                    }

                    // Mô phỏng tạo ô mới
                    while (amount > 0)
                    {
                        bool foundEmpty = false;
                        foreach (var slot in tempSlots)
                        {
                            if (slot.item == null)
                            {
                                int quantityToAdd = Mathf.Min(amount, itemData.maxStackSize);
                                slot.item = itemData;
                                slot.quantity = quantityToAdd;
                                amount -= quantityToAdd;
                                foundEmpty = true;
                                break;
                            }
                        }

                        if (!foundEmpty)
                        {
                            return false; // Không đủ chỗ chứa tất cả
                        }
                    }
                }
                else
                {
                    // Mô phỏng đồ không xếp chồng
                    while (amount > 0)
                    {
                        bool foundEmpty = false;
                        foreach (var slot in tempSlots)
                        {
                            if (slot.item == null)
                            {
                                slot.item = itemData;
                                slot.quantity = 1;
                                amount--;
                                foundEmpty = true;
                                break;
                            }
                        }

                        if (!foundEmpty)
                        {
                            return false; // Không đủ chỗ chứa tất cả
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Thêm vật phẩm vào túi đồ
        /// </summary>
        public bool AddItem(ItemData itemToAdd, int amount)
        {
            if (itemToAdd == null || amount <= 0) return false;

            int originalAmount = amount;

            if (itemToAdd.isStackable)
            {
                // Cộng dồn vào các ô đã có
                foreach (var slot in slots)
                {
                    if (slot != null && slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
                    {
                        int spaceLeft = itemToAdd.maxStackSize - slot.quantity;
                        if (amount <= spaceLeft)
                        {
                            slot.quantity += amount;
                            amount = 0;
                            break;
                        }
                        else
                        {
                            slot.quantity += spaceLeft;
                            amount -= spaceLeft;
                        }
                    }
                }

                // Nếu vẫn còn thừa, tìm ô trống
                while (amount > 0)
                {
                    bool foundEmpty = false;
                    foreach (var slot in slots)
                    {
                        if (slot != null && slot.item == null)
                        {
                            int quantityToAdd = Mathf.Min(amount, itemToAdd.maxStackSize);
                            slot.item = itemToAdd;
                            slot.quantity = quantityToAdd;
                            amount -= quantityToAdd;
                            foundEmpty = true;
                            break;
                        }
                    }

                    if (!foundEmpty)
                    {
                        Debug.LogWarning("Túi đồ đã đầy!");
                        OnInventoryChanged?.Invoke();
                        int addedAmount = originalAmount - amount;
                        if (addedAmount > 0)
                        {
                            OnItemAdded?.Invoke(itemToAdd, addedAmount);
                        }
                        return false;
                    }
                }
            }
            else
            {
                // Đồ không stack được thì mỗi cái chiếm 1 ô
                while (amount > 0)
                {
                    bool foundEmpty = false;
                    foreach (var slot in slots)
                    {
                        if (slot != null && slot.item == null)
                        {
                            slot.item = itemToAdd;
                            slot.quantity = 1;
                            amount--;
                            foundEmpty = true;
                            break;
                        }
                    }

                    if (!foundEmpty)
                    {
                        Debug.LogWarning("Túi đồ đã đầy!");
                        OnInventoryChanged?.Invoke();
                        int addedAmount = originalAmount - amount;
                        if (addedAmount > 0)
                        {
                            OnItemAdded?.Invoke(itemToAdd, addedAmount);
                        }
                        return false;
                    }
                }
            }

            OnInventoryChanged?.Invoke(); // Báo cho UI cập nhật
            int totalAdded = originalAmount - amount;
            if (totalAdded > 0)
            {
                OnItemAdded?.Invoke(itemToAdd, totalAdded);
            }
            return true;
        }

        /// <summary>
        /// Kiểm tra xem có đủ số lượng vật phẩm không
        /// </summary>
        public bool HasItem(ItemData itemToCheck, int amountRequired)
        {
            if (itemToCheck == null) return false;
            int currentAmount = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.item == itemToCheck)
                {
                    currentAmount += slot.quantity;
                }
            }
            return currentAmount >= amountRequired;
        }

        /// <summary>
        /// Xóa vật phẩm khỏi túi
        /// </summary>
        public bool RemoveItem(ItemData itemToRemove, int amount)
        {
            if (itemToRemove == null || !HasItem(itemToRemove, amount)) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.item == itemToRemove)
                {
                    if (slot.quantity >= amount)
                    {
                        slot.quantity -= amount;
                        if (slot.quantity == 0)
                        {
                            slot.item = null;
                            slot.quantity = 0;
                        }
                        amount = 0;
                        break;
                    }
                    else
                    {
                        amount -= slot.quantity;
                        slot.item = null;
                        slot.quantity = 0;
                    }
                }
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Hoán đổi vị trí hoặc gộp vật phẩm giữa hai slot
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count) return;
            if (indexA == indexB) return;

            ItemSlot slotA = slots[indexA];
            ItemSlot slotB = slots[indexB];

            if (slotA == null || slotB == null) return;

            // Xử lý gộp vật phẩm (Merge) nếu cùng loại và stackable
            if (slotA.item != null && slotB.item != null && slotA.item == slotB.item && slotA.item.isStackable)
            {
                int maxStack = slotB.item.maxStackSize;
                if (slotB.quantity < maxStack)
                {
                    int spaceLeft = maxStack - slotB.quantity;
                    int amountToTransfer = Mathf.Min(slotA.quantity, spaceLeft);

                    slotB.quantity += amountToTransfer;
                    slotA.quantity -= amountToTransfer;

                    if (slotA.quantity <= 0)
                    {
                        slotA.item = null;
                        slotA.quantity = 0;
                    }

                    OnInventoryChanged?.Invoke();
                    return;
                }
            }

            // Hoán đổi bình thường
            ItemData tempItem = slotA.item;
            int tempQuantity = slotA.quantity;

            slotA.item = slotB.item;
            slotA.quantity = slotB.quantity;

            slotB.item = tempItem;
            slotB.quantity = tempQuantity;

            OnInventoryChanged?.Invoke();
        }
    }
}

