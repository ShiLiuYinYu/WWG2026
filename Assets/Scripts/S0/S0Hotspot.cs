using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class S0Hotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public S0Controller controller;
    public string action;
    public string label;

    private Image image;
    private WWG.HotspotHoverOutline outlineHover;

    private void Awake()
    {
        image = GetComponent<Image>();
        outlineHover = GetComponent<WWG.HotspotHoverOutline>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            WWG.UISound.Ok();
            controller.HandleHotspot(action);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outlineHover == null && image != null)
            image.color = WWG.HotspotHoverStyle.Fill;
        Image arrow = WWG.NavigationHintLayout.FindArrow(transform);
        controller.SetHover(outlineHover == null || arrow != null ? label : "", arrow);
        WWG.UISound.Cursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverColor();
        controller.SetHover("");
    }

    private void OnDisable() => ResetHoverColor();

    private void ResetHoverColor()
    {
        if (outlineHover == null && image != null)
            image.color = Color.clear;
    }
}
