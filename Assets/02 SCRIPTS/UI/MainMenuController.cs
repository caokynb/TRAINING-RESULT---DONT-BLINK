using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Nút bắt đầu trò chơi.")]
    [SerializeField] private Button playButton;

    [Tooltip("Nút thoát trò chơi.")]
    [SerializeField] private Button quitButton;

    [Header("Configuration")]
    [Tooltip("Tên scene gameplay chính để nạp khi bắt đầu.")]
    [SerializeField] private string gameSceneName = "Main";

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
