using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AssemblySceneSetup: Editor tool để tự động setup Assembly Scene
/// - Tạo tất cả GameObject cần thiết
/// - Gắn component
/// - Config cơ bản
/// 
/// Sử dụng: Menu → Forsaken Core → Setup Assembly Scene
/// </summary>
public class AssemblySceneSetup : MonoBehaviour
{
    [MenuItem("Forsaken Core/Setup Assembly Scene")]
    public static void SetupScene()
    {
        Debug.Log("[AssemblySceneSetup] Bắt đầu setup Assembly Scene...");

        // Xóa scene cũ
        GameObject[] allGOs = FindObjectsOfType<GameObject>();
        foreach (GameObject go in allGOs)
        {
            if (go.transform.parent == null && go.name != "AssemblyManager")
                DestroyImmediate(go);
        }

        // Tạo Main Camera
        CreateMainCamera();

        // Tạo Lighting
        CreateLighting();

        // Tạo Floor
        CreateFloor();

        // Tạo Canvas
        CreateCanvas();

        // Tạo AssemblyManager
        CreateAssemblyManager();

        Debug.Log("[AssemblySceneSetup] ✓ Setup hoàn tất!");
        EditorUtility.SetDirty(FindObjectOfType<Canvas>());
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";

        cameraObj.transform.position = new Vector3(0, 2, -8);
        cameraObj.transform.rotation = Quaternion.Euler(15, 0, 0);

        AudioListener audioListener = cameraObj.AddComponent<AudioListener>();

        Debug.Log("✓ Main Camera created");
    }

    private static void CreateLighting()
    {
        GameObject lightObj = new GameObject("Main Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;

        lightObj.transform.rotation = Quaternion.Euler(45, 45, 0);

        Debug.Log("✓ Main Light created");
    }

    private static void CreateFloor()
    {
        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorObj.name = "Floor";

        floorObj.transform.position = new Vector3(0, -1, 0);
        floorObj.transform.localScale = new Vector3(10, 1, 10);

        // Xóa collider gốc
        DestroyImmediate(floorObj.GetComponent<BoxCollider>());

        // Tạo material
        Material floorMat = new Material(Shader.Find("Standard"));
        floorMat.color = new Color(0.5f, 0.5f, 0.5f);

        Renderer renderer = floorObj.GetComponent<Renderer>();
        renderer.material = floorMat;

        Debug.Log("✓ Floor created");
    }

    private static void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log("✓ Canvas created");
    }

    private static void CreateAssemblyManager()
    {
        GameObject managerObj = new GameObject("AssemblyManager");

        // Gắn components
        RobotAssemblyManager assemblyMgr = managerObj.AddComponent<RobotAssemblyManager>();
        ModuleSelector selector = managerObj.AddComponent<ModuleSelector>();
        AssemblyUIBuilder uiBuilder = managerObj.AddComponent<AssemblyUIBuilder>();
        AssemblyDebugger debugger = managerObj.AddComponent<AssemblyDebugger>();

        // Config UI references
        Canvas canvas = FindObjectOfType<Canvas>();

        // SerializedObject để config properties
        SerializedObject assemblySerialObj = new SerializedObject(assemblyMgr);

        // Tìm hoặc tạo các text objects
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvas.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "PHÒNG CHẾ TẠO ROBOT";
        titleText.alignment = TextAnchor.MiddleCenter;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(0, -50);
        titleRect.offsetMax = Vector2.zero;

        // Instruction text
        GameObject instructionObj = new GameObject("Instruction");
        instructionObj.transform.SetParent(canvas.transform);
        Text instructionText = instructionObj.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.text = "Chọn các linh kiện và xác nhận";
        instructionText.alignment = TextAnchor.MiddleCenter;

        RectTransform instructionRect = instructionObj.GetComponent<RectTransform>();
        instructionRect.anchorMin = Vector2.zero;
        instructionRect.anchorMax = Vector2.one;

        // Gắn references
        assemblySerialObj.FindProperty("titleText").objectReferenceValue = titleText;
        assemblySerialObj.FindProperty("instructionText").objectReferenceValue = instructionText;
        assemblySerialObj.FindProperty("mainCanvas").objectReferenceValue = canvas;
        assemblySerialObj.ApplyModifiedProperties();

        SerializedObject selectorSerialObj = new SerializedObject(selector);
        selectorSerialObj.FindProperty("modulesListContent").objectReferenceValue = canvas.transform;
        selectorSerialObj.FindProperty("selectedModulesText").objectReferenceValue = instructionText;
        selectorSerialObj.ApplyModifiedProperties();

        SerializedObject uiSerialObj = new SerializedObject(uiBuilder);
        uiSerialObj.FindProperty("mainCanvas").objectReferenceValue = canvas;
        uiSerialObj.ApplyModifiedProperties();

        Debug.Log("✓ AssemblyManager created and configured");
    }
}

#endif
