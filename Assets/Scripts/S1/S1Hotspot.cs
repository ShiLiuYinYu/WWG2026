using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class S1Hotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public S1Controller controller;
    public string action;
    public string label;
    public RectTransform tapeTarget;

    private Image image;
    private RectTransform rect;
    private Vector2 dragStart;
    private bool dropSucceeded;

    private void Awake() { image = GetComponent<Image>(); rect = GetComponent<RectTransform>(); }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !dropSucceeded)
        {
            if (!WWG.GameplayOverlay.TryUseSelected(action)) controller.HandleHotspot(action);
        }
    }

    public void OnPointerDown(PointerEventData eventData) { dropSucceeded = false; }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (action != "chair" || tapeTarget == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(tapeTarget, eventData.position, null)) return;
        dropSucceeded = true;
        controller.HandleHotspot("chair");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null && action != "chair") image.color = new Color(0.65f, 0.88f, 0.95f, 0.16f);
        controller.SetHover(label);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null && action != "chair") image.color = new Color(0.65f, 0.88f, 0.95f, 0.005f);
        controller.SetHover("");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (action != "chair") return;
        dragStart = rect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (action != "chair") return;
        RectTransform parent = rect.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, null, out Vector2 local))
            rect.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (action != "chair") return;
        if (!dropSucceeded) rect.anchoredPosition = dragStart;
    }
}
