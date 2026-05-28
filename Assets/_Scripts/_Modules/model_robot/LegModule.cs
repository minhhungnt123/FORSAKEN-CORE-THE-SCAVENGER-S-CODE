using UnityEngine;

/// <summary>
/// LegModule: Bộ phận CHÂN của robot.
/// Khi lắp vào chassis, sẽ thông báo cho RobotAnimatorController để
/// liên kết LegAnimator và phát animation lắp ráp.
/// </summary>
public class LegModule : RobotModule
{
    public override string RequiredSocketTag => "LEG_SOCKET";

    [Header("Leg Stats")]
    public float moveSpeedBonus = 0f;

    [Header("Animator")]
    [Tooltip("Script LegAnimator gắn lên mesh con bên trong prefab này")]
    [SerializeField] private LegAnimator legAnimator;

    protected override void Awake()
    {
        base.Awake();
        moduleName = "Chân địa hình";
        moduleDescription = "Chân vững chắc, di chuyển tốt trên mọi địa hình";

        // Tự tìm LegAnimator nếu chưa được assign trong Inspector
        if (legAnimator == null)
            legAnimator = GetComponentInChildren<LegAnimator>();
    }

    public override void OnAssembled(Transform socket)
    {
        base.OnAssembled(socket);

        // Thông báo cho RobotAnimatorController trên chassis
        var ctrl = socket.GetComponentInParent<RobotAnimatorController>();
        if (ctrl != null)
            ctrl.OnModuleAttached(this);

        // Phát animation lắp ráp trực tiếp
        legAnimator?.TriggerAssemble();

        Debug.Log("[LegModule] Chân robot đã lắp vào chassis.");
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

        Debug.Log("[LegModule] Chân robot đã được tháo ra.");
    }

    /// <summary>Trả về LegAnimator của module này.</summary>
    public LegAnimator GetLegAnimator() => legAnimator;
}