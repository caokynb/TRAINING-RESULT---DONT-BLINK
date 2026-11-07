using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
public class PortalMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("Thanh chữ nhật highlight nền mờ xuất hiện khi rê chuột.")]
    [SerializeField] private Image highlightBar;

    [Tooltip("Chữ tiêu đề của nút (TextMeshPro).")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Color Settings")]
    [Tooltip("Màu chữ ở trạng thái bình thường.")]
    [SerializeField] private Color normalTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("Màu chữ khi rê chuột vào.")]
    [SerializeField] private Color hoverTextColor = Color.white;

    [Header("Animation Settings")]
    [Tooltip("Độ dịch chuyển chữ sang phải (pixel) khi hover.")]
    [SerializeField] private float slideOffset = 12f;

    [Tooltip("Tốc độ chuyển động mượt của chữ.")]
    [SerializeField] private float transitionSpeed = 14f;

    private Vector2 originalTextPos;
    private Vector2 targetTextPos;
    private bool isHovered = false;

    private void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            originalTextPos = buttonText.rectTransform.anchoredPosition;
            targetTextPos = originalTextPos;
            buttonText.color = normalTextColor;

            // QUAN TRỌNG: Tắt Raycast Target trên Text con để không nuốt tia chuột của Button
            buttonText.raycastTarget = false;
        }

        if (highlightBar != null)
        {
            highlightBar.gameObject.SetActive(false);
            highlightBar.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        isHovered = false;
        if (highlightBar != null)
            highlightBar.gameObject.SetActive(false);

        if (buttonText != null)
        {
            buttonText.color = normalTextColor;
            buttonText.rectTransform.anchoredPosition = originalTextPos;
            targetTextPos = originalTextPos;
            buttonText.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (buttonText != null)
        {
            buttonText.rectTransform.anchoredPosition = Vector2.Lerp(
                buttonText.rectTransform.anchoredPosition,
                targetTextPos,
                Time.unscaledDeltaTime * transitionSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (highlightBar != null)
            highlightBar.gameObject.SetActive(true);

        if (buttonText != null)
        {
            buttonText.color = hoverTextColor;
            targetTextPos = originalTextPos + new Vector2(slideOffset, 0f);
        }

        AudioManager.Instance?.PlayUIClick();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (highlightBar != null)
            highlightBar.gameObject.SetActive(false);

        if (buttonText != null)
        {
            buttonText.color = normalTextColor;
            targetTextPos = originalTextPos;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. Nếu script này nằm trên object con (Text) thay vì Button cha, chuyển tiếp click lên Button cha
        Button parentButton = GetComponentInParent<Button>();
        if (parentButton != null && parentButton.gameObject != gameObject)
        {
            if (parentButton.interactable)
            {
                parentButton.onClick.Invoke();
            }
            return;
        }

        // 2. Nếu script này nằm trên chính Button thì Button đã tự gọi onClick
        AudioManager.Instance?.PlayUIClick();
    }
}
