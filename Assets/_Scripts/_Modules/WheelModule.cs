using UnityEngine;

public class WheelModule : RobotModule
{
    [Header("Wheel Specs")]
    public float motorTorque = 150f;

    protected override void Awake()
    {
        base.Awake(); // Gọi hàm Awake của class cha để lấy Rigidbody
        moduleName = "Bánh xe địa hình";
    }

    // Hàm nhận lệnh di chuyển, tuân thủ tuyệt đối việc dùng Lực (Torque)
    public void ApplyMotorForce(float input)
    {
        // Chú ý: Trong thực tế bạn có thể dùng WheelCollider hoặc HingeJoint motor
        rb.AddTorque(transform.right * input * motorTorque, ForceMode.Acceleration);
    }
}