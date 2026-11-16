using UnityEngine;
using UnityEngine.EventSystems;

public sealed class S2InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string item;
    [System.NonSerialized] public S2InventoryGrid owner;
    private Vector3 originalScale;

    private void Awake() { originalScale = transform.localScale; }
    public void OnPointerEnter(PointerEventData data)
    {
        transform.localScale = originalScale * 1.12f;
        if (owner != null) owner.Hover(item);
    }
    public void OnPointerExit(PointerEventData data) { transform.localScale = originalScale; }
    public void OnPointerClick(PointerEventData data)
    {
        if (owner != null) owner.Hover(item);
    }
}
