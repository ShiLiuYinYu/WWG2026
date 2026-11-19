using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Shows a destination hint and an outline for a scene navigation button.</summary>
    public sealed class NavigationArrowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image outline;
        [SerializeField] private Text hintText;
        [SerializeField] private string hint;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            Hide();
        }

        private void OnDisable() { Hide(); }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.IsInteractable()) return;
            if (outline != null) outline.enabled = true;
            if (hintText != null) hintText.text = hint;
        }

        public void OnPointerExit(PointerEventData eventData) { Hide(); }

        private void Hide()
        {
            if (outline != null) outline.enabled = false;
            if (hintText != null && hintText.text == hint) hintText.text = "";
        }

#if UNITY_EDITOR
        public void Configure(Image edge, Text caption, string destination)
        {
            outline = edge;
            hintText = caption;
            hint = destination;
        }
#endif
    }
}
