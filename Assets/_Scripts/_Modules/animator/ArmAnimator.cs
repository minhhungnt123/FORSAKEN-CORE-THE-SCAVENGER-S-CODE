using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý animation của tay (đung đưa khi đi bộ/chạy, tấn công, lắp ráp).
/// </summary>
public class ArmAnimator : MonoBehaviour
{
    public enum ArmSide { Left, Right }

    [Header("Cài đặt đung đưa (Swing)")]
    [Tooltip("Trục xoay của vai (trục X để đung đưa tới lui)")]
    public Vector3 swingAxis = new Vector3(1f, 0f, 0f);

    
    [Tooltip("Góc đung đưa tối đa khi đi bộ")]
    public float walkSwingAngle = 25f;
    
    [Tooltip("Góc đung đưa tối đa khi chạy")]
    public float runSwingAngle = 45f;
    
    [Tooltip("Tốc độ đung đưa (nên gần bằng walkFrequency của LegAnimator)")]
    public float swingSpeed = 12f;

    [Header("─ Cài đặt Xương (Bone Settings) ─")]
    [Tooltip("Trục gập của cùi chỏ (Thường là X hoặc Z)")]
    public Vector3 elbowBendAxis = new Vector3(1, 0, 0);
    public float elbowBendMax = 45f;

    // Trạng thái nội bộ
    private ArmSide side;
    private RobotAnimatorController.RobotState currentState = RobotAnimatorController.RobotState.Idle;
    
    private Transform pivotTransform;
    private Quaternion baseRotation;
    
    // Deep Bone Cache
    private Transform[] bones;
    private Quaternion[] baseRots;
    private Quaternion[] currentBoneRots;
    private Quaternion currentPivotRot;

    private float swingPhase = 0f;
    private float blendWeight = 0f;
    
    [HideInInspector]
    public bool IsPlayingSpecialRoutine = false;
    private RobotController robotController;

    private void Awake()
    {
        pivotTransform = transform;
        baseRotation = pivotTransform.localRotation;
        
        // Tự động tìm chuỗi xương (Armature)
        Transform armature = GetFirstChildStartingWith(transform, "Armature");
        if (armature != null && armature.childCount > 0)
        {
            bones = ExtractBoneChain(armature.GetChild(0));
            baseRots = new Quaternion[bones.Length];
            currentBoneRots = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                baseRots[i] = bones[i].localRotation;
                currentBoneRots[i] = baseRots[i];
            }
        }
        currentPivotRot = baseRotation;

        // Tự động nhận diện side nếu chưa được Initialize
        if (gameObject.name.Contains("Left") || (transform.parent != null && transform.parent.name.Contains("Left")))
            side = ArmSide.Left;
        else
            side = ArmSide.Right;
    }

    private void Start()
    {
        robotController = GetComponentInParent<RobotController>();
    }

    public void Initialize(RobotAnimatorController ctrl, ArmSide armSide)
    {
        side = armSide;
        
        // Re-cache base rotation after being parented to socket
        pivotTransform = transform;
        baseRotation = pivotTransform.localRotation;
        currentPivotRot = baseRotation;
        
        // Re-cache bone rotations to ensure perfect synchronization
        if (bones != null)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    baseRots[i] = bones[i].localRotation;
                    currentBoneRots[i] = baseRots[i];
                }
            }
        }
    }

    // Helper: Tìm Armature
    private Transform GetFirstChildStartingWith(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix)) return child;
        }
        return null;
    }

    // Helper: Trích xuất chuỗi xương
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


    public void OnRobotStateChanged(RobotAnimatorController.RobotState newState)
    {
        currentState = newState;
        
        // Bỏ qua các trạng thái đung đưa nếu đang thực hiện hành động đặc biệt
        if (newState == RobotAnimatorController.RobotState.Idle || 
            newState == RobotAnimatorController.RobotState.Jumping)
        {
            // Sẽ dần lerp blendWeight về 0 trong Update
        }
    }

    private void Update()
    {
        if (pivotTransform == null || IsPlayingSpecialRoutine) return;

        // Tự động đồng bộ trạng thái với RobotController (nếu có) để script này hoạt động độc lập
        if (robotController != null)
        {
            if (robotController.IsSprinting) currentState = RobotAnimatorController.RobotState.Running;
            else if (robotController.IsMoving) currentState = RobotAnimatorController.RobotState.Walking;
            else currentState = RobotAnimatorController.RobotState.Idle;
        }

        // Xác định mục tiêu đung đưa
        float targetAngle = 0f;
        float targetBlend = 0f;
        float currentFreq = swingSpeed;

        if (currentState == RobotAnimatorController.RobotState.Walking)
        {
            targetBlend = 1f;
            targetAngle = walkSwingAngle;

            // Nếu đi lùi thì đứng yên tay hoàn toàn
            bool isWalkingBackward = false;
            
            // Tìm InputManager trong scene (tối ưu: có thể cache lại trong Start)
            InputManager inputMgr = Object.FindFirstObjectByType<InputManager>();
            if (inputMgr != null)
            {
                isWalkingBackward = inputMgr.MoveInput.y < -0.1f;
            }
            else
            {
                // Fallback nếu không có InputManager
                isWalkingBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            }

            if (isWalkingBackward)
            {
                targetAngle = 0f; // Về 0 hoàn toàn
            }
        }
        else if (currentState == RobotAnimatorController.RobotState.Running)
        {
            targetBlend = 1f;
            targetAngle = runSwingAngle;
            currentFreq = swingSpeed * 1.3f;
        }

        // Tốc độ chuyển đổi mượt mà giữa đứng yên và đi bộ
        blendWeight = Mathf.MoveTowards(blendWeight, targetBlend, Time.deltaTime * 5f);

        if (blendWeight > 0.01f)
        {
            // Tiếp tục tăng phase
            swingPhase += currentFreq * Time.deltaTime;
        }
        else
        {
            // Trả phase về 0 khi dừng hẳn để lần sau bước luôn bắt đầu từ số 0
            swingPhase = Mathf.Lerp(swingPhase, 0f, Time.deltaTime * 10f);
        }

        // Tính góc xoay hiện tại bằng sóng Sine
        float sinWave = Mathf.Sin(swingPhase);
        
        // Tay trái và phải đung đưa ngược chiều nhau
        if (side == ArmSide.Left) sinWave = -sinWave;

        float currentRotAngle = sinWave * targetAngle * blendWeight;

        // Nếu có chuỗi xương: Giữ nguyên Xương [0] (Khối vai) để không bị hở, chỉ vung Xương [1] (Bắp tay)
        if (bones != null && bones.Length > 1)
        {
            if (bones[1] != null)
            {
                currentBoneRots[1] = Quaternion.Slerp(
                    currentBoneRots[1], 
                    baseRots[1] * Quaternion.Euler(swingAxis * currentRotAngle), 
                    Time.deltaTime * 15f);
                bones[1].localRotation = currentBoneRots[1];
            }

            // Gập cùi chỏ Xương [2]
            if (bones.Length > 2 && bones[2] != null)
            {
                float elbowBend = Mathf.Abs(currentRotAngle) * 0.6f;
                elbowBend = Mathf.Clamp(elbowBend, 0f, elbowBendMax);

                currentBoneRots[2] = Quaternion.Slerp(
                    currentBoneRots[2], 
                    baseRots[2] * Quaternion.Euler(elbowBendAxis * elbowBend), 
                    Time.deltaTime * 15f);
                bones[2].localRotation = currentBoneRots[2];
            }
        }
        else
        {
            // Fallback: Nếu không có xương con, xoay toàn bộ cụm
            currentPivotRot = Quaternion.Slerp(
                currentPivotRot,
                baseRotation * Quaternion.Euler(swingAxis * currentRotAngle),
                Time.deltaTime * 15f);
            pivotTransform.localRotation = currentPivotRot;
        }
    }

    // ─────────────────────────────────────────────
    //  CÁC ANIMATION ĐẶC BIỆT
    // ─────────────────────────────────────────────
    
    public void TriggerAttack()
    {
        if (IsPlayingSpecialRoutine) return;
        StartCoroutine(AttackRoutine());
    }

    public void TriggerAssemble()
    {
        if (IsPlayingSpecialRoutine) return;
        StartCoroutine(AssembleRoutine());
    }

    public void TriggerCelebrate()
    {
        if (IsPlayingSpecialRoutine) return;
        StartCoroutine(CelebrateRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        IsPlayingSpecialRoutine = true;
        float duration = 0.3f;
        float elapsed = 0f;
        
        // Vung tay chém (xoay trục X âm)
        float attackAngle = -70f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Nhanh lên chậm xuống
            float curve = Mathf.Sin(t * Mathf.PI);
            
            pivotTransform.localRotation = baseRotation * Quaternion.Euler(swingAxis * (attackAngle * curve));
            yield return null;
        }

        pivotTransform.localRotation = baseRotation;
        IsPlayingSpecialRoutine = false;
    }

    private IEnumerator AssembleRoutine()
    {
        IsPlayingSpecialRoutine = true;
        Vector3 targetScale = pivotTransform.localScale;
        pivotTransform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Hiệu ứng nảy (overshoot)
            float scale = Mathf.Clamp01(t * 1.5f);
            pivotTransform.localScale = targetScale * scale;
            yield return null;
        }
        
        pivotTransform.localScale = targetScale;
        IsPlayingSpecialRoutine = false;
    }

    private IEnumerator CelebrateRoutine()
    {
        IsPlayingSpecialRoutine = true;
        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Giơ tay lên ăn mừng (xoay trục X -120 độ)
            float t = Mathf.PingPong(elapsed * 2f, 1f);
            float angle = Mathf.Lerp(0f, -120f, t);
            
            pivotTransform.localRotation = baseRotation * Quaternion.Euler(swingAxis * angle);
            yield return null;
        }

        pivotTransform.localRotation = baseRotation;
        IsPlayingSpecialRoutine = false;
    }
}
