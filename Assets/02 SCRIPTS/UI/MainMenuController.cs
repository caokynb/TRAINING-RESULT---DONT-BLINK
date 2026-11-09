using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Nút bắt đầu trò chơi.")]
    [SerializeField] private Button playButton;

    [Tooltip("Nút mở bảng Credit.")]
    [SerializeField] private Button creditButton;

    [Tooltip("Nút thoát trò chơi.")]
    [SerializeField] private Button quitButton;

    [Header("Menu Elements To Hide When Viewing Credit")]
    [Tooltip("Tiêu đề game (ví dụ: Tittle / Logo 'DON'T BLINK').")]
    [SerializeField] private GameObject titleObject;

    [Tooltip("Cụm chứa các nút Play, Credit, Quit.")]
    [SerializeField] private GameObject menuButtonsObject;

    [Header("Credit UI Settings")]
    [Tooltip("Panel chứa màn hình Credit.")]
    [SerializeField] private GameObject creditPanel;

    [Tooltip("ScrollRect của bảng Credit.")]
    [SerializeField] private ScrollRect creditScrollRect;

    [Tooltip("Tốc độ tự động cuộn chữ (pixel/giây).")]
    [SerializeField] private float autoScrollSpeed = 45f;

    [Tooltip("Nội dung Credit.")]
    [TextArea(15, 30)]
    [SerializeField] private string creditContent;

    [Header("Configuration")]
    [Tooltip("Tên scene gameplay chính để nạp khi bắt đầu.")]
    [SerializeField] private string gameSceneName = "Main";

    private bool isViewingCredit = false;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResolveReferences();
        EnsureCreditPanel();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureCreditPanel();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (creditButton != null)
            creditButton.onClick.AddListener(OpenCredit);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        if (creditButton != null)
            creditButton.onClick.RemoveListener(OpenCredit);
    }

    private void Update()
    {
        if (!isViewingCredit)
            return;

        // Tự động cuộn chữ mượt mà xuống dưới theo thời gian
        if (creditScrollRect != null && creditScrollRect.content != null)
        {
            float contentHeight = creditScrollRect.content.rect.height;
            float viewportHeight = creditScrollRect.viewport != null ? creditScrollRect.viewport.rect.height : 600f;
            float scrollableHeight = contentHeight - viewportHeight;

            if (scrollableHeight > 0f)
            {
                float deltaNorm = (autoScrollSpeed / scrollableHeight) * Time.unscaledDeltaTime;
                creditScrollRect.verticalNormalizedPosition = Mathf.Clamp01(creditScrollRect.verticalNormalizedPosition - deltaNorm);
            }
        }

        // Nhấn phím Escape hoặc Backspace để thoát khỏi màn hình Credit
        bool escPressed = false;
        if (Keyboard.current != null)
        {
            escPressed = Keyboard.current.escapeKey.wasPressedThisFrame ||
                         Keyboard.current.backspaceKey.wasPressedThisFrame;
        }

        if (escPressed)
        {
            CloseCredit();
        }
    }

    public void OpenCredit()
    {
        AudioManager.Instance?.PlayUIClick();
        EnsureCreditPanel();

        if (titleObject != null)
            titleObject.SetActive(false);

        if (menuButtonsObject != null)
            menuButtonsObject.SetActive(false);

        if (creditPanel != null)
        {
            creditPanel.SetActive(true);
            if (creditScrollRect != null)
            {
                // Đặt lại vị trí ban đầu ở trên cùng
                creditScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        isViewingCredit = true;
    }

    public void CloseCredit()
    {
        AudioManager.Instance?.PlayUIClick();

        if (creditPanel != null)
            creditPanel.SetActive(false);

        if (titleObject != null)
            titleObject.SetActive(true);

        if (menuButtonsObject != null)
            menuButtonsObject.SetActive(true);

        isViewingCredit = false;
    }

    public void PlayGame()
    {
        AudioManager.Instance?.PlayUIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        AudioManager.Instance?.PlayUIClick();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ResolveReferences()
    {
        if (playButton == null)
        {
            GameObject playObj = GameObject.Find("PlayButton");
            if (playObj != null) playButton = playObj.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            GameObject quitObj = GameObject.Find("QuitButton");
            if (quitObj != null) quitButton = quitObj.GetComponent<Button>();
        }

        if (creditButton == null)
        {
            GameObject creditObj = GameObject.Find("Credit");
            if (creditObj != null) creditButton = creditObj.GetComponent<Button>();
        }

        if (titleObject == null)
        {
            titleObject = GameObject.Find("Tittle");
        }

        if (menuButtonsObject == null)
        {
            menuButtonsObject = GameObject.Find("ButtonMainMenu");
        }
    }

    [ContextMenu("Tạo / Hiển thị Credit Panel (Edit Mode)")]
    public void EnsureCreditPanel()
    {
        ResolveReferences();

        if (creditPanel != null)
            return;

        // Tìm Canvas của menu
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("MenuCanvas");
            if (canvasObj != null) canvas = canvasObj.GetComponent<Canvas>();
        }

        if (canvas == null)
            return;

        // Kiểm tra xem đã có sẵn CreditPanel con trong Canvas chưa
        Transform existingPanel = canvas.transform.Find("CreditPanel");
        if (existingPanel != null)
        {
            creditPanel = existingPanel.gameObject;
            creditScrollRect = creditPanel.GetComponentInChildren<ScrollRect>(true);
            return;
        }

        // 1. Tạo CreditPanel chính (Phủ toàn màn hình với nền tối mờ)
        GameObject panelObj = new GameObject("CreditPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.04f, 0.90f);

        // 2. Tạo Viewport giới hạn vùng cuộn (RectMask2D)
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(panelObj.transform, false);

        RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.12f, 0.12f);
        viewportRect.anchorMax = new Vector2(0.88f, 0.94f);
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;

        // 3. Tạo Content chứa text cuộn
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 600f);

        VerticalLayoutGroup layout = contentObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(20, 20, 30, 80);

        ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Tạo TextMeshPro Text hiển thị Credit
        GameObject textObj = new GameObject("CreditText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(contentObj.transform, false);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = creditContent;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.lineSpacing = 12f;
        tmp.paragraphSpacing = 16f;

        // 5. Gắn ScrollRect vào panel
        creditScrollRect = panelObj.AddComponent<ScrollRect>();
        creditScrollRect.viewport = viewportRect;
        creditScrollRect.content = contentRect;
        creditScrollRect.horizontal = false;
        creditScrollRect.vertical = true;
        creditScrollRect.movementType = ScrollRect.MovementType.Clamped;
        creditScrollRect.scrollSensitivity = 30f;

        // 6. Nút / Dòng nhắc nhở [ Nhấn ESC để quay lại ] ở góc dưới
        GameObject escHintObj = new GameObject("EscHintButton", typeof(RectTransform), typeof(Button));
        escHintObj.transform.SetParent(panelObj.transform, false);

        RectTransform escRect = escHintObj.GetComponent<RectTransform>();
        escRect.anchorMin = new Vector2(0.5f, 0f);
        escRect.anchorMax = new Vector2(0.5f, 0f);
        escRect.pivot = new Vector2(0.5f, 0f);
        escRect.sizeDelta = new Vector2(360f, 50f);
        escRect.anchoredPosition = new Vector2(0f, 15f);

        Button escButton = escHintObj.GetComponent<Button>();
        escButton.onClick.AddListener(CloseCredit);

        GameObject escTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        escTextObj.transform.SetParent(escHintObj.transform, false);

        RectTransform escTextRect = escTextObj.GetComponent<RectTransform>();
        escTextRect.anchorMin = Vector2.zero;
        escTextRect.anchorMax = Vector2.one;
        escTextRect.sizeDelta = Vector2.zero;
        escTextRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI escTmp = escTextObj.GetComponent<TextMeshProUGUI>();
        escTmp.text = "[ Press Esc to back ]";
        escTmp.fontSize = 20;
        escTmp.alignment = TextAlignmentOptions.Center;
        escTmp.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
        escTmp.raycastTarget = false;

        creditPanel = panelObj;
        creditPanel.SetActive(false);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.EditorUtility.SetDirty(panelObj);
#endif
    }
}
