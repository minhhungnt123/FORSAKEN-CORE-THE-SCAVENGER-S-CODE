using UnityEngine;
using System.Collections;

/// <summary>
/// SwordArmAnimator: Quản lý animation chém kiếm cho tay trái.
/// 
/// Cách dùng:
///   1. Gắn script này vào tay trái (ArmLeftModule hoặc cùng GameObject với ArmAnimator).
///   2. Gọi TriggerSlash() hoặc dùng chuột trái (khi không nhắm súng) để chém.
/// </summary>
public class SwordArmAnimator : MonoBehaviour
{
    [Header("── Tư thế cầm kiếm (Idle Pose) ──")]
    [Tooltip("Góc bắp tay khi bình thường (cộng dồn lên animation đi bộ)")]
    public Vector3 swordIdleShoulder = new Vector3(0f, 0f, 0f);
    [Tooltip("Góc cẳng tay khi bình thường")]
    public Vector3 swordIdleElbow = new Vector3(0f, 0f, 0f);

    [Header("── Chém Kiếm (Slash Poses) ──")]
    [Tooltip("Tư thế vung tay ra sau (chuẩn bị)")]
    public Vector3 swordWindUpShoulder = new Vector3(70f, 0f, -45f);
    public Vector3 swordWindUpElbow = new Vector3(80f, 0f, 0f);

    [Tooltip("Tư thế chém tới trước (chéo)")]
    public Vector3 swordHitShoulder = new Vector3(-75f, 0f, 45f);
    public Vector3 swordHitElbow = new Vector3(0f, 0f, 0f);

    [Header("── Thời gian (Duration) ──")]
    public float windUpTime = 0.15f;
    public float slashTime = 0.1f;
    public float recoverTime = 0.25f;

    // Trạng thái
    private bool isSlashing = false;
    private float slashWeight = 0f;
    private Quaternion currentShoulderTarget = Quaternion.identity;
    private Quaternion currentElbowTarget = Quaternion.identity;

    private Transform[] bones;
    private Quaternion[] baseRots;

    private InputManager inputManager;
    private ArmAnimator armAnimator;
    private bool isLeftArm = true;

    private void Awake()
    {
        CacheBones();
        armAnimator = GetComponent<ArmAnimator>();

        if (gameObject.name.Contains("Right") || (transform.parent != null && transform.parent.name.Contains("Right")))
        {
            isLeftArm = false;
        }
    }

    private void Start()
    {
        inputManager = Object.FindFirstObjectByType<InputManager>();
    }

    private void Update()
    {
        // Khóa chém nếu đang lơ lửng (chưa gắn) hoặc đang trong màn hình lắp ráp
        bool isAttached = false;
        bool isMovementEnabled = false;

        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) { isAttached = true; if (pm.enabled) isMovementEnabled = true; }

        var rc = GetComponentInParent<RobotController>();
        if (rc != null) { isAttached = true; if (rc.enabled) isMovementEnabled = true; }

        if (!isAttached || !isMovementEnabled) return;

        // Xử lý input
        if (inputManager != null)
        {
            // Chỉ chém khi bấm chuột trái VÀ không đang nhắm súng (chuột phải)
            if (inputManager.FireTriggered && !inputManager.IsHoldingFire)
            {
                TriggerSlash();
            }
        }
        else
        {
            // Fallback
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
            {
                TriggerSlash();
            }
        }
    }

    private void LateUpdate()
    {
        if (bones == null || bones.Length < 2) return;

        // Khóa chém và idle pose nếu đang lơ lửng hoặc trong màn hình lắp ráp
        bool isAttached = false;
        bool isMovementEnabled = false;

        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) { isAttached = true; if (pm.enabled) isMovementEnabled = true; }

        var rc = GetComponentInParent<RobotController>();
        if (rc != null) { isAttached = true; if (rc.enabled) isMovementEnabled = true; }

        if (!isAttached || !isMovementEnabled) return;

        if (isSlashing)
        {
            // Áp dụng góc xoay chém kiếm lên bắp tay (Xương 1)
            if (bones[1] != null)
            {
                Quaternion slashRot = baseRots[1] * currentShoulderTarget;
                bones[1].localRotation = Quaternion.Slerp(bones[1].localRotation, slashRot, slashWeight);
            }

            // Áp dụng góc xoay lên cẳng tay (Xương 2)
            if (bones.Length > 2 && bones[2] != null)
            {
                Quaternion slashRot = baseRots[2] * currentElbowTarget;
                bones[2].localRotation = Quaternion.Slerp(bones[2].localRotation, slashRot, slashWeight);
            }
        }
        else
        {
            // Không chém: Áp dụng thế cầm kiếm (Idle Pose) cộng dồn lên hoạt ảnh đi bộ
            if (bones[1] != null)
            {
                bones[1].localRotation *= Quaternion.Euler(swordIdleShoulder);
            }
            if (bones.Length > 2 && bones[2] != null)
            {
                bones[2].localRotation *= Quaternion.Euler(swordIdleElbow);
            }
        }
    }

    public void TriggerSlash()
    {
        if (isSlashing) return;
        StartCoroutine(SlashRoutine());
    }

    private IEnumerator SlashRoutine()
    {
        isSlashing = true;

        // Tính toán góc chém: Nếu là tay phải thì đảo ngược trục Y và Z để chém đối xứng
        Vector3 actualWindUpShoulder = swordWindUpShoulder;
        Vector3 actualSlashShoulder = swordHitShoulder;
        Vector3 actualWindUpElbow = swordWindUpElbow;
        Vector3 actualSlashElbow = swordHitElbow;

        if (!isLeftArm)
        {
            actualWindUpShoulder.y = -actualWindUpShoulder.y;
            actualWindUpShoulder.z = -actualWindUpShoulder.z;
            actualSlashShoulder.y = -actualSlashShoulder.y;
            actualSlashShoulder.z = -actualSlashShoulder.z;

            actualWindUpElbow.y = -actualWindUpElbow.y;
            actualWindUpElbow.z = -actualWindUpElbow.z;
            actualSlashElbow.y = -actualSlashElbow.y;
            actualSlashElbow.z = -actualSlashElbow.z;
        }

        // Tạm dừng đung đưa tay của ArmAnimator
        if (armAnimator != null) armAnimator.IsPlayingSpecialRoutine = true;

        // Pha 1: Vung kiếm (Wind up)
        float elapsed = 0f;
        while (elapsed < windUpTime)
        {
            elapsed += Time.deltaTime;
            slashWeight = Mathf.Clamp01(elapsed / windUpTime);
            currentShoulderTarget = Quaternion.Euler(actualWindUpShoulder);
            currentElbowTarget = Quaternion.Euler(actualWindUpElbow);
            yield return null;
        }

        // Pha 2: Chém tới trước (Slash)
        elapsed = 0f;
        while (elapsed < slashTime)
        {
            elapsed += Time.deltaTime;
            slashWeight = Mathf.Clamp01(elapsed / slashTime);
            // Blend từ WindUp sang Slash
            currentShoulderTarget = Quaternion.Slerp(Quaternion.Euler(actualWindUpShoulder), Quaternion.Euler(actualSlashShoulder), slashWeight);
            currentElbowTarget = Quaternion.Slerp(Quaternion.Euler(actualWindUpElbow), Quaternion.Euler(actualSlashElbow), slashWeight);
            yield return null;
        }
        slashWeight = 1f;

        // Pha 3: Hồi về (Recover)
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            slashWeight = Mathf.Lerp(1f, 0f, elapsed / recoverTime);
            yield return null;
        }

        slashWeight = 0f;
        isSlashing = false;

        // Trả lại đung đưa tay cho ArmAnimator
        if (armAnimator != null) armAnimator.IsPlayingSpecialRoutine = false;
    }

    private void CacheBones()
    {
        Transform armature = FindChildStartingWith(transform, "Armature");
        Transform chainRoot = null;

        if (armature != null && armature.childCount > 0)
            chainRoot = armature.GetChild(0);
        else if (transform.childCount > 0)
            chainRoot = transform.GetChild(0);

        if (chainRoot == null) return;

        var chain = new System.Collections.Generic.List<Transform>();
        Transform cur = chainRoot;
        while (cur != null)
        {
            chain.Add(cur);
            cur = cur.childCount > 0 ? cur.GetChild(0) : null;
        }

        bones = chain.ToArray();
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
