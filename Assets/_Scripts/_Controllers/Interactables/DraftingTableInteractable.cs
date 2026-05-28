using UnityEngine;
using RoboticsProject.Interfaces;

namespace RoboticsProject.Controllers.Interactables
{
    /// <summary>
    /// Bàn thiết kế bản vẽ (Blueprint).
    /// Giai đoạn 1: Tạo ra các bản thiết kế thô (raw blueprint) cho từng bộ phận riêng lẻ (Đầu, thân, tay, chân).
    /// </summary>
    public class DraftingTableInteractable : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            return "Vẽ bản thiết kế [E]";
        }

        public void OnInteract()
        {
            Debug.Log("[DraftingTable] Mở UI vẽ Blueprint...");
            // TODO: Mở UI cho phép người chơi chọn vẽ Đầu, Thân, Tay, Chân...
            // Kết quả của bước này là tạo ra các vật phẩm dạng Blueprint lưu vào Inventory.
        }
    }
}
