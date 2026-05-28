using UnityEngine;
using System.Collections;

/// <summary>
/// GunArmAnimator: Quản lý tư thế cầm súng và hiệu ứng giật khi bắn.
///
/// Cách dùng:
///   1. Gắn script này vào cùng GameObject với ArmAnimator (hoặc tay súng).
///   2. Chỉnh "Aim Angle" để set góc giơ tay lên khi cầm súng.
///   3. Gọi TriggerFire() từ script bắn súng khi đạn được bắn ra.
///
/// Kiến trúc:
///   - Tự động tìm chuỗi xương Armature → Shoulder → UpperArm → LowerArm.
///   - Xương [1] (UpperArm/Bắp tay) chịu trách nhiệm giơ tay lên (Aim Pose).
///   - Xương [2] (LowerArm/Cẳng tay) chịu hiệu ứng giật (Recoil).
///   - Khi bật IsAiming, tư thế Aim được blend mượt vào; khi tắt thì blend ra.
/// </summary>
public class GunArmAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────

    [Header("── Tư thế cầm súng (Aim Pose) ──")]
    [Tooltip("Bật/tắt trạng thái cầm súng (giơ tay lên)")]
    public bool IsAiming = false;

    [Tooltip("Trục xoay của bắp tay và cẳng tay để giơ lên. Thử lần lượt: (1,0,0), (0,1,0), (0,0,1)")]
    public Vector3 aimAxis = new Vector3(0f, 0f, 1f);

    [Tooltip("Góc giơ bắp tay (Xương 1). Nâng vai lên một chút cho đỡ khúm núm")]
    [Range(-180f, 180f)]
    public float upperArmAimAngle = 30f;

    [Tooltip("Góc giơ cẳng tay (Xương 2).")]
    [Range(-180f, 180f)]
    public float forearmAimAngle = 60f;

    [Tooltip("Đảo chiều xoay nếu tay đi sai hướng")]
    public bool invertAim = false;

    [Tooltip("Tốc độ blend vào/ra tư thế cầm súng")]
    public float aimBlendSpeed = 8f;

    [Header("── Hiệu ứng giật (Recoil) ──")]
    [Tooltip("Chỉ số xương chịu hiệu ứng giật (thường là 2 = Cẳng tay)")]
    public int recoilBoneIndex = 2;

    [Tooltip("Trục giật của cẳng tay (thường là X)")]
    public Vector3 recoilAxis = new Vector3(1f, 0f, 0f);

    [Tooltip("Góc giật mạnh về sau khi bắn (độ)")]
    [Range(0f, 45f)]
    public float recoilAngle = 15f;

    [Tooltip("Thời gian giật (giây)")]
    public float recoilDuration = 0.08f;

    [Tooltip("Thời gian hồi về sau khi giật (giây)")]
    public float recoilRecoverDuration = 0.18f;

    [Header("── Debug ──")]
    [Tooltip("Khi tick vào sẽ luôn hiển thị pose nhắm súng trong Editor")]
    public bool previewAimPose = false;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    // Chuỗi xương
    private Transform[] bones;
    private Quaternion[] baseRots;

    // Blend aim
    private float aimWeight = 0f;

    // Recoil
    private float recoilWeight = 0f;
    private bool  isFiring = false;

    // Input
    private InputManager inputManager;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        CacheBones();
    }

    private void Start()
    {
        inputManager = Object.FindFirstObjectByType<InputManager>();

        // DEBUG ─ xác định vấn đề
        if (inputManager == null)
            Debug.LogError("[GunArmAnimator] Không tìm thấy InputManager trong scene!", this);
        else
            Debug.Log("[GunArmAnimator] Đã kết nối InputManager.", this);

        if (bones == null || bones.Length < 2)
            Debug.LogError("[GunArmAnimator] Không tịm được chuỗi xương! Kiểm tra cấu trúc Armature của GameObject này.", this);
        else
            Debug.Log($"[GunArmAnimator] Đã cầu xương: {bones.Length} xương. [0]={bones[0].name}, [1]={bones[1]?.name}", this);
    }

    private void Update()
    {
        // Khóa súng hoàn toàn nếu đang lơ lửng (chưa gắn vào thân) hoặc trong màn hình lắp ráp
        bool isAttached = false;
        bool isMovementEnabled = false;

        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) { isAttached = true; if (pm.enabled) isMovementEnabled = true; }

        var rc = GetComponentInParent<RobotController>();
        if (rc != null) { isAttached = true; if (rc.enabled) isMovementEnabled = true; }

        if (!isAttached || !isMovementEnabled)
        {
            IsAiming = false;
            return;
        }

        // Fallback khẩn cấp: nếu không có InputManager, dùng legacy input
        if (inputManager == null)
        {
            bool rightHeld = Input.GetMouseButton(1);
            bool leftDown = Input.GetMouseButtonDown(0);
            
            IsAiming = rightHeld;
            
            // Chỉ cho bắn khi đang nhắm súng
            if (IsAiming && leftDown) 
                TriggerFire();
                
            return;
        }

        // Giữ chuột phải = nhắm súng
        IsAiming = inputManager.IsHoldingFire;

        // Click chuột trái = bắn (chỉ giật khi đang nhắm)
        if (IsAiming && inputManager.FireTriggered)
        {
            TriggerFire();
        }
    }

    private void LateUpdate()
    {
        if (bones == null || bones.Length < 2) return;

        // Blend aimWeight mượt mà
        float targetAim = (IsAiming || previewAimPose) ? 1f : 0f;
        aimWeight = Mathf.MoveTowards(aimWeight, targetAim, Time.deltaTime * aimBlendSpeed);

        if (aimWeight < 0.001f && !isFiring) return;

        // ── Tính toán xoay nhắm súng (Aim) ──
        float upperAngle = invertAim ? -upperArmAimAngle : upperArmAimAngle;
        float foreAngle = invertAim ? -forearmAimAngle : forearmAimAngle;

        // Xương 1 (Bắp tay)
        if (bones.Length > 1 && bones[1] != null)
        {
            Quaternion aimRot1 = baseRots[1] * Quaternion.Euler(aimAxis * upperAngle);
            bones[1].localRotation = Quaternion.Slerp(baseRots[1], aimRot1, aimWeight);
        }

        // Xương 2 (Cẳng tay)
        if (bones.Length > 2 && bones[2] != null)
        {
            Quaternion aimRot2 = baseRots[2] * Quaternion.Euler(aimAxis * foreAngle);
            bones[2].localRotation = Quaternion.Slerp(baseRots[2], aimRot2, aimWeight);
        }

        // ── Tính toán xoay giật (Recoil) ──
        if (recoilBoneIndex >= 0 && recoilBoneIndex < bones.Length && bones[recoilBoneIndex] != null)
        {
            Quaternion recoilRot = baseRots[recoilBoneIndex] * Quaternion.Euler(recoilAxis * recoilAngle);
            
            // Nếu xương giật là xương 2 (cẳng tay), ta cộng dồn recoil lên nền aim hiện tại
            if (recoilBoneIndex == 2)
            {
                Quaternion baseAimRot = baseRots[2] * Quaternion.Euler(aimAxis * foreAngle);
                Quaternion combinedRot = baseAimRot * Quaternion.Euler(recoilAxis * recoilAngle);
                
                Quaternion currentAimPose = bones[2].localRotation;
                bones[2].localRotation = Quaternion.Slerp(currentAimPose, combinedRot, recoilWeight);
            }
            else if (recoilBoneIndex == 1)
            {
                Quaternion baseAimRot = baseRots[1] * Quaternion.Euler(aimAxis * upperAngle);
                Quaternion combinedRot = baseAimRot * Quaternion.Euler(recoilAxis * recoilAngle);
                
                Quaternion currentAimPose = bones[1].localRotation;
                bones[1].localRotation = Quaternion.Slerp(currentAimPose, combinedRot, recoilWeight);
            }
            else
            {
                // Xương khác (không dùng aim)
                bones[recoilBoneIndex].localRotation = Quaternion.Slerp(
                    bones[recoilBoneIndex].localRotation,
                    recoilRot,
                    recoilWeight
                );
            }
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gọi hàm này từ script bắn súng mỗi khi bắn một phát đạn.
    /// </summary>
    public void TriggerFire()
    {
        if (isFiring) return;
        StartCoroutine(RecoilRoutine());
    }

    /// <summary>
    /// Bật chế độ nhắm súng (giơ tay lên).
    /// </summary>
    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
    }

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────

    private IEnumerator RecoilRoutine()
    {
        isFiring = true;

        // Pha 1: Giật nhanh về sau
        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            elapsed += Time.deltaTime;
            recoilWeight = Mathf.Clamp01(elapsed / recoilDuration);
            yield return null;
        }
        recoilWeight = 1f;

        // Pha 2: Hồi về mượt mà
        elapsed = 0f;
        while (elapsed < recoilRecoverDuration)
        {
            elapsed += Time.deltaTime;
            recoilWeight = Mathf.Lerp(1f, 0f, elapsed / recoilRecoverDuration);
            yield return null;
        }
        recoilWeight = 0f;

        isFiring = false;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void CacheBones()
    {
        // Tự tìm Armature trong con, sau đó trích xuất chuỗi xương
        Transform armature = FindChildStartingWith(transform, "Armature");
        Transform chainRoot = null;

        if (armature != null && armature.childCount > 0)
            chainRoot = armature.GetChild(0);
        else if (transform.childCount > 0)
            chainRoot = transform.GetChild(0); // fallback: lấy child đầu tiên

        if (chainRoot == null) return;

        var chain = new System.Collections.Generic.List<Transform>();
        Transform cur = chainRoot;
        while (cur != null)
        {
            chain.Add(cur);
            cur = cur.childCount > 0 ? cur.GetChild(0) : null;
        }

        bones    = chain.ToArray();
        baseRots = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            baseRots[i] = bones[i].localRotation;
    }

    private Transform FindChildStartingWith(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
            if (child.name.StartsWith(prefix)) return child;
        return null;
    }
}
