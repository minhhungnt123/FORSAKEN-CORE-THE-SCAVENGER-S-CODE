using UnityEngine;
using System.Collections.Generic;
using RoboticsProject.Interfaces;
using RoboticsProject.Modules.Inventory;
using RoboticsProject.Managers;

namespace RoboticsProject.Controllers.Inventory
{
    /// <summary>
    /// Script gắn vào các đối tượng vật phẩm nhặt được trên thế giới (Loot Item).
    /// Triển khai IInteractable để tích hợp với hệ thống tương tác có sẵn của dự án.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [Tooltip("Dòng chữ hiển thị khi người chơi nhìn vào vật phẩm")]
        [SerializeField] private string interactPrompt = "Nhặt vật phẩm [E]";

        [Header("Loot Configuration")]
        [Tooltip("Danh sách các ItemData có thể xuất hiện ngẫu nhiên")]
        [SerializeField] private List<ItemData> lootPool = new List<ItemData>();

        [Header("Quantity Limits")]
        [Tooltip("Số lượng tối thiểu của mỗi vật phẩm khi nhặt")]
        [SerializeField] private int minAmountPerItem = 1;
        
        [Tooltip("Số lượng tối đa của mỗi vật phẩm khi nhặt")]
        [SerializeField] private int maxAmountPerItem = 5;

        [Header("Unique Items Per Pickup")]
        [Tooltip("Số loại vật phẩm tối thiểu nhận được trong 1 lần nhặt")]
        [SerializeField] private int minUniqueItems = 1;

        [Tooltip("Số loại vật phẩm tối đa nhận được trong 1 lần nhặt")]
        [SerializeField] private int maxUniqueItems = 3;

        /// <summary>
        /// Triển khai IInteractable: Trả về dòng chữ thông báo
        /// </summary>
        public string GetInteractPrompt()
        {
            return interactPrompt;
        }

        /// <summary>
        /// Triển khai IInteractable: Hành động khi người chơi tương tác
        /// </summary>
        public void OnInteract()
        {
            if (lootPool == null || lootPool.Count == 0)
            {
                Debug.LogWarning($"[ItemPickup] Loot Pool trống trên GameObject: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[ItemPickup] Không tìm thấy Instance của InventoryManager trong Scene!");
                return;
            }

            // Tiến hành Random danh sách vật phẩm nhận được và thêm vào túi đồ
            bool success = GenerateAndAddLoot();

            // Chỉ hủy đối tượng sau khi đã nhặt xong thành công tất cả vật phẩm
            if (success)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Lựa chọn ngẫu nhiên các vật phẩm từ Loot Pool và thêm vào InventoryManager
        /// </summary>
        private bool GenerateAndAddLoot()
        {
            // Xác định số lượng loại vật phẩm độc nhất sẽ nhận được
            int poolSize = lootPool.Count;
            int targetUniqueCount = Random.Range(minUniqueItems, maxUniqueItems + 1);
            targetUniqueCount = Mathf.Clamp(targetUniqueCount, 1, poolSize); // Không vượt quá kích thước lootPool

            // Sao chép danh sách lootPool để thực hiện xáo trộn tránh trùng lặp
            List<ItemData> tempPool = new List<ItemData>(lootPool);
            
            // Xáo trộn danh sách (Fisher-Yates Shuffle)
            for (int i = tempPool.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                ItemData temp = tempPool[i];
                tempPool[i] = tempPool[randomIndex];
                tempPool[randomIndex] = temp;
            }

            // Lấy ra các vật phẩm ngẫu nhiên đầu tiên và tạo danh sách dự kiến nhặt
            List<ItemSlot> itemsToPickup = new List<ItemSlot>();
            for (int i = 0; i < targetUniqueCount; i++)
            {
                ItemData selectedItem = tempPool[i];
                if (selectedItem == null) continue;

                // Xác định số lượng ngẫu nhiên cho vật phẩm này
                int randomAmount = Random.Range(minAmountPerItem, maxAmountPerItem + 1);
                itemsToPickup.Add(new ItemSlot(selectedItem, randomAmount));
            }

            // Kiểm tra xem túi đồ có đủ sức chứa toàn bộ danh sách này không
            if (InventoryManager.Instance.CanAddItems(itemsToPickup))
            {
                foreach (var itemSlot in itemsToPickup)
                {
                    InventoryManager.Instance.AddItem(itemSlot.item, itemSlot.quantity);
                    Debug.Log($"<color=#00FF00>[Inventory]</color> Đã nhặt: <b>{itemSlot.item.itemName}</b> x{itemSlot.quantity}!");
                }
                return true;
            }
            else
            {
                Debug.LogWarning("[ItemPickup] Không thể nhặt! Túi đồ đã đầy.");
                return false;
            }
        }
    }
}
