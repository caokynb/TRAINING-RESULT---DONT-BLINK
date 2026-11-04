using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement), typeof(PlayerLook))]
public sealed class TutorialScreenView : MonoBehaviour
{
    [SerializeField] private Texture2D instructionImage;
    [SerializeField] private Vector2 instructionImageSize = new Vector2(1500f, 844f);
    [SerializeField] private Vector2 confirmButtonSize = new Vector2(300f, 76f);
    [SerializeField, Min(1)] private int buttonFontSize = 40;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private Canvas tutorialCanvas;
    private EventSystem ownedEventSystem;
    private Sprite runtimeInstructionSprite;
    private float previousTimeScale;
    private bool isShowingTutorial;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
    }

    private void Start()
    {
        if (instructionImage == null)
        {
            Debug.LogError("TutorialScreenView: Assign the instruction image in the Inspector.", this);
            return;
        }

        EnsureEventSystem();
        BuildTutorialCanvas();
        ShowTutorial();
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

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("Tutorial EventSystem");
        ownedEventSystem = eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildTutorialCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Tutorial Screen",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

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
        runtimeInstructionSprite = Sprite.Create(
            instructionImage,
            new Rect(0f, 0f, instructionImage.width, instructionImage.height),
            new Vector2(0.5f, 0.5f));
        instruction.sprite = runtimeInstructionSprite;
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
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = confirmButtonSize;
        buttonRect.anchoredPosition = new Vector2(0f, 36f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.88f, 0.82f, 0.55f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ResumeGameplay);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text label = labelObject.GetComponent<Text>();
        label.text = "Xác nhận";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = buttonFontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;
        StretchToParent(label.rectTransform);
    }

    private void ShowTutorial()
    {
        previousTimeScale = Time.timeScale;
        isShowingTutorial = true;
        Time.timeScale = 0f;

        playerMovement.SetMovementLocked(true);
        playerLook.enabled = false;
        inputReader.enabled = false;
        Cursor.visible = true;
    }

    private void ResumeGameplay()
    {
        if (!isShowingTutorial)
            return;

        isShowingTutorial = false;
        Time.timeScale = previousTimeScale;
        playerMovement.SetMovementLocked(false);
        playerLook.enabled = true;
        inputReader.enabled = true;

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
            Destroy(tutorialCanvas.gameObject);
            tutorialCanvas = null;
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
