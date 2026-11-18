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
        private GameObject pausePanel, settingsPanel, detailPanel, inventoryPanel, inventoryButton, tooltip, topBar;
        private GameObject shade;
        private Slider volumeSlider, textSpeedSlider;
        private Text textSpeedLabel;
        private Text timeLabel, objectiveLabel, messageLabel;
        private VitalsBars vitals;
        private Text detailName, detailBody, selectionLabel, volumeLabel;
        private Text tooltipName;
        private RectTransform carriedItem;
        private GameObject photoSlot, badgeSlot;
        private string selectedItem;
        private string detailItem;
        private bool tooltipPinned;
        private float messageUntil;

        private void Awake()
        {
            current = this;
            AudioListener.volume = PlayerPrefs.GetFloat("WWG_MasterVolume", 1f);
            s0 = GetComponentInParent<S0Controller>();
            s1 = GetComponentInParent<S1Controller>();
            pausePanel = transform.Find("Pause panel").gameObject;
            settingsPanel = pausePanel.transform.Find("Settings panel").gameObject;
            detailPanel = transform.Find("Item detail").gameObject;
            inventoryPanel = transform.Find("Inventory panel").gameObject;
            inventoryButton = transform.Find("Inventory button").gameObject;
            tooltip = inventoryPanel.transform.Find("Tooltip").gameObject;
            tooltipName = tooltip.transform.Find("Name").GetComponent<Text>();
            carriedItem = transform.Find("Inventory ghost") as RectTransform;
            carriedItem.gameObject.SetActive(false);
            topBar = transform.Find("Top bar").gameObject;
            var shadeTr = transform.Find("Shade");
            if (shadeTr != null) shade = shadeTr.gameObject;
            timeLabel = FindText("Top bar/Time");
            objectiveLabel = FindText("Top bar/Objective");
            var vitalsTr = transform.Find("Top bar/Vitals");
            if (vitalsTr != null) vitals = vitalsTr.GetComponent<VitalsBars>();
            messageLabel = FindText("Toast/Message");
            detailName = FindText("Item detail/Name");
            detailBody = FindText("Item detail/Body");
            selectionLabel = FindText("Inventory panel/Selection");
            volumeLabel = FindText("Pause panel/Settings panel/Volume value");
            photoSlot = transform.Find("Inventory panel/Grid/Photo slot").gameObject;
            badgeSlot = transform.Find("Inventory panel/Grid/Badge slot").gameObject;
            Bind("Top bar/Pause button", TogglePause);
            Bind("Top bar/Record button", OpenRecord);
            Bind("Inventory button", ToggleInventory);
            Bind("Inventory panel/Close", ToggleInventory);
            Bind("Inventory panel/Tooltip/Use", UseTooltipItem);
            Bind("Inventory panel/Tooltip/Inspect", InspectTooltipItem);
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
            foreach (var slot in GetComponentsInChildren<GameplayInventorySlot>(true)) slot.owner = this;
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
            AttachSound("Top bar/Pause button", TitleButtonFx.ClickSound.Ok);
            AttachSound("Top bar/Record button", TitleButtonFx.ClickSound.Ok);
            AttachSound("Inventory button", TitleButtonFx.ClickSound.Ok);
            AttachSound("Inventory panel/Close", TitleButtonFx.ClickSound.Cancel);
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
            if (SaveSlotPanel.IsOpen) return;
            bool dialogueRunning = DialogueService.Instance != null && DialogueService.Instance.IsRunning;
            if (!dialogueRunning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel.activeSelf) CloseSettings();
                else if (detailPanel.activeSelf) CloseDetail();
                else if (inventoryPanel.activeSelf) ToggleInventory();
                else if (selectedItem != null) { selectedItem = null; ClearCarriedItem(); }
                else TogglePause();
            }
            if (carriedItem != null && carriedItem.gameObject.activeSelf && Mouse.current != null)
                carriedItem.position = Mouse.current.position.ReadValue();
            if (s0 != null)
            {
                timeLabel.text = s0.timeText.text;
                objectiveLabel.text = s0.objectiveText.text;
                if (vitals != null)
                {
                    vitals.gameObject.SetActive(true);
                    vitals.SetSan(s0.San, 100);
                    vitals.SetHealth(s0.Health, 3);
                }
            }
            else if (s1 != null)
            {
                timeLabel.text = s1.timeText.text;
                objectiveLabel.text = s1.objectiveText.text;
                if (vitals != null)
                {
                    vitals.gameObject.SetActive(true);
                    vitals.SetSan(s1.San, 100);
                    vitals.SetHealth(s1.Health, 3);
                }
            }
            photoSlot.SetActive(s1 != null && s1.HasPhoto);
            badgeSlot.SetActive(s1 != null && s1.HasBadge);
            selectionLabel.text = selectedItem == null ? (s1 == null || (!s1.HasPhoto && !s1.HasBadge) ? "暂无物品" : "悬停物品后选择使用或查看") :
                "已选择 " + (selectedItem == "photo" ? "旧照片" : "旧工牌") + " · 点击场景热点使用";
            inventoryButton.SetActive(!dialogueRunning && !pausePanel.activeSelf && !GameplayModalOpen && !inventoryPanel.activeSelf);
            if (topBar != null) topBar.SetActive(!dialogueRunning && !inventoryPanel.activeSelf);
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

        private bool GameplayModalOpen => s0 != null ? s0.IsGameplayModalOpen : s1 != null && s1.IsGameplayModalOpen;

        public void TogglePause()
        {
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;
            if (pausePanel.activeSelf) ClosePause();
            else
            {
                CloseGameplayModals();
                detailPanel.SetActive(false);
                inventoryPanel.SetActive(false);
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
                if (s1.photoPanel != null && s1.photoPanel.activeSelf) s1.ClosePhoto();
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

        private SaveSlotSnapshot CaptureSlot() => s0 != null ? s0.CaptureSaveSlot() :
            s1 != null ? s1.CaptureSaveSlot() : null;

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
            if (s0 != null) s0.ToggleMemo();
            else if (s1 != null) s1.ToggleMemo();
        }

        public void ToggleInventory()
        {
            if (pausePanel.activeSelf || GameplayModalOpen ||
                (DialogueService.Instance != null && DialogueService.Instance.IsRunning)) return;
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            tooltip.SetActive(false);
            tooltipPinned = false;
        }

        public void HoverItem(string item)
        {
            if (tooltipPinned) return;
            ShowItemTooltip(item);
        }

        public void OpenItem(string item, bool allowSwitch = false)
        {
            if (GameplayModalOpen || pausePanel.activeSelf || (tooltipPinned && !allowSwitch)) return;
            ShowItemTooltip(item);
            tooltipPinned = true;
        }

        private void ShowItemTooltip(string item)
        {
            if (GameplayModalOpen || pausePanel.activeSelf) return;
            detailItem = item;
            tooltipName.text = item == "photo" ? "去年的活动室照片" : "林未的旧工牌";
            tooltip.SetActive(true);
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

        private void UseTooltipItem() { SelectDetailItem(); }
        private void InspectTooltipItem() { InspectDetailItem(); }

        private void CloseDetail() { detailPanel.SetActive(false); }
        private void SelectDetailItem()
        {
            selectedItem = detailItem;
            tooltip.SetActive(false);
            tooltipPinned = false;
            inventoryPanel.SetActive(false);
            CloseDetail();
            ShowDragGhost(selectedItem);
            ShowToast("选择了" + (selectedItem == "photo" ? "旧照片" : "旧工牌") + "，点击场景热点使用");
        }
        private void InspectDetailItem()
        {
            string item = detailItem;
            tooltip.SetActive(false);
            tooltipPinned = false;
            inventoryPanel.SetActive(false);
            CloseDetail();
            if (s1 == null) { OpenDetail(item); return; }
            if (item == "photo") s1.InspectPhoto();
            else s1.InspectBadge();
        }

        public static bool TryUseSelected(string hotspot)
        {
            if (current == null || current.selectedItem == null || current.s1 == null) return false;
            var item = current.selectedItem;
            current.selectedItem = null;
            current.ClearCarriedItem();
            current.s1.UseInventoryItem(item, hotspot);
            return true;
        }

        public void DragItemTo(string item, PointerEventData eventData)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                var hotspot = hit.gameObject.GetComponentInParent<S1Hotspot>();
                if (hotspot == null) continue;
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

        public void HideDragGhost() { ClearCarriedItem(); }

        private void ShowToast(string message)
        {
            messageLabel.text = message;
            messageLabel.transform.parent.gameObject.SetActive(true);
            messageUntil = Time.unscaledTime + 3f;
        }
    }

}
