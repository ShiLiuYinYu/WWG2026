using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    public sealed class GameplayInventorySlot : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GameplayOverlay owner;
        public string item;
        private CanvasGroup group;
        private RectTransform ghost;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
                owner.OpenItem(item);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var go = new GameObject("Dragged item", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(owner.transform, false);
            ghost = go.GetComponent<RectTransform>();
            ghost.sizeDelta = new Vector2(68, 68);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.75f, 0.86f, 0.91f, 0.75f);
            image.raycastTarget = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null) ghost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (group != null) group.blocksRaycasts = true;
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            owner.DragItemTo(item, eventData);
        }
    }
}
