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
        private GameObject pausePanel, settingsPanel, detailPanel, dock;
        private Text timeLabel, objectiveLabel, statusLabel, messageLabel;
        private Text detailName, detailBody, selectionLabel, volumeLabel;
        private GameObject photoSlot, badgeSlot;
        private string selectedItem;
        private string detailItem;
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
            dock = transform.Find("Inventory dock").gameObject;
            timeLabel = FindText("Top bar/Time");
            objectiveLabel = FindText("Top bar/Objective");
            statusLabel = FindText("Top bar/Status");
            messageLabel = FindText("Toast/Message");
            detailName = FindText("Item detail/Name");
            detailBody = FindText("Item detail/Body");
            selectionLabel = FindText("Inventory dock/Selection");
            volumeLabel = FindText("Pause panel/Settings panel/Volume value");
            photoSlot = transform.Find("Inventory dock/Photo slot").gameObject;
            badgeSlot = transform.Find("Inventory dock/Badge slot").gameObject;
            Bind("Top bar/Pause button", TogglePause);
            Bind("Top bar/Record button", OpenRecord);
            Bind("Pause panel/Resume", ClosePause);
            Bind("Pause panel/Save", Save);
            Bind("Pause panel/Load", Load);
            Bind("Pause panel/Settings", OpenSettings);
            Bind("Pause panel/Title", ReturnToTitle);
            Bind("Pause panel/Close", ClosePause);
            Bind("Pause panel/Settings panel/Volume", CycleVolume);
            Bind("Pause panel/Settings panel/Back", CloseSettings);
            Bind("Item detail/Use", SelectDetailItem);
            Bind("Item detail/Inspect", InspectDetailItem);
            Bind("Item detail/Close", CloseDetail);
            foreach (var slot in GetComponentsInChildren<GameplayInventorySlot>(true)) slot.owner = this;
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            detailPanel.SetActive(false);
            UpdateVolumeLabel();
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
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
                else TogglePause();
            }
            if (s0 != null)
            {
                timeLabel.text = s0.timeText.text;
                objectiveLabel.text = s0.objectiveText.text;
                statusLabel.text = "白天调查  ·  安全";
            }
            else if (s1 != null)
            {
                timeLabel.text = s1.timeText.text;
                objectiveLabel.text = s1.objectiveText.text;
                statusLabel.text = "SAN  " + s1.San + "/100    生命  " + s1.Health + "/3";
            }
            photoSlot.SetActive(s1 != null && s1.HasPhoto);
            badgeSlot.SetActive(s1 != null && s1.HasBadge);
            selectionLabel.text = selectedItem == null ? (s1 == null || (!s1.HasPhoto && !s1.HasBadge) ? "暂无物品" : "右键查看 · 拖到场景使用") :
                "已选择 " + (selectedItem == "photo" ? "旧照片" : "旧工牌") + " · 点击场景热点使用";
            dock.SetActive(!pausePanel.activeSelf && !GameplayModalOpen);
            if (messageLabel.transform.parent.gameObject.activeSelf && Time.unscaledTime >= messageUntil)
                messageLabel.transform.parent.gameObject.SetActive(false);
        }

        private bool GameplayModalOpen => s0 != null ? s0.IsGameplayModalOpen : s1 != null && s1.IsGameplayModalOpen;

        public void TogglePause()
        {
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;
            if (pausePanel.activeSelf) ClosePause();
            else
            {
                detailPanel.SetActive(false);
                pausePanel.SetActive(true);
                Time.timeScale = 0f;
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
        private void CycleVolume()
        {
            AudioListener.volume = AudioListener.volume > 0.75f ? 0.5f : AudioListener.volume > 0.25f ? 0f : 1f;
            PlayerPrefs.SetFloat("WWG_MasterVolume", AudioListener.volume);
            PlayerPrefs.Save();
            UpdateVolumeLabel();
        }
        private void UpdateVolumeLabel() { volumeLabel.text = "主音量  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%"; }
        private void ReturnToTitle() { ClosePause(); GameFlow.LoadLocation(GameFlow.TitleScene); }
        private void OpenRecord()
        {
            if (pausePanel.activeSelf || detailPanel.activeSelf) return;
            if (s0 != null) s0.ToggleMemo();
            else if (s1 != null) s1.ToggleMemo();
        }

        public void OpenItem(string item)
        {
            if (GameplayModalOpen || pausePanel.activeSelf) return;
            detailItem = item;
            detailName.text = item == "photo" ? "去年的活动室照片" : "林未的旧工牌";
            detailBody.text = item == "photo"
                ? "【社团自留资料】\n\n去年拍摄的 5F 活动室。窗帘、旧桌灯和第五把椅子都清晰可见。背面写着 23:30。\n\n可用于核对活动室里的摆设。"
                : "【社团自留资料】\n\n48H GameJam 参赛证，姓名：林未，编号 05。背面手写：玻璃两边，各存一半。4F。\n\n关键物品，不会消耗。";
            detailPanel.SetActive(true);
        }

        private void CloseDetail() { detailPanel.SetActive(false); }
        private void SelectDetailItem()
        {
            selectedItem = detailItem;
            CloseDetail();
            ShowToast("选择了" + (selectedItem == "photo" ? "旧照片" : "旧工牌") + "，点击场景热点使用");
        }
        private void InspectDetailItem()
        {
            string item = detailItem;
            CloseDetail();
            if (s1 == null) return;
            if (item == "photo") s1.InspectPhoto();
            else s1.InspectBadge();
        }

        public static bool TryUseSelected(string hotspot)
        {
            if (current == null || current.selectedItem == null || current.s1 == null) return false;
            var item = current.selectedItem;
            current.selectedItem = null;
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

        private void ShowToast(string message)
        {
            messageLabel.text = message;
            messageLabel.transform.parent.gameObject.SetActive(true);
            messageUntil = Time.unscaledTime + 3f;
        }
    }

}
