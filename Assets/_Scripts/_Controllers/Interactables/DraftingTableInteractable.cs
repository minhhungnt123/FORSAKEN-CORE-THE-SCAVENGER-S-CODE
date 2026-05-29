using UnityEngine;
using RoboticsProject.Interfaces;
using RoboticsProject.UI;

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
            if (DraftingTableUI.Instance != null)
            {
                DraftingTableUI.Instance.OpenUI();
            }
            else
            {
                Debug.LogWarning("[DraftingTableInteractable] Không tìm thấy DraftingTableUI.Instance trong Scene!");
            }
        }
    }
}
