using UnityEngine;

/// <summary>
/// RobotController – Script ALL-IN-ONE điều khiển robot:
///   1) Di chuyển bằng WASD / Arrow Keys
///   2) Animation procedural cho từng bộ phận bằng code thuần
///
/// CÁCH DÙNG (không cần kéo thả gì):
///   Gắn script này lên cùng GameObject với ChassisModule.
///   Script tự tìm các bộ phận từ socket sau 0.5s (sau khi lắp xong).
///   Hoặc nhấn nút [Tìm bộ phận] trong Inspector.
///
/// KHÔNG CẦN: Animator Controller, Animation Clip, hay bất kỳ script nào khác.
/// </summary>
public class RobotController : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════
    //  INSPECTOR – CÀI ĐẶT DI CHUYỂN
    // ════════════════════════════════════════════════════════════
    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Tốc độ đi bộ (m/s)")]
    public float walkSpeed    = 5f;
    [Tooltip("Tốc độ chạy khi giữ Shift (m/s)")]
    public float sprintSpeed  = 10f;
    [Tooltip("Tốc độ xoay mặt robot")]
    public float rotateSpeed  = 8f;
    [Tooltip("Tắt OFF để robot di chuyển ngay kể cả chưa lắp đủ bộ phận")]
    public bool requireFullAssembly = false;

    [Header("=== NHẢY & TRỌNG LỰC ===")]
    [Tooltip("Lực nhảy")]
    public float jumpForce = 6f;
    [Tooltip("Trọng lực tác dụng khi nhảy")]
    public float gravity = -18f;

    // ════════════════════════════════════════════════════════════
    //  INSPECTOR – TRANSFORM BỘ PHẬN (TỰ TÌM hoặc kéo thả)
    // ════════════════════════════════════════════════════════════
    [Header("=== BỘ PHẬN (để trống = tự tìm) ===")]
    public Transform bodyTransform;
    public Transform headTransform;
    public Transform armLeftTransform;
    public Transform armRightTransform;
    public Transform legTransform;

    // ════════════════════════════════════════════════════════════
    //  INSPECTOR – THAM SỐ ANIMATION
    // ════════════════════════════════════════════════════════════
    [Header("=== ANIMATION ===")]
    [Range(0f, 0.2f)] public float bodyBobAmount    = 0.05f;
    [Range(0f, 10f)]  public float bodyBobFrequency = 5f;
    [Range(0f, 10f)]  public float bodyTiltAngle    = 3f;
    [Range(0f, 10f)]  public float bodyLeanAngle    = 8f;
    [Tooltip("Góc vặn thân khi đi (trục Y)")]
    [Range(0f, 15f)]  public float bodyTwistAngle   = 5f;

    [Range(0f, 0.1f)] public float headBobAmount    = 0.03f;
    [Range(0f, 5f)]   public float headIdleSway     = 3f;

    [Range(0f, 60f)]  public float armSwingAngle    = 30f;

    [Range(0f, 0.2f)] public float legBobAmount     = 0.06f;
    [Range(0f, 10f)]  public float legTiltAngle      = 3f;

    [Range(1f, 20f)]  public float blendSpeed       = 8f;

    // ════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ════════════════════════════════════════════════════════════
    private ChassisModule chassis;

    // Base transforms (cached khi lắp xong)
    private Vector3    bodyBase, headBase, armLBase, armRBase, legBase;
    private Quaternion bodyBaseRot, headBaseRot, armLBaseRot, armRBaseRot, legBaseRot;
    private bool       basesCached = false;

    [Header("─ Advanced Procedural Settings ─")]
    public float elbowBendMax = 45f;
    public float legSwingAngle = 30f;
    public float kneeBendMax = 60f;
    [Tooltip("Hệ số nhân tốc độ vung tay chân (Giảm nếu robot đi quá nhanh/tăng động)")]
    public float animSpeedMultiplier = 0.5f;

    [Tooltip("Trục xoay của bắp tay/đùi (Thường là X hoặc Z)")]
    public Vector3 armSwingAxis = new Vector3(1, 0, 0);
    public Vector3 legSwingAxis = new Vector3(1, 0, 0);

    [Header("─ Sửa lỗi khớp vai (Pivot Fix) ─")]
    [Tooltip("Khoảng cách ép tay sát vào thân")]
    public float armInwardShift = 0.6f;
    [Tooltip("Tâm xoay ảo của vai (Kéo lên để vai không bị rơi ra)")]
    public Vector3 armPivotOffset = new Vector3(0f, 0.6f, 0f);
    
    [Tooltip("Trục gập của cùi chỏ/đầu gối (Thường là X hoặc Z)")]
    public Vector3 elbowBendAxis = new Vector3(1, 0, 0);
    public Vector3 kneeBendAxis = new Vector3(1, 0, 0);

    // --- Deep Bone Caches ---
    private Transform[] armLeftBones;  // [0]=Shoulder, [1]=Elbow
    private Transform[] armRightBones;
    private Quaternion[] armLeftBaseRots;
    private Quaternion[] armRightBaseRots;

    private Transform legPelvis;
    private Transform[] legLeftBones;  // [0]=Hip, [1]=Knee
    private Transform[] legRightBones;
    private Quaternion[] legLeftBaseRots;
    private Quaternion[] legRightBaseRots;

    // Animation state
    private float speedBlend   = 0f; // 0=idle, 1=walk, 2=run
    private float walkCycle    = 0f;
    private float idleCycle    = 0f;

    // Movement state (public để HUD đọc)
    [System.NonSerialized] public float  CurrentSpeed = 0f;
    [System.NonSerialized] public bool   IsMoving     = false;
    [System.NonSerialized] public bool   IsSprinting  = false;

    // Jump state (public để LegAnimator đọc)
    private bool isGrounded = true;
    private float verticalVelocity = 0f;
    private float groundY;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => verticalVelocity;

    // ════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════
    private void Awake()
    {
        chassis = GetComponent<ChassisModule>();
    }

    private void Start()
    {
        groundY = transform.position.y;
        // Thử tự tìm bộ phận sau 0.5s (chờ lắp xong)
        Invoke(nameof(TryAutoDiscoverParts), 0.5f);
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimCycles();
    }

    private void LateUpdate()
    {
        // Animation chạy sau movement để không bị override
        ApplyAnimations();
    }

    // ════════════════════════════════════════════════════════════
    //  MOVEMENT
    // ════════════════════════════════════════════════════════════
    private void HandleMovement()
    {
        // Kiểm tra điều kiện lắp ráp
        if (requireFullAssembly && chassis != null && !chassis.IsFullyAssembled())
        {
            CurrentSpeed = 0f;
            IsMoving     = false;
            IsSprinting  = false;
            return;
        }

        // Đọc input
        float turnInput = 0f, moveInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  turnInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turnInput =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  moveInput = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    moveInput =  1f;
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        bool isTurning = Mathf.Abs(turnInput) > 0.01f;
        bool isWalking = Mathf.Abs(moveInput) > 0.01f;
        bool hasInput = isTurning || isWalking;

        // Cập nhật tốc độ (acceleration)
        float targetSpeed = isWalking ? (sprint ? sprintSpeed : walkSpeed) : 0f;
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, walkSpeed * 5f * Time.deltaTime);

        // Chỉ kích hoạt hiệu ứng bước đi (IsMoving) khi thực sự có lệnh đi tới/lui
        IsMoving    = isWalking;
        IsSprinting = isWalking && sprint;

        // Xoay robot (Tank controls)
        if (isTurning)
        {
            transform.Rotate(0f, turnInput * rotateSpeed * 30f * Time.deltaTime, 0f);
        }

        // Di chuyển tới lui theo góc xoay (Move according to rotation)
        if (isWalking && CurrentSpeed > 0.01f)
        {
            // Di chuyển theo trục Z (forward) của chính robot
            Vector3 moveDir = transform.forward * Mathf.Sign(moveInput);
            transform.position += moveDir * CurrentSpeed * Time.deltaTime;
        }

        // Đọc input nhảy (Phím Cách / Space)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            groundY = transform.position.y; // Cập nhật cao độ mặt đất thực tế ngay khi nhảy
            verticalVelocity = jumpForce;
            isGrounded = false;

            // Đồng bộ trạng thái nhảy lên RobotAnimatorController
            var animCtrl = GetComponent<RobotAnimatorController>();
            if (animCtrl != null)
            {
                animCtrl.SetState(RobotAnimatorController.RobotState.Jumping);
            }
        }

        // Cập nhật trọng lực vật lý khi đang nhảy
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y += verticalVelocity * Time.deltaTime;

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                verticalVelocity = 0f;
                isGrounded = true;

                // Đồng bộ trả về trạng thái Idle khi chạm đất
                var animCtrl = GetComponent<RobotAnimatorController>();
                if (animCtrl != null)
                {
                    animCtrl.SetState(RobotAnimatorController.RobotState.Idle);
                    
                    // Kích hoạt nhún giảm chấn tiếp đất trên LegAnimator nếu có
                    var legAnim = GetComponentInChildren<LegAnimator>();
                    if (legAnim != null)
                    {
                        legAnim.TriggerLandingSquash();
                    }
                }
            }
            transform.position = pos;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  ANIMATION CYCLES
    // ════════════════════════════════════════════════════════════
    private void UpdateAnimCycles()
    {
        // Blend: 0=idle, 1=walk, 2=run
        float targetBlend = IsMoving ? (IsSprinting ? 2f : 1f) : 0f;
        speedBlend = Mathf.MoveTowards(speedBlend, targetBlend, blendSpeed * Time.deltaTime);

        // Walk cycle phase (tăng khi di chuyển)
        float freq = IsSprinting ? bodyBobFrequency * 1.6f : bodyBobFrequency;
        if (IsMoving)
            walkCycle += freq * Time.deltaTime * animSpeedMultiplier;
        else
            walkCycle = Mathf.MoveTowards(walkCycle, Mathf.Round(walkCycle), 3f * Time.deltaTime);

        idleCycle += Time.deltaTime;
    }

    // ════════════════════════════════════════════════════════════
    //  APPLY ANIMATIONS
    // ════════════════════════════════════════════════════════════
    private void ApplyAnimations()
    {
        if (!basesCached) return;

        float walkT = Mathf.Clamp01(speedBlend);        // 0→1 idle→walk
        float runT  = Mathf.Clamp01(speedBlend - 1f);   // 0→1 walk→run
        float dt    = Time.deltaTime * blendSpeed;

        AnimateBody(walkT, runT, dt);
        AnimateHead(walkT, runT, dt);
        AnimateArm(armLeftTransform,  armLBase, armLBaseRot, walkT, runT, dt, 0f);
        AnimateArm(armRightTransform, armRBase, armRBaseRot, walkT, runT, dt, Mathf.PI);
        AnimateLeg(walkT, runT, dt);
    }

    private void AnimateBody(float walkT, float runT, float dt)
    {
        if (bodyTransform == null) return;
        
        // CỰC KỲ QUAN TRỌNG: Không bao giờ animate localPosition/localRotation của Transform Gốc (Chassis).
        // Nếu làm vậy, nó sẽ đánh nhau với script di chuyển (HandleMovement) và gây ra lỗi giật cục (rubber-banding).
        if (bodyTransform == transform) return;

        float bobAmp = Mathf.Lerp(0f, bodyBobAmount, walkT);
               bobAmp = Mathf.Lerp(bobAmp, bodyBobAmount * 1.8f, runT);
        float bobY    = Mathf.Abs(Mathf.Sin(walkCycle * Mathf.PI)) * bobAmp;

        float tilt = Mathf.Lerp(0f, bodyTiltAngle, walkT);
               tilt = Mathf.Lerp(tilt, bodyTiltAngle * 1.8f, runT);
        float tiltZ  = Mathf.Sin(walkCycle * Mathf.PI) * tilt;

        float twist = Mathf.Lerp(0f, bodyTwistAngle, walkT);
               twist = Mathf.Lerp(twist, bodyTwistAngle * 1.5f, runT);
        float twistY = Mathf.Sin(walkCycle * Mathf.PI) * twist;

        float lean = runT * bodyLeanAngle;

        bodyTransform.localPosition = Vector3.Lerp(
            bodyTransform.localPosition, bodyBase + new Vector3(0f, bobY, 0f), dt);
        bodyTransform.localRotation = Quaternion.Slerp(
            bodyTransform.localRotation,
            bodyBaseRot * Quaternion.Euler(lean, twistY, tiltZ), dt);
    }

    private void AnimateHead(float walkT, float runT, float dt)
    {
        if (headTransform == null) return;

        float idleSway = Mathf.Sin(idleCycle * 0.7f) * headIdleSway * (1f - walkT);
        float bobAmp   = Mathf.Lerp(0f, headBobAmount, walkT);
               bobAmp  = Mathf.Lerp(bobAmp, headBobAmount * 1.5f, runT);
        float bobY = Mathf.Abs(Mathf.Sin(walkCycle * Mathf.PI)) * bobAmp;

        // Lấy input xoay để xoay đầu
        float turnInput = 0f;
        InputManager inputMgr = Object.FindFirstObjectByType<InputManager>();
        if (inputMgr != null) turnInput = inputMgr.MoveInput.x;
        else
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turnInput = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turnInput = 1f;
        }

        // Xoay đầu đón hướng (tối đa 45 độ)
        float headTurnAngle = turnInput * 45f;

        headTransform.localRotation = Quaternion.Slerp(
            headTransform.localRotation,
            headBaseRot * Quaternion.Euler(0f, idleSway + headTurnAngle, 0f), dt * 3f);
    }

    private void AnimateArm(Transform arm, Vector3 basePos, Quaternion baseRot,
                             float walkT, float runT, float dt, float phaseOffset)
    {
        if (arm == null) return;

        // Nếu người dùng đã gắn ArmAnimator (chuẩn hóa module), nhường quyền cho nó
        if (arm.GetComponentInChildren<ArmAnimator>() != null) return;

        float swing = Mathf.Lerp(0f, armSwingAngle, walkT);
               swing = Mathf.Lerp(swing, armSwingAngle * 1.6f, runT);

        // Nếu đi lùi thì không đung đưa tay
        bool isBackward = false;
        InputManager inputMgr = Object.FindFirstObjectByType<InputManager>();
        if (inputMgr != null) isBackward = inputMgr.MoveInput.y < -0.1f;
        else isBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (isBackward)
        {
            swing = 0f;
        }

        float swingX = Mathf.Sin(walkCycle * Mathf.PI + phaseOffset) * swing;

        // BEND ELBOW & SWING UPPER ARM (Dùng mảng xương con để giữ khối vai đứng yên)
        Transform[] bones = (arm == armLeftTransform) ? armLeftBones : armRightBones;
        Quaternion[] baseRots = (arm == armLeftTransform) ? armLeftBaseRots : armRightBaseRots;
        
        if (bones != null && bones.Length > 1)
        {
            // Xương [0] là Khối Vai (giữ nguyên không xoay để dính chặt vào thân)
            // Xương [1] là Bắp Tay (Upper Arm) -> Đây mới là phần vung như quả lắc
            if (bones[1] != null)
            {
                bones[1].localRotation = Quaternion.Slerp(
                    bones[1].localRotation, 
                    baseRots[1] * Quaternion.Euler(armSwingAxis * swingX), 
                    dt * 1.5f);
            }

            // Xương [2] là Cẳng Tay (Lower Arm) -> Cùi Chỏ (Elbow)
            if (bones.Length > 2 && bones[2] != null)
            {
                float elbowBend = Mathf.Abs(swingX) * 0.6f + (walkT * 5f);
                elbowBend = Mathf.Clamp(elbowBend, 0f, elbowBendMax);

                bones[2].localRotation = Quaternion.Slerp(
                    bones[2].localRotation, 
                    baseRots[2] * Quaternion.Euler(elbowBendAxis * elbowBend), 
                    dt * 1.5f);
            }
        }
        else
        {
            // Nếu không có xương con, đành phải xoay cả cụm (cách cũ)
            arm.localRotation = Quaternion.Slerp(arm.localRotation, baseRot * Quaternion.Euler(armSwingAxis * swingX), dt);
        }
    }

    private void AnimateLeg(float walkT, float runT, float dt)
    {
        if (legTransform == null) return;

        // Nếu phát hiện có LegAnimator xử lý chuyển động chuyên sâu cho chân,
        // nhường quyền điều khiển hoàn toàn để tránh đè xương gây xung đột hoạt ảnh.
        if (legTransform.GetComponentInChildren<LegAnimator>() != null) return;

        float bobAmp = Mathf.Lerp(0f, legBobAmount, walkT);
               bobAmp = Mathf.Lerp(bobAmp, legBobAmount * 1.8f, runT);
        float bobY = Mathf.Abs(Mathf.Sin(walkCycle * Mathf.PI)) * bobAmp;

        float tilt = Mathf.Lerp(0f, legTiltAngle, walkT);
        float tiltZ = Mathf.Sin(walkCycle * Mathf.PI) * tilt;

        // Idle breath
        float breath = Mathf.Sin(idleCycle) * 0.008f * (1f - walkT);

        // Rotate root leg socket
        legTransform.localRotation = Quaternion.Slerp(
            legTransform.localRotation, legBaseRot * Quaternion.Euler(0f, 0f, tiltZ), dt);

        // BEND HIPS AND KNEES (Nếu có mảng xương đùi - cẳng chân)
        // Xương [0] là Pelvis offset, [1] là Thigh (Đùi), [2] là Calf (Cẳng chân/Knee), [3] là Foot.
        if (legLeftBones != null && legLeftBones.Length > 2 && legRightBones != null && legRightBones.Length > 2)
        {
            // swingL/swingR là góc xoay của đùi. Dương = vung ra trước, Âm = đưa ra sau.
            float swingL = Mathf.Sin(walkCycle * Mathf.PI) * legSwingAngle * walkT;
            float swingR = Mathf.Sin(walkCycle * Mathf.PI + Mathf.PI) * legSwingAngle * walkT;

            // Đầu gối gập lại (Knee bend) khi cẳng chân được nhấc lên (di chuyển từ sau ra trước).
            float kneeL = Mathf.Max(0f, Mathf.Cos(walkCycle * Mathf.PI)) * kneeBendMax * walkT;
            float kneeR = Mathf.Max(0f, Mathf.Cos(walkCycle * Mathf.PI + Mathf.PI)) * kneeBendMax * walkT;

            // Đùi (Hip) - Quay xương đùi [1] thay vì xương xương chậu [0]
            legLeftBones[1].localRotation = Quaternion.Slerp(legLeftBones[1].localRotation, legLeftBaseRots[1] * Quaternion.Euler(legSwingAxis * swingL), dt * 1.5f);
            legRightBones[1].localRotation = Quaternion.Slerp(legRightBones[1].localRotation, legRightBaseRots[1] * Quaternion.Euler(legSwingAxis * swingR), dt * 1.5f);

            // Gối (Knee) - Quay xương cẳng chân [2] thay vì đùi [1]
            legLeftBones[2].localRotation = Quaternion.Slerp(legLeftBones[2].localRotation, legLeftBaseRots[2] * Quaternion.Euler(kneeBendAxis * kneeL), dt * 1.8f);
            legRightBones[2].localRotation = Quaternion.Slerp(legRightBones[2].localRotation, legRightBaseRots[2] * Quaternion.Euler(kneeBendAxis * kneeR), dt * 1.8f);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  AUTO-DISCOVER & CACHE
    // ════════════════════════════════════════════════════════════
    [ContextMenu("Tìm bộ phận (chạy lúc Play)")]
    public void TryAutoDiscoverParts()
    {
        if (chassis == null) chassis = GetComponent<ChassisModule>();
        if (chassis == null)
        {
            Debug.LogWarning("[RobotController] Không tìm thấy ChassisModule!");
            return;
        }

        // Body = Lấy child có tên BodyMesh để animate, tránh đè lên Transform gốc đang dùng để di chuyển
        if (bodyTransform == null) 
        {
            Transform bodyMesh = transform.Find("BodyMesh");
            bodyTransform = bodyMesh != null ? bodyMesh : transform;
        }

        // Head
        if (headTransform == null && chassis.headSocket != null)
            headTransform = GetModuleTransform(chassis.headSocket);

        // Arms
        if (armLeftTransform == null && chassis.armLeftSocket != null)
            armLeftTransform = GetModuleTransform(chassis.armLeftSocket);
        if (armRightTransform == null && chassis.armRightSocket != null)
            armRightTransform = GetModuleTransform(chassis.armRightSocket);

        // Leg
        if (legTransform == null && chassis.legSocket != null)
            legTransform = GetModuleTransform(chassis.legSocket);

        CacheBaseTransforms();

        Debug.Log("[RobotController] Tự tìm bộ phận xong:\n" +
                  "  Body: " + N(bodyTransform) + "\n" +
                  "  Head: " + N(headTransform) + "\n" +
                  "  ArmL: " + N(armLeftTransform) + "\n" +
                  "  ArmR: " + N(armRightTransform) + "\n" +
                  "  Leg:  " + N(legTransform));
    }

    // Helper: tên GameObject hoặc "Null"
    private static string N(Transform t) => t != null ? t.name : "Null";

    private Transform GetModuleTransform(Transform socket)
    {
        // QUAN TRỌNG: Phải trả về chính cái socket, vì socket đóng vai trò là "khớp" (joint).
        // Nếu trả về module bên trong, nó sẽ bị xoay sai tâm (vì pivot của module có thể nằm ở gốc tọa độ).
        return socket;
    }

    private Transform GetFirstChildStartingWith(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix)) return child;
        }
        return null;
    }

    private Transform[] ExtractBoneChain(Transform root)
    {
        System.Collections.Generic.List<Transform> chain = new System.Collections.Generic.List<Transform>();
        Transform current = root;
        while (current != null)
        {
            chain.Add(current);
            if (current.childCount > 0) current = current.GetChild(0);
            else break;
        }
        return chain.ToArray();
    }

    private Quaternion[] GetBaseRotations(Transform[] bones)
    {
        if (bones == null) return null;
        Quaternion[] rots = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++) rots[i] = bones[i].localRotation;
        return rots;
    }

    private void CacheBaseTransforms()
    {
        if (bodyTransform    != null) { bodyBase  = bodyTransform.localPosition;    bodyBaseRot  = bodyTransform.localRotation; }
        if (headTransform    != null) { headBase  = headTransform.localPosition;    headBaseRot  = headTransform.localRotation; }
        if (armLeftTransform != null) { armLBase  = armLeftTransform.localPosition; armLBaseRot  = armLeftTransform.localRotation; }
        if (armRightTransform!= null) { armRBase  = armRightTransform.localPosition;armRBaseRot  = armRightTransform.localRotation; }
        if (legTransform     != null) { legBase   = legTransform.localPosition;     legBaseRot   = legTransform.localRotation; }

        // --- Deep Bone Auto-Discovery ---
        if (armLeftTransform != null)
        {
            Transform armLModule = armLeftTransform.childCount > 0 ? armLeftTransform.GetChild(0) : null;
            if (armLModule != null)
            {
                Transform armature = GetFirstChildStartingWith(armLModule, "Armature");
                if (armature != null && armature.childCount > 0)
                {
                    armLeftBones = ExtractBoneChain(armature.GetChild(0));
                    armLeftBaseRots = GetBaseRotations(armLeftBones);
                }
            }
        }

        if (armRightTransform != null)
        {
            Transform armRModule = armRightTransform.childCount > 0 ? armRightTransform.GetChild(0) : null;
            if (armRModule != null)
            {
                Transform armature = GetFirstChildStartingWith(armRModule, "Armature");
                if (armature != null && armature.childCount > 0)
                {
                    armRightBones = ExtractBoneChain(armature.GetChild(0));
                    armRightBaseRots = GetBaseRotations(armRightBones);
                }
            }
        }

        if (legTransform != null)
        {
            Transform legModule = legTransform.childCount > 0 ? legTransform.GetChild(0) : null;
            if (legModule != null)
            {
                Transform armature = GetFirstChildStartingWith(legModule, "Armature");
                if (armature != null && armature.childCount > 0)
                {
                    legPelvis = armature.GetChild(0);
                    if (legPelvis.childCount >= 2)
                    {
                        Transform child0 = legPelvis.GetChild(0);
                        Transform child1 = legPelvis.GetChild(1);
                        
                        // Detect left/right by name (_R or _R. or _L)
                        if (child0.name.Contains("_R") && !child0.name.Contains("_L"))
                        {
                            legRightBones = ExtractBoneChain(child0);
                            legLeftBones = ExtractBoneChain(child1);
                        }
                        else
                        {
                            legLeftBones = ExtractBoneChain(child0);
                            legRightBones = ExtractBoneChain(child1);
                        }
                        
                        legLeftBaseRots = GetBaseRotations(legLeftBones);
                        legRightBaseRots = GetBaseRotations(legRightBones);
                    }
                }
            }
        }

        basesCached = true;
        Debug.Log("[RobotController] Base transforms đã được cache. Animation sẵn sàng!");
    }

    // Gọi từ ChassisModule sau khi lắp xong
    public void NotifyFullyAssembled()
    {
        // Đợi 1 frame để module connect() hoàn tất
        Invoke(nameof(TryAutoDiscoverParts), 0.1f);
    }

    // ════════════════════════════════════════════════════════════
    //  ON-SCREEN HUD DEBUG
    // ════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.UpperLeft };
        style.normal.textColor = Color.white;

        string cached = basesCached ? "✅" : "❌ (chưa cache - chờ lắp xong)";
        string info =
            "🤖 ROBOT CONTROLLER\n" +
            "──────────────────────\n" +
            $"Speed  : {CurrentSpeed:F2} m/s\n" +
            $"Blend  : {speedBlend:F2} (0=idle 2=run)\n" +
            $"Moving : {IsMoving}\n" +
            $"Sprint : {IsSprinting}\n" +
            $"Cached : {cached}\n" +
            "──────────────────────\n" +
            "WASD / Arrow = Di chuyển\n" +
            "Shift + WASD = Chạy nhanh\n";

        GUI.Box(new Rect(10, 10, 260, 185), info, style);
    }
}
