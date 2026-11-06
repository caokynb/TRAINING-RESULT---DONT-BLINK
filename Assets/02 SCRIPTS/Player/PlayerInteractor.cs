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
    [Tooltip("Layer mask cho các vật thể tương tác")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("UI & Visuals (K24)")]
    [SerializeField] private Sprite keyIconSprite;
    [SerializeField] private Font customFont;

    private Camera playerCamera;
    private PlayerInputReader inputReader;
    private BlinkSystem blinkSystem;

    private IInteractable currentTarget;
    private GameObject currentTargetObject;
    private bool isEyesClosed;

    // UI Elements
    private Canvas interactorCanvas;
    private GameObject promptRoot;
    private Image promptKeyIcon;
    private Text promptText;
    private Image crosshairDot;
    private GameObject hudKeyBadge;

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
        BuildUI();
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
        if (interactorCanvas != null)
        {
            Destroy(interactorCanvas.gameObject);
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

    private void PerformInteractionRaycast()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers, QueryTriggerInteraction.Collide);

        if (hitSomething && hit.collider != null)
        {
            // Kiểm tra IInteractable trên collider hoặc cha
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            GameObject hitObj = null;

            if (interactable != null)
            {
                hitObj = (interactable as MonoBehaviour)?.gameObject;
            }
            else
            {
                // Fallback kiểm tra trực tiếp KeyPickup hoặc ExitDoorController
                KeyPickup kp = hit.collider.GetComponentInParent<KeyPickup>();
                if (kp != null)
                {
                    interactable = kp;
                    hitObj = kp.gameObject;
                }
                else
                {
                    ExitDoorController door = hit.collider.GetComponentInParent<ExitDoorController>();
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
            Debug.Log($"[PlayerInteractor] Tương tác với: {currentTargetObject?.name}");
            currentTarget.Interact();

            // Cập nhật lại UI sau tương tác
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

    private void HandleEyesClosed()
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

        // Nếu là chìa khóa, hiện icon chìa khóa cạnh prompt
        bool isKey = (interactable is KeyPickup) || (currentTargetObject != null && currentTargetObject.name.Contains("Key"));
        if (promptKeyIcon != null)
        {
            promptKeyIcon.gameObject.SetActive(isKey && keyIconSprite != null);
        }

        // Đổi màu prompt (đỏ nếu cửa bị khóa, trắng/xanh nếu có thể tương tác)
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

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("Player_Interaction_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        interactorCanvas = canvasObj.GetComponent<Canvas>();
        interactorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        interactorCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

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
        promptText.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
        badgeLabel.font = promptText.font;
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
