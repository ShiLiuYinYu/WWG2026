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
            if (hintText != null)
            {
                hintText.text = hint;
                PositionHint();
            }
        }

        /// <summary>把提示文本贴到当前箭头正下方，避免固定在屏幕底部导致与箭头距离忽远忽近。</summary>
        private void PositionHint()
        {
            RectTransform captionRect = hintText.rectTransform;
            RectTransform parentRect = captionRect.parent as RectTransform;
            RectTransform arrowRect = transform as RectTransform;
            if (parentRect == null || arrowRect == null) return;

            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 0.5f);
            captionRect.pivot = new Vector2(0.5f, 1f);

            Vector3 bottomWorld = arrowRect.TransformPoint(new Vector3(0f, arrowRect.rect.yMin, 0f));
            Vector2 localBottom = parentRect.InverseTransformPoint(bottomWorld);
            captionRect.anchoredPosition = new Vector2(localBottom.x, localBottom.y - 10f);
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
