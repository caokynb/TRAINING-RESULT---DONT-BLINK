using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndScreenView : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;
    [SerializeField] private string mainSceneName = "Main";

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (gameManager == null)
            Debug.LogError("EndScreenView: GameManager was not found in the loaded scenes.", this);

        if (resultText == null || retryButton == null || resultPanel == null)
            Debug.LogError("EndScreenView: Assign the panel, result text, and retry button in the Inspector.", this);
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
            HandleStateChanged(gameManager.CurrentState);
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(ReloadMainScene);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleStateChanged;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(ReloadMainScene);
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (resultPanel == null)
            return;

        bool hasEnded = state == GameManager.GameState.Won || state == GameManager.GameState.Lost;
        resultPanel.SetActive(hasEnded);

        if (!hasEnded || resultText == null)
            return;

        resultText.text = state == GameManager.GameState.Won
            ? "Thoát được rồi!"
            : "Mày chậm quá!";
    }

    private void ReloadMainScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }
}
