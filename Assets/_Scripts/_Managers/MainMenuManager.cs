using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý Main Menu Screen
/// - Play Game (Chuyển đến scene chơi)
/// - Settings (Mở settings)
/// - Credits (Xem credits)
/// - Quit (Thoát game)
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup mainMenuPanel;               // Panel main menu
    public CanvasGroup settingsPanel;               // Panel settings
    public CanvasGroup creditsPanel;                // Panel credits

    void Start()
    {
        Debug.Log("🏠 [MainMenuManager] Main Menu khởi động!", gameObject);

        // Đảm bảo main menu hiển thị
        if (mainMenuPanel != null) mainMenuPanel.alpha = 1f;
        if (settingsPanel != null) settingsPanel.alpha = 0f;
        if (creditsPanel != null) creditsPanel.alpha = 0f;
    }

    /// <summary>
    /// Nút Play Game
    /// </summary>
    public void OnPlayGame()
    {
        Debug.Log("🎮 [MainMenuManager] Play Game bấm!", gameObject);

        if (SceneManagerSingleton.Instance != null)
        {
            SceneManagerSingleton.Instance.LoadGameplay();
        }
    }

    /// <summary>
    /// Nút Settings
    /// </summary>
    public void OnSettings()
    {
        if (mainMenuPanel == null || settingsPanel == null) return;

        Debug.Log("⚙️ [MainMenuManager] Settings bấm!", gameObject);
        
        // Fade out main menu, fade in settings
        StartCoroutine(FadePanel(mainMenuPanel, 1f, 0f, 0.5f));
        StartCoroutine(FadePanel(settingsPanel, 0f, 1f, 0.5f));
    }

    /// <summary>
    /// Nút Credits
    /// </summary>
    public void OnCredits()
    {
        if (mainMenuPanel == null || creditsPanel == null) return;

        Debug.Log("📜 [MainMenuManager] Credits bấm!", gameObject);
        
        // Fade out main menu, fade in credits
        StartCoroutine(FadePanel(mainMenuPanel, 1f, 0f, 0.5f));
        StartCoroutine(FadePanel(creditsPanel, 0f, 1f, 0.5f));
    }

    /// <summary>
    /// Nút Back (Quay lại từ Settings/Credits)
    /// </summary>
    public void OnBack()
    {
        Debug.Log("⬅️ [MainMenuManager] Back bấm!", gameObject);
        
        // Fade out tất cả sub-panels, fade in main menu
        if (settingsPanel != null) 
            StartCoroutine(FadePanel(settingsPanel, 1f, 0f, 0.5f));
        if (creditsPanel != null) 
            StartCoroutine(FadePanel(creditsPanel, 1f, 0f, 0.5f));
        if (mainMenuPanel != null) 
            StartCoroutine(FadePanel(mainMenuPanel, 0f, 1f, 0.5f));
    }

    /// <summary>
    /// Nút Quit (Thoát game)
    /// </summary>
    public void OnQuit()
    {
        Debug.Log("🔚 [MainMenuManager] Quit bấm! Thoát game...", gameObject);
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Fade panel (Từ từ hiện/mất)
    /// </summary>
    private System.Collections.IEnumerator FadePanel(CanvasGroup panel, float startAlpha, float endAlpha, float duration)
    {
        if (panel == null) yield break;

        panel.alpha = startAlpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        panel.alpha = endAlpha;
    }
}
