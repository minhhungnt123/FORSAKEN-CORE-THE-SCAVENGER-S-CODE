using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI Manager - Quản lý tất cả UI elements
/// - Responsive buttons
/// - Navigation
/// - Visual feedback
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Button References")]
    public Button playGameButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button backButton;
    public Button quitButton;

    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.8f, 0f);      // Vàng
    public Color pressedColor = new Color(1f, 0.6f, 0f);    // Cam

    [Header("Sound Settings")]
    public bool playButtonSounds = true;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    private AudioSource audioSource;

    private MainMenuManager menuManager;

    void Start()
    {
        menuManager = GetComponent<MainMenuManager>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupButtons();
        Debug.Log("🎨 [UIManager] UI setup hoàn tất!", gameObject);
    }

    /// <summary>
    /// Setup tất cả button events
    /// </summary>
    private void SetupButtons()
    {
        // Play Game Button
        if (playGameButton != null)
        {
            playGameButton.onClick.AddListener(() => {
                PlayClickSound();
                menuManager.OnPlayGame();
            });
            AddButtonHoverEffects(playGameButton);
        }

        // Settings Button
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => {
                PlayClickSound();
                menuManager.OnSettings();
            });
            AddButtonHoverEffects(settingsButton);
        }

        // Credits Button
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(() => {
                PlayClickSound();
                menuManager.OnCredits();
            });
            AddButtonHoverEffects(creditsButton);
        }

        // Back Button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                PlayClickSound();
                menuManager.OnBack();
            });
            AddButtonHoverEffects(backButton);
        }

        // Quit Button
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() => {
                PlayClickSound();
                menuManager.OnQuit();
            });
            AddButtonHoverEffects(quitButton);
        }

        Debug.Log("✅ [UIManager] Button listeners gắn thành công!", gameObject);
    }

    /// <summary>
    /// Thêm hover effects cho button
    /// </summary>
    private void AddButtonHoverEffects(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Hover Enter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnButtonHoverEnter(button));
        trigger.triggers.Add(pointerEnter);

        // Hover Exit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => OnButtonHoverExit(button));
        trigger.triggers.Add(pointerExit);
    }

    /// <summary>
    /// Khi di chuột vào button
    /// </summary>
    private void OnButtonHoverEnter(Button button)
    {
        if (button.image != null)
        {
            button.image.color = hoverColor;
        }
        
        PlayHoverSound();
        Debug.Log($"🖱️ [UIManager] Di vào button: {button.name}", button);
    }

    /// <summary>
    /// Khi di chuột ra khỏi button
    /// </summary>
    private void OnButtonHoverExit(Button button)
    {
        if (button.image != null)
        {
            button.image.color = normalColor;
        }
        
        Debug.Log($"🖱️ [UIManager] Ra khỏi button: {button.name}", button);
    }

    /// <summary>
    /// Play hover sound
    /// </summary>
    private void PlayHoverSound()
    {
        if (!playButtonSounds || audioSource == null || hoverSound == null) return;
        audioSource.PlayOneShot(hoverSound, 0.5f);
    }

    /// <summary>
    /// Play click sound
    /// </summary>
    private void PlayClickSound()
    {
        if (!playButtonSounds || audioSource == null || clickSound == null) return;
        audioSource.PlayOneShot(clickSound, 0.7f);
    }
}
