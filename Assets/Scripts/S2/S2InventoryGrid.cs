using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>The authored 5x5 S2 bag UI; it never creates cells at runtime.</summary>
public sealed class S2InventoryGrid : MonoBehaviour
{
    [SerializeField] private GameObject[] sceneInteractions = System.Array.Empty<GameObject>();

    private S2Controller controller;
    private S3Controller s3Controller;
    private S4Controller s4Controller;
    private GameObject inventoryContents;
    private Image inventoryBackground;
    private GameObject tooltip;
    private Text nameLabel;
    private string selected;
    private GameObject bagButton;
    private VitalsBars vitals;
    private CanvasGroup[] sceneInteractionGroups;
    private S2InventorySlot[] slots;

    private bool InventoryBlocked => controller != null ? controller.IsInventoryBlocked :
        s3Controller != null ? s3Controller.IsInventoryBlocked : s4Controller == null || s4Controller.IsInventoryBlocked;
    private bool CanInteract => isActiveAndEnabled && !InventoryBlocked;
    internal bool CanInteractWithSlots => inventoryContents.activeInHierarchy && CanInteract;
    private bool CanInteractWithItemActions => tooltip.activeInHierarchy && CanInteract;

    private void Awake()
    {
        controller = FindFirstObjectByType<S2Controller>();
        if (controller == null) s3Controller = FindFirstObjectByType<S3Controller>();
        if (controller == null && s3Controller == null) s4Controller = FindFirstObjectByType<S4Controller>();
        SharedInteractionHud hud = transform.parent.GetComponentInChildren<SharedInteractionHud>(true);
        if (hud != null)
        {
            bagButton = hud.inventoryButton.gameObject;
            vitals = hud.vitals;
        }
        else
            foreach (Transform child in transform.parent)
                if (child.name == "Inventory" && child.GetComponent<Button>() != null)
                    bagButton = child.gameObject;
        sceneInteractionGroups = new CanvasGroup[sceneInteractions.Length];
        for (int i = 0; i < sceneInteractions.Length; i++)
        {
            if (sceneInteractions[i] != null)
                sceneInteractionGroups[i] = sceneInteractions[i].AddComponent<CanvasGroup>();
        }

        inventoryContents = transform.Find("Bag contents").gameObject;
        inventoryBackground = GetComponent<Image>();
        tooltip = transform.Find("Tooltip").gameObject;
        nameLabel = tooltip.transform.Find("Name").GetComponent<Text>();
        tooltip.transform.Find("Use").GetComponent<Button>().onClick.AddListener(Use);
        tooltip.transform.Find("Inspect").GetComponent<Button>().onClick.AddListener(Inspect);
        tooltip.transform.Find("Close").GetComponent<Button>().onClick.AddListener(CloseItemActions);
        slots = GetComponentsInChildren<S2InventorySlot>(true);
        foreach (var slot in slots) slot.owner = this;
        tooltip.SetActive(false);
    }

    private void OnEnable()
    {
        SetSceneControlsVisible(false);
        if (!InventoryBlocked) RefreshSlots();
    }
    private void OnDisable()
    {
        ResetItemActions();
        SetSceneControlsVisible(true);
    }

    private void SetSceneControlsVisible(bool visible)
    {
        if (bagButton != null) bagButton.SetActive(visible);
        if (vitals != null) vitals.gameObject.SetActive(visible);
        foreach (var group in sceneInteractionGroups)
        {
            if (group == null) continue;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }

    private void Update()
    {
        if (InventoryBlocked)
        {
            gameObject.SetActive(false);
            return;
        }
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        foreach (var slot in slots)
        {
            bool owned = controller != null ? controller.HasItem(slot.item) :
                s3Controller != null ? s3Controller.HasItem(slot.item) : s4Controller.HasItem(slot.item);
            slot.SetOccupied(owned);
        }
    }

    public void Select(string item)
    {
        if (!CanInteractWithSlots) return;
        selected = item;
        nameLabel.text = ItemName(item);
        SetInventoryContentsVisible(false);
        tooltip.SetActive(true);
    }

    private void Use()
    {
        if (!CanInteractWithItemActions || selected == null) return;
        string item = selected;
        gameObject.SetActive(false);
        if (controller != null) controller.ShowInventoryUseHint(item);
        else if (s3Controller != null) s3Controller.ShowInventoryUseHint(item);
        else s4Controller.UseInventoryItem(item);
    }

    private void Inspect()
    {
        if (!CanInteractWithItemActions || selected == null) return;
        string item = selected;
        gameObject.SetActive(false);
        if (controller != null) controller.InspectInventoryItem(item);
        else if (s3Controller != null) s3Controller.InspectInventoryItem(item);
        else s4Controller.InspectInventoryItem(item);
    }

    private void SetInventoryContentsVisible(bool visible)
    {
        inventoryBackground.enabled = visible;
        inventoryContents.SetActive(visible);
    }

    private void CloseItemActions()
    {
        if (CanInteractWithItemActions) ResetItemActions();
    }

    private void ResetItemActions()
    {
        tooltip.SetActive(false);
        selected = null;
        SetInventoryContentsVisible(true);
    }

    public static string ItemName(string item) => item == StoryIds.Item.Photo ? "去年的活动室照片" :
        item == StoryIds.Item.Badge ? "林未的旧工牌" :
        item == StoryIds.Item.MachineNote ? "机房借用便签" :
        item == StoryIds.Item.TeamSheet ? "分工表残页" :
        item == StoryIds.Item.OldProjectUsb ? "装有旧工程的 U 盘" : item;
}

