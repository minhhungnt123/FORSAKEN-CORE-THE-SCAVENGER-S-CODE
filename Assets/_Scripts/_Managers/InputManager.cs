using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference MoveAction;
    public InputActionReference JumpAction;
    public InputActionReference LookAction;
    public InputActionReference LockOnAction;
    public InputActionReference SprintAction;
    public InputActionReference InteractAction;
    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsLockOn { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool InteractTriggered { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        if(MoveAction != null) MoveAction.action.Enable();
        if(JumpAction != null) JumpAction.action.Enable();
        if(LookAction != null) LookAction.action.Enable();
        if(LockOnAction != null) LockOnAction.action.Enable();
        if(SprintAction != null) SprintAction.action.Enable();
        if(InteractAction != null) InteractAction.action.Enable();
    }

    private void OnDisable()
    {
        if(MoveAction != null) { 
            MoveAction.action.Disable(); 
        }
        if(JumpAction != null) { 
            JumpAction.action.Disable(); 
        }
        if(LookAction != null) { 
            LookAction.action.Disable();
        }
        if(LockOnAction != null ){ 
            LockOnAction.action.Disable();
        }
        if (SprintAction != null) {
            SprintAction.action.Disable();
        }
        if (InteractAction != null) {
            InteractAction.action.Disable();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (MoveAction != null) {
            MoveInput = MoveAction.action.ReadValue<Vector2>();
        }
        if (JumpAction != null) { 
            JumpTriggered = JumpAction.action.WasPressedThisFrame();
        }
        if (LookAction != null) { 
            LookInput = LookAction.action.ReadValue<Vector2>();
        }
        if (LockOnAction != null) { 
            IsLockOn = LockOnAction.action.IsPressed();
        }
        if (SprintAction != null) { 
            IsSprinting = SprintAction.action.IsPressed();
        }
        if (InteractAction != null) { 
            InteractTriggered = InteractAction.action.WasPressedThisFrame();
        }
    }
}
