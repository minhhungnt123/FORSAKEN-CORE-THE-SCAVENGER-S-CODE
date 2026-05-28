// FILE NÀY ĐÃ ĐƯỢC THAY THẾ BỞI RobotController.cs
// Giữ lại file rỗng để tránh lỗi reference trong scene.
using UnityEngine;

/// <summary>DEPRECATED – Dùng RobotController.cs thay thế.</summary>
public class RobotProceduralAnimator : MonoBehaviour
{
    // Các public field/method giả để các script khác không bị lỗi compile
    public float CurrentBlend => 0f;

    public void SetupParts(Transform body, Transform head,
                           Transform armLeft, Transform armRight, Transform leg) { }

    public void RefreshBaseTransforms() { }

    public void AutoDiscoverPartsFromChassis() { }
}
