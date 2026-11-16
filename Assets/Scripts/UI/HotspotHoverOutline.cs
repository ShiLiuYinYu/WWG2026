using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Highlights an authored cutout image while its hotspot is hovered.</summary>
    public sealed class HotspotHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic highlight;

        private void Awake() { Hide(); }
        private void OnDisable() { Hide(); }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlight != null) highlight.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData) { Hide(); }

        private void Hide()
        {
            if (highlight != null) highlight.enabled = false;
        }
    }
}
