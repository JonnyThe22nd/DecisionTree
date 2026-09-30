using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Skalierung beim Hover (1 = keine Änderung)")]
    public float hoverScale = 1.12f;
    [Tooltip("Farbe des Elements beim Hover")]
    public Color hoverColor = new Color(1f, 0.8f, 0.25f, 1f);
    [Tooltip("Wird eingefärbt. Leer = Target Graphic des Buttons bzw. die Graphic auf diesem Objekt")]
    public Graphic targetGraphic;
    public float animationSpeed = 14f;

    Vector3 baseScale;
    Color baseColor;
    bool hovered;
    UIHoverEffect parentEffect;

    void Awake()
    {
        var button = GetComponent<Button>();
        if (button != null)
        {
            if (targetGraphic == null) targetGraphic = button.targetGraphic;
            button.transition = Selectable.Transition.None;
        }
        if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();

        baseScale = transform.localScale;
        if (targetGraphic != null) baseColor = targetGraphic.color;
    }

    void Start()
    {
        if (transform.parent != null)
            parentEffect = transform.parent.GetComponentInParent<UIHoverEffect>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null) return;
        if (eventData.pointerEnter.GetComponentInParent<UIHoverEffect>() != this) return;

        hovered = true;
        if (parentEffect != null) parentEffect.hovered = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;

        if (parentEffect != null && eventData.hovered.Contains(parentEffect.gameObject))
            parentEffect.hovered = true;
    }

    void Update()
    {
        float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);

        transform.localScale = Vector3.Lerp(transform.localScale, hovered ? baseScale * hoverScale : baseScale, t);
        if (targetGraphic != null)
            targetGraphic.color = Color.Lerp(targetGraphic.color, hovered ? hoverColor : baseColor, t);
    }

    void OnDisable()
    {
        hovered = false;
        transform.localScale = baseScale;
        if (targetGraphic != null) targetGraphic.color = baseColor;
    }
}
