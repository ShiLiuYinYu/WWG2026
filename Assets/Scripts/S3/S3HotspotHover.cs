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
        if (prompt != null) prompt.SetActive(false);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (prompt != null) prompt.SetActive(true);
        if (surface != null) surface.color = new Color(1f, 0.82f, 0.34f, 0.12f);
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
