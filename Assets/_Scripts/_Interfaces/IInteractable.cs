using UnityEngine;

namespace RoboticsProject.Interfaces
{
    /// <summary>
    /// Interface chuẩn hóa mọi đối tượng có thể tương tác trong game.
    /// Áp dụng nguyên lý Dependency Inversion (SOLID).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Trả về chuỗi thông báo khi người chơi nhìn vào đối tượng (VD: "Nhấn E để ngủ").
        /// </summary>
        string GetInteractPrompt();

        /// <summary>
        /// Hành động thực tế xảy ra khi người chơi bấm nút tương tác.
        /// </summary>
        void OnInteract();
    }
}