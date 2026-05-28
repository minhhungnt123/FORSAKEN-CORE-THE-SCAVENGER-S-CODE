using UnityEngine;

/// <summary>
/// HeadModule: Bộ phận ĐẦU của robot.
/// Khi lắp vào chassis, sẽ thông báo cho RobotAnimatorController để
/// liên kết HeadAnimator và phát animation lắp ráp.
/// </summary>
public class HeadModule : RobotModule
{
    public override string RequiredSocketTag => "HEAD_SOCKET";

    [Header("Animator")]
    [Tooltip("Script HeadAnimator gắn lên mesh con bên trong prefab này")]
    [SerializeField] private HeadAnimator headAnimator;

    protected override void Awake()
    {
        base.Awake();
        moduleName = "Đầu chiến đấu Mk.1";
        moduleDescription = "Đầu tiêu chuẩn của robot chiến đấu";

        // Tự tìm HeadAnimator nếu chưa được assign trong Inspector
        if (headAnimator == null)
            headAnimator = GetComponentInChildren<HeadAnimator>();
    }

    public override void OnAssembled(Transform socket)
    {
        base.OnAssembled(socket);

        // Thông báo cho RobotAnimatorController trên chassis để liên kết animator
        var ctrl = socket.GetComponentInParent<RobotAnimatorController>();
        if (ctrl != null)
            ctrl.OnModuleAttached(this);

        // Phát animation lắp ráp trực tiếp nếu có
        headAnimator?.TriggerAssemble();

        Debug.Log("[HeadModule] Đầu robot đã lắp vào chassis.");
    }

    public override void OnDetached()
    {
        base.OnDetached();

        // Thông báo tháo module
        ChassisModule chassis = FindObjectOfType<ChassisModule>();
        if (chassis != null)
        {
            var ctrl = chassis.GetComponent<RobotAnimatorController>();
            ctrl?.OnModuleDetached(this);
        }

        Debug.Log("[HeadModule] Đầu robot đã được tháo ra.");
    }

    /// <summary>Trả về HeadAnimator của module này.</summary>
    public HeadAnimator GetHeadAnimator() => headAnimator;
}