using System;
using UnityEngine;
using RoboticsProject.Managers;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Transform mainCameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.15f;
    [SerializeField] private LayerMask groundMask;

    // Các biến nội bộ
    private CharacterController controller;
    private Vector3 velocity;
    private float rotationSmoothVelocity;
    private bool isGrounded;

    // --- CLEAN CODE: Dùng Action (Event) thay vì biến bool JustJumped ---
    // Giúp các script khác (như Animation, Audio, VFX) lắng nghe sự kiện nhảy mà không cần check mỗi frame
    public event Action OnJumped;

    // Properties cho phép get nhưng không cho phép set từ bên ngoài
    public bool IsGrounded => isGrounded;
    public Vector3 Velocity => velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // Tự động gán reference cho InputManager (Lỗi 4)
        if (inputManager == null)
        {
            inputManager = InputManager.Instance;
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<InputManager>();
            }
        }
    }

    private void Update()
    {
        // Chặn di chuyển khi bất kỳ menu/UI nào đang mở
        bool isInputBlocked = inputManager != null && inputManager.IsGameplayInputBlocked;

        // 1. Tính toán logic (truyền trạng thái chặn vào để giới hạn hành động)
        CalculateGravityAndJump(isInputBlocked);
        Vector3 finalMovement = CalculateMovement(isInputBlocked);

        // 2. Thực thi di chuyển (Chỉ gọi 1 lần duy nhất)
        controller.Move(finalMovement * Time.deltaTime);

        // 3. FIX LỖI CHECK GROUND: Cập nhật isGrounded SAU KHI Move() đã giải quyết xong va chạm
        UpdateGroundStatus();
    }

    private void UpdateGroundStatus()
    {
        // Nếu đang nhảy lên (vận tốc Y dương), tuyệt đối không thể chạm đất
        if (velocity.y > 0)
        {
            isGrounded = false;
            return;
        }

        // Kết hợp cả CheckSphere của ta VÀ isGrounded nội tại của CharacterController
        // Điều này đảm bảo độ chính xác tuyệt đối khi đứng trên các vật thể phức tạp hoặc bậc thang
        bool sphereCastGround = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        isGrounded = controller.isGrounded || sphereCastGround;

        // Reset gia tốc rơi nếu chạm đất
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Lực bám đất cố định
        }
    }

    private void CalculateGravityAndJump(bool disableJump)
    {
        // Xử lý Input Nhảy (chỉ cho phép nhảy khi túi đồ không mở)
        if (!disableJump && inputManager != null && inputManager.JumpTriggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            OnJumped?.Invoke(); // Phát sự kiện nhảy
        }

        // Xử lý gia tốc trọng trường (Rơi nhanh hơn khi qua đỉnh nhảy)
        if (velocity.y < 0 && !isGrounded)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private Vector3 CalculateMovement(bool disableMovement)
    {
        // Nếu túi đồ đang mở, tắt di chuyển ngang hoàn toàn (Lỗi 3)
        if (disableMovement || inputManager == null)
        {
            return Vector3.up * velocity.y;
        }

        Vector2 moveInput = inputManager.MoveInput;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float currentSpeed = inputManager.IsSprinting ? sprintSpeed : moveSpeed;
        Vector3 horizontalMove = Vector3.zero;

        if (inputManager.IsLockOn)
        {
            // Trạng thái Lock-On
            float targetAngle = mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * currentSpeed;
        }
        else if (direction.magnitude >= 0.1f)
        {
            // Trạng thái Free-Look
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            horizontalMove = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * currentSpeed;
        }

        // Trả về Vector tổng hợp (Ngang + Dọc) để hàm Update gọi Move()
        return horizontalMove + (Vector3.up * velocity.y);
    }
}