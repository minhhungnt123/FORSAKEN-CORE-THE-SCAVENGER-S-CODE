using UnityEngine;
using RoboticsProject.Interfaces;

namespace RoboticsProject.Controllers.Interactables
{
    public class BedInteractable : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            return "Ngủ qua ngày";
        }

        public void OnInteract()
        {
            Debug.Log("[Bed] Đang chuyển cảnh... Ngủ qua ngày mới.");
            // TODO: Gọi GameManager/TimeManager để tua thời gian và reset thể lực.
        }
    }
}
