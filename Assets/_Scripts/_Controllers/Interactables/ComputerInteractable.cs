using UnityEngine;
using RoboticsProject.Interfaces;

namespace RoboticsProject.Controllers.Interactables
{
    public class ComputerInteractable : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            return "Mở bảng thiết kế Robot";
        }

        public void OnInteract()
        {
            Debug.Log("[Computer] Mở UI thiết kế bảng mạch/vật lý Robot...");
            // TODO: Gọi UIManager để kích hoạt Canvas bảng thiết kế, đồng thời disable di chuyển của Player.
        }
    }
}
