using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuView : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel chứa giao diện menu tạm dừng.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Nút tiếp tục trò chơi.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Nút chơi lại màn hiện tại.")]
    [SerializeField] private Button restartButton;

    [Tooltip("Nút quay về menu chính (tùy chọn).")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Nút thoát trò chơi.")]
    [SerializeField] private Button quitButton;

    [Header("Configuration")]
    [Tooltip("Tên scene gameplay chính để nạp lại khi bấm Chơi lại.")]
    [SerializeField] private string mainSceneName = "Main";

    [Tooltip("Tên scene menu chính để quay về.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool IsPaused => isPaused;

    private GameManager gameManager;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private BlinkSystem blinkSystem;
    private bool isPaused;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.StateChanged += HandleGameStateChanged;

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleGameStateChanged;

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        if (isPaused)
            ResumeGame();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        if (gameManager != null && gameManager.CurrentState != GameManager.GameState.Playing)
            return;

        TutorialScreenView tutorial = FindFirstObjectByType<TutorialScreenView>();
        if (tutorial != null && tutorial.IsShowingTutorial)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        FindPlayerComponents();

        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        if (playerLook != null)
            playerLook.enabled = false;

        if (blinkSystem != null)
            blinkSystem.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        FindPlayerComponents();

        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);

        if (playerLook != null)
            playerLook.enabled = true;

        if (blinkSystem != null)
            blinkSystem.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Playing && isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }
    }

    private void FindPlayerComponents()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerLook == null)
            playerLook = FindFirstObjectByType<PlayerLook>();

        if (blinkSystem == null)
            blinkSystem = FindFirstObjectByType<BlinkSystem>();
    }
}
