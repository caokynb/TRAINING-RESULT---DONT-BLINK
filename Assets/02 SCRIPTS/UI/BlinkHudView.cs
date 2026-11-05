using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BlinkSystem))]
public sealed class BlinkHudView : MonoBehaviour
{
    [Header("Blink Overlay")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.08f;
    [SerializeField] private Vector2 fatigueBarSize = new Vector2(128f, 720f);
    [SerializeField, Range(0f, 1f)] private float redTransitionStart = 0.75f;

    [Header("Fatigue Screen Effects")]
    [Tooltip("Ngưỡng fatigue bắt đầu hiển thị viền tối (0 - 1).")]
    [SerializeField, Range(0f, 1f)] private float darknessStart = 0.5f;
    [Tooltip("Alpha tối đa của viền tối.")]
    [SerializeField, Range(0f, 1f)] private float maxDarknessAlpha = 0.6f;
    [Tooltip("Màu của viền tối.")]
    [SerializeField] private Color darknessColor = Color.black;

    [Tooltip("Ngưỡng fatigue bắt đầu hiển thị viền đỏ (0 - 1).")]
    [SerializeField, Range(0f, 1f)] private float redVignetteStart = 0.65f;
    [Tooltip("Alpha tối đa của viền đỏ.")]
    [SerializeField, Range(0f, 1f)] private float maxRedAlpha = 0.75f;
    [Tooltip("Màu của viền đỏ.")]
    [SerializeField] private Color redVignetteColor = new Color(0.85f, 0.05f, 0.05f, 1f);

    [Tooltip("Tốc độ chuyển đổi alpha của các hiệu ứng viền.")]
    [SerializeField, Min(0.1f)] private float effectFadeSpeed = 2f;

    [Header("Vignette Pulse")]
    [Tooltip("Bật hiệu ứng dao động alpha viền đỏ khi fatigue cao.")]
    [SerializeField] private bool pulseRedOnHighFatigue = true;
    [Tooltip("Tỷ lệ tiến trình viền đỏ để kích hoạt dao động alpha (0 - 1).")]
    [SerializeField, Range(0.1f, 0.9f)] private float pulseMinProgress = 0.5f;
    [Tooltip("Tần số dao động của viền đỏ.")]
    [SerializeField, Range(1f, 15f)] private float pulseSpeed = 6f;
    [Tooltip("Biên độ dao động alpha của viền đỏ.")]
    [SerializeField, Range(0.05f, 0.4f)] private float pulseAmplitude = 0.15f;

    [Header("Fatigue Camera Shake")]
    [Tooltip("Ngưỡng fatigue bắt đầu rung camera (0 - 1).")]
    [SerializeField, Range(0f, 1f)] private float shakeStart = 0.75f;
    [Tooltip("Độ lệch vị trí tối đa khi rung camera (m).")]
    [SerializeField, Range(0.005f, 0.1f)] private float maxShakePosition = 0.02f;
    [Tooltip("Góc xoay tối đa khi rung camera (độ).")]
    [SerializeField, Range(0.1f, 3f)] private float maxShakeAngle = 0.6f;
    [Tooltip("Tần số rung của camera.")]
    [SerializeField, Range(5f, 35f)] private float shakeFrequency = 18f;
    [Tooltip("Tốc độ hồi vị trí camera về mặc định khi ngừng rung.")]
    [SerializeField, Min(0.1f)] private float cameraRecoveryPositionSpeed = 1f;
    [Tooltip("Tốc độ hồi góc xoay camera về mặc định khi ngừng rung.")]
    [SerializeField, Min(1f)] private float cameraRecoveryRotationSpeed = 30f;

    [Header("Procedural Vignette")]
    [Tooltip("Bán kính vùng trong suốt ở tâm vignette (0 - 0.6).")]
    [SerializeField, Range(0f, 0.6f)] private float vignetteInnerRadius = 0.2f;

    private BlinkSystem blinkSystem;
    private Slider fatigueSlider;
    private Image fatigueFill;
    private Image darknessOverlay;
    private Image redVignetteOverlay;
    private Image closedEyesOverlay;
    private Canvas hudCanvas;
    private Sprite vignetteSprite;

    private float targetOverlayAlpha;
    private float overlayAlpha;
    private float currentDarknessAlpha;
    private float currentRedAlpha;

    private Transform cameraTransform;
    private Vector3 initialCameraLocalPosition;
    private Quaternion initialCameraLocalRotation;

    private void Awake()
    {
        blinkSystem = GetComponent<BlinkSystem>();

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            initialCameraLocalPosition = cameraTransform.localPosition;
            initialCameraLocalRotation = cameraTransform.localRotation;
        }

        vignetteSprite = CreateVignetteSprite(vignetteInnerRadius);
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
        ResetCameraShake();
    }

    private void Update()
    {
        // 1. Màn đen nhắm mắt
        overlayAlpha = Mathf.MoveTowards(
            overlayAlpha,
            targetOverlayAlpha,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
        SetOverlayAlpha(overlayAlpha);

        // 2. Hiệu ứng viền tối và viền đỏ theo fatigue (K15)
        UpdateFatigueVisualEffects();

        // 3. Rung nhẹ camera khi mỏi cao (K15)
        UpdateCameraShake();
    }

    private void OnDestroy()
    {
        ResetCameraShake();

        if (hudCanvas != null)
            Destroy(hudCanvas.gameObject);

        if (vignetteSprite != null)
        {
            if (vignetteSprite.texture != null)
                Destroy(vignetteSprite.texture);
            Destroy(vignetteSprite);
        }
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

        // Thanh mỏi mắt
        BuildFatigueSlider(canvasObject.transform);

        // Lớp 1: Viền tối ngoại vi khi mỏi mắt
        darknessOverlay = CreateImage("Darkness Vignette Overlay", canvasObject.transform);
        StretchToParent(darknessOverlay.rectTransform);
        darknessOverlay.sprite = vignetteSprite;
        darknessOverlay.color = new Color(darknessColor.r, darknessColor.g, darknessColor.b, 0f);
        darknessOverlay.raycastTarget = false;

        // Lớp 2: Viền đỏ căng thẳng khi mỏi cao
        redVignetteOverlay = CreateImage("Red Vignette Overlay", canvasObject.transform);
        StretchToParent(redVignetteOverlay.rectTransform);
        redVignetteOverlay.sprite = vignetteSprite;
        redVignetteOverlay.color = new Color(redVignetteColor.r, redVignetteColor.g, redVignetteColor.b, 0f);
        redVignetteOverlay.raycastTarget = false;

        // Lớp 3: Màn đen hoàn toàn khi nhắm mắt
        closedEyesOverlay = CreateImage("Closed Eyes Overlay", canvasObject.transform);
        StretchToParent(closedEyesOverlay.rectTransform);
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

    private void UpdateFatigueVisualEffects()
    {
        if (blinkSystem == null)
            return;

        float currentFatigue = blinkSystem.Fatigue;
        bool isClosed = blinkSystem.IsBlinking;

        // Viền tối ngoại vi
        float targetDarkAlpha = 0f;
        if (!isClosed && currentFatigue > darknessStart)
        {
            float t = Mathf.InverseLerp(darknessStart, 1f, currentFatigue);
            targetDarkAlpha = t * maxDarknessAlpha;
        }
        currentDarknessAlpha = Mathf.MoveTowards(currentDarknessAlpha, targetDarkAlpha, Time.deltaTime * effectFadeSpeed);
        if (darknessOverlay != null)
        {
            Color c = darknessColor;
            c.a = currentDarknessAlpha;
            darknessOverlay.color = c;
        }

        // Viền đỏ căng thẳng
        float targetRedAlpha = 0f;
        if (!isClosed && currentFatigue > redVignetteStart)
        {
            float t = Mathf.InverseLerp(redVignetteStart, 1f, currentFatigue);
            targetRedAlpha = t * maxRedAlpha;

            if (pulseRedOnHighFatigue && t > pulseMinProgress)
            {
                float baseMultiplier = 1f - pulseAmplitude;
                float pulse = baseMultiplier + pulseAmplitude * Mathf.Sin(Time.time * pulseSpeed);
                targetRedAlpha *= pulse;
            }
        }
        currentRedAlpha = Mathf.MoveTowards(currentRedAlpha, targetRedAlpha, Time.deltaTime * effectFadeSpeed);
        if (redVignetteOverlay != null)
        {
            Color c = redVignetteColor;
            c.a = currentRedAlpha;
            redVignetteOverlay.color = c;
        }
    }

    private void UpdateCameraShake()
    {
        if (cameraTransform == null || blinkSystem == null)
            return;

        float currentFatigue = blinkSystem.Fatigue;
        bool shouldShake = !blinkSystem.IsBlinking && currentFatigue > shakeStart;

        if (shouldShake)
        {
            float t = Mathf.InverseLerp(shakeStart, 1f, currentFatigue);
            float time = Time.time * shakeFrequency;
            float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * maxShakePosition * t;
            float offsetY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * maxShakePosition * t;
            float rotZ = (Mathf.PerlinNoise(time, time) - 0.5f) * 2f * maxShakeAngle * t;

            cameraTransform.localPosition = initialCameraLocalPosition + new Vector3(offsetX, offsetY, 0f);
            cameraTransform.localRotation = initialCameraLocalRotation * Quaternion.Euler(0f, 0f, rotZ);
        }
        else
        {
            cameraTransform.localPosition = Vector3.MoveTowards(
                cameraTransform.localPosition, initialCameraLocalPosition, Time.deltaTime * cameraRecoveryPositionSpeed);
            cameraTransform.localRotation = Quaternion.RotateTowards(
                cameraTransform.localRotation, initialCameraLocalRotation, Time.deltaTime * cameraRecoveryRotationSpeed);
        }
    }

    private void ResetCameraShake()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = initialCameraLocalPosition;
            cameraTransform.localRotation = initialCameraLocalRotation;
        }
    }

    private static Sprite CreateVignetteSprite(float innerRadius)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(0.5f, 0.5f);
        float clampedInner = Mathf.Clamp(innerRadius, 0f, 0.8f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2((float)x / (size - 1), (float)y / (size - 1));
                float dist = Vector2.Distance(uv, center) * 1.4142f;
                float alpha = Mathf.SmoothStep(clampedInner, 1.0f, dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
        if (closedEyesOverlay != null)
        {
            Color overlayColor = closedEyesOverlay.color;
            overlayColor.a = alpha;
            closedEyesOverlay.color = overlayColor;
        }
    }

    private void UpdateFatigue(float fatigue)
    {
        fatigue = Mathf.Clamp01(fatigue);
        if (fatigueSlider != null)
            fatigueSlider.SetValueWithoutNotify(fatigue);

        float redAmount = Mathf.InverseLerp(redTransitionStart, 1f, fatigue);
        if (fatigueFill != null)
            fatigueFill.color = Color.Lerp(Color.green, new Color(1f, 0f, 0f), redAmount);
    }

    private void SetEyesClosed(bool eyesClosed)
    {
        targetOverlayAlpha = eyesClosed ? 1f : 0f;
    }
}
