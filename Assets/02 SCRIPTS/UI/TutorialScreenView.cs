using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement), typeof(PlayerLook))]
public sealed class TutorialScreenView : MonoBehaviour
{
    [SerializeField] private Texture2D instructionImage;
    [SerializeField] private Vector2 instructionImageSize = new Vector2(1500f, 844f);
    [SerializeField] private Vector2 confirmButtonSize = new Vector2(300f, 76f);
    [SerializeField, Min(1)] private int buttonFontSize = 40;

    [Header("UI Hierarchy References (Tùy chỉnh trong Editor)")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private Button confirmButton;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private EventSystem ownedEventSystem;
    private Sprite runtimeInstructionSprite;
    private float previousTimeScale = 1f;
    private bool isShowingTutorial;

    public bool IsShowingTutorial => isShowingTutorial;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();

        ResolveExistingReferences();
    }

    private void Start()
    {
        ResolveExistingReferences();

        if (tutorialCanvas == null && instructionImage == null)
        {
            Debug.LogError("TutorialScreenView: Assign the instruction image in the Inspector.", this);
            return;
        }

        EnsureEventSystem();
        BuildTutorialCanvas();
        ShowTutorial();
    }

    private void Update()
    {
        if (!isShowingTutorial)
            return;

        // Cho phép người chơi nhấn phím Space, Enter, E hoặc Escape để đóng màn hình hướng dẫn
        bool dismissRequested = false;

        if (Keyboard.current != null)
        {
            dismissRequested = Keyboard.current.spaceKey.wasPressedThisFrame ||
                               Keyboard.current.enterKey.wasPressedThisFrame ||
                               Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                               Keyboard.current.eKey.wasPressedThisFrame ||
                               Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (dismissRequested)
        {
            ResumeGameplay();
        }
    }

    private void OnDisable()
    {
        if (isShowingTutorial)
            ResumeGameplay();

        DestroyTutorialObjects();
    }

    private void OnDestroy()
    {
        if (runtimeInstructionSprite != null)
            Destroy(runtimeInstructionSprite);
    }

    public bool ResolveExistingReferences()
    {
        if (tutorialCanvas == null)
        {
            Transform found = transform.Find("Tutorial Screen");
            if (found == null)
            {
                Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
                foreach (Canvas c in canvases)
                {
                    if (c.name == "Tutorial Screen")
                    {
                        found = c.transform;
                        break;
                    }
                }
            }

            if (found != null)
                tutorialCanvas = found.GetComponent<Canvas>();
        }

        if (tutorialCanvas != null)
        {
            if (confirmButton == null)
            {
                Transform btnTransform = tutorialCanvas.transform.Find("Confirm Button");
                if (btnTransform != null)
                    confirmButton = btnTransform.GetComponent<Button>();
                else
                    confirmButton = tutorialCanvas.GetComponentInChildren<Button>(true);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ResumeGameplay);
                confirmButton.onClick.AddListener(ResumeGameplay);
            }
        }

        return tutorialCanvas != null;
    }

    [ContextMenu("Tạo / Hiển thị Tutorial trong Scene (Edit Mode)")]
    public void SetupTutorialInEditor()
    {
        if (ResolveExistingReferences())
        {
            tutorialCanvas.gameObject.SetActive(true);
            return;
        }

        BuildTutorialCanvas();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (tutorialCanvas != null)
            UnityEditor.EditorUtility.SetDirty(tutorialCanvas.gameObject);
#endif
    }

    [ContextMenu("Ẩn Tutorial Preview")]
    public void HideTutorialInEditor()
    {
        if (tutorialCanvas != null)
            tutorialCanvas.gameObject.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("Tutorial EventSystem");
        ownedEventSystem = eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void BuildTutorialCanvas()
    {
        if (ResolveExistingReferences())
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ResumeGameplay);
                confirmButton.onClick.AddListener(ResumeGameplay);
            }
            return;
        }

        GameObject canvasObject = new GameObject(
            "Tutorial Screen",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        tutorialCanvas = canvasObject.GetComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage("Background", canvasObject.transform);
        StretchToParent(background.rectTransform);
        background.color = new Color(0f, 0f, 0f, 0.92f);

        Image instruction = CreateImage("Instruction Image", canvasObject.transform);
        if (instructionImage != null)
        {
            runtimeInstructionSprite = Sprite.Create(
                instructionImage,
                new Rect(0f, 0f, instructionImage.width, instructionImage.height),
                new Vector2(0.5f, 0.5f));
            instruction.sprite = runtimeInstructionSprite;
        }
        instruction.preserveAspect = true;
        instruction.raycastTarget = false;

        RectTransform instructionRect = instruction.rectTransform;
        instructionRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionRect.pivot = new Vector2(0.5f, 0.5f);
        instructionRect.sizeDelta = instructionImageSize;
        instructionRect.anchoredPosition = new Vector2(0f, 95f);

        BuildConfirmButton(canvasObject.transform);
    }

    private void BuildConfirmButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "Confirm Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = confirmButtonSize;
        buttonRect.anchoredPosition = new Vector2(0f, 80f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.88f, 0.82f, 0.55f, 1f);

        confirmButton = buttonObject.GetComponent<Button>();
        confirmButton.targetGraphic = buttonImage;
        confirmButton.onClick.RemoveListener(ResumeGameplay);
        confirmButton.onClick.AddListener(ResumeGameplay);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text label = labelObject.GetComponent<Text>();
        label.text = "Xác nhận";
        try
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            label.font = Font.CreateDynamicFontFromOSFont("Arial", buttonFontSize);
        }
        label.fontSize = buttonFontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;
        StretchToParent(label.rectTransform);
    }

    private void ShowTutorial()
    {
        ResolveExistingReferences();

        if (tutorialCanvas == null)
            return;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ResumeGameplay);
            confirmButton.onClick.AddListener(ResumeGameplay);
        }

        tutorialCanvas.gameObject.SetActive(true);
        isShowingTutorial = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (playerMovement != null) playerMovement.SetMovementLocked(true);
        if (playerLook != null) playerLook.enabled = false;
        if (inputReader != null) inputReader.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGameplay()
    {
        if (!isShowingTutorial)
            return;

        isShowingTutorial = false;
        AudioManager.Instance?.PlayUIClick();
        Time.timeScale = (previousTimeScale > 0f) ? previousTimeScale : 1f;

        if (playerMovement != null) playerMovement.SetMovementLocked(false);
        if (playerLook != null) playerLook.enabled = true;
        if (inputReader != null) inputReader.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        DestroyTutorialCanvas();
        DestroyOwnedEventSystem();
    }

    private void DestroyTutorialObjects()
    {
        DestroyTutorialCanvas();
        DestroyOwnedEventSystem();
    }

    private void DestroyTutorialCanvas()
    {
        if (tutorialCanvas != null)
        {
            if (Application.isPlaying)
            {
                tutorialCanvas.gameObject.SetActive(false);
            }
        }
    }

    private void DestroyOwnedEventSystem()
    {
        if (ownedEventSystem == null)
            return;

        Destroy(ownedEventSystem.gameObject);
        ownedEventSystem = null;
    }

    private static Image CreateImage(string objectName, Transform parent)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
