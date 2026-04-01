using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public InputManager inputManager;
    public Transform mainCameraTransform;

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 12f;
    public float rotationSmoothTime = 0.1f;
    private float rotationSmoothVelocity;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -15f;
    public float fallMultiplier = 2.5f; // Gia tốc rơi

    [Header("Ground Detection")]
    public Transform groundCheck;       // Vị trí đặt quả cầu quét (Kéo object GroundCheck vào đây)
    public float groundDistance = 0.15f; // Bán kính quả cầu (0.4 là vừa vặn lòng bàn chân)
    public LayerMask groundMask;        // Xác định Layer nào là mặt đất

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // Các Properties public để PlayerAnimation lấy dữ liệu
    public bool IsGrounded => isGrounded;
    public Vector3 Velocity => velocity;
    public bool JustJumped { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (mainCameraTransform == null && Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        HandleGravityAndJump();
        HandleMovement();
    }

    private void HandleGravityAndJump()
    {
        // SỬ DỤNG CÔNG NGHỆ QUÉT QUẢ CẦU THAY VÌ MẶC ĐỊNH
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Lực ép -2f là hoàn toàn an toàn và đủ dùng với SphereCast
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        JustJumped = false;

        if (inputManager.JumpTriggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            JustJumped = true;
        }

        if (velocity.y < 0 && !isGrounded)
        {
            // Khi đã qua đỉnh cú nhảy và bắt đầu rơi: Tăng gia tốc trọng trường lên (Mario/Genshin style)
            // Cảm giác rơi sẽ rất nặng, đầm và chân thực, không bị lơ lửng như mặt trăng nữa.
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            // Khi đang bật lên không trung: Trọng lực bình thường
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = inputManager.MoveInput;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        float currentSpeed = inputManager.IsSprinting ? sprintSpeed : moveSpeed;
        Vector3 horizontalMove = Vector3.zero;

        if (inputManager.IsLockOn)
        {
            // --- TRẠNG THÁI LOCK-ON ---
            float targetAngle = mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * currentSpeed;
        }
        else
        {
            // --- TRẠNG THÁI FREE-LOOK ---
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                horizontalMove = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * currentSpeed;
            }
        }

        // GỘP CHUNG BƯỚC DI CHUYỂN NGANG VÀ RƠI DỌC VÀO 1 LỆNH DUY NHẤT
        Vector3 finalMovement = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMovement * Time.deltaTime);
    }
}