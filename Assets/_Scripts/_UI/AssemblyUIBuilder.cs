using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AssemblyUIBuilder : MonoBehaviour
{
    [Header("Canvas Settings")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);

    [Header("Button Colors")]
    [SerializeField] private Color typeButtonColor = new Color(0.18f, 0.35f, 0.55f);
    [SerializeField] private Color modelButtonColor = new Color(0.2f, 0.55f, 0.75f);
    [SerializeField] private Color confirmColor = new Color(0.2f, 0.7f, 0.3f);
    [SerializeField] private Color resetColor = new Color(0.75f, 0.55f, 0.2f);
    [SerializeField] private Color exitColor = new Color(0.75f, 0.2f, 0.2f);

    private Font defaultFont;
    private Transform modelListContent;
    private AssemblyModuleType currentType = AssemblyModuleType.Chassis;

    private void Start()
    {
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        if (mainCanvas == null)
        {
            Debug.LogError("[AssemblyUIBuilder] Không tìm thấy Canvas.");
            return;
        }

        SetupCanvas();
        SetupEventSystem();
        BuildUI();

        Debug.Log("[AssemblyUIBuilder] Assembly UI đã được tạo.");
    }

    private void SetupCanvas()
    {
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
    }

    private void BuildUI()
    {
        foreach (Transform child in mainCanvas.transform)
        {
            Destroy(child.gameObject);
        }

        CreateTopPanel();
        CreateLeftPanel();
        CreatePreviewPanel();
        CreateBottomPanel();

        RefreshModelList(AssemblyModuleType.Chassis);
    }

    private void CreateTopPanel()
    {
        GameObject panel = CreatePanel("TopPanel", mainCanvas.transform, panelColor);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(0, -90);
        rect.offsetMax = new Vector2(0, 0);

        Text title = CreateText("Title", panel.transform, "PHÒNG LẮP RÁP ROBOT", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private void CreateLeftPanel()
    {
        GameObject panel = CreatePanel("LeftPanel", mainCanvas.transform, panelColor);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(330, 0);
        rect.offsetMin = new Vector2(0, 80);
        rect.offsetMax = new Vector2(330, -90);

        Text typeTitle = CreateText("TypeTitle", panel.transform, "LOẠI LINH KIỆN", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(typeTitle.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 0));

        GameObject typeList = new GameObject("TypeList");
        typeList.transform.SetParent(panel.transform, false);

        RectTransform typeListRect = typeList.AddComponent<RectTransform>();
        typeListRect.anchorMin = new Vector2(0, 1);
        typeListRect.anchorMax = new Vector2(1, 1);
        typeListRect.pivot = new Vector2(0.5f, 1);
        typeListRect.offsetMin = new Vector2(15, -330);
        typeListRect.offsetMax = new Vector2(-15, -60);

        VerticalLayoutGroup typeLayout = typeList.AddComponent<VerticalLayoutGroup>();
        typeLayout.spacing = 8;
        typeLayout.childControlWidth = true;
        typeLayout.childControlHeight = false;
        typeLayout.childForceExpandWidth = true;
        typeLayout.childForceExpandHeight = false;

        CreateTypeButton(typeList.transform, "THÂN", AssemblyModuleType.Chassis);
        CreateTypeButton(typeList.transform, "ĐẦU", AssemblyModuleType.Head);
        CreateTypeButton(typeList.transform, "TAY TRÁI", AssemblyModuleType.ArmLeft);
        CreateTypeButton(typeList.transform, "TAY PHẢI", AssemblyModuleType.ArmRight);
        CreateTypeButton(typeList.transform, "CHÂN", AssemblyModuleType.Leg);

        Text modelTitle = CreateText("ModelTitle", panel.transform, "MODEL", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(modelTitle.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -390), new Vector2(0, -340));

        GameObject modelList = new GameObject("ModelList");
        modelList.transform.SetParent(panel.transform, false);

        RectTransform modelListRect = modelList.AddComponent<RectTransform>();
        modelListRect.anchorMin = new Vector2(0, 0);
        modelListRect.anchorMax = new Vector2(1, 1);
        modelListRect.offsetMin = new Vector2(15, 15);
        modelListRect.offsetMax = new Vector2(-15, -400);

        VerticalLayoutGroup modelLayout = modelList.AddComponent<VerticalLayoutGroup>();
        modelLayout.spacing = 8;
        modelLayout.childControlWidth = true;
        modelLayout.childControlHeight = false;
        modelLayout.childForceExpandWidth = true;
        modelLayout.childForceExpandHeight = false;

        modelListContent = modelList.transform;
    }

    private void CreatePreviewPanel()
    {
        GameObject panel = CreatePanel("RobotPreviewPanel", mainCanvas.transform, new Color(0.05f, 0.05f, 0.07f, 0.3f));

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(340, 90);
        rect.offsetMax = new Vector2(-10, -100);

        Text placeholder = CreateText("PreviewText", panel.transform, "ROBOT PREVIEW AREA", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        placeholder.color = new Color(1, 1, 1, 0.35f);

        RectTransform textRect = placeholder.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateBottomPanel()
    {
        GameObject panel = CreatePanel("BottomPanel", mainCanvas.transform, panelColor);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.offsetMin = new Vector2(340, 10);
        rect.offsetMax = new Vector2(-10, 75);

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateControlButton(panel.transform, "XÁC NHẬN", confirmColor, () =>
        {
            Debug.Log("[AssemblyUIBuilder] Confirm clicked.");
            ModuleSelector selector = FindFirstObjectByType<ModuleSelector>();
            if (selector != null)
                selector.ConfirmAssembly();
        });

        CreateControlButton(panel.transform, "RESET", resetColor, () =>
        {
            Debug.Log("[AssemblyUIBuilder] Reset clicked.");
            RobotAssemblyManager manager = FindFirstObjectByType<RobotAssemblyManager>();
            if (manager != null)
                manager.ResetAssembly();
        });

        CreateControlButton(panel.transform, "THOÁT", exitColor, () =>
        {
            Debug.Log("[AssemblyUIBuilder] Exit clicked.");
            RobotAssemblyManager manager = FindFirstObjectByType<RobotAssemblyManager>();
            if (manager != null)
                manager.ExitAssembly();
        });
    }

    private void CreateTypeButton(Transform parent, string label, AssemblyModuleType type)
    {
        Button button = CreateButton(parent, label, typeButtonColor, 290, 45);
        button.onClick.AddListener(() =>
        {
            currentType = type;
            RefreshModelList(type);
            Debug.Log($"[AssemblyUIBuilder] Selected category: {type}");
        });
    }

    private void RefreshModelList(AssemblyModuleType type)
    {
        if (modelListContent == null)
            return;

        foreach (Transform child in modelListContent)
        {
            Destroy(child.gameObject);
        }

        string legacyTag = GetLegacyModuleTag(type);
        ModuleSelector selector = FindFirstObjectByType<ModuleSelector>();
        
        if (selector == null)
        {
            Debug.LogError("[AssemblyUIBuilder] Không tìm thấy ModuleSelector.");
            return;
        }

        int count = selector.GetModuleCount(legacyTag);
        if (count == 0)
        {
            // Hiển thị nút trống nếu không có bộ phận nào
            Button lockedButton = CreateButton(
                modelListContent,
                $"{GetTypeDisplayName(type)} (Empty)",
                Color.gray,
                290,
                45
            );
            lockedButton.interactable = false;
            return;
        }

        // Tạo nút bấm cho từng lựa chọn có sẵn trong danh sách
        for (int i = 0; i < count; i++)
        {
            string moduleName = selector.GetModulePrefabName(legacyTag, i);
            int index = i; // Lưu biến cục bộ cho lambda

            Button modelButton = CreateButton(
                modelListContent,
                moduleName,
                modelButtonColor,
                290,
                45
            );

            modelButton.onClick.AddListener(() =>
            {
                Debug.Log($"[AssemblyUIBuilder] Selected module: {moduleName}");
                selector.SelectModule(legacyTag, index);
            });
        }
    }

    private void CreateControlButton(Transform parent, string label, Color color, System.Action callback)
    {
        Button button = CreateButton(parent, label, color, 170, 45);
        button.onClick.AddListener(() => callback?.Invoke());
    }

    private Button CreateButton(Transform parent, string label, Color color, float width, float height)
    {
        GameObject buttonObj = new GameObject($"Button_{label}");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minHeight = height;

        Image image = buttonObj.AddComponent<Image>();
        image.color = color;

        Button button = buttonObj.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(
            Mathf.Clamp01(color.r * 1.2f),
            Mathf.Clamp01(color.g * 1.2f),
            Mathf.Clamp01(color.b * 1.2f)
        );
        colors.pressedColor = new Color(
            Mathf.Clamp01(color.r * 0.8f),
            Mathf.Clamp01(color.g * 0.8f),
            Mathf.Clamp01(color.b * 0.8f)
        );
        button.colors = colors;

        Text text = CreateText("Text", buttonObj.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        obj.AddComponent<RectTransform>();

        Image image = obj.AddComponent<Image>();
        image.color = color;

        return obj;
    }

    private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        obj.AddComponent<RectTransform>();

        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = defaultFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;

        return text;
    }

    private void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
    private string GetLegacyModuleTag(AssemblyModuleType type)
    {
        return type switch
        {
            AssemblyModuleType.Chassis => "CHASSIS",
            AssemblyModuleType.Head => "HEAD",
            AssemblyModuleType.ArmLeft => "ARM_LEFT",
            AssemblyModuleType.ArmRight => "ARM_RIGHT",
            AssemblyModuleType.Leg => "LEG",
            _ => ""
        };
    }

    private string GetTypeDisplayName(AssemblyModuleType type)
    {
        return type switch
        {
            AssemblyModuleType.Chassis => "Chassis",
            AssemblyModuleType.Head => "Head",
            AssemblyModuleType.ArmLeft => "Arm Left",
            AssemblyModuleType.ArmRight => "Arm Right",
            AssemblyModuleType.Leg => "Leg",
            _ => "Module"
        };
    }
}