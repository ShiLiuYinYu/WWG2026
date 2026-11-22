using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    internal static class HotspotHoverStyle
    {
        internal static readonly Color Caption = new Color32(255, 221, 82, 255);
        internal static readonly Color Fill = new Color32(255, 221, 82, 42);
    }

    /// <summary>Shows a cutout outline or a caption and highlight on hover.</summary>
    public sealed class HotspotHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic highlight;
        private Image surface;
        private Color idleColor;

        private void Awake()
        {
            if (highlight is Text caption)
            {
                caption.color = HotspotHoverStyle.Caption;
                Image image = GetComponent<Image>();
                if (image != null && image.sprite == null && image.color.a <= 0.005f)
                {
                    surface = image;
                    idleColor = image.color;
                }
            }
            Hide();
        }

        private void OnDisable() { Hide(); }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Hide();
        }

        private void Update()
        {
            if (highlight == null || !highlight.enabled) return;
            EventSystem current = EventSystem.current;
            if (current != null && !current.IsPointerOverGameObject()) Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlight != null) highlight.enabled = true;
            if (surface != null) surface.color = HotspotHoverStyle.Fill;
            if (highlight is Text caption && NavigationHintLayout.FindArrow(transform) is Image arrow)
                NavigationHintLayout.Place(caption, arrow);
        }

        public void OnPointerExit(PointerEventData eventData) { Hide(); }

        private void Hide()
        {
            if (highlight != null) highlight.enabled = false;
            if (surface != null) surface.color = idleColor;
        }
    }
}
