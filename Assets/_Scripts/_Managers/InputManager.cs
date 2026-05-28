using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input Actions")]
    public InputActionReference MoveAction;
    public InputActionReference JumpAction;
    public InputActionReference LookAction;
    public InputActionReference LockOnAction;
    public InputActionReference SprintAction;
    public InputActionReference InteractAction;
    public InputActionReference FireAction;
    public InputActionReference InventoryAction;

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsLockOn { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool InteractTriggered { get; private set; }
    public bool FireTriggered { get; private set; }   // True trong frame bấm chuột phải
    public bool IsHoldingFire { get; private set; }  // True khi giữ chuột phải
    public bool InventoryTriggered { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        if(MoveAction != null) MoveAction.action.Enable();
        if(JumpAction != null) JumpAction.action.Enable();
        if(LookAction != null) LookAction.action.Enable();
        if(LockOnAction != null) LockOnAction.action.Enable();
        if(SprintAction != null) SprintAction.action.Enable();
        if(InteractAction != null) InteractAction.action.Enable();
        if(FireAction != null) FireAction.action.Enable();
        if(InventoryAction != null) InventoryAction.action.Enable();
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
        if (FireAction != null) {
            FireAction.action.Disable();
        if (InventoryAction != null) {
            InventoryAction.action.Disable();
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
        // Chuột phải: bắn súng
        if (FireAction != null)
        {
            FireTriggered    = FireAction.action.WasPressedThisFrame();
            IsHoldingFire    = FireAction.action.IsPressed();
        }
        else
        {
            // Fallback: Chuột phải (1) để nhắm, Chuột trái (0) để bắn
            FireTriggered    = Input.GetMouseButtonDown(0);
            IsHoldingFire    = Input.GetMouseButton(1);
        }
        if (InventoryAction != null) {
            InventoryTriggered = InventoryAction.action.WasPressedThisFrame();
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái hiển thị và khóa của chuột.
    /// </summary>
    public void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    /// <summary>
    /// Lấy chuỗi mô tả phím bấm tương tác động và rút gọn tối đa (ví dụ: "E").
    /// </summary>
    public string GetInteractBindingName()
    {
        if (InteractAction != null && InteractAction.action != null)
        {
            // Lấy chuỗi hiển thị phím từ Input System (ví dụ: "E")
            string bindingString = InteractAction.action.GetBindingDisplayString();
            if (string.IsNullOrEmpty(bindingString)) return "E";

            // 1. Tách các binding song song (ngăn cách bởi dấu '|') và chọn cái đầu tiên (thường là Keyboard)
            string firstBinding = bindingString;
            if (bindingString.Contains("|"))
            {
                string[] bindings = bindingString.Split('|');
                if (bindings.Length > 0)
                {
                    firstBinding = bindings[0];
                }
            }

            // 2. Loại bỏ các từ khóa hành vi của Input System như "Hold", "Press", "Tap" (không phân biệt hoa thường)
            string cleaned = System.Text.RegularExpressions.Regex.Replace(firstBinding, @"(?i)\b(hold|press|tap)\b\s*", "");

            // 3. Loại bỏ thông tin thiết bị trong ngoặc nếu có (VD: "E (Keyboard)" -> "E")
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*[\(\[][^\]\)]*[\)\]]\s*", "");

            // 4. Viết hoa và xóa khoảng trắng thừa
            return cleaned.ToUpper().Trim();
        }
        return "E"; // Fallback mặc định
    }
}

