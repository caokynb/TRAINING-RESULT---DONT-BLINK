using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BlinkSystem))]
public sealed class BlinkHudView : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.08f;
    [SerializeField] private Vector2 fatigueBarSize = new Vector2(128f, 720f);
    [SerializeField, Range(0f, 1f)] private float redTransitionStart = 0.75f;

    private BlinkSystem blinkSystem;
    private Slider fatigueSlider;
    private Image fatigueFill;
    private Image closedEyesOverlay;
    private Canvas hudCanvas;
    private float targetOverlayAlpha;
    private float overlayAlpha;

    private void Awake()
    {
        blinkSystem = GetComponent<BlinkSystem>();
        BuildHud();
        SetOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        blinkSystem.FatigueChanged += UpdateFatigue;
        blinkSystem.EyesClosedChanged += SetEyesClosed;
        UpdateFatigue(blinkSystem.Fatigue);
        SetEyesClosed(blinkSystem.IsBlinking);
    }

    private void OnDisable()
    {
        blinkSystem.FatigueChanged -= UpdateFatigue;
        blinkSystem.EyesClosedChanged -= SetEyesClosed;
    }

    private void Update()
    {
        overlayAlpha = Mathf.MoveTowards(
            overlayAlpha,
            targetOverlayAlpha,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
        SetOverlayAlpha(overlayAlpha);
    }

    private void OnDestroy()
    {
        if (hudCanvas != null)
            Destroy(hudCanvas.gameObject);
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("Blink HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hudCanvas = canvasObject.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = -1;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildFatigueSlider(canvasObject.transform);

        closedEyesOverlay = CreateImage("Closed Eyes Overlay", canvasObject.transform);
        RectTransform overlayRect = closedEyesOverlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        closedEyesOverlay.color = new Color(0f, 0f, 0f, 0f);
        closedEyesOverlay.raycastTarget = false;
    }

    private void BuildFatigueSlider(Transform canvasTransform)
    {
        GameObject sliderObject = new GameObject("Fatigue Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(canvasTransform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.965f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.965f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = fatigueBarSize;

        Image background = CreateImage("Background", sliderObject.transform);
        StretchToParent(background.rectTransform);
        background.color = Color.black;
        background.raycastTarget = false;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        StretchToParent(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        fatigueFill = CreateImage("Fill", fillAreaObject.transform);
        RectTransform fillRect = fatigueFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fatigueFill.raycastTarget = false;

        fatigueSlider = sliderObject.GetComponent<Slider>();
        fatigueSlider.minValue = 0f;
        fatigueSlider.maxValue = 1f;
        fatigueSlider.direction = Slider.Direction.BottomToTop;
        fatigueSlider.interactable = false;
        fatigueSlider.transition = Selectable.Transition.None;
        fatigueSlider.targetGraphic = background;
        fatigueSlider.fillRect = fillRect;
        fatigueSlider.SetValueWithoutNotify(blinkSystem.Fatigue);
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Image CreateImage(string objectName, Transform parent)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color overlayColor = closedEyesOverlay.color;
        overlayColor.a = alpha;
        closedEyesOverlay.color = overlayColor;
    }

    private void UpdateFatigue(float fatigue)
    {
        fatigue = Mathf.Clamp01(fatigue);
        fatigueSlider.SetValueWithoutNotify(fatigue);

        float redAmount = Mathf.InverseLerp(redTransitionStart, 1f, fatigue);
        fatigueFill.color = Color.Lerp(Color.green, new Color(1f, 0f, 0f), redAmount);
    }

    private void SetEyesClosed(bool eyesClosed)
    {
        targetOverlayAlpha = eyesClosed ? 1f : 0f;
    }
}
