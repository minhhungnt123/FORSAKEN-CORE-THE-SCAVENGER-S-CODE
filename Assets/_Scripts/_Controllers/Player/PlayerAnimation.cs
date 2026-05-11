using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private PlayerMovement playerMovement;

    // --- CLEAN CODE: StringToHash ---
    // Sử dụng Hash thay vì truyền chuỗi (string) trực tiếp vào Animator. 
    // Điều này tăng hiệu suất lên đáng kể trong game 3D.
    private readonly int hashInputX = Animator.StringToHash("InputX");
    private readonly int hashInputY = Animator.StringToHash("InputY");
    private readonly int hashIsMoving = Animator.StringToHash("IsMoving");
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int hashJumpTrigger = Animator.StringToHash("JumpTrigger");

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện nhảy từ PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.OnJumped += HandleJumpAnimation;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi object tắt để tránh rò rỉ bộ nhớ (Memory Leak)
        if (playerMovement != null)
        {
            playerMovement.OnJumped -= HandleJumpAnimation;
        }
    }

    private void LateUpdate()
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
        bool isMoving = input.magnitude > 0.1f;

        // Multiplier để scale tốc độ animation khi chạy
        float speedMultiplier = inputManager.IsSprinting ? 2f : 1f;

        if (inputManager.IsLockOn)
        {
            targetX = input.x * speedMultiplier;
            targetY = input.y * speedMultiplier;
        }
        else if (isMoving)
        {
            targetX = 0f;
            targetY = 1f * speedMultiplier;
        }

        animator.SetFloat(hashInputX, targetX, 0.1f, Time.deltaTime);
        animator.SetFloat(hashInputY, targetY, 0.1f, Time.deltaTime);
        animator.SetBool(hashIsMoving, isMoving);
    }

    private void UpdateAirborne()
    {
        animator.SetBool(hashIsGrounded, playerMovement.IsGrounded);
        animator.SetFloat(hashVerticalVelocity, playerMovement.Velocity.y);
    }

    // Hàm này chỉ được gọi KHI VÀ CHỈ KHI sự kiện nhảy diễn ra
    private void HandleJumpAnimation()
    {
        animator.SetTrigger(hashJumpTrigger);
    }
}