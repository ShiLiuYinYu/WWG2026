using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class S0Hotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public S0Controller controller;
    public string action;
    public string label;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
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
        if (image != null) image.color = new Color(0.65f, 0.88f, 0.95f, 0.20f);
        controller.SetHover(label);
        WWG.UISound.Cursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null) image.color = new Color(0.65f, 0.88f, 0.95f, 0.005f);
        controller.SetHover("");
    }
}
