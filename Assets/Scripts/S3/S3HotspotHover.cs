using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Hover prompt for an object still baked into the scene illustration.</summary>
public sealed class S3HotspotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject prompt;
    private Image surface;

    private void Awake()
    {
        surface = GetComponent<Image>();
        if (prompt != null)
        {
            if (prompt.GetComponentInChildren<Text>(true) is Text caption)
                caption.color = WWG.HotspotHoverStyle.Caption;
            prompt.SetActive(false);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (prompt != null) prompt.SetActive(true);
        if (surface != null) surface.color = WWG.HotspotHoverStyle.Fill;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (prompt != null) prompt.SetActive(false);
        if (surface != null) surface.color = Color.clear;
    }
    private void OnDisable()
    {
        if (prompt != null) prompt.SetActive(false);
        if (surface != null) surface.color = Color.clear;
    }
}
