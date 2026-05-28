using UnityEngine;
using System.Collections;

/// <summary>
/// ShieldArmAnimator: Quản lý animation đỡ khiên.
///
/// Cách dùng:
///   1. Gắn script này vào module tay khiên (cùng GameObject với ArmAnimator).
///   2. Giữ chuột phải để giơ khiên lên đỡ.
///   3. Thả chuột phải để hạ khiên xuống.
///   4. Khi đang đỡ mà bị trúng đòn, gọi TriggerImpact() để có hiệu ứng giật khiên.
///
/// Kiến trúc (rig 2 xương):
///   - Tự động tìm chuỗi xương Armature → chainRoot → child.
///   - Xương [0] (xương gốc / Shoulder): xương dài nằm ngang, điều khiển hướng mặt khiên.
///   - Xương [1] (xương con  / Handle) : xương ngắn ở đầu tay cầm, giữ tương đối ổn định.
///   - Blend mượt mà vào/ra tư thế đỡ khiên.
/// </summary>
public class ShieldArmAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────

    [Header("── Tư thế đỡ khiên (Block Pose) ──")]
    [Tooltip("Bật/tắt trạng thái đang đỡ khiên")]
    public bool IsBlocking = false;

    [Tooltip("Góc xương gốc (Shoulder) khi đỡ khiên — nâng lên + xoay mặt khiên ra trước")]
    public Vector3 blockShoulder = new Vector3(70f, -40f, 20f);

    [Tooltip("Góc xương con (Handle) khi đỡ khiên — thường để nhỏ hoặc bằng 0")]
    public Vector3 blockHandle = new Vector3(0f, 0f, 0f);

    [Tooltip("Đảo chiều xoay nếu khiên đi sai hướng")]
    public bool invertBlock = false;

    [Tooltip("Tốc độ blend vào/ra tư thế đỡ khiên (càng cao càng nhanh)")]
    public float blockBlendSpeed = 10f;

    [Header("── Hiệu ứng va chạm (Impact) ──")]
    [Tooltip("Góc giật xương gốc khi bị trúng đòn")]
    public Vector3 impactAngle = new Vector3(-20f, 0f, -15f);

    [Tooltip("Thời gian giật khi bị đánh vào khiên (giây)")]
    public float impactDuration = 0.08f;

    [Tooltip("Thời gian hồi về sau khi giật (giây)")]
    public float impactRecoverDuration = 0.25f;

    [Header("── Thời gian (Duration) ──")]
    [Tooltip("Ép buộc sử dụng thông số mặc định (Bỏ check nếu muốn tự chỉnh)")]
    public bool useDefaultPose = true;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    // Chuỗi xương (2 xương: [0] = gốc, [1] = con)
    private Transform[] bones;
    private Quaternion[] baseRots;

    // Blend block
    private float blockWeight = 0f;

    // Impact
    private float impactWeight = 0f;
    private bool isImpacting = false;

    // Input & Animator
    private InputManager inputManager;
    private ArmAnimator armAnimator;

    private bool isLeftArm = true;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        CacheBones();
        armAnimator = GetComponent<ArmAnimator>();

        // Xác định tay trái/phải qua tên GameObject
        if (gameObject.name.Contains("Right") ||
            (transform.parent != null && transform.parent.name.Contains("Right")))
        {
            isLeftArm = false;
        }

        if (useDefaultPose)
        {
            // ── Thông số mặc định (đã test trực tiếp trong Unity Inspector) ──
            //
            //  blockShoulder.Y = -90 → xoay mặt khiên ra trước (đã xác nhận)
            //  blockShoulder.Z =  60 → nâng tay lên ngang ngực
            //  blockHandle     =  0  → xương tay cầm giữ nguyên (gập sẽ xoay khiên sai)

            blockShoulder      = new Vector3(0f, -90f, 60f);
            blockHandle        = new Vector3(0f,   0f,  0f);
            blockBlendSpeed    = 10f;

            impactAngle           = new Vector3(0f, 15f, -10f);
            impactDuration        = 0.08f;
            impactRecoverDuration = 0.25f;
        }
    }

    private void Start()
    {
        inputManager = Object.FindFirstObjectByType<InputManager>();
    }

    private void Update()
    {
        // Khóa khiên nếu chưa gắn vào robot hoặc đang trong màn hình lắp ráp
        if (!IsAttachedAndMoving())
        {
            IsBlocking = false;
            return;
        }

        // Xử lý input: Giữ chuột phải = giơ khiên đỡ
        if (inputManager != null)
            IsBlocking = inputManager.IsHoldingFire;
        else
            IsBlocking = Input.GetMouseButton(1);   // Fallback
    }

    private void LateUpdate()
    {
        if (bones == null || bones.Length < 1) return;
        if (!IsAttachedAndMoving()) return;

        // ── Blend blockWeight mượt mà ──
        float targetBlock = IsBlocking ? 1f : 0f;
        blockWeight = Mathf.MoveTowards(blockWeight, targetBlock, Time.deltaTime * blockBlendSpeed);

        // Thông báo cho ArmAnimator ngừng đung đưa khi đang giơ khiên
        if (armAnimator != null)
            armAnimator.IsPlayingSpecialRoutine = blockWeight > 0.01f;

        // Khi không đỡ và không có impact → trả xương về baseRots sạch sẽ
        if (blockWeight < 0.001f && !isImpacting)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].localRotation = baseRots[i];
            return;
        }

        // ── Tính góc thực tế (đối xứng tay phải/trái) ──
        Vector3 actualShoulder = blockShoulder;
        Vector3 actualHandle   = blockHandle;
        Vector3 actualImpact   = impactAngle;

        if (!isLeftArm)
        {
            // Đối xứng qua trục Y và Z cho tay phải
            actualShoulder.y = -actualShoulder.y;
            actualShoulder.z = -actualShoulder.z;
            actualHandle.y   = -actualHandle.y;
            actualHandle.z   = -actualHandle.z;
            actualImpact.y   = -actualImpact.y;
            actualImpact.z   = -actualImpact.z;
        }

        if (invertBlock)
        {
            actualShoulder = -actualShoulder;
            actualHandle   = -actualHandle;
        }

        // ── Xương [0]: xương gốc (Shoulder / xương cyan dài) ──
        if (bones.Length > 0 && bones[0] != null)
        {
            Quaternion blockRot = baseRots[0] * Quaternion.Euler(actualShoulder);
            Quaternion blended  = Quaternion.Slerp(baseRots[0], blockRot, blockWeight);

            // Cộng dồn hiệu ứng va chạm lên xương gốc
            if (isImpacting && impactWeight > 0.01f)
                blended *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(actualImpact), impactWeight);

            bones[0].localRotation = blended;
        }

        // ── Xương [1]: xương con (Handle / xương đen ngắn) ──
        if (bones.Length > 1 && bones[1] != null)
        {
            Quaternion blockRot = baseRots[1] * Quaternion.Euler(actualHandle);
            bones[1].localRotation = Quaternion.Slerp(baseRots[1], blockRot, blockWeight);
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gọi khi khiên bị trúng đòn → tạo hiệu ứng giật.
    /// Chỉ có tác dụng khi IsBlocking = true.
    /// </summary>
    public void TriggerImpact()
    {
        if (!IsBlocking || isImpacting) return;
        StartCoroutine(ImpactRoutine());
    }

    /// <summary>Bật/tắt trạng thái đỡ khiên từ code.</summary>
    public void SetBlocking(bool blocking) => IsBlocking = blocking;

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────

    private IEnumerator ImpactRoutine()
    {
        isImpacting = true;

        // Pha 1: Giật nhanh (khiên bị đẩy lùi)
        float elapsed = 0f;
        while (elapsed < impactDuration)
        {
            elapsed    += Time.deltaTime;
            impactWeight = Mathf.Clamp01(elapsed / impactDuration);
            yield return null;
        }
        impactWeight = 1f;

        // Pha 2: Hồi về mượt mà
        elapsed = 0f;
        while (elapsed < impactRecoverDuration)
        {
            elapsed    += Time.deltaTime;
            impactWeight = Mathf.Lerp(1f, 0f, elapsed / impactRecoverDuration);
            yield return null;
        }
        impactWeight = 0f;
        isImpacting  = false;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    /// <summary>Kiểm tra xem module đã gắn vào robot và đang chạy chưa.</summary>
    private bool IsAttachedAndMoving()
    {
        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null && pm.enabled) return true;

        var rc = GetComponentInParent<RobotController>();
        if (rc != null && rc.enabled) return true;

        return false;
    }

    /// <summary>
    /// Build chain xương: Armature → chainRoot → child → ...
    /// Với rig 2 xương trong hình: bones[0] = xương cyan, bones[1] = xương đen.
    /// </summary>
    private void CacheBones()
    {
        Transform armature = FindChildStartingWith(transform, "Armature");
        Transform chainRoot;

        if (armature != null && armature.childCount > 0)
            chainRoot = armature.GetChild(0);
        else if (transform.childCount > 0)
            chainRoot = transform.GetChild(0);
        else
            return;

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