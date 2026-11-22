using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Shared, scene-local HUD for the existing fixed-view locations.</summary>
    public sealed class GameplayOverlay : MonoBehaviour
    {
        private static GameplayOverlay current;
        private S0Controller s0;
        private S1Controller s1;
        private IChapterInventory chapter;
        private GameObject pausePanel, settingsPanel, detailPanel, inventoryPanel, inventoryButton, tooltip, topBar;
        private GameObject inventoryContents;
        private Image inventoryBackground;
        private GameObject shade;
        private Slider volumeSlider, textSpeedSlider;
        private Text textSpeedLabel;
        private Text timeLabel, messageLabel;
        private VitalsBars vitals;
        private Text detailName, detailBody, selectionLabel, volumeLabel;
        private Text tooltipName;
        private RectTransform carriedItem;
        private GameplayInventorySlot[] slots;
        private string selectedItem;
        private string detailItem;
        private float messageUntil;

        internal static bool IsInventoryOpen => current != null && current.inventoryPanel.activeInHierarchy;

        private bool InventoryBlocked => GameplayModalOpen || SaveSlotPanel.IsOpen || pausePanel.activeSelf ||
            (chapter != null && DialogueService.Instance != null && DialogueService.Instance.IsRunning);
        internal bool CanInteractWithInventorySlots => inventoryContents.activeInHierarchy && !InventoryBlocked;
        internal bool CanDragInventoryItems => s1 != null && CanInteractWithInventorySlots;
        private bool CanInteractWithItemActions => tooltip.activeInHierarchy && !InventoryBlocked;

        private void Awake()
        {
            current = this;
            AudioListener.volume = PlayerPrefs.GetFloat("WWG_MasterVolume", 1f);
            s0 = GetComponentInParent<S0Controller>();
            s1 = GetComponentInParent<S1Controller>();
            chapter = GetComponentInParent<IChapterInventory>();
            pausePanel = transform.Find("Pause panel").gameObject;
            settingsPanel = pausePanel.transform.Find("Settings panel").gameObject;
            detailPanel = transform.Find("Item detail").gameObject;
            inventoryPanel = transform.Find("Inventory panel").gameObject;
            inventoryContents = inventoryPanel.transform.Find("Bag contents").gameObject;
            inventoryBackground = inventoryPanel.GetComponent<Image>();
            SharedInteractionHud hud = GetComponentInChildren<SharedInteractionHud>(true);
            inventoryButton = hud.inventoryButton.gameObject;
            tooltip = inventoryPanel.transform.Find("Tooltip").gameObject;
            tooltipName = tooltip.transform.Find("Name").GetComponent<Text>();
            carriedItem = transform.Find("Inventory ghost") as RectTransform;
            carriedItem.gameObject.SetActive(false);
            topBar = hud.topBar;
            var shadeTr = transform.Find("Shade");
            if (shadeTr != null)
            {
                shade = shadeTr.gameObject;
                if (chapter != null) shade.GetComponent<Image>().raycastTarget = true;
            }
            timeLabel = hud.timeText;
            vitals = hud.vitals;
            messageLabel = FindText("Toast/Message");
            detailName = FindText("Item detail/Name");
            detailBody = FindText("Item detail/Body");
            selectionLabel = FindText("Inventory panel/Bag contents/Selection");
            volumeLabel = FindText("Pause panel/Settings panel/Volume value");
            hud.pauseButton.onClick.AddListener(TogglePause);
            hud.recordButton.onClick.AddListener(OpenRecord);
            hud.inventoryButton.onClick.AddListener(ToggleInventory);
            Bind("Inventory panel/Bag contents/Close", ToggleInventory);
            Bind("Inventory panel/Tooltip/Use", UseTooltipItem);
            Bind("Inventory panel/Tooltip/Inspect", InspectTooltipItem);
            Bind("Inventory panel/Tooltip/Close", CloseItemActions);
            Bind("Pause panel/Resume", ClosePause);
            Bind("Pause panel/Save", Save);
            Bind("Pause panel/Load", Load);
            Bind("Pause panel/Settings", OpenSettings);
            Bind("Pause panel/Title", ReturnToTitle);
            Bind("Pause panel/Close", ClosePause);
            Bind("Pause panel/Settings panel/Back", CloseSettings);
            var volumeSlider = transform.Find("Pause panel/Settings panel/Volume").GetComponent<Slider>();
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.SetValueWithoutNotify(AudioListener.volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            textSpeedSlider = transform.Find("Pause panel/Settings panel/Text speed")?.GetComponent<Slider>();
            textSpeedLabel = FindText("Pause panel/Settings panel/Text speed value");
            if (textSpeedSlider != null)
            {
                textSpeedSlider.minValue = 5f;
                textSpeedSlider.maxValue = 80f;
                textSpeedSlider.wholeNumbers = true;
                textSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("WWG_TextSpeed", 30f));
                textSpeedSlider.onValueChanged.AddListener(SetTextSpeed);
            }
            UpdateTextSpeedLabel();
            Bind("Item detail/Use", SelectDetailItem);
            Bind("Item detail/Inspect", InspectDetailItem);
            Bind("Item detail/Close", CloseDetail);
            slots = GetComponentsInChildren<GameplayInventorySlot>(true);
            foreach (var slot in slots) slot.owner = this;
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            detailPanel.SetActive(false);
            inventoryPanel.SetActive(false);
            tooltip.SetActive(false);
            UpdateVolumeLabel();
            AttachButtonSounds();
        }

        private void AttachButtonSounds()
        {
            AttachSound("Pause panel/Resume", TitleButtonFx.ClickSound.Ok);
            AttachSound("Pause panel/Save", TitleButtonFx.ClickSound.Ok);
            AttachSound("Pause panel/Load", TitleButtonFx.ClickSound.Ok);
            AttachSound("Pause panel/Settings", TitleButtonFx.ClickSound.Ok);
            AttachSound("Pause panel/Title", TitleButtonFx.ClickSound.Cancel);
            AttachSound("Pause panel/Close", TitleButtonFx.ClickSound.Cancel);
            AttachSound("Pause panel/Settings panel/Back", TitleButtonFx.ClickSound.Cancel);
            AttachSound("Item detail/Use", TitleButtonFx.ClickSound.Ok);
            AttachSound("Item detail/Inspect", TitleButtonFx.ClickSound.Ok);
            AttachSound("Item detail/Close", TitleButtonFx.ClickSound.Cancel);
            AttachSound("SharedInteractionHud/Top bar/Pause", TitleButtonFx.ClickSound.Ok);
            AttachSound("SharedInteractionHud/Top bar/Record", TitleButtonFx.ClickSound.Ok);
            AttachSound("SharedInteractionHud/Inventory", TitleButtonFx.ClickSound.Ok);
            AttachSound("Inventory panel/Bag contents/Close", TitleButtonFx.ClickSound.Cancel);
            AttachSound("Inventory panel/Tooltip/Close", TitleButtonFx.ClickSound.Cancel);
        }

        private void AttachSound(string path, TitleButtonFx.ClickSound sound)
        {
            Transform t = transform.Find(path);
            if (t == null) return;
            Button b = t.GetComponent<Button>();
            if (b == null) return;
            TitleButtonFx fx = t.GetComponent<TitleButtonFx>();
            if (fx == null) fx = t.gameObject.AddComponent<TitleButtonFx>();
            fx.SetClickSound(sound);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
            ClearCarriedItem();
            Time.timeScale = 1f;
        }

        private Text FindText(string path) => transform.Find(path).GetComponent<Text>();
        private void Bind(string path, UnityEngine.Events.UnityAction action) =>
            transform.Find(path).GetComponent<Button>().onClick.AddListener(action);

        private void Update()
        {
            if (inventoryPanel.activeSelf && InventoryBlocked) CloseInventory();
            if (SaveSlotPanel.IsOpen) return;
            bool dialogueRunning = DialogueService.Instance != null && DialogueService.Instance.IsRunning;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame &&
                (chapter == null || !chapter.CloseGameplayModal()) && !dialogueRunning)
            {
                if (settingsPanel.activeSelf) CloseSettings();
                else if (detailPanel.activeSelf) CloseDetail();
                else if (tooltip.activeSelf) CloseItemActions();
                else if (inventoryPanel.activeSelf) ToggleInventory();
                else if (selectedItem != null) { selectedItem = null; ClearCarriedItem(); }
                else TogglePause();
            }
            if (carriedItem != null && carriedItem.gameObject.activeSelf && Mouse.current != null)
                carriedItem.position = Mouse.current.position.ReadValue();
            if (s0 != null)
            {
                timeLabel.text = s0.timeText.text;
                if (vitals != null)
                {
                    vitals.SetSan(s0.San, 100);
                    vitals.SetHealth(s0.Health, 3);
                }
            }
            else if (s1 != null)
            {
                timeLabel.text = s1.timeText.text;
                if (vitals != null)
                {
                    vitals.SetSan(s1.San, 100);
                    vitals.SetHealth(s1.Health, 3);
                }
            }
            if (chapter != null) timeLabel.text = chapter.ClockText;
            bool hasAnyItem = false;
            foreach (var slot in slots)
            {
                bool occupied = HasItem(slot.item);
                slot.SetOccupied(occupied);
                hasAnyItem |= occupied;
            }
            selectionLabel.text = selectedItem == null ? (hasAnyItem ? "点击物品后选择使用或查看" : "暂无物品") :
                "已选择 " + (selectedItem == "photo" ? "旧照片" : "旧工牌") + " · 点击场景热点使用";
            inventoryButton.SetActive(!InventoryBlocked && !detailPanel.activeSelf && !inventoryPanel.activeSelf);
            if (topBar != null) topBar.SetActive(!dialogueRunning && !inventoryPanel.activeSelf && (chapter == null || !GameplayModalOpen));
            if (shade != null)
            {
                bool wantShade = pausePanel.activeSelf || detailPanel.activeSelf || inventoryPanel.activeSelf || settingsPanel.activeSelf || GameplayModalOpen;
                if (shade.activeSelf != wantShade) shade.SetActive(wantShade);
            }
            if (dialogueRunning)
            {
                if (messageLabel.transform.parent.gameObject.activeSelf)
                    messageLabel.transform.parent.gameObject.SetActive(false);
            }
            else if (messageLabel.transform.parent.gameObject.activeSelf && Time.unscaledTime >= messageUntil)
                messageLabel.transform.parent.gameObject.SetActive(false);
        }

        private bool GameplayModalOpen => chapter?.IsGameplayModalOpen ??
            (s0 != null ? s0.IsGameplayModalOpen : s1 != null && s1.IsGameplayModalOpen);

        private bool HasItem(string item) => chapter?.HasItem(item) ??
            (s1 != null && (item == StoryIds.Item.Photo && s1.HasPhoto || item == StoryIds.Item.Badge && s1.HasBadge));

        private string ItemName(string item) => chapter != null ? S2InventoryGrid.ItemName(item) :
            item == StoryIds.Item.Photo ? "去年的活动室照片" : "林未的旧工牌";

        public void TogglePause()
        {
            if (SaveSlotPanel.IsOpen || DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;
            if (chapter != null && GameplayModalOpen) return;
            if (pausePanel.activeSelf) ClosePause();
            else
            {
                CloseGameplayModals();
                CloseDetail();
                CloseInventory();
                pausePanel.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        /// <summary>信息窗口排在常驻 HUD 之上，暂停前先收起，避免暂停菜单被窗口遮住。</summary>
        private void CloseGameplayModals()
        {
            if (s0 != null)
            {
                if (s0.infoPanel != null && s0.infoPanel.activeSelf) s0.CloseInfo();
                if (s0.memoPanel != null && s0.memoPanel.activeSelf) s0.ToggleMemo();
            }
            else if (s1 != null)
            {
                if (s1.infoPanel != null && s1.infoPanel.activeSelf) s1.CloseInfo();
                if (s1.memoPanel != null && s1.memoPanel.activeSelf) s1.ToggleMemo();
                if (s1.photoView != null) s1.photoView.Close();
            }
        }

        public void ClosePause()
        {
            settingsPanel.SetActive(false);
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void Save()
        {
            ClosePause();
            SaveSlotPanel.OpenSave(CaptureSlot);
        }

        private void Load()
        {
            ClosePause();
            SaveSlotPanel.OpenLoad(CaptureSlot);
        }

        private SaveSlotSnapshot CaptureSlot() => chapter != null ? chapter.CaptureSaveSlot() :
            s0 != null ? s0.CaptureSaveSlot() : s1 != null ? s1.CaptureSaveSlot() : null;

        private void OpenSettings() { settingsPanel.SetActive(true); UpdateVolumeLabel(); }
        private void CloseSettings() { settingsPanel.SetActive(false); }
        private void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("WWG_MasterVolume", value);
            PlayerPrefs.Save();
            UpdateVolumeLabel();
        }
        private void UpdateVolumeLabel() { volumeLabel.text = "主音量  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%"; }

        private void SetTextSpeed(float value)
        {
            PlayerPrefs.SetFloat("WWG_TextSpeed", value);
            PlayerPrefs.Save();
            UpdateTextSpeedLabel();
        }

        private void UpdateTextSpeedLabel()
        {
            if (textSpeedLabel == null) return;
            float v = PlayerPrefs.GetFloat("WWG_TextSpeed", 30f);
            textSpeedLabel.text = v <= 5f ? "文字速度  逐字" : "文字速度  " + Mathf.RoundToInt(v) + " 字/秒";
        }
        private void ReturnToTitle() { ClosePause(); GameFlow.LoadLocation(GameFlow.TitleScene); }
        private void OpenRecord()
        {
            if (pausePanel.activeSelf || detailPanel.activeSelf) return;
            if (chapter != null) chapter.OpenRecord();
            else if (s0 != null) s0.ToggleMemo();
            else if (s1 != null) s1.ToggleMemo();
        }

        public void ToggleInventory()
        {
            if (inventoryPanel.activeSelf) CloseInventory();
            else if (!InventoryBlocked && !detailPanel.activeSelf) SetInventoryOpen(true);
        }

        private void SetInventoryOpen(bool open)
        {
            inventoryPanel.SetActive(open);
            if (vitals != null) vitals.gameObject.SetActive(!open);
            if (s0 != null) s0.UpdateInteractionVisibility();
            if (s1 != null) s1.UpdateInteractionVisibility();
        }

        public bool OpenItem(string item)
        {
            if (!CanInteractWithInventorySlots || !HasItem(item)) return false;
            detailItem = item;
            tooltipName.text = ItemName(item);
            SetInventoryContentsVisible(false);
            tooltip.SetActive(true);
            return true;
        }

        private void OpenDetail(string item)
        {
            detailItem = item;
            detailName.text = item == "photo" ? "去年的活动室照片" : "林未的旧工牌";
            detailBody.text = item == "photo"
                ? "【社团自留资料】\n\n去年拍摄的 5F 活动室。窗帘、旧桌灯和第五把椅子都清晰可见。背面写着 23:30。\n\n可用于核对活动室里的摆设。"
                : "【社团自留资料】\n\n48H GameJam 参赛证，姓名：林未，编号 05。背面手写：玻璃两边，各存一半。4F。\n\n关键物品，不会消耗。";
            detailPanel.SetActive(true);
        }

        private void SetInventoryContentsVisible(bool visible)
        {
            inventoryBackground.enabled = visible;
            inventoryContents.SetActive(visible);
        }

        private void CloseItemActions()
        {
            if (!CanInteractWithItemActions) return;
            tooltip.SetActive(false);
            detailItem = null;
            SetInventoryContentsVisible(true);
        }

        private void CloseInventory()
        {
            tooltip.SetActive(false);
            if (inventoryPanel.activeSelf) SetInventoryOpen(false);
            detailItem = null;
            SetInventoryContentsVisible(true);
        }

        private void UseTooltipItem()
        {
            if (CanInteractWithItemActions) SelectDetailItem();
        }

        private void InspectTooltipItem()
        {
            if (CanInteractWithItemActions) InspectDetailItem();
        }

        private void CloseDetail()
        {
            detailPanel.SetActive(false);
            detailItem = null;
        }

        private void SelectDetailItem()
        {
            if (InventoryBlocked || detailItem == null) return;
            if (chapter != null)
            {
                string item = detailItem;
                CloseInventory();
                CloseDetail();
                chapter.UseInventoryItem(item);
                return;
            }
            selectedItem = detailItem;
            CloseInventory();
            CloseDetail();
            ShowDragGhost(selectedItem);
            ShowToast("选择了" + (selectedItem == "photo" ? "旧照片" : "旧工牌") + "，点击场景热点使用");
        }
        private void InspectDetailItem()
        {
            if (InventoryBlocked || detailItem == null) return;
            string item = detailItem;
            CloseInventory();
            CloseDetail();
            if (chapter != null) { chapter.InspectInventoryItem(item); return; }
            if (s1 == null) { OpenDetail(item); return; }
            if (item == "photo") s1.InspectPhoto();
            else s1.InspectBadge();
        }

        public static bool TryUseSelected(string hotspot)
        {
            if (current == null || current.selectedItem == null || current.s1 == null || current.InventoryBlocked ||
                IsInventoryOpen) return false;
            var item = current.selectedItem;
            current.selectedItem = null;
            current.ClearCarriedItem();
            current.s1.UseInventoryItem(item, hotspot);
            return true;
        }

        public void DragItemTo(string item, PointerEventData eventData)
        {
            if (s1 == null || !CanInteractWithInventorySlots) return;
            var hits = new List<RaycastResult>();
            // Keep scene controls invisible while checking the drop target.
            s1.SetSceneHotspotRaycasts(true);
            try
            {
                EventSystem.current.RaycastAll(eventData, hits);
            }
            finally
            {
                s1.SetSceneHotspotRaycasts(false);
            }
            foreach (var hit in hits)
            {
                var hotspot = hit.gameObject.GetComponentInParent<S1Hotspot>();
                if (hotspot == null) continue;
                CloseInventory();
                selectedItem = item;
                TryUseSelected(hotspot.action);
                return;
            }
            ShowToast("把物品拖到可交互区域使用");
        }

        private void ClearCarriedItem()
        {
            if (carriedItem != null) carriedItem.gameObject.SetActive(false);
        }

        public void ShowDragGhost(string item)
        {
            if (carriedItem == null) return;
            var name = carriedItem.Find("Name")?.GetComponent<Text>();
            if (name != null) name.text = item == "photo" ? "旧照片" : "旧工牌";
            carriedItem.gameObject.SetActive(true);
            carriedItem.SetAsLastSibling();
        }

        public void MoveDragGhost(Vector2 position)
        {
            if (carriedItem != null) carriedItem.position = position;
        }

        public void HideDragGhost()
        {
            if (selectedItem == null) ClearCarriedItem();
            else ShowDragGhost(selectedItem);
        }

        private void ShowToast(string message)
        {
            messageLabel.text = message;
            messageLabel.transform.parent.gameObject.SetActive(true);
            messageUntil = Time.unscaledTime + 3f;
        }
    }

}
