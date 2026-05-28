using UnityEngine;
using System.Collections;

/// <summary>
/// RobotAnimatorController: Điều phối toàn bộ animation của robot lắp ráp.
///
/// Kiến trúc phân tách trách nhiệm:
///   - Script này đọc INPUT (InputManager, PlayerMovement) và quyết định trạng thái.
///   - LegAnimator KHÔNG tự quyết định trạng thái — chỉ thực thi lệnh từ đây.
///   - HeadAnimator / ArmAnimator vẫn dùng RobotState chung như cũ.
///
/// Luồng xử lý:
///   InputManager → DetectMovementState() → SetLegState() → LegAnimator.PlayXxx()
/// </summary>
[RequireComponent(typeof(ChassisModule))]
public class RobotAnimatorController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────
    [Header("Module Animator References")]
    [Tooltip("Animator script gắn lên GameObject của đầu robot")]
    [SerializeField] private HeadAnimator headAnimator;

    [Tooltip("Animator script gắn lên GameObject tay trái robot")]
    [SerializeField] private ArmAnimator armLeftAnimator;

    [Tooltip("Animator script gắn lên GameObject tay phải robot")]
    [SerializeField] private ArmAnimator armRightAnimator;

    [Tooltip("Animator script gắn lên GameObject chân robot")]
    [SerializeField] private LegAnimator legAnimator;

    [Header("Movement Settings")]
    [Tooltip("Ngưỡng input trục Y để kích hoạt Walk/Run")]
    [SerializeField] private float inputDeadzone = 0.15f;

    [Header("Torso Twist Settings (Vặn Mình)")]
    [Tooltip("Góc vặn thân khi đi bộ (theo trục Y)")]
    [SerializeField] private float walkTwistAngle = 5f;
    [Tooltip("Góc vặn thân khi chạy")]
    [SerializeField] private float runTwistAngle = 10f;
    [Tooltip("Tốc độ vặn mình (nên khớp với walkFrequency của LegAnimator)")]
    [SerializeField] private float twistFrequency = 12f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private ChassisModule     chassis;
    private PlayerMovement    playerMovement;
    private InputManager      inputManager;

    private Quaternion baseLocalRotation;
    private float twistPhase = 0f;
    private float twistBlendWeight = 0f;

    // Cờ nhảy: bật lên ngay khi PlayerMovement.OnJumped phát ra
    // → Không phụ thuộc vào polling IsGrounded bị trễ frame
    private bool isAirborne = false;

    // Trạng thái tổng quát của robot (dùng cho Head, Arm, v.v.)
    private RobotState currentState = RobotState.Idle;

    public enum RobotState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Attacking,
        Assembling,
        Assembled
    }

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        chassis = GetComponent<ChassisModule>();
        baseLocalRotation = transform.localRotation;
    }

    private void Start()
    {
        // Tìm kiếm PlayerMovement và InputManager trong cấu trúc nhân vật
        playerMovement = GetComponentInParent<PlayerMovement>();
        inputManager   = GetComponentInParent<InputManager>();

        // Fallback: tìm ở bất kỳ đâu trong scene nếu không nằm trong cha
        if (inputManager == null)
            inputManager = Object.FindFirstObjectByType<InputManager>();

        // Lắng nghe event nhảy trực tiếp từ PlayerMovement
        // Event này phát tức thì khi người chơi nhảy, không bị trễ frame như IsGrounded polling
        if (playerMovement != null)
            playerMovement.OnJumped += HandleJumped;

        SetState(RobotState.Idle);
    }

    private void OnDestroy()
    {
        // Hủy đăng ký event để tránh memory leak
        if (playerMovement != null)
            playerMovement.OnJumped -= HandleJumped;
    }

    /// <summary>
    /// Gọi ngay lập tức khi người chơi nhảy (qua event, không polling).
    /// Bật cờ isAirborne để giữ trạng thái Jumping cho đến khi chạm đất.
    /// </summary>
    private void HandleJumped()
    {
        isAirborne = true;
        SetState(RobotState.Jumping);
        SetLegState(LegAnimator.LegAnimState.Jumping);
    }

    private void Update()
    {
        DetectMovementState();
        UpdateTorsoTwist();
    }

    // ─────────────────────────────────────────────
    //  PHÁT HIỆN TRẠNG THÁI CHUYỂN ĐỘNG & VẶN MÌNH
    // ─────────────────────────────────────────────

    private void UpdateTorsoTwist()
    {
        if (currentState == RobotState.Attacking || 
            currentState == RobotState.Assembling || 
            currentState == RobotState.Assembled)
            return;

        float targetAngle = 0f;
        float targetBlend = 0f;
        float currentFreq = twistFrequency;

        if (currentState == RobotState.Walking)
        {
            targetBlend = 1f;
            targetAngle = walkTwistAngle;
        }
        else if (currentState == RobotState.Running)
        {
            targetBlend = 1f;
            targetAngle = runTwistAngle;
            // Tăng tốc độ vặn mình khi chạy để khớp với LegAnimator (runFrequency thường x1.3 hoặc x1.5)
            currentFreq = twistFrequency * 1.3f;
        }

        twistBlendWeight = Mathf.MoveTowards(twistBlendWeight, targetBlend, Time.deltaTime * 5f);

        if (twistBlendWeight > 0.01f)
            twistPhase += currentFreq * Time.deltaTime;
        else
            twistPhase = Mathf.Lerp(twistPhase, 0f, Time.deltaTime * 10f);

        // Dùng sóng Sine để tạo chuyển động vặn mình
        float currentRotAngle = Mathf.Sin(twistPhase) * targetAngle * twistBlendWeight;
        
        // Cập nhật localRotation của thân robot (xoay quanh trục Y)
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, currentRotAngle, 0f);
    }

    /// <summary>
    /// Đọc input thực tế và trạng thái vật lý để quyết định
    /// trạng thái animator chân cần kích hoạt.
    /// </summary>
    private void DetectMovementState()
    {
        // Bỏ qua khi đang trong hành động đặc biệt (lắp ráp, tấn công)
        if (currentState == RobotState.Attacking  ||
            currentState == RobotState.Assembling ||
            currentState == RobotState.Assembled)
            return;

        // ──────────────────────────────────────────
        // CẬP NHẬT THAM CHIẾU (nếu chưa tìm được)
        // ──────────────────────────────────────────
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        if (inputManager == null)
            inputManager = Object.FindFirstObjectByType<InputManager>();

        // ──────────────────────────────────────────
        // ƯU TIÊN 1: XỬ LÝ TRẠNG THÁI NHẢY / TIẾP ĐẤT
        // Dùng cờ isAirborne (bật qua event OnJumped) + xác nhận IsGrounded
        // để tránh lỗi trễ frame khi polling IsGrounded trực tiếp.
        // ──────────────────────────────────────────
        if (isAirborne)
        {
            if (playerMovement != null && playerMovement.IsGrounded)
            {
                // Đã chạm đất → tắt cờ, kích hoạt nhún giảm chấn
                isAirborne = false;
                legAnimator?.TriggerLanding();
                SetState(RobotState.Idle);
                return;
            }

            // Vẫn đang trên không → giữ nguyên tư thế co chân
            SetState(RobotState.Jumping);
            SetLegState(LegAnimator.LegAnimState.Jumping);
            return;
        }

        // Fallback cho môi trường không có PlayerMovement (ví dụ scene lắp ráp):
        // Phát hiện trạng thái trên không qua IsGrounded
        if (playerMovement != null && !playerMovement.IsGrounded)
        {
            isAirborne = true;
            SetState(RobotState.Jumping);
            SetLegState(LegAnimator.LegAnimState.Jumping);
            return;
        }

        // ──────────────────────────────────────────
        // ƯU TIÊN 2: XÁC ĐỊNH TỪ INPUT (MoveInput)
        // ──────────────────────────────────────────
        if (inputManager != null)
        {
            Vector2 moveInput  = inputManager.MoveInput;
            bool    isSprint   = inputManager.IsSprinting;
            float   inputY     = moveInput.y;   // + = tiến, - = lùi
            float   inputX     = moveInput.x;   // strafe (ngang)
            bool    hasInput   = moveInput.magnitude > inputDeadzone;

            // Đi lùi (ưu tiên kiểm tra trước tiến để tránh nhập nhằng)
            if (inputY < -inputDeadzone)
            {
                SetState(RobotState.Walking);
                SetLegState(LegAnimator.LegAnimState.WalkBackward);
                return;
            }

            // Chạy tiến (W + Shift)
            if (inputY > inputDeadzone && isSprint)
            {
                SetState(RobotState.Running);
                SetLegState(LegAnimator.LegAnimState.RunForward);
                return;
            }

            // Đi tiến (W hoặc Stafe ngang)
            if (hasInput)
            {
                SetState(RobotState.Walking);
                SetLegState(LegAnimator.LegAnimState.WalkForward);
                return;
            }

            // Đứng yên
            SetState(RobotState.Idle);
            SetLegState(LegAnimator.LegAnimState.Idle);
            return;
        }

        // ──────────────────────────────────────────
        // FALLBACK: Phát hiện từ vận tốc vị trí
        // (Dùng khi không có InputManager / PlayerMovement)
        // ──────────────────────────────────────────
        SetState(RobotState.Idle);
        SetLegState(LegAnimator.LegAnimState.Idle);
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    /// <summary>Gọi SetLegState trên LegAnimator nếu tồn tại.</summary>
    private void SetLegState(LegAnimator.LegAnimState legState)
    {
        legAnimator?.SetLegState(legState);
    }

    // ─────────────────────────────────────────────
    //  STATE MACHINE (cho Head / Arm / v.v.)
    // ─────────────────────────────────────────────
    public void SetState(RobotState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        BroadcastState(newState);
    }

    private void BroadcastState(RobotState state)
    {
        headAnimator?.OnRobotStateChanged(state);
        armLeftAnimator?.OnRobotStateChanged(state);
        armRightAnimator?.OnRobotStateChanged(state);
        legAnimator?.OnRobotStateChanged(state);
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API – GỌI TỪ BÊN NGOÀI (UI, GameEvent)
    // ─────────────────────────────────────────────

    /// <summary>Kích hoạt animation tấn công.</summary>
    public void TriggerAttack()
    {
        SetState(RobotState.Attacking);
        armLeftAnimator?.TriggerAttack();
        armRightAnimator?.TriggerAttack();
        headAnimator?.TriggerAttack();
        StartCoroutine(ReturnToIdleAfter(0.8f));
    }

    /// <summary>Phát animation lắp bộ phận mới.</summary>
    public void TriggerAssembleEffect(string partName)
    {
        SetState(RobotState.Assembling);
        headAnimator?.TriggerAssemble();
        StartCoroutine(FinishAssembleAfter(partName, 0.5f));
    }

    /// <summary>Phát animation hoàn thành lắp ráp toàn bộ robot.</summary>
    public void TriggerFullAssembledCelebration()
    {
        SetState(RobotState.Assembled);
        headAnimator?.TriggerCelebrate();
        armLeftAnimator?.TriggerCelebrate();
        armRightAnimator?.TriggerCelebrate();
        legAnimator?.TriggerCelebrate();
        StartCoroutine(ReturnToIdleAfter(2.0f));
    }

    // ─────────────────────────────────────────────
    //  AUTO-DISCOVER MODULE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gọi khi một module được lắp vào chassis để tự động liên kết animator.
    /// </summary>
    public void OnModuleAttached(RobotModule module)
    {
        switch (module)
        {
            case HeadModule _:
                headAnimator = module.GetComponentInChildren<HeadAnimator>();
                if (headAnimator != null)
                {
                    headAnimator.Initialize(this);
                    Debug.Log("[RobotAnimatorController] Đã liên kết HeadAnimator.");
                }
                break;

            case ArmLeftModule _:
                armLeftAnimator = module.GetComponentInChildren<ArmAnimator>();
                if (armLeftAnimator != null)
                {
                    armLeftAnimator.Initialize(this, ArmAnimator.ArmSide.Left);
                    Debug.Log("[RobotAnimatorController] Đã liên kết ArmAnimator (Trái).");
                }
                break;

            case ArmRightModule _:
                armRightAnimator = module.GetComponentInChildren<ArmAnimator>();
                if (armRightAnimator != null)
                {
                    armRightAnimator.Initialize(this, ArmAnimator.ArmSide.Right);
                    Debug.Log("[RobotAnimatorController] Đã liên kết ArmAnimator (Phải).");
                }
                break;

            case LegModule _:
                legAnimator = module.GetComponentInChildren<LegAnimator>();
                if (legAnimator != null)
                {
                    legAnimator.Initialize(this);
                    Debug.Log("[RobotAnimatorController] Đã liên kết LegAnimator.");
                }
                break;
        }

        TriggerAssembleEffect(module.moduleName);
    }

    /// <summary>Gọi khi một module bị tháo ra.</summary>
    public void OnModuleDetached(RobotModule module)
    {
        switch (module)
        {
            case HeadModule _:      headAnimator      = null; break;
            case ArmLeftModule _:   armLeftAnimator   = null; break;
            case ArmRightModule _:  armRightAnimator  = null; break;
            case LegModule _:       legAnimator       = null; break;
        }
    }

    // ─────────────────────────────────────────────
    //  GETTERS
    // ─────────────────────────────────────────────
    public RobotState CurrentState => currentState;

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────
    private IEnumerator ReturnToIdleAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetState(RobotState.Idle);
    }

    private IEnumerator FinishAssembleAfter(string partName, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[RobotAnimatorController] Lắp {partName} hoàn tất!");
        SetState(RobotState.Idle);
    }
}
