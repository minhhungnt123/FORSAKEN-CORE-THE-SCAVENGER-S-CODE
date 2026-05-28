using UnityEngine;
using System.Collections;
using RoboticsProject.Interfaces;

namespace RoboticsProject.Controllers.Interactables
{
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door Setup")]
        [Tooltip("Kéo thả object 'Door' (cánh cửa thực sự chứa Handle) vào đây. Vì bạn đã set tâm (origin) chuẩn ở Blender nên nó sẽ quay đúng bản lề.")]
        [SerializeField] private Transform doorTransform;

        [Header("Animation Settings")]
        [Tooltip("Góc xoay khi mở cửa (nếu mở ngược thì đổi thành -90)")]
        [SerializeField] private float openAngle = -90f;
        
        [Tooltip("Tốc độ mở/đóng")]
        [SerializeField] private float rotationSpeed = 5f;

        private bool isOpen = false;
        private bool isAnimating = false;
        private Quaternion closedRotation;
        private Quaternion openRotation;

        private void Start()
        {
            // Nếu không kéo thả thì mặc định lấy Transform của object đang gắn script
            if (doorTransform == null)
            {
                doorTransform = transform;
            }

            // Lưu lại góc xoay ban đầu (Đóng)
            closedRotation = doorTransform.localRotation;
            
            // Tính toán góc xoay lúc Mở (Xoay quanh trục Z)
            openRotation = closedRotation * Quaternion.Euler(0, 0, openAngle);
        }

        public string GetInteractPrompt()
        {
            return isOpen ? "Đóng cửa [E]" : "Mở cửa [E]";
        }

        public void OnInteract()
        {
            // Nếu cửa đang trong quá trình xoay thì không cho bấm liên tục
            if (isAnimating) return; 

            isOpen = !isOpen;
            StartCoroutine(AnimateDoor(isOpen ? openRotation : closedRotation));
        }

        private IEnumerator AnimateDoor(Quaternion targetRotation)
        {
            isAnimating = true;
            
            // Xoay từ từ cho mượt (Interpolation)
            while (Quaternion.Angle(doorTransform.localRotation, targetRotation) > 0.1f)
            {
                doorTransform.localRotation = Quaternion.Lerp(doorTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
                yield return null;
            }
            
            // Đảm bảo góc xoay chính xác tuyệt đối ở khung hình cuối
            doorTransform.localRotation = targetRotation;
            isAnimating = false;
        }
    }
}
