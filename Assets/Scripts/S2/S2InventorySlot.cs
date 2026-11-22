using UnityEngine;
using UnityEngine.EventSystems;
using WWG;

public sealed class S2InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string item;
    [System.NonSerialized] public S2InventoryGrid owner;
    private Vector3 originalScale;
    private InventorySlotContents contents;

    private void Awake() { originalScale = transform.localScale; }
    private void OnDisable() { transform.localScale = originalScale; }

    internal void SetOccupied(bool occupied)
    {
        contents ??= new InventorySlotContents(this);
        contents.SetOccupied(occupied);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (owner == null || !owner.CanInteractWithSlots) return;
        transform.localScale = originalScale * 1.12f;
        UISound.Cursor();
    }
    public void OnPointerExit(PointerEventData data) { transform.localScale = originalScale; }
    public void OnPointerClick(PointerEventData data)
    {
        if ((data.button == PointerEventData.InputButton.Left || data.button == PointerEventData.InputButton.Right) && owner != null)
            owner.Select(item);
    }
}
