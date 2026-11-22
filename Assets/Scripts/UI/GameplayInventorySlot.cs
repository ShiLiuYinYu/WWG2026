using UnityEngine;
using UnityEngine.EventSystems;

namespace WWG
{
    public sealed class GameplayInventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GameplayOverlay owner;
        public string item;
        private CanvasGroup group;
        private Vector3 originalScale;
        private bool dragging;
        private InventorySlotContents contents;

        private void Awake() { originalScale = transform.localScale; }

        internal void SetOccupied(bool occupied)
        {
            contents ??= new InventorySlotContents(this);
            contents.SetOccupied(occupied);
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
            StopDrag();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner == null || !owner.CanInteractWithInventorySlots) return;
            transform.localScale = originalScale * 1.12f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if ((eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
                && owner != null && owner.OpenItem(item))
                UISound.Ok();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || owner == null || !owner.CanDragInventoryItems) return;
            UISound.Cursor();
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            dragging = true;
            owner.ShowDragGhost(item);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging) owner.MoveDragGhost(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            StopDrag();
            owner.DragItemTo(item, eventData);
        }

        private void StopDrag()
        {
            if (!dragging) return;
            dragging = false;
            if (group != null) group.blocksRaycasts = true;
            if (owner != null) owner.HideDragGhost();
        }
    }
}
