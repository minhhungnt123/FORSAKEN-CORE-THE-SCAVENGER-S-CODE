using UnityEngine;

/// <summary>
/// DEPRECATED: Đã được thay thế bởi RobotController (All-in-one).
/// Giữ lại file này dưới dạng file rỗng để tránh lỗi missing script trên các prefab cũ.
/// </summary>
public class HeadAnimator : MonoBehaviour
{
    public void Initialize(RobotAnimatorController ctrl) { }
    public void OnRobotStateChanged(RobotAnimatorController.RobotState newState) { }
    public void TriggerAttack() { }
    public void TriggerAssemble() { }
    public void TriggerCelebrate() { }
}
