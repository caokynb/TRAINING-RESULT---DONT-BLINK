using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EndScreenView : MonoBehaviour
{
    public enum ResultDisplayMode
    {
        Text,
        Image
    }

    [Serializable]
    public class StudentResultData
    {
        public bool success;
        public string msv;
        public string name;
        public bool isPassed;
        public string resultText;
        public string message;
    }

    [Header("Default References (K09)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;
    [Tooltip("Nút quay về Main Menu khi thắng")]
    [SerializeField] private Button mainMenuButton;
    [Tooltip("Khung chứa các nút điều hướng Retry / Menu (tuỳ chọn)")]
    [SerializeField] private GameObject navigationButtonsRoot;
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Server API")]
    [Tooltip("URL Web App triển khai từ Google Apps Script")]
    [SerializeField] private string webAppUrl = "https://script.google.com/macros/s/AKfycbyPmzk98vAeNMOf7kJdCNPJMDMfRe3_kewm2mCv7qmXVU9hcEPMAeoOxokXkz77nD5dWw/exec";

    [Header("Win / Input Section")]
    [Tooltip("Khung chứa Text box và Button Confirm (tự ẩn khi thua)")]
    [SerializeField] private GameObject winInputSection;
    [Tooltip("Text box (Input Field) để nhập mã sinh viên")]
    [SerializeField] private TMP_InputField msvInputField;
    [Tooltip("Nút bấm xác nhận tra cứu")]
    [SerializeField] private Button confirmButton;
    [Tooltip("Text hiển thị trạng thái (Đang kiểm tra..., lỗi, v.v.)")]
    [SerializeField] private TMP_Text statusMessageText;

    [Header("Result Letter Panel (Overlay)")]
    [Tooltip("Panel đè lên trên hiển thị bức thư kết quả")]
    [SerializeField] private GameObject studentResultPanel;
    [Tooltip("Khung text lớn hiển thị toàn bộ nội dung dòng thư")]
    [SerializeField] private TMP_Text letterText;
    [Tooltip("Nút đóng panel kết quả")]
    [SerializeField] private Button closeResultButton;

    [Header("Result Format Settings")]
    [Tooltip("Chọn cách hiển thị kết quả: Text (dùng mẫu chữ) hoặc Image (dùng ảnh thư thiết kế sẵn)")]
    [SerializeField] private ResultDisplayMode displayMode = ResultDisplayMode.Text;
    [Tooltip("Image component dùng để hiển thị ảnh thư (khi ở chế độ Image)")]
    [SerializeField] private Image resultLetterImage;
    [Tooltip("Ảnh thư hiển thị khi đỗ (Passed)")]
    [SerializeField] private Sprite passLetterSprite;
    [Tooltip("Ảnh thư hiển thị khi trượt (Failed)")]
    [SerializeField] private Sprite failLetterSprite;

    [Header("Letter Templates (Text Mode)")]
    [Tooltip("Dùng {name} ở nơi muốn chèn tên sinh viên")]
    [TextArea(5, 10)]
    [SerializeField] private string passLetterTemplate = 
        "Chào em {name},\n\n" +
        "Chúc mừng em đã xuất sắc vượt qua vòng training và chính thức trở thành thành viên mới của Game Creator Club!\n\n" +
        "Chào mừng em gia nhập đại gia đình GCC!";

    [Tooltip("Dùng {name} ở nơi muốn chèn tên sinh viên")]
    [TextArea(5, 10)]
    [SerializeField] private string failLetterTemplate = 
        "Chào em {name},\n\n" +
        "Cảm ơn em đã nỗ lực hết mình và đồng hành cùng CLB trong suốt chặng đường training vừa qua. Rất tiếc là lần này chúng ta chưa có cơ hội cùng nhau đi tiếp.\n\n" +
        "Chúc em luôn giữ vững niềm đam mê và hẹn gặp lại em ở những sự kiện tiếp theo!";

    [SerializeField] private Color errorStatusColor = new Color(0.95f, 0.25f, 0.25f);

    private GameManager gameManager;
    private Coroutine queryCoroutine;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        if (navigationButtonsRoot == null && retryButton != null)
        {
            Transform parent = retryButton.transform.parent;
            if (parent != null && (resultPanel == null || parent.gameObject != resultPanel))
            {
                navigationButtonsRoot = parent.gameObject;
            }
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (studentResultPanel != null)
            studentResultPanel.SetActive(false);

        if (resultLetterImage != null && resultLetterImage.gameObject != studentResultPanel)
            resultLetterImage.gameObject.SetActive(false);

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

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenuScene);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (msvInputField != null)
            msvInputField.onSubmit.AddListener(OnInputSubmitted);

        if (closeResultButton != null)
            closeResultButton.onClick.AddListener(OnCloseResultClicked);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleStateChanged;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(ReloadMainScene);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(LoadMainMenuScene);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (msvInputField != null)
            msvInputField.onSubmit.RemoveListener(OnInputSubmitted);

        if (closeResultButton != null)
            closeResultButton.onClick.RemoveListener(OnCloseResultClicked);

        if (queryCoroutine != null)
        {
            StopCoroutine(queryCoroutine);
            queryCoroutine = null;
        }
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (resultPanel == null)
            return;

        bool hasEnded = state == GameManager.GameState.Won || state == GameManager.GameState.Lost;
        resultPanel.SetActive(hasEnded);

        if (!hasEnded)
            return;

        // Mở con trỏ chuột để click UI
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (studentResultPanel != null)
            studentResultPanel.SetActive(false);

        if (statusMessageText != null)
            statusMessageText.text = string.Empty;

        if (state == GameManager.GameState.Won)
        {
            if (resultText != null)
                resultText.text = "ESCAPED!";

            SetInputSectionActive(true);
            SetNavigationButtonsActive(true);

            if (msvInputField != null)
            {
                msvInputField.text = string.Empty;
                msvInputField.interactable = true;
                msvInputField.ActivateInputField();
            }

            if (confirmButton != null)
                confirmButton.interactable = true;
        }
        else // Lost
        {
            if (resultText != null)
                resultText.text = "CATCHED!";

            SetInputSectionActive(false);
            SetNavigationButtonsActive(true);
        }
    }

    private void SetNavigationButtonsActive(bool active)
    {
        if (navigationButtonsRoot != null)
            navigationButtonsRoot.SetActive(active);

        if (retryButton != null)
            retryButton.gameObject.SetActive(active);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(active);
    }

    private void SetInputSectionActive(bool active)
    {
        if (winInputSection != null)
        {
            winInputSection.SetActive(active);
        }
        else
        {
            if (msvInputField != null)
                msvInputField.gameObject.SetActive(active);
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(active);
            if (statusMessageText != null)
                statusMessageText.gameObject.SetActive(active);
        }
    }

    private void OnInputSubmitted(string _)
    {
        OnConfirmClicked();
    }

    private void OnConfirmClicked()
    {
        AudioManager.Instance?.PlayUIClick();

        if (queryCoroutine != null)
            return;

        if (msvInputField == null)
            return;

        string msv = msvInputField.text.Trim();
        if (string.IsNullOrEmpty(msv))
        {
            if (statusMessageText != null)
            {
                statusMessageText.color = errorStatusColor;
                statusMessageText.text = "Please enter your student ID...";
            }
            return;
        }

        if (statusMessageText != null)
        {
            statusMessageText.color = Color.white;
            statusMessageText.text = "Checking...";
        }

        if (confirmButton != null)
            confirmButton.interactable = false;

        queryCoroutine = StartCoroutine(QueryRoutine(msv));
    }

    private IEnumerator QueryRoutine(string msv)
    {
        string url = $"{webAppUrl}?msv={UnityWebRequest.EscapeURL(msv)}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                if (statusMessageText != null)
                {
                    statusMessageText.color = errorStatusColor;
                    statusMessageText.text = $"Internet Error ({www.error}). Please try again!";
                }

                if (confirmButton != null)
                    confirmButton.interactable = true;

                queryCoroutine = null;
                yield break;
            }

            string json = www.downloadHandler.text;
            StudentResultData data = null;

            try
            {
                data = JsonUtility.FromJson<StudentResultData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EndScreenView] Lỗi parse dữ liệu từ server: {ex.Message}\nRaw JSON: {json}");
            }

            if (data != null && data.success)
            {
                if (statusMessageText != null)
                    statusMessageText.text = string.Empty;

                DisplayStudentResult(data);
            }
            else
            {
                if (statusMessageText != null)
                {
                    statusMessageText.color = errorStatusColor;
                    statusMessageText.text = data != null && !string.IsNullOrEmpty(data.message)
                        ? data.message
                        : "Cannot find your Student ID in List";
                }
            }

            if (confirmButton != null)
                confirmButton.interactable = true;

            queryCoroutine = null;
        }
    }

    private void DisplayStudentResult(StudentResultData data)
    {
        if (studentResultPanel != null)
            studentResultPanel.SetActive(true);

        SetInputSectionActive(false);
        SetNavigationButtonsActive(true);

        if (displayMode == ResultDisplayMode.Image)
        {
            if (letterText != null)
                letterText.gameObject.SetActive(false);

            if (resultLetterImage != null)
            {
                Sprite spriteToShow = data.isPassed ? passLetterSprite : failLetterSprite;
                if (spriteToShow != null)
                {
                    resultLetterImage.sprite = spriteToShow;
                }
                else
                {
                    Debug.LogWarning($"[EndScreenView] {(data.isPassed ? "passLetterSprite" : "failLetterSprite")} chưa được gán trong Inspector!", this);
                }

                resultLetterImage.preserveAspect = true;

                if (resultLetterImage.gameObject != studentResultPanel)
                    resultLetterImage.gameObject.SetActive(true);
                else
                    resultLetterImage.enabled = true;
            }
            else
            {
                Debug.LogWarning("[EndScreenView] displayMode đang chọn Image nhưng resultLetterImage chưa được gán trong Inspector!", this);
            }
        }
        else // Text Mode
        {
            if (resultLetterImage != null && resultLetterImage.gameObject != studentResultPanel)
                resultLetterImage.gameObject.SetActive(false);

            if (letterText != null)
            {
                letterText.gameObject.SetActive(true);

                string template = data.isPassed ? passLetterTemplate : failLetterTemplate;
                string studentName = string.IsNullOrEmpty(data.name) ? "em" : data.name.Trim();

                // Thay thế biến {name} hoặc {0} vào dòng thư
                string formattedLetter = template
                    .Replace("{name}", studentName)
                    .Replace("{0}", studentName);

                letterText.text = formattedLetter;
            }
        }
    }

    private void OnCloseResultClicked()
    {
        AudioManager.Instance?.PlayUIClick();

        if (studentResultPanel != null)
            studentResultPanel.SetActive(false);

        SetInputSectionActive(true);
        SetNavigationButtonsActive(true);
    }

    private void ReloadMainScene()
    {
        AudioManager.Instance?.PlayUIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }

    private void LoadMainMenuScene()
    {
        AudioManager.Instance?.PlayUIClick();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }
}
