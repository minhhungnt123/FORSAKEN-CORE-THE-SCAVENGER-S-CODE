using UnityEngine;
using RoboticsProject.Interfaces;

namespace RoboticsProject.Controllers.Interactables
{
    public class ChairInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Vị trí để player ngồi vào")]
        [SerializeField] private Transform sitPoint;

        public string GetInteractPrompt()
        {
            return "Ngồi xuống";
        }

        public void OnInteract()
        {
            Debug.Log("[Chair] Bắt đầu animation ngồi...");
            // TODO: Khóa di chuyển của Player, di chuyển Player đến tọa độ sitPoint và bật Animation "Sit".
        }
    }
}
