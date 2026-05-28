using UnityEngine;
using RoboticsProject.Interfaces;

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
            // TODO: Mở UI yêu cầu người chơi đưa Blueprint vào cùng nguyên liệu.
            // Kết quả của bước này là sinh ra các GameObject (Đầu, Thân, Tay) thực tế.
        }
    }
}
