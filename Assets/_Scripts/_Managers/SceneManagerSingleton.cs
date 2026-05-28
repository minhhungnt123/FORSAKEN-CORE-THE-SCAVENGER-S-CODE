using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Scene Manager - Quản lý chuyển đổi scene với loading effect
/// - Fade in/out transition
/// - Loading progress (nếu scene nặng)
/// - Persistent between scenes
/// </summary>
public class SceneManagerSingleton : MonoBehaviour
{
    public static SceneManagerSingleton Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "Gameplay";

    [Header("Transition Settings")]
    public float fadeDuration = 1f;              // Thời gian fade
    public Color fadeColor = Color.black;        // Màu fade
    public bool useLoadingScreen = false;        // Dùng loading screen?

    [Header("UI References")]
    public CanvasGroup fadeCanvasGroup;          // Canvas để fade
    public Image loadingBar;                     // Progress bar (optional)

    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("✅ [SceneManager] Singleton initialized!", gameObject);
    }

    void Start()
    {
        // Tạo fade canvas nếu chưa có
        if (fadeCanvasGroup == null)
        {
            CreateFadeCanvas();
        }
    }

    /// <summary>
    /// Load scene với fade transition
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;

        Debug.Log($"📂 [SceneManager] Loading scene: {sceneName}", gameObject);
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    /// <summary>
    /// Load Main Menu
    /// </summary>
    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Load Gameplay
    /// </summary>
    public void LoadGameplay()
    {
        LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Restart current scene
    /// </summary>
    public void RestartScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    /// <summary>
    /// Coroutine: Load scene với fade
    /// </summary>
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(Fade(1f)); // 0 → 1 (mất)

        // Load scene
        Debug.Log($"🔄 [SceneManager] Scene đang load: {sceneName}");
        
        if (useLoadingScreen)
        {
            yield return StartCoroutine(LoadSceneAsyncWithProgress(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
            yield return null; // Chờ frame
        }

        // Fade from black
        yield return StartCoroutine(Fade(0f)); // 1 → 0 (sáng)

        isTransitioning = false;
        Debug.Log($"✅ [SceneManager] Scene loaded: {sceneName}", gameObject);
    }

    /// <summary>
    /// Load scene async (hiển thị progress)
    /// </summary>
    private IEnumerator LoadSceneAsyncWithProgress(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            // Progress: 0 → 0.9 (Unity giữ 0.9 cho finalization)
            float progress = asyncLoad.progress / 0.9f;
            
            if (loadingBar != null)
            {
                loadingBar.fillAmount = progress;
            }

            Debug.Log($"📊 [SceneManager] Loading progress: {progress * 100:F0}%");
            yield return null;
        }

        if (loadingBar != null)
        {
            loadingBar.fillAmount = 1f;
        }
    }

    /// <summary>
    /// Fade canvas
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// Tạo fade canvas nếu không có
    /// </summary>
    private void CreateFadeCanvas()
    {
        Debug.Log("🎨 [SceneManager] Tạo fade canvas...", gameObject);

        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;

        // Tạo image toàn màn hình
        Image fadeImage = canvasObj.AddComponent<Image>();
        fadeImage.color = fadeColor;

        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log("✅ [SceneManager] Fade canvas created!", canvasObj);
    }
}
