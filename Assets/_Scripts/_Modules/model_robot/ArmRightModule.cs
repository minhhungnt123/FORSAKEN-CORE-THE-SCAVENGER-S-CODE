using UnityEngine;

/// <summary>
/// ArmRightModule: Bộ phận TAY PHẢI của robot.
/// Khi lắp vào chassis, sẽ thông báo cho RobotAnimatorController để
/// liên kết ArmAnimator (Right) và phát animation lắp ráp.
/// </summary>
public class ArmRightModule : RobotModule
{
    public override string RequiredSocketTag => "ARM_R_SOCKET";

    [Header("Animator")]
    [Tooltip("Script ArmAnimator gắn lên mesh con bên trong prefab này")]
    [SerializeField] private ArmAnimator armAnimator;

    protected override void Awake()
    {
        base.Awake();
        moduleName = "Tay Phải";
        moduleDescription = "Cánh tay phải chiến đấu";

        // Tự tìm ArmAnimator nếu chưa được assign trong Inspector
        if (armAnimator == null)
            armAnimator = GetComponentInChildren<ArmAnimator>();
    }

    public override void OnAssembled(Transform socket)
    {
        base.OnAssembled(socket);

        // Thông báo cho RobotAnimatorController trên chassis
        var ctrl = socket.GetComponentInParent<RobotAnimatorController>();
        if (ctrl != null)
            ctrl.OnModuleAttached(this);

        // Phát animation lắp ráp trực tiếp
        armAnimator?.TriggerAssemble();

        Debug.Log("[ArmRightModule] Tay phải đã lắp vào chassis.");
    }

    public override void OnDetached()
    {
        base.OnDetached();

        ChassisModule chassis = FindObjectOfType<ChassisModule>();
        if (chassis != null)
        {
            var ctrl = chassis.GetComponent<RobotAnimatorController>();
            ctrl?.OnModuleDetached(this);
        }

        Debug.Log("[ArmRightModule] Tay phải đã được tháo ra.");
    }

    /// <summary>Trả về ArmAnimator của module này.</summary>
    public ArmAnimator GetArmAnimator() => armAnimator;
}
