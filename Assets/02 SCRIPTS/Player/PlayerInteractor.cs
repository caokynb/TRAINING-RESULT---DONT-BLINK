using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Khoảng cách tương tác tối đa (K23 yêu cầu khoảng 3m)")]
    [SerializeField] private float interactDistance = 3.0f;
    [Tooltip("Bán kính tia ngắm để nhặt dễ dàng (m)")]
    [SerializeField] private float sphereCastRadius = 0.2f;
    [Tooltip("Layer mask cho các vật thể tương tác")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("UI & Visuals (K24)")]
    [SerializeField] private Sprite keyIconSprite;
    [SerializeField] private Font customFont;

    [Header("UI Hierarchy References (Tùy chỉnh trong Editor)")]
    [SerializeField] private Canvas interactorCanvas;
    [SerializeField] private Image crosshairDot;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Image promptKeyIcon;
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject hudKeyBadge;

    private Camera playerCamera;
    private PlayerInputReader inputReader;
    private BlinkSystem blinkSystem;

    private IInteractable currentTarget;
    private GameObject currentTargetObject;
    private bool isEyesClosed;

    // Highlight tracking
    private readonly List<Renderer> highlightedRenderers = new List<Renderer>();
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        blinkSystem = GetComponent<BlinkSystem>();

        ResolveCamera();
        ResolveKeyIcon();

        try
        {
            BuildUI();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayerInteractor] BuildUI warning: {ex.Message}");
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.InteractPressed += HandleInteractPressed;
        }

        BlinkEvents.OnEyesClosed += HandleEyesClosed;
        BlinkEvents.OnEyesOpened += HandleEyesOpened;
        GameEvents.OnKeyPickedUp += HandleKeyPickedUp;

        if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey)
        {
            SetHudKeyVisible(true);
        }
        else
        {
            SetHudKeyVisible(false);
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.InteractPressed -= HandleInteractPressed;
        }

        BlinkEvents.OnEyesClosed -= HandleEyesClosed;
        BlinkEvents.OnEyesOpened -= HandleEyesOpened;
        GameEvents.OnKeyPickedUp -= HandleKeyPickedUp;

        ClearTarget();
    }

    private void OnDestroy()
    {
        if (interactorCanvas != null && Application.isPlaying)
        {
            // Không xóa nếu là đối tượng persistent trong Hierarchy
        }
    }

    private void Update()
    {
        ResolveCamera();

        // K23: Nhắm mắt thì không raycast / tương tác
        if (isEyesClosed || (blinkSystem != null && blinkSystem.IsBlinking))
        {
            ClearTarget();
            return;
        }

        PerformInteractionRaycast();
    }

    private void ResolveCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }

    private void ResolveKeyIcon()
    {
        if (keyIconSprite != null)
            return;

#if UNITY_EDITOR
        keyIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/03 ASSETS/UI/Key_Icon.png");
#endif
        if (keyIconSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("Key_Icon");
            if (tex != null)
            {
                keyIconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
    }

    public bool ResolveExistingReferences()
    {
        if (interactorCanvas == null)
        {
            Transform found = transform.Find("Player_Interaction_Canvas");
            if (found != null)
                interactorCanvas = found.GetComponent<Canvas>();
        }

        if (interactorCanvas == null)
            return false;

        if (crosshairDot == null)
        {
            Transform t = interactorCanvas.transform.Find("CrosshairDot");
            if (t != null) crosshairDot = t.GetComponent<Image>();
        }

        if (promptRoot == null)
        {
            Transform t = interactorCanvas.transform.Find("PromptPanel");
            if (t != null)
            {
                promptRoot = t.gameObject;
                Transform iconT = t.Find("PromptKeyIcon");
                if (iconT != null) promptKeyIcon = iconT.GetComponent<Image>();
                Transform textT = t.Find("PromptText");
                if (textT != null) promptText = textT.GetComponent<Text>();
            }
        }

        if (hudKeyBadge == null)
        {
            Transform t = interactorCanvas.transform.Find("HUD_KeyBadge");
            if (t != null) hudKeyBadge = t.gameObject;
        }

        return crosshairDot != null && promptRoot != null;
    }

    [ContextMenu("Tạo / Hiển thị HUD trong Scene (Edit Mode)")]
    public void SetupUIInEditor()
    {
        ResolveKeyIcon();

        if (ResolveExistingReferences())
        {
            // Hiển thị preview các thành phần để căn chỉnh
            if (crosshairDot != null) crosshairDot.gameObject.SetActive(true);
            if (promptRoot != null)
            {
                promptRoot.SetActive(true);
                if (promptText != null) promptText.text = "Chuột trái / E: Nhặt chìa khóa";
                if (promptKeyIcon != null && keyIconSprite != null)
                {
                    promptKeyIcon.sprite = keyIconSprite;
                    promptKeyIcon.gameObject.SetActive(true);
                }
            }
            if (hudKeyBadge != null)
            {
                hudKeyBadge.SetActive(true);
            }
            return;
        }

        BuildUI();

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
            if (promptText != null) promptText.text = "Chuột trái / E: Nhặt chìa khóa";
            if (promptKeyIcon != null && keyIconSprite != null)
            {
                promptKeyIcon.sprite = keyIconSprite;
                promptKeyIcon.gameObject.SetActive(true);
            }
        }

        if (hudKeyBadge != null)
        {
            hudKeyBadge.SetActive(true);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (interactorCanvas != null)
            UnityEditor.EditorUtility.SetDirty(interactorCanvas.gameObject);
#endif
    }

    [ContextMenu("Ẩn Preview HUD (Sẵn sàng Play)")]
    public void HidePreviewInEditor()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
        if (hudKeyBadge != null) hudKeyBadge.SetActive(false);
    }

    private void PerformInteractionRaycast()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Bắn tia dạng SphereCast với bán kính nhỏ để người chơi không phải ngắm tuyệt đối chính xác từng pixel
        RaycastHit[] hits = Physics.SphereCastAll(ray, sphereCastRadius, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            hits = Physics.RaycastAll(ray, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);
        }

        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                    continue;

                // QUAN TRỌNG: Bỏ qua chính collider của Player (CharacterController / Camera)
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                GameObject hitObj = null;

                if (interactable != null)
                {
                    hitObj = (interactable as MonoBehaviour)?.gameObject;
                }
                else
                {
                    // Fallback 1: KeyPickup
                    KeyPickup kp = hit.collider.GetComponentInParent<KeyPickup>();
                    if (kp != null)
                    {
                        interactable = kp;
                        hitObj = kp.gameObject;
                    }
                    else
                    {
                        // Fallback 2: ExitDoorController
                        ExitDoorController door = hit.collider.GetComponentInParent<ExitDoorController>();
                        if (door == null && (hit.collider.name.Contains("Door") || hit.collider.CompareTag("Exit") || hit.collider.CompareTag("ExitDoor")))
                        {
                            door = FindFirstObjectByType<ExitDoorController>();
                        }

                        if (door != null)
                        {
                            interactable = door;
                            hitObj = door.gameObject;
                        }
                    }
                }

                if (interactable != null && interactable.CanInteract())
                {
                    SetTarget(interactable, hitObj);
                    return;
                }

                if (!hit.collider.isTrigger && hit.distance > 0.05f)
                {
                    break;
                }
            }
        }

        ClearTarget();
    }

    private void SetTarget(IInteractable interactable, GameObject targetObj)
    {
        if (currentTarget != interactable || currentTargetObject != targetObj)
        {
            ClearTarget();
            currentTarget = interactable;
            currentTargetObject = targetObj;
            ApplyHighlight(targetObj);
        }

        UpdatePromptUI(interactable);
    }

    private void ClearTarget()
    {
        if (currentTargetObject != null)
        {
            RemoveHighlight();
        }

        currentTarget = null;
        currentTargetObject = null;

        if (promptRoot != null && promptRoot.activeSelf)
        {
            promptRoot.SetActive(false);
        }
    }

    private void HandleInteractPressed()
    {
        if (isEyesClosed || (blinkSystem != null && blinkSystem.IsBlinking))
            return;

        if (currentTarget != null && currentTarget.CanInteract())
        {
            Debug.Log($"[PlayerInteractor] Thực hiện tương tác với: {currentTargetObject?.name}");
            currentTarget.Interact();

            if (currentTarget == null || !currentTarget.CanInteract())
            {
                ClearTarget();
            }
            else
            {
                UpdatePromptUI(currentTarget);
            }
        }
    }

    private void HandleEyesClosed(float duration)
    {
        isEyesClosed = true;
        ClearTarget();
    }

    private void HandleEyesOpened()
    {
        isEyesClosed = false;
    }

    private void HandleKeyPickedUp()
    {
        SetHudKeyVisible(true);
        ClearTarget();
    }

    private void ApplyHighlight(GameObject target)
    {
        if (target == null)
            return;

        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            highlightedRenderers.Add(r);
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorId, new Color(0.35f, 0.35f, 0.12f, 1f));
            r.SetPropertyBlock(propBlock);
        }
    }

    private void RemoveHighlight()
    {
        for (int i = 0; i < highlightedRenderers.Count; i++)
        {
            Renderer r = highlightedRenderers[i];
            if (r != null)
            {
                r.SetPropertyBlock(null);
            }
        }

        highlightedRenderers.Clear();
    }

    private void UpdatePromptUI(IInteractable interactable)
    {
        if (promptRoot == null || promptText == null)
            return;

        string prompt = interactable.GetInteractionPrompt();
        if (string.IsNullOrEmpty(prompt))
        {
            promptRoot.SetActive(false);
            return;
        }

        promptText.text = prompt;

        bool isKey = (interactable is KeyPickup) || (currentTargetObject != null && currentTargetObject.name.Contains("Key"));
        if (promptKeyIcon != null)
        {
            promptKeyIcon.gameObject.SetActive(isKey && keyIconSprite != null);
        }

        if (prompt.Contains("khóa"))
        {
            promptText.color = new Color(1f, 0.35f, 0.35f, 1f);
        }
        else
        {
            promptText.color = Color.white;
        }

        promptRoot.SetActive(true);
    }

    private void SetHudKeyVisible(bool visible)
    {
        if (hudKeyBadge != null)
        {
            hudKeyBadge.SetActive(visible);
        }
    }

    private static Font GetDefaultFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { }

        if (font == null)
        {
            try
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            }
            catch { }
        }

        return font;
    }

    private void BuildUI()
    {
        if (ResolveExistingReferences())
        {
            if (Application.isPlaying)
            {
                if (promptRoot != null) promptRoot.SetActive(false);
                if (hudKeyBadge != null) hudKeyBadge.SetActive(false);
            }
            return;
        }

        GameObject canvasObj = new GameObject("Player_Interaction_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        interactorCanvas = canvasObj.GetComponent<Canvas>();
        interactorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        interactorCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font resolvedFont = customFont != null ? customFont : GetDefaultFont();

        // 1. Crosshair Dot ở giữa màn hình
        GameObject dotObj = new GameObject("CrosshairDot", typeof(RectTransform), typeof(Image));
        dotObj.transform.SetParent(canvasObj.transform, false);
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(6f, 6f);
        dotRect.anchoredPosition = Vector2.zero;
        crosshairDot = dotObj.GetComponent<Image>();
        crosshairDot.color = new Color(1f, 1f, 1f, 0.6f);
        crosshairDot.raycastTarget = false;

        // 2. Prompt Root (ngay dưới crosshair)
        promptRoot = new GameObject("PromptPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        promptRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchoredPosition = new Vector2(0f, -55f);

        HorizontalLayoutGroup layout = promptRoot.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = promptRoot.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Icon chìa khóa trong prompt
        GameObject iconObj = new GameObject("PromptKeyIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(promptRoot.transform, false);
        promptKeyIcon = iconObj.GetComponent<Image>();
        if (keyIconSprite != null)
        {
            promptKeyIcon.sprite = keyIconSprite;
        }
        promptKeyIcon.rectTransform.sizeDelta = new Vector2(40f, 40f);
        promptKeyIcon.raycastTarget = false;
        iconObj.SetActive(false);

        // Text prompt
        GameObject textObj = new GameObject("PromptText", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(promptRoot.transform, false);
        promptText = textObj.GetComponent<Text>();
        if (resolvedFont != null)
            promptText.font = resolvedFont;
        promptText.fontSize = 24;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        promptRoot.SetActive(false);

        // 3. HUD Key Badge (Góc trên bên phải màn hình khi đã nhặt chìa)
        hudKeyBadge = new GameObject("HUD_KeyBadge", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        hudKeyBadge.transform.SetParent(canvasObj.transform, false);
        RectTransform badgeRect = hudKeyBadge.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.anchoredPosition = new Vector2(-40f, -40f);

        VerticalLayoutGroup badgeLayout = hudKeyBadge.GetComponent<VerticalLayoutGroup>();
        badgeLayout.childAlignment = TextAnchor.MiddleCenter;
        badgeLayout.spacing = 4f;

        ContentSizeFitter badgeFitter = hudKeyBadge.GetComponent<ContentSizeFitter>();
        badgeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        badgeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject badgeIconObj = new GameObject("BadgeIcon", typeof(RectTransform), typeof(Image));
        badgeIconObj.transform.SetParent(hudKeyBadge.transform, false);
        Image badgeIcon = badgeIconObj.GetComponent<Image>();
        if (keyIconSprite != null)
        {
            badgeIcon.sprite = keyIconSprite;
        }
        badgeIcon.rectTransform.sizeDelta = new Vector2(56f, 56f);
        badgeIcon.raycastTarget = false;

        GameObject badgeTextObj = new GameObject("BadgeLabel", typeof(RectTransform), typeof(Text), typeof(Outline));
        badgeTextObj.transform.SetParent(hudKeyBadge.transform, false);
        Text badgeLabel = badgeTextObj.GetComponent<Text>();
        if (resolvedFont != null)
            badgeLabel.font = resolvedFont;
        badgeLabel.fontSize = 18;
        badgeLabel.fontStyle = FontStyle.Bold;
        badgeLabel.text = "CHÌA KHÓA";
        badgeLabel.color = new Color(1f, 0.9f, 0.3f, 1f);
        badgeLabel.alignment = TextAnchor.MiddleCenter;
        badgeLabel.raycastTarget = false;

        Outline badgeOutline = badgeTextObj.GetComponent<Outline>();
        badgeOutline.effectColor = Color.black;
        badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

        hudKeyBadge.SetActive(false);
    }
}
