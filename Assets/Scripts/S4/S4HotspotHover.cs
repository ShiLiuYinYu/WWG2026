using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Hover cue for S4 hotspots without a separate cutout image.</summary>
public sealed class S4HotspotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text hint;
    public string caption;
    public Image image;

    public void OnPointerEnter(PointerEventData data)
    {
        if (hint != null)
        {
            hint.text = caption;
            hint.color = WWG.HotspotHoverStyle.Caption;
            hint.gameObject.SetActive(true);
        }
        if (image != null) image.color = WWG.HotspotHoverStyle.Fill;
    }
    public void OnPointerExit(PointerEventData data) => Hide();
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (hint != null) hint.gameObject.SetActive(false);
        if (image != null) image.color = new Color(1f, 1f, 1f, 0.001f);
    }
}
