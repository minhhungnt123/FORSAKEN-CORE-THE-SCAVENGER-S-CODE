using UnityEngine;

// Gan script nay vao ca hai FBX fighting_arm_left_1 va fighting_arm_right_1
// Chi can doi truong armSide trong Inspector
public class ArmModule : RobotModule
{
    public enum Side { Left, Right }

    [Header("Tay Trai hay Tay Phai?")]
    [SerializeField] private Side armSide = Side.Left;

    // Tag tu dong doi theo Side duoc chon trong Inspector
    public override string RequiredSocketTag =>
        armSide == Side.Left ? "ARM_L_SOCKET" : "ARM_R_SOCKET";

    protected override void Awake()
    {
        base.Awake();
        moduleName = armSide == Side.Left ? "Tay Trai" : "Tay Phai";
        moduleDescription = "Canh tay chien dau co kha nang lap vu khi";
    }
}