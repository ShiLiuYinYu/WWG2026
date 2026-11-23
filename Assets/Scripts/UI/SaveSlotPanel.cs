using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Shared save/load screen used by the title and every playable chapter.</summary>
    public sealed class SaveSlotPanel : MonoBehaviour
    {
        public Text heading, hint, confirmationText;
        public Text[] slotTitles, slotDetails, slotActions;
        public Button[] slotButtons, overwriteButtons, deleteButtons;
        public Button backButton, yesButton, noButton;
        public Button latestButton;
        public Text latestLabel;
        public GameObject confirmationPanel;

        private enum PendingAction { None, Overwrite, Delete }
        private static SaveSlotPanel current;
        private Func<SaveSlotSnapshot> capture;
        private bool saveMode;
        private PendingAction pendingAction;
        private int pendingIndex;
        private float previousTimeScale;

        public static bool IsOpen => current != null;

        public static void OpenSave(Func<SaveSlotSnapshot> captureSnapshot) => Open(true, captureSnapshot);
        public static void OpenLoad(Func<SaveSlotSnapshot> captureSnapshot = null) => Open(false, captureSnapshot);

        private static void Open(bool save, Func<SaveSlotSnapshot> snapshot)
        {
            if (current != null) { current.Configure(save, snapshot); return; }
            SaveSlotPanel prefab = Resources.Load<SaveSlotPanel>("UI/SaveSlotPanel");
            if (prefab == null) { Debug.LogError("[SaveSlots] 缺少 Resources/UI/SaveSlotPanel.prefab"); return; }
            SaveSlotPanel panel = Instantiate(prefab);
            panel.name = "Save slot screen";
            panel.Configure(save, snapshot);
        }

        private void Awake()
        {
            current = this;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Font font = WWG.WwgFonts.Body;
            if (font != null)
                foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;
            for (int i = 0; i < SaveSlotStore.SlotCount; i++)
            {
                int index = i;
                slotButtons[i].onClick.AddListener(() => Select(index));
                overwriteButtons[i].onClick.AddListener(() => AskOverwrite(index));
                deleteButtons[i].onClick.AddListener(() => AskDelete(index));
            }
            backButton.onClick.AddListener(Close);
            latestButton.onClick.AddListener(LoadLatest);
            yesButton.onClick.AddListener(Confirm);
            noButton.onClick.AddListener(CancelConfirmation);
            confirmationPanel.SetActive(false);
            AttachSounds();
        }

        /// <summary>存档面板里的按钮也要有悬停音和点击音；返回/取消一类的走选择-取消。</summary>
        private void AttachSounds()
        {
            AttachAll(slotButtons, TitleButtonFx.ClickSound.Ok);
            AttachAll(overwriteButtons, TitleButtonFx.ClickSound.Ok);
            AttachAll(deleteButtons, TitleButtonFx.ClickSound.Cancel);
            Attach(latestButton, TitleButtonFx.ClickSound.Ok);
            Attach(backButton, TitleButtonFx.ClickSound.Cancel);
            Attach(yesButton, TitleButtonFx.ClickSound.Ok);
            Attach(noButton, TitleButtonFx.ClickSound.Cancel);
        }

        private static void AttachAll(Button[] buttons, TitleButtonFx.ClickSound sound)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++) Attach(buttons[i], sound);
        }

        private static void Attach(Button button, TitleButtonFx.ClickSound sound)
        {
            if (button == null) return;
            TitleButtonFx.Attach(button, animate: true).SetClickSound(sound);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
            Time.timeScale = previousTimeScale;
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (confirmationPanel.activeSelf) CancelConfirmation();
            else Close();
        }

        private void Configure(bool save, Func<SaveSlotSnapshot> snapshot)
        {
            SaveSlotStore.ImportLegacyCheckpointOnce();
            saveMode = save;
            capture = snapshot;
            heading.text = save ? "保存游戏" : "读取存档";
            hint.text = save ? "选择空槽位保存；已有存档需确认覆盖。" :
                "点击存档条直接读取。删除需再次确认。";
            confirmationPanel.SetActive(false);
            Refresh();
        }

        private void Refresh()
        {
            SaveSlotSnapshot latest = SaveSlotStore.ReadLatestCheckpoint();
            latestButton.gameObject.SetActive(!saveMode && latest != null);
            if (latest != null) latestLabel.text = "读取最近检查点";
            for (int i = 0; i < SaveSlotStore.SlotCount; i++)
            {
                SaveSlotSnapshot slot = SaveSlotStore.Read(i);
                bool occupied = slot != null;
                slotTitles[i].text = "存档 " + (i + 1).ToString("00");
                slotDetails[i].text = "游戏内时间  " + (occupied ? SaveSlotStore.GetGameTime(slot) : "—") +
                    "\n存档时间  " + (occupied ? slot.savedAt : "—");
                slotActions[i].gameObject.SetActive(false);
                slotButtons[i].interactable = saveMode || occupied;
                overwriteButtons[i].gameObject.SetActive(occupied && capture != null);
                deleteButtons[i].gameObject.SetActive(occupied);
            }
        }

        private void Select(int index)
        {
            if (saveMode)
            {
                if (SaveSlotStore.Read(index) != null) AskOverwrite(index);
                else Write(index);
                return;
            }
            if (!SaveSlotStore.Load(index, out string error)) hint.text = error;
        }

        private void LoadLatest()
        {
            if (!SaveSlotStore.LoadLatestCheckpoint(out string error)) hint.text = error;
        }

        private void Write(int index)
        {
            SaveSlotSnapshot snapshot = capture?.Invoke();
            if (snapshot == null) { hint.text = "当前无法保存检查点。"; return; }
            if (!SaveSlotStore.Write(index, snapshot, out string error)) { hint.text = error; return; }
            hint.text = "已保存到存档 " + (index + 1).ToString("00") + "。";
            Refresh();
        }

        private void AskOverwrite(int index)
        {
            if (capture == null) { hint.text = "当前没有可覆盖的游戏进度。"; return; }
            pendingAction = PendingAction.Overwrite;
            pendingIndex = index;
            confirmationText.text = "覆盖存档 " + (index + 1).ToString("00") + "？\n原有进度将被替换。";
            confirmationPanel.SetActive(true);
        }

        private void AskDelete(int index)
        {
            pendingAction = PendingAction.Delete;
            pendingIndex = index;
            confirmationText.text = "删除存档 " + (index + 1).ToString("00") + "？";
            confirmationPanel.SetActive(true);
        }

        private void Confirm()
        {
            confirmationPanel.SetActive(false);
            if (pendingAction == PendingAction.Overwrite) Write(pendingIndex);
            else if (pendingAction == PendingAction.Delete)
            {
                SaveSlotStore.Delete(pendingIndex);
                hint.text = "已删除存档 " + (pendingIndex + 1).ToString("00") + "。";
                Refresh();
            }
            pendingAction = PendingAction.None;
        }

        private void CancelConfirmation()
        {
            pendingAction = PendingAction.None;
            confirmationPanel.SetActive(false);
        }

        private void Close() => Destroy(gameObject);
    }
}
