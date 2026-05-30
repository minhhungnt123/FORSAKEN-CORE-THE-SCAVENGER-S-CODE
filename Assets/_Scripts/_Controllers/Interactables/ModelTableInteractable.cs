using UnityEngine;
using RoboticsProject.Interfaces;
using RoboticsProject.UI;

namespace RoboticsProject.Controllers.Interactables
{
    /// <summary>
    /// Bàn chế tạo mô hình 3D (Model Table).
    /// Giai đoạn 2: Tiêu thụ các bản thiết kế (Blueprint) và nguyên liệu (sắt, đồng...) để tạo ra các bộ phận vật lý (Model).
    /// </summary>
    public class ModelTableInteractable : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            return "Chế tạo mô hình [E]";
        }

        public void OnInteract()
        {
            Debug.Log("[ModelTable] Mở UI chế tạo mô hình...");
            if (ModelTableUI.Instance != null)
            {
                ModelTableUI.Instance.OpenUI();
            }
            else
            {
                Debug.LogWarning("[ModelTableInteractable] Không tìm thấy ModelTableUI.Instance trong Scene!");
            }
        }
    }
}
