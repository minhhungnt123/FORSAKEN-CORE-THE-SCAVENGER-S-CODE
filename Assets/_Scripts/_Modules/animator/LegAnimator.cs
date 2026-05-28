using UnityEngine;
using System.Collections;

/// <summary>
/// LegAnimator: Bộ hoạt ảnh chuyên biệt cho chân (Modular Leg Animator).
///
/// Kiến trúc phân tách trạng thái:
///   - Mỗi trạng thái chuyển động (đi tiến, đi lùi, chạy, nhảy, tiếp đất...)
///     có một hàm Play riêng biệt, độc lập.
///   - RobotAnimatorController đọc input và gọi SetLegState() để chọn đúng
///     trạng thái cần kích hoạt. LegAnimator không tự quyết định trạng thái.
///
/// Các trạng thái:
///   Idle          – Đứng yên, hai chân thả thẳng
///   WalkForward   – Đi tiến (8-pose walk cycle)
///   WalkBackward  – Đi lùi (walk cycle phase đảo chiều)
///   RunForward    – Chạy tiến (tần số cao, biên độ rộng)
///   Jumping       – Trên không, co chân lên (air tuck)
///   Landing       – Vừa chạm đất, nhún giảm chấn rồi tự chuyển về Idle
/// </summary>
public class LegAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  TRẠNG THÁI CHÂN (Enum)
    // ─────────────────────────────────────────────

    /// <summary>Tập hợp trạng thái chuyển động của chân robot.</summary>
    public enum LegAnimState
    {
        Idle,
        WalkForward,
        WalkBackward,
        RunForward,
        Jumping,
        Landing
    }

    // ─────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────
    [Header("Tần số & Tốc độ")]
    [Tooltip("Tần số bước chân khi đi bộ (số bước / giây) – chỉ dùng khi không có PlayerMovement")]
    public float walkFrequency = 3.5f;

    [Tooltip("Tần số bước chân khi chạy nhanh – chỉ dùng khi không có PlayerMovement")]
    public float runFrequency = 6.0f;

    [Tooltip("Tốc độ chuyển về tư thế đứng khi dừng")]
    public float returnToStandSpeed = 5.0f;

    [Tooltip("Tốc độ blend mượt mà giữa các trạng thái")]
    public float blendSpeed = 8.0f;

    [Tooltip("Chiều dài mỗi bước (mét) – điều chỉnh để hoạt ảnh khớp với di chuyển")]
    public float strideLength = 1.4f;

    [Header("Đi lùi")]
    [Tooltip("Biên độ giơ chân khi đi lùi (0.5 = giơ chân ít hơn 50%)")]
    [Range(0.1f, 1f)]
    public float backwardAmplitudeScale = 0.5f;

    [Tooltip("Tốc độ bước khi đi lùi so với tiến (0.6 = chậm hơn 40%)")]
    [Range(0.1f, 1f)]
    public float backwardSpeedScale = 0.6f;

    [Header("Biên độ")]
    [Tooltip("Hệ số nhân biên độ vung chân khi chạy nhanh")]
    public float sprintAmplitudeScale = 1.4f;

    [Tooltip("Hệ số giảm đá chân về phía sau (0 = không đá sau, 1 = bình thường)")]
    [Range(0f, 1f)]
    public float backKickScale = 0.2f;


    [Header("Trục xoay khớp")]
    [Tooltip("Trục vung của hông/đùi (thường là X)")]
    public Vector3 legSwingAxis = new Vector3(1, 0, 0);

    [Tooltip("Trục gập của đầu gối (thường là X)")]
    public Vector3 kneeBendAxis = new Vector3(1, 0, 0);

    [Tooltip("Trục gập cổ chân (thường là X)")]
    public Vector3 ankleAxis = new Vector3(1, 0, 0);

    [Header("Hiệu chỉnh hướng trục")]
    [Tooltip("Đảo chiều vung hông (nếu chân vung ngược ra sau thay vì giơ trước)")]
    public bool invertHipSwing = true;

    [Tooltip("Đảo chiều gập đầu gối")]
    public bool invertKneeBend = false;

    [Tooltip("Đảo chiều gập cổ chân")]
    public bool invertAnkleFlex = false;

    // ─────────────────────────────────────────────
    //  DỮ LIỆU 8 TƯ THẾ ĐI BỘ
    // ─────────────────────────────────────────────
    private struct LegPose
    {
        public float hipAngle;
        public float kneeAngle;
        public float ankleAngle;
    }

    // 8 tư thế chuẩn theo chu kỳ đi bộ khoa học
    private readonly LegPose[] walkPoses = new LegPose[8]
    {
        new LegPose { hipAngle =  0f,  kneeAngle =   0f, ankleAngle =  0f },  // 1: Chuẩn bị
        new LegPose { hipAngle = -5f,  kneeAngle = -18f, ankleAngle = -4f },  // 2: Nhấc chân  (âm = đá trước với invertHip=true)
        new LegPose { hipAngle = -9f,  kneeAngle = -12f, ankleAngle = -5f },  // 3: Co gối     (đỉnh về trước)
        new LegPose { hipAngle =-12f,  kneeAngle =  -8f, ankleAngle =  0f },  // 4: Đưa trước  (max về trước)
        new LegPose { hipAngle = -8f,  kneeAngle =  -2f, ankleAngle =  0f },  // 5: Duỗi gối
        new LegPose { hipAngle = -4f,  kneeAngle =   0f, ankleAngle =  5f },  // 6: Gót chạm đất
        new LegPose { hipAngle =  2f,  kneeAngle =  -3f, ankleAngle =  2f },  // 7: Dồn lực    (dương = đẩy sau nhẹ)
        new LegPose { hipAngle =  3f,  kneeAngle =  -1f, ankleAngle =  0f },  // 8: Đạp mũi    (dương = đẩy sau, giới hạn bởi backKickScale)
    };







    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private RobotAnimatorController animatorController;

    // Bone components
    private Transform   legPelvis;
    private Transform[] legLeftBones;   // [0]=Root, [1]=Đùi, [2]=Cẳng chân, [3]=Bàn chân
    private Transform[] legRightBones;

    // Base rotations cached
    private Quaternion[] legLeftBaseRots;
    private Quaternion[] legRightBaseRots;

    private bool  basesCached  = false;
    private float walkCycle    = 0f;
    private float speedBlend   = 0f;

    private LegAnimState currentLegState = LegAnimState.Idle;
    private bool isPlayingSpecialRoutine = false;
    private bool isAssembling   = false;
    private bool isCelebrating  = false;

    // Tham chiếu để đọc tốc độ thực
    private PlayerMovement playerMovement;
    private RobotController robotController;

    // ─────────────────────────────────────────────
    //  KHỞI TẠO
    // ─────────────────────────────────────────────
    public void Initialize(RobotAnimatorController ctrl)
    {
        animatorController = ctrl;
        CacheBaseTransforms();
    }

    /// <summary>Gọi từ RobotAnimatorController để đặt trạng thái animator chân.</summary>
    public void SetLegState(LegAnimState newState)
    {
        // Không ngắt coroutine Landing đang chạy bằng cách ép về trạng thái khác
        if (isPlayingSpecialRoutine && newState != LegAnimState.Landing && newState != LegAnimState.Jumping)
            return;

        currentLegState = newState;
    }

    /// <summary>Gọi khi trạng thái robot thay đổi (giữ tương thích ngược với hệ thống module).</summary>
    public void OnRobotStateChanged(RobotAnimatorController.RobotState newState)
    {
        // Mapping từ RobotState sang LegAnimState cho các trạng thái đặc biệt
        switch (newState)
        {
            case RobotAnimatorController.RobotState.Assembling:
                isAssembling = true;
                TriggerAssemble();
                break;
            case RobotAnimatorController.RobotState.Assembled:
                isCelebrating = false;
                break;
            case RobotAnimatorController.RobotState.Idle:
                isAssembling  = false;
                isCelebrating = false;
                break;
        }
    }

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = Object.FindFirstObjectByType<PlayerMovement>();

        robotController = GetComponentInParent<RobotController>();
        if (robotController == null)
            robotController = Object.FindFirstObjectByType<RobotController>();

        if (animatorController == null)
        {
            animatorController = GetComponentInParent<RobotAnimatorController>();
            if (animatorController != null && !basesCached)
                CacheBaseTransforms();
        }

        if (!basesCached) CacheBaseTransforms();
    }

    // Tính tần số hoạt ảnh dựa trên tốc độ thực để hoạt ảnh khớp bước chân
    // freq (steps/s) = actualSpeed (m/s) / strideLength (m/step) * 8 (phases/step)
    private float GetSyncedFrequency(float fallbackFreq)
    {
        if (strideLength <= 0f) return fallbackFreq;
        
        float speed = 0f;
        if (robotController != null) speed = robotController.CurrentSpeed;
        else if (playerMovement != null) speed = playerMovement.Velocity.magnitude;

        if (speed < 0.1f) return fallbackFreq;
        return (speed / strideLength) * 8f;
    }

    // ─────────────────────────────────────────────
    //  ANIMATION UPDATE LOOP
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        if (!basesCached) return;
        if (isAssembling || isCelebrating) return;

        // Đọc trạng thái từ RobotController nếu có
        if (robotController != null && !isPlayingSpecialRoutine)
        {
            if (robotController.IsMoving)
            {
                // Kiểm tra đi lùi
                bool isBackward = false;
                InputManager inputMgr = Object.FindFirstObjectByType<InputManager>();
                if (inputMgr != null) isBackward = inputMgr.MoveInput.y < -0.1f;
                else isBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

                if (isBackward) currentLegState = LegAnimState.WalkBackward;
                else if (robotController.IsSprinting) currentLegState = LegAnimState.RunForward;
                else currentLegState = LegAnimState.WalkForward;
            }
            else
            {
                currentLegState = LegAnimState.Idle;
            }
        }

        // Dispatch đúng hàm animator theo trạng thái
        switch (currentLegState)
        {
            case LegAnimState.Idle:
                PlayIdle();
                break;
            case LegAnimState.WalkForward:
                PlayWalkForward();
                break;
            case LegAnimState.WalkBackward:
                PlayWalkBackward();
                break;
            case LegAnimState.RunForward:
                PlayRunForward();
                break;
            case LegAnimState.Jumping:
                PlayJumping();
                break;
            case LegAnimState.Landing:
                // Landing chạy qua coroutine, LateUpdate không can thiệp
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  6 HÀM ANIMATOR RIÊNG BIỆT THEO TRẠNG THÁI
    // ─────────────────────────────────────────────

    /// <summary>
    /// IDLE – Đứng yên.
    /// Trả hai chân về tư thế đứng thẳng (phase 0) một cách mượt mà.
    /// </summary>
    private void PlayIdle()
    {
        // Đưa walkCycle về 0 mượt mà
        walkCycle = Mathf.MoveTowards(walkCycle, 0f, returnToStandSpeed * Time.deltaTime);

        // Blend về 0 (idle)
        speedBlend = Mathf.MoveTowards(speedBlend, 0f, blendSpeed * Time.deltaTime);
        float walkT = Mathf.Clamp01(speedBlend);

        ApplyPosePair(walkCycle, walkT, 1f);
    }

    /// <summary>
    /// WALK FORWARD – Đi tiến.
    /// Chu kỳ đi bộ 8 tư thế, hai chân lệch phase nửa chu kỳ.
    /// </summary>
    private void PlayWalkForward()
    {
        float freq = GetSyncedFrequency(walkFrequency);
        walkCycle += freq * Time.deltaTime;
        if (walkCycle >= 8f) walkCycle -= 8f;

        // Blend về 1 (walk)
        speedBlend = Mathf.MoveTowards(speedBlend, 1f, blendSpeed * Time.deltaTime);
        float walkT = Mathf.Clamp01(speedBlend);

        ApplyPosePair(walkCycle, walkT, 1f);
    }

    /// <summary>
    /// WALK BACKWARD – Đi lùi.
    /// Chu kỳ đi bộ chạy ngược (phase giảm dần), tạo cảm giác bước lùi tự nhiên.
    /// </summary>
    private void PlayWalkBackward()
    {
        // Giơ chân ít hơn và đi chậm hơn khi lùi
        float freq = GetSyncedFrequency(walkFrequency) * backwardSpeedScale;
        walkCycle -= freq * Time.deltaTime;
        if (walkCycle < 0f) walkCycle += 8f;

        // Blend về 1 (walk)
        speedBlend = Mathf.MoveTowards(speedBlend, 1f, blendSpeed * Time.deltaTime);
        float walkT = Mathf.Clamp01(speedBlend);

        ApplyPosePair(walkCycle, walkT, backwardAmplitudeScale);
    }

    /// <summary>
    /// RUN FORWARD – Chạy tiến.
    /// Tần số cao hơn và biên độ rộng hơn so với Walk.
    /// </summary>
    private void PlayRunForward()
    {
        float freq = GetSyncedFrequency(runFrequency);
        walkCycle += freq * Time.deltaTime;
        if (walkCycle >= 8f) walkCycle -= 8f;

        // Blend về 2 (run)
        speedBlend = Mathf.MoveTowards(speedBlend, 2f, blendSpeed * Time.deltaTime);
        float walkT = Mathf.Clamp01(speedBlend);
        float runT  = Mathf.Clamp01(speedBlend - 1f);

        float amplitudeScale = Mathf.Lerp(1f, sprintAmplitudeScale, runT);
        ApplyPosePair(walkCycle, walkT, amplitudeScale);
    }

    /// <summary>
    /// JUMPING – Trên không.
    /// Cả hai chân co lên thật gọn gàng (air tuck).
    /// </summary>
    private void PlayJumping()
    {
        // Tư thế co chân khi trên không – gập chân lên phía trước
        // invertHipSwing=true → hipSign=-1 → targetHip âm * -1 = dương (ra trước)
        float targetHip   = -45f;    // Sau khi nhân với hipSign(-1) → +45 (đùi ra trước)
        float targetKnee  = -60f;    // Gập gối co chân lên
        float targetAnkle = -10f;    // Cổ chân duỗi nhẹ

        float hipSign   = invertHipSwing   ? -1f : 1f;
        float kneeSign  = invertKneeBend   ? -1f : 1f;
        float ankleSign = invertAnkleFlex  ? -1f : 1f;

        ApplySymmetricalRotations(
            targetHip   * hipSign,
            targetKnee  * kneeSign,
            targetAnkle * ankleSign
        );
    }

    /// <summary>
    /// LANDING – Tiếp đất.
    /// Kích hoạt nhún lò xo giảm chấn đàn hồi rồi tự chuyển về Idle.
    /// </summary>
    public void TriggerLanding()
    {
        if (isPlayingSpecialRoutine) return;
        currentLegState = LegAnimState.Landing;
        StartCoroutine(LandingRoutine());
    }

    private IEnumerator LandingRoutine()
    {
        isPlayingSpecialRoutine = true;

        float elapsed  = 0f;
        float duration = 0.35f;

        float hipSign   = invertHipSwing   ? -1f : 1f;
        float kneeSign  = invertKneeBend   ? -1f : 1f;
        float ankleSign = invertAnkleFlex  ? -1f : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Hàm nhún đàn hồi: tăng nhanh rồi tắt dần
            float squatStrength = Mathf.Sin(t * Mathf.PI) * 22f * (1f - t);

            ApplySymmetricalRotations(
                squatStrength * 0.8f  * hipSign,
               -squatStrength * 2.0f  * kneeSign,
                squatStrength * 1.2f  * ankleSign
            );
            yield return null;
        }

        ResetLegRotations();
        isPlayingSpecialRoutine = false;

        // Sau khi tiếp đất xong, trả về Idle
        currentLegState = LegAnimState.Idle;
    }

    // ─────────────────────────────────────────────
    //  HIỆU ỨNG ĐẶC BIỆT (LẮP RÁP & ĂN MỪNG)
    // ─────────────────────────────────────────────

    /// <summary>Nhún lò xo khi lắp ráp module chân xong.</summary>
    public void TriggerAssemble()
    {
        if (isAssembling) return;
        StartCoroutine(AssembleSquatRoutine());
    }

    private IEnumerator AssembleSquatRoutine()
    {
        isAssembling = true;
        if (!basesCached) CacheBaseTransforms();

        if (basesCached)
        {
            float elapsed  = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float strength = Mathf.Sin(t * Mathf.PI) * 15f * (1f - t);
                ApplySymmetricalRotations(strength, -strength * 2.5f, strength * 1.5f);
                yield return null;
            }
            ResetLegRotations();
        }
        isAssembling = false;
    }

    /// <summary>Điệu nhảy ăn mừng khi lắp ráp hoàn tất toàn bộ robot.</summary>
    public void TriggerCelebrate()
    {
        if (isCelebrating) return;
        StartCoroutine(CelebrateRoutine());
    }

    private IEnumerator CelebrateRoutine()
    {
        isCelebrating = true;

        if (basesCached)
        {
            float elapsed  = 0f;
            float duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float danceL = Mathf.Sin(elapsed * 12f) * 20f;
                float danceR = Mathf.Cos(elapsed * 12f) * 20f;
                float kneeL  = Mathf.Max(0f, -danceL) * 2.2f;
                float kneeR  = Mathf.Max(0f, -danceR) * 2.2f;

                if (legLeftBones.Length  > 1 && legLeftBones[1]  != null)
                    legLeftBones[1].localRotation  = legLeftBaseRots[1]  * Quaternion.Euler(legSwingAxis  * danceL);
                if (legRightBones.Length > 1 && legRightBones[1] != null)
                    legRightBones[1].localRotation = legRightBaseRots[1] * Quaternion.Euler(legSwingAxis  * danceR);
                if (legLeftBones.Length  > 2 && legLeftBones[2]  != null)
                    legLeftBones[2].localRotation  = legLeftBaseRots[2]  * Quaternion.Euler(kneeBendAxis  * kneeL);
                if (legRightBones.Length > 2 && legRightBones[2] != null)
                    legRightBones[2].localRotation = legRightBaseRots[2] * Quaternion.Euler(kneeBendAxis  * kneeR);

                yield return null;
            }
            ResetLegRotations();
        }
        isCelebrating = false;
    }

    // ─────────────────────────────────────────────
    //  HÀM NỘI BỘ – TÍNH TOÁN & ÁP DỤNG GÓC KHỚP
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tính toán và áp dụng góc khớp cho CẢ HAI chân dựa vào phase hiện tại.
    /// Chân trái lệch phase nửa chu kỳ (+4 bước) so với chân phải.
    /// </summary>
    private void ApplyPosePair(float phase, float walkBlend, float amplitudeScale)
    {
        float phaseR = phase;
        float phaseL = (phase + 4f) % 8f;

        LegPose poseR = GetInterpolatedPose(phaseR);
        LegPose poseL = GetInterpolatedPose(phaseL);

        // Giới hạn đạp chân về phía sau (chỉ áp dụng cho góc DƯƠNG = đẩy sau)
        if (poseR.hipAngle > 0f) poseR.hipAngle *= backKickScale;
        if (poseL.hipAngle > 0f) poseL.hipAngle *= backKickScale;


        float hipSign   = invertHipSwing   ? -1f : 1f;
        float kneeSign  = invertKneeBend   ? -1f : 1f;
        float ankleSign = invertAnkleFlex  ? -1f : 1f;

        float hipR   = poseR.hipAngle   * walkBlend * amplitudeScale * hipSign;
        float kneeR  = poseR.kneeAngle  * walkBlend * amplitudeScale * kneeSign;
        float ankleR = poseR.ankleAngle * walkBlend * amplitudeScale * ankleSign;

        float hipL   = poseL.hipAngle   * walkBlend * amplitudeScale * hipSign;
        float kneeL  = poseL.kneeAngle  * walkBlend * amplitudeScale * kneeSign;
        float ankleL = poseL.ankleAngle * walkBlend * amplitudeScale * ankleSign;

        // Áp dụng lên xương
        if (legLeftBones.Length  > 1 && legLeftBones[1]  != null)
            legLeftBones[1].localRotation  = legLeftBaseRots[1]  * Quaternion.Euler(legSwingAxis  * hipL);
        if (legRightBones.Length > 1 && legRightBones[1] != null)
            legRightBones[1].localRotation = legRightBaseRots[1] * Quaternion.Euler(legSwingAxis  * hipR);

        if (legLeftBones.Length  > 2 && legLeftBones[2]  != null)
            legLeftBones[2].localRotation  = legLeftBaseRots[2]  * Quaternion.Euler(kneeBendAxis  * kneeL);
        if (legRightBones.Length > 2 && legRightBones[2] != null)
            legRightBones[2].localRotation = legRightBaseRots[2] * Quaternion.Euler(kneeBendAxis  * kneeR);

        if (legLeftBones.Length  > 3 && legLeftBones[3]  != null)
            legLeftBones[3].localRotation  = legLeftBaseRots[3]  * Quaternion.Euler(ankleAxis     * ankleL);
        if (legRightBones.Length > 3 && legRightBones[3] != null)
            legRightBones[3].localRotation = legRightBaseRots[3] * Quaternion.Euler(ankleAxis     * ankleR);
    }

    private LegPose GetInterpolatedPose(float phase)
    {
        int   index1      = Mathf.FloorToInt(phase) % 8;
        int   index2      = (index1 + 1) % 8;
        float lerpFactor  = phase - Mathf.Floor(phase);

        return new LegPose
        {
            hipAngle   = Mathf.Lerp(walkPoses[index1].hipAngle,   walkPoses[index2].hipAngle,   lerpFactor),
            kneeAngle  = Mathf.Lerp(walkPoses[index1].kneeAngle,  walkPoses[index2].kneeAngle,  lerpFactor),
            ankleAngle = Mathf.Lerp(walkPoses[index1].ankleAngle, walkPoses[index2].ankleAngle, lerpFactor),
        };
    }

    private void ApplySymmetricalRotations(float hip, float knee, float ankle)
    {
        if (legLeftBones.Length  > 1 && legLeftBones[1]  != null)
            legLeftBones[1].localRotation  = legLeftBaseRots[1]  * Quaternion.Euler(legSwingAxis  * hip);
        if (legRightBones.Length > 1 && legRightBones[1] != null)
            legRightBones[1].localRotation = legRightBaseRots[1] * Quaternion.Euler(legSwingAxis  * hip);

        if (legLeftBones.Length  > 2 && legLeftBones[2]  != null)
            legLeftBones[2].localRotation  = legLeftBaseRots[2]  * Quaternion.Euler(kneeBendAxis  * knee);
        if (legRightBones.Length > 2 && legRightBones[2] != null)
            legRightBones[2].localRotation = legRightBaseRots[2] * Quaternion.Euler(kneeBendAxis  * knee);

        if (legLeftBones.Length  > 3 && legLeftBones[3]  != null)
            legLeftBones[3].localRotation  = legLeftBaseRots[3]  * Quaternion.Euler(ankleAxis     * ankle);
        if (legRightBones.Length > 3 && legRightBones[3] != null)
            legRightBones[3].localRotation = legRightBaseRots[3] * Quaternion.Euler(ankleAxis     * ankle);
    }

    private void ResetLegRotations()
    {
        for (int i = 0; i < legLeftBones.Length;  i++)
            if (legLeftBones[i]  != null) legLeftBones[i].localRotation  = legLeftBaseRots[i];
        for (int i = 0; i < legRightBones.Length; i++)
            if (legRightBones[i] != null) legRightBones[i].localRotation = legRightBaseRots[i];
    }

    // ─────────────────────────────────────────────
    //  KHÁM PHÁ & CACHE XƯƠNG TỰ ĐỘNG
    // ─────────────────────────────────────────────
    [ContextMenu("Cache Bones & Base Rotations")]
    public void CacheBaseTransforms()
    {
        Transform armature = FindArmature(transform);
        if (armature == null && transform.parent != null)
            armature = FindArmature(transform.parent);

        if (armature == null)
        {
            Debug.LogError($"[LegAnimator] Không tìm thấy Armature dưới '{name}'! Kiểm tra cấu trúc prefab.");
            return;
        }

        if (armature.childCount > 0)
        {
            legPelvis = armature.GetChild(0);
            if (legPelvis.childCount >= 2)
            {
                Transform child0 = legPelvis.GetChild(0);
                Transform child1 = legPelvis.GetChild(1);

                // Phân biệt chân trái / phải dựa vào tên xương (_L / _R)
                if (child0.name.Contains("_R") && !child0.name.Contains("_L"))
                {
                    legRightBones = ExtractBoneChain(child0);
                    legLeftBones  = ExtractBoneChain(child1);
                }
                else
                {
                    legLeftBones  = ExtractBoneChain(child0);
                    legRightBones = ExtractBoneChain(child1);
                }

                legLeftBaseRots  = GetBaseRotations(legLeftBones);
                legRightBaseRots = GetBaseRotations(legRightBones);
                basesCached      = true;

                Debug.Log($"[LegAnimator] Cache xương hoàn tất '{gameObject.name}':\n" +
                          $"  Chân Trái : {legLeftBones[1].name} ({legLeftBones.Length} bones)\n" +
                          $"  Chân Phải : {legRightBones[1].name} ({legRightBones.Length} bones)");
            }
            else
            {
                Debug.LogError("[LegAnimator] Pelvis không đủ 2 chân con!");
            }
        }
        else
        {
            Debug.LogError("[LegAnimator] Armature bị rỗng!");
        }
    }

    private Transform FindArmature(Transform current)
    {
        if (current.name.StartsWith("Armature")) return current;
        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindArmature(current.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    private Transform[] ExtractBoneChain(Transform root)
    {
        var chain   = new System.Collections.Generic.List<Transform>();
        Transform c = root;
        while (c != null)
        {
            chain.Add(c);
            if (c.childCount > 0) c = c.GetChild(0);
            else break;
        }
        return chain.ToArray();
    }

    private Quaternion[] GetBaseRotations(Transform[] bones)
    {
        if (bones == null) return null;
        var rots = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++) rots[i] = bones[i].localRotation;
        return rots;
    }

    // ─────────────────────────────────────────────
    //  GETTERS
    // ─────────────────────────────────────────────
    public LegAnimState CurrentLegState => currentLegState;

    // Backward compatibility – gọi TriggerLanding thay TriggerLandingSquash
    public void TriggerLandingSquash() => TriggerLanding();
}
