using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>The authored 5x5 S2 bag UI; it never creates cells at runtime.</summary>
public sealed class S2InventoryGrid : MonoBehaviour
{
    private S2Controller controller;
    private S3Controller s3Controller;
    private S4Controller s4Controller;
    private GameObject tooltip;
    private Text nameLabel;
    private string selected;
    private bool tooltipPinned;
    private GameObject bagButton;
    private S2InventorySlot[] slots;

    private void Awake()
    {
        controller = FindFirstObjectByType<S2Controller>();
        if (controller == null) s3Controller = FindFirstObjectByType<S3Controller>();
        if (controller == null && s3Controller == null) s4Controller = FindFirstObjectByType<S4Controller>();
        foreach (Transform child in transform.parent)
            if (child.name == "Inventory" && child.GetComponent<Button>() != null)
                bagButton = child.gameObject;
        tooltip = transform.Find("Tooltip").gameObject;
        nameLabel = tooltip.transform.Find("Name").GetComponent<Text>();
        tooltip.transform.Find("Use").GetComponent<Button>().onClick.AddListener(Use);
        tooltip.transform.Find("Inspect").GetComponent<Button>().onClick.AddListener(Inspect);
        slots = GetComponentsInChildren<S2InventorySlot>(true);
        foreach (var slot in slots) slot.owner = this;
        tooltip.SetActive(false);
    }

    private void OnEnable()
    {
        if (tooltip != null) tooltip.SetActive(false);
        selected = null;
        tooltipPinned = false;
        if (bagButton != null) bagButton.SetActive(false);
    }
    private void OnDisable()
    {
        if (bagButton != null) bagButton.SetActive(true);
    }

    private void Update()
    {
        if (controller == null && s3Controller == null && s4Controller == null) return;
        if (controller != null && controller.CurrentState == null) return;
        if (s3Controller != null && s3Controller.CurrentState == null) return;
        if (s4Controller != null && s4Controller.CurrentState == null) return;
        foreach (var slot in slots)
        {
            bool owned = controller != null ? controller.HasItem(slot.item) :
                s3Controller != null ? s3Controller.HasItem(slot.item) : s4Controller.HasItem(slot.item);
            if (slot.gameObject.activeSelf != owned) slot.gameObject.SetActive(owned);
        }
        // S2 items currently serve as evidence; none has a scene-use target yet.
    }

    public void Hover(string item)
    {
        if (tooltipPinned) return;
        ShowTooltip(item);
    }

    public void Select(string item)
    {
        ShowTooltip(item);
        tooltipPinned = true;
    }

    private void ShowTooltip(string item)
    {
        selected = item;
        nameLabel.text = ItemName(item);
        tooltip.SetActive(true);
    }

    private void Use()
    {
        if (selected == null) return;
        if (controller != null) { controller.CloseInventory(); controller.ShowInventoryUseHint(selected); }
        else if (s3Controller != null) { s3Controller.CloseInventory(); s3Controller.ShowInventoryUseHint(selected); }
        else { s4Controller.CloseInventory(); s4Controller.ShowInventoryUseHint(selected); }
        selected = null;
        tooltipPinned = false;
    }

    private void Inspect()
    {
        if (selected == null) return;
        string item = selected;
        selected = null;
        tooltipPinned = false;
        if (controller != null) { controller.CloseInventory(); controller.InspectInventoryItem(item); }
        else if (s3Controller != null) { s3Controller.CloseInventory(); s3Controller.InspectInventoryItem(item); }
        else { s4Controller.CloseInventory(); s4Controller.InspectInventoryItem(item); }
    }

    public static string ItemName(string item) => item == StoryIds.Item.Photo ? "去年的活动室照片" :
        item == StoryIds.Item.Badge ? "林未的旧工牌" :
        item == StoryIds.Item.MachineNote ? "机房借用便签" :
        item == StoryIds.Item.TeamSheet ? "分工表残页" :
        item == StoryIds.Item.OldProjectUsb ? "装有旧工程的 U 盘" : item;
}

