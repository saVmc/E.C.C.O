using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class NeonUIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Color idleColor = new Color(0.75f, 0.9f, 1f, 0.92f);
    [SerializeField] private Color hoverColor = new Color(0.45f, 1f, 0.98f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.45f, 0.98f, 1f);
    [SerializeField] private float pulseAmount = 0.04f;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.94f;

    private Vector3 baseScale;
    private bool isActive = true;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();

        baseScale = targetRect != null ? targetRect.localScale : Vector3.one;
        ApplyState();
    }

    private void Update()
    {
        if (!isActive || targetGraphic == null || targetRect == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        Vector3 scale = baseScale * pulse;

        if (isPressed)
            scale *= pressedScale;
        else if (isHovered)
            scale *= hoverScale;

        targetRect.localScale = scale;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        isHovered = false;
        isPressed = false;
        ApplyState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActive) return;
        isHovered = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActive) return;
        isHovered = false;
        isPressed = false;
        ApplyState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;
        isPressed = true;
        ApplyState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isActive) return;
        isPressed = false;
        ApplyState();
    }

    private void ApplyState()
    {
        if (targetGraphic == null)
            return;

        if (!isActive)
        {
            targetGraphic.color = Color.Lerp(idleColor, Color.black, 0.45f);
            return;
        }

        if (isPressed)
            targetGraphic.color = pressedColor;
        else if (isHovered)
            targetGraphic.color = hoverColor;
        else
            targetGraphic.color = idleColor;
    }
}
