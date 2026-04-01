using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public InputManager inputManager;
    public PlayerMovement playerMovement;

    void LateUpdate()
    {
        if (animator == null || inputManager == null || playerMovement == null) return;

        UpdateLocomotion();
        UpdateAirborne();
    }

    private void UpdateLocomotion()
    {
        Vector2 input = inputManager.MoveInput;
        float targetX = 0f;
        float targetY = 0f;

        if (inputManager.IsLockOn)
        {
            // --- TRONG CHẾ ĐỘ LOCK-ON ---
            // Chuyển thẳng trục tọa độ phím bấm vào Blend Tree để kích hoạt đi ngang, đi lùi
            targetX = input.x;
            targetY = input.y;
            if(inputManager.IsSprinting)
            {
                // Nếu đang sprint, ép trục Y = 1 để phát animation chạy tới (bất kể input Y là gì)
                targetX *= 2f;
                targetY *= 2f;
            }
        }
        else
        {
            // --- TRONG CHẾ ĐỘ FREE-LOOK ---
            // Bỏ qua trục X (không bước ngang). 
            // Chỉ cần có di chuyển (magnitude > 0), ép trục Y = 1 để phát animation chạy tới.
            targetX = 0f;
            if(input.magnitude > 0.1f)
            {
                targetY = 1f;
                if(inputManager.IsSprinting)
                {
                    targetY = 2f; // Nếu đang sprint, ép trục Y = 2 để phát animation chạy nhanh
                }
            }
        }

        // Đưa thông số vào Animator với damping (0.1f) để mượt mà
        animator.SetFloat("InputX", targetX, 0.1f, Time.deltaTime);
        animator.SetFloat("InputY", targetY, 0.1f, Time.deltaTime);
        animator.SetBool("IsMoving", input.magnitude > 0.1f);
    }

    private void UpdateAirborne()
    {
        // Lấy dữ liệu vật lý từ script Movement để set điều kiện Rơi/Nhảy
        animator.SetBool("IsGrounded", playerMovement.IsGrounded);
        animator.SetFloat("VerticalVelocity", playerMovement.Velocity.y);
        if (playerMovement.JustJumped)
        {
            animator.SetTrigger("JumpTrigger");
        }
    }
}