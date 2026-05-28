using UnityEngine;
using System.Collections;

/// <summary>
/// PunchArmAnimator: Quản lý animation đấm tay không.
/// 
/// Hỗ trợ đấm luân phiên (combo 1-2) nếu lắp 2 tay không.
/// Nếu chỉ lắp 1 tay không, sẽ đấm theo nhịp 1-nghỉ-1.
/// </summary>
public class PunchArmAnimator : MonoBehaviour
{
    public enum ArmSide { AutoDetect, Left, Right }

    [Header("── Phân biệt tay (Arm Side) ──")]
    [Tooltip("Nếu đấm bị chéo ra ngoài thay vì chéo vào trong, hãy tự chọn tay Left hoặc Right ở đây.")]
    public ArmSide armSide = ArmSide.AutoDetect;

    [Header("── Tư thế đấm (Punch Poses) ──")]
    [Tooltip("Check vào đây nếu đấm bị ngược ra sau lưng")]
    public bool invertXAxis = true;
    
    [Tooltip("Ép buộc sử dụng form đấm chéo chuẩn (Bỏ check nếu muốn tự chỉnh thông số bên dưới)")]
    public bool useDefaultCrossPunch = true;

    [Tooltip("Tư thế lấy đà (Co vuông góc trước ngực)")]
    public Vector3 windUpShoulder = new Vector3(-30f, 0f, 0f);
    public Vector3 windUpElbow = new Vector3(-90f, 0f, 0f);

    [Tooltip("Tư thế vung đấm tới (đấm chéo)")]
    public Vector3 hitShoulder = new Vector3(-80f, -25f, 0f);
    public Vector3 hitElbow = new Vector3(0f, 0f, 0f);

    [Header("── Thời gian (Duration) ──")]
    public float windUpTime = 0.5f;
    public float punchTime = 0.4f;
    public float recoverTime = 0.6f;

    // --- Quản lý nhịp độ đấm chung cho cả 2 tay ---
    private static System.Collections.Generic.List<PunchArmAnimator> punchers = new System.Collections.Generic.List<PunchArmAnimator>();
    private static float globalCooldown = 0f;
    private static float bufferedPunchTime = 0f; // Input Buffer để tránh bị miss khi spam click

    // Trạng thái cục bộ
    private bool isPunching = false;
    private float punchWeight = 0f;
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

        // Ép thông số đấm chéo chuẩn (giá trị cho TAY TRÁI làm gốc, tay phải sẽ tự lật trong PunchRoutine)
        if (useDefaultCrossPunch)
        {
            windUpShoulder = new Vector3(-30f, 0f, 0f);
            windUpElbow = new Vector3(-90f, 0f, 0f);
            hitShoulder = new Vector3(-80f, -25f, 0f);
            hitElbow = new Vector3(0f, 0f, 0f);

            windUpTime = 0.5f;
            punchTime = 0.4f;
            recoverTime = 0.6f;
        }
    }

    private void OnEnable()
    {
        if (!punchers.Contains(this)) punchers.Add(this);
    }

    private void OnDisable()
    {
        punchers.Remove(this);
    }

    private void Start()
    {
        inputManager = Object.FindFirstObjectByType<InputManager>();
    }

    private void Update()
    {
        bool isAttached = false;
        bool isMovementEnabled = false;

        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) { isAttached = true; if (pm.enabled) isMovementEnabled = true; }

        var rc = GetComponentInParent<RobotController>();
        if (rc != null) { isAttached = true; if (rc.enabled) isMovementEnabled = true; }

        if (!isAttached || !isMovementEnabled) return;
        // Giúp spam click không bị trượt nhịp (Missed clicks)
        if (Input.GetMouseButtonDown(0))
        {
            bufferedPunchTime = Time.time + 0.2f;
        }

        bool isFiring = Time.time <= bufferedPunchTime;

        if (isFiring && Time.time >= globalCooldown && punchers.Count > 0)
        {
            if (punchers.Count >= 2)
            {
                // Thay phiên đấm
                if (punchers[0] == this)
                {
                    bufferedPunchTime = 0f; // Tiêu thụ input
                    TriggerPunch();
                    
                    // Xoay vòng danh sách để đổi lượt cho tay kia
                    punchers.RemoveAt(0);
                    punchers.Add(this);
                    
                    globalCooldown = Time.time + (windUpTime + punchTime + recoverTime * 0.5f); // Nhịp nhanh
                }
            }
            else
            {
                bufferedPunchTime = 0f; // Tiêu thụ input
                // Chỉ có 1 tay đấm -> 1 đấm 1 ngưng
                TriggerPunch();
                globalCooldown = Time.time + (windUpTime + punchTime + recoverTime * 1.5f); // Nhịp chậm hơn do phải chờ nghỉ
            }
        }
    }

    private void LateUpdate()
    {
        if (bones == null || bones.Length < 2) return;

        bool isAttached = false;
        bool isMovementEnabled = false;

        var pm = GetComponentInParent<PlayerMovement>();
        if (pm != null) { isAttached = true; if (pm.enabled) isMovementEnabled = true; }

        var rc = GetComponentInParent<RobotController>();
        if (rc != null) { isAttached = true; if (rc.enabled) isMovementEnabled = true; }

        if (!isAttached || !isMovementEnabled) return;

        if (isPunching)
        {
            if (bones[1] != null)
            {
                Quaternion slashRot = baseRots[1] * currentShoulderTarget;
                bones[1].localRotation = Quaternion.Slerp(bones[1].localRotation, slashRot, punchWeight);
            }

            if (bones.Length > 2 && bones[2] != null)
            {
                Quaternion slashRot = baseRots[2] * currentElbowTarget;
                bones[2].localRotation = Quaternion.Slerp(bones[2].localRotation, slashRot, punchWeight);
            }
        }
    }

    public void TriggerPunch()
    {
        if (isPunching) return;
        StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        isPunching = true;

        Vector3 actualWindUpShoulder = windUpShoulder;
        Vector3 actualSlashShoulder = hitShoulder;
        Vector3 actualWindUpElbow = windUpElbow;
        Vector3 actualSlashElbow = hitElbow;

        // Đảo chiều trục X nếu bị ngược (do config)
        if (invertXAxis)
        {
            actualWindUpShoulder.x = -actualWindUpShoulder.x;
            actualSlashShoulder.x = -actualSlashShoulder.x;
            actualWindUpElbow.x = -actualWindUpElbow.x;
            actualSlashElbow.x = -actualSlashElbow.x;
        }

        // Nhận diện tay Trái/Phải tại thời điểm đấm (chính xác 100% vì tay đã được gắn vào robot)
        Vector3 toArm = transform.position - transform.root.position;
        float dot = Vector3.Dot(toArm, transform.root.right);
        bool isRightArm = dot > 0;
        Debug.Log($"[PunchArmAnimator] PUNCH: {gameObject.name} | dot={dot:F2} | isRightArm={isRightArm}");

        // Nếu là tay Phải: lật Y và Z để đấm đối xứng vào trong (giống SwordArmAnimator)
        if (isRightArm)
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

        if (armAnimator != null) armAnimator.IsPlayingSpecialRoutine = true;

        float elapsed = 0f;
        while (elapsed < windUpTime)
        {
            elapsed += Time.deltaTime;
            punchWeight = Mathf.Clamp01(elapsed / windUpTime);
            currentShoulderTarget = Quaternion.Euler(actualWindUpShoulder);
            currentElbowTarget = Quaternion.Euler(actualWindUpElbow);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < punchTime)
        {
            elapsed += Time.deltaTime;
            punchWeight = Mathf.Clamp01(elapsed / punchTime);
            currentShoulderTarget = Quaternion.Slerp(Quaternion.Euler(actualWindUpShoulder), Quaternion.Euler(actualSlashShoulder), punchWeight);
            currentElbowTarget = Quaternion.Slerp(Quaternion.Euler(actualWindUpElbow), Quaternion.Euler(actualSlashElbow), punchWeight);
            yield return null;
        }
        punchWeight = 1f;

        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float recoverWeight = Mathf.Clamp01(elapsed / recoverTime);
            // Nội suy mượt mà từ tư thế đấm trở về tư thế nghỉ (0,0,0)
            currentShoulderTarget = Quaternion.Slerp(Quaternion.Euler(actualSlashShoulder), Quaternion.identity, recoverWeight);
            currentElbowTarget = Quaternion.Slerp(Quaternion.Euler(actualSlashElbow), Quaternion.identity, recoverWeight);
            
            // Giữ punchWeight = 1 để LateUpdate áp dụng hoàn toàn currentShoulderTarget
            punchWeight = 1f; 
            yield return null;
        }

        punchWeight = 0f;
        isPunching = false;

        if (armAnimator != null) armAnimator.IsPlayingSpecialRoutine = false;
        else 
        {
            // Đảm bảo tay không bị kẹt nếu không có ArmAnimator
            if (bones.Length > 1 && bones[1] != null) bones[1].localRotation = baseRots[1];
            if (bones.Length > 2 && bones[2] != null) bones[2].localRotation = baseRots[2];
        }
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
