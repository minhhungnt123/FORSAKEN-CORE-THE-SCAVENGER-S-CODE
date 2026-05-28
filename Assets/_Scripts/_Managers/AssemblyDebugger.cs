using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// AssemblyDebugger: Helper script để debug scene lắp ráp
/// - Hiển thị trạng thái hiện tại
/// - Kiểm tra config
/// - In ra console
/// </summary>
public class AssemblyDebugger : MonoBehaviour
{
    [SerializeField] private bool showDebugPanel = true;
    [SerializeField] private Text debugText;

    private RobotAssemblyManager assemblyManager;
    private ModuleSelector moduleSelector;
    private List<string> debugLogs = new();

    private void Start()
    {
        assemblyManager = FindObjectOfType<RobotAssemblyManager>();
        moduleSelector = FindObjectOfType<ModuleSelector>();

        CheckConfiguration();
        CreateDebugPanel();
    }

    private void Update()
    {
        if (showDebugPanel && debugText != null)
        {
            UpdateDebugDisplay();
        }

        // Phím tắt
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugPanel = !showDebugPanel;
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            PrintConfiguration();
        }
    }

    private void CheckConfiguration()
    {
        debugLogs.Clear();

        if (assemblyManager == null)
            debugLogs.Add("❌ RobotAssemblyManager: NOT FOUND");
        else
            debugLogs.Add("✓ RobotAssemblyManager: OK");

        if (moduleSelector == null)
            debugLogs.Add("❌ ModuleSelector: NOT FOUND");
        else
            debugLogs.Add("✓ ModuleSelector: OK");

        if (FindObjectOfType<Canvas>() == null)
            debugLogs.Add("❌ Canvas: NOT FOUND");
        else
            debugLogs.Add("✓ Canvas: OK");

        var chassis = moduleSelector?.GetCurrentChassis();
        if (chassis == null)
            debugLogs.Add("⊙ Chassis: NOT CREATED YET");
        else
            debugLogs.Add("✓ Chassis: CREATED");

        Debug.Log("[AssemblyDebugger] Configuration Check:\n" + string.Join("\n", debugLogs));
    }

    private void CreateDebugPanel()
    {
        if (!showDebugPanel || debugText != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[AssemblyDebugger] Canvas not found!");
            return;
        }

        // Tạo panel
        GameObject panelObj = new GameObject("DebugPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(-300, -200);
        rectTransform.offsetMax = Vector2.zero;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // Tạo text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.one * 5;
        textRect.offsetMax = -Vector2.one * 5;

        debugText = textObj.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 11;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.color = Color.green;
        debugText.text = "[DEBUG]\nF1: Toggle Debug\nF2: Print Config";
    }

    private void UpdateDebugDisplay()
    {
        if (debugText == null) return;

        string text = "[DEBUG PANEL - F1 to toggle]\n\n";

        var selectedModules = moduleSelector?.GetSelectedModules();
        if (selectedModules != null)
        {
            text += $"Selected Modules: {selectedModules.Count}\n";
            foreach (var kvp in selectedModules)
            {
                string status = kvp.Value != null ? "✓" : "✗";
                text += $"  {status} {kvp.Key}\n";
            }
        }

        var chassis = moduleSelector?.GetCurrentChassis();
        if (chassis != null)
        {
            text += $"\nChassis Mass: {chassis.moduleMass} kg\n";
            text += $"Total Mass: {chassis.GetTotalMass()} kg\n";
            text += $"Fully Assembled: {(chassis.IsFullyAssembled() ? "✓ YES" : "✗ NO")}\n";
        }

        text += $"\nAssembly Complete: {(assemblyManager?.IsAssemblyComplete() ?? false ? "✓ YES" : "✗ NO")}\n";

        text += "\n--- Shortcuts ---\n";
        text += "F1: Toggle Debug\n";
        text += "F2: Print Config\n";
        text += "Delete: Remove module\n";
        text += "RMB+Drag: Rotate camera\n";

        debugText.text = text;
    }

    private void PrintConfiguration()
    {
        Debug.Log("=== ASSEMBLY SCENE CONFIGURATION ===\n");

        Debug.Log("Managers:");
        Debug.Log($"  - RobotAssemblyManager: {(assemblyManager != null ? "✓ Found" : "✗ NOT FOUND")}");
        Debug.Log($"  - ModuleSelector: {(moduleSelector != null ? "✓ Found" : "✗ NOT FOUND")}");
        Debug.Log($"  - AssemblyUIBuilder: {(FindObjectOfType<AssemblyUIBuilder>() != null ? "✓ Found" : "✗ NOT FOUND")}");

        Debug.Log("\nScene Objects:");
        Debug.Log($"  - Canvas: {(FindObjectOfType<Canvas>() != null ? "✓ Found" : "✗ NOT FOUND")}");
        Debug.Log($"  - Main Camera: {(Camera.main != null ? "✓ Found" : "✗ NOT FOUND")}");
        Debug.Log($"  - Directional Light: {(FindObjectOfType<Light>() != null ? "✓ Found" : "✗ NOT FOUND")}");

        Debug.Log("\nModule Prefabs:");
        var selector = FindObjectOfType<ModuleSelector>();
        if (selector != null)
        {
            var fields = selector.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance
            );

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(RobotModule)))
                {
                    var prefab = field.GetValue(selector);
                    Debug.Log($"  - {field.Name}: {(prefab != null ? "✓ Assigned" : "✗ NOT ASSIGNED")}");
                }
            }
        }

        Debug.Log("\n=== END CONFIGURATION ===");
    }

    /// <summary>
    /// Ghi log có timestamp
    /// </summary>
    public static void Log(string message)
    {
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss}] {message}");
    }
}
