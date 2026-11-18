using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    public sealed class GameplayInventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GameplayOverlay owner;
        public string item;
        private CanvasGroup group;
        private Vector3 originalScale;

        private void Awake() { originalScale = transform.localScale; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = originalScale * 1.12f;
            if (owner != null) owner.HoverItem(item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
            {
                UISound.Ok();
                owner.OpenItem(item);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            UISound.Cursor();
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            owner.ShowDragGhost(item);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner.MoveDragGhost(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (group != null) group.blocksRaycasts = true;
            owner.HideDragGhost();
            owner.DragItemTo(item, eventData);
        }
    }
}
