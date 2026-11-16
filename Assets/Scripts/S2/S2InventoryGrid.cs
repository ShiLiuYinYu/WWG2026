using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>The authored 5x5 S2 bag UI; it never creates cells at runtime.</summary>
public sealed class S2InventoryGrid : MonoBehaviour
{
    private S2Controller controller;
    private GameObject tooltip;
    private Text nameLabel;
    private string selected;
    private GameObject bagButton;
    private S2InventorySlot[] slots;

    private void Awake()
    {
        controller = FindFirstObjectByType<S2Controller>();
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
        if (bagButton != null) bagButton.SetActive(false);
    }
    private void OnDisable()
    {
        if (bagButton != null) bagButton.SetActive(true);
    }

    private void Update()
    {
        if (controller == null || controller.CurrentState == null) return;
        foreach (var slot in slots)
        {
            bool owned = controller.HasItem(slot.item);
            if (slot.gameObject.activeSelf != owned) slot.gameObject.SetActive(owned);
        }
        // S2 items currently serve as evidence; none has a scene-use target yet.
    }

    public void Hover(string item)
    {
        selected = item;
        nameLabel.text = ItemName(item);
        tooltip.SetActive(true);
    }

    private void Use()
    {
        if (selected == null) return;
        controller.CloseInventory();
        controller.ShowInventoryUseHint(selected);
        selected = null;
    }

    private void Inspect()
    {
        if (selected == null) return;
        string item = selected;
        selected = null;
        controller.CloseInventory();
        controller.InspectInventoryItem(item);
    }

    public static string ItemName(string item) => item == StoryIds.Item.Photo ? "去年的活动室照片" :
        item == StoryIds.Item.Badge ? "林未的旧工牌" :
        item == StoryIds.Item.MachineNote ? "机房借用便签" :
        item == StoryIds.Item.TeamSheet ? "分工表残页" : item;
}

