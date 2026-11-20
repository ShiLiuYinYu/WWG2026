using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace WWG
{
    /// <summary>uGUI line presenter that matches the existing S0 dialogue layout.</summary>
    public sealed class WwgDialoguePresenter : DialoguePresenterBase
    {
        public GameObject dialogueRoot;
        public GameObject documentPanel;
        public GameObject shade;
        public Text speakerText;
        public Text bodyText;
        public Image portraitImage;
        public DialogueCast cast;

        [Header("可在预制体中调整的对白排版")]
        [SerializeField] private float textLeftWithoutPortrait = 36f;
        [SerializeField] private float textLeftWithPortrait = 36f;
        [SerializeField] private float textRightMargin = 110f;

        [Tooltip("每秒显示多少个字。0 表示不做打字机效果。")]
        [SerializeField] private float charsPerSecond = 30f;

        public event Action<string> NodeExited;
        public event Action DialogueFinished;

        public string CurrentNode { get; private set; }

        private Vector2 portraitBasePos;
        private bool portraitBaseCached;

        private bool advanceRequested;
        private string fullBody = string.Empty;
        private float typeElapsed;
        private bool typing;

        public void RequestAdvance() => advanceRequested = true;

        // 预制体里让对话框与立绘保持可见，方便在 Prefab 模式手动调整位置；运行时先隐藏。
        private void Awake()
        {
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            if (portraitImage != null)
            {
                portraitImage.enabled = false;
                portraitBasePos = portraitImage.rectTransform.anchoredPosition;
                portraitBaseCached = true;
            }
        }

        private void Update()
        {
            if (shade != null)
            {
                bool want = (dialogueRoot != null && dialogueRoot.activeSelf)
                         || (documentPanel != null && documentPanel.activeSelf);
                if (shade.activeSelf != want) shade.SetActive(want);
            }
            if (!typing || bodyText == null) return;
            typeElapsed += Time.unscaledDeltaTime;
            float cps = PlayerPrefs.GetFloat("WWG_TextSpeed", charsPerSecond);
            int shown = Mathf.Clamp(Mathf.FloorToInt(typeElapsed * cps), 0, fullBody.Length);
            bodyText.text = fullBody.Substring(0, shown);
            if (shown >= fullBody.Length) typing = false;
        }

        private void FinishTyping()
        {
            typing = false;
            if (bodyText != null) bodyText.text = fullBody;
        }

        public override void OnNodeEnter(string nodeName)
        {
            CurrentNode = nodeName;
        }

        public override void OnNodeExit(string nodeName)
        {
            CurrentNode = nodeName;
            NodeExited?.Invoke(nodeName);
        }

        public void HideImmediate()
        {
            advanceRequested = false;
            typing = false;
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            if (portraitImage != null) portraitImage.enabled = false;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            advanceRequested = false;
            if (portraitImage != null) portraitImage.enabled = false;
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (dialogueRoot != null && !dialogueRoot.activeSelf) dialogueRoot.SetActive(true);
            ShowLine(line);
            advanceRequested = false;

            while (typing)
            {
                if (advanceRequested)
                {
                    FinishTyping();
                    advanceRequested = false;
                }
                if (token.IsNextContentRequested || token.IsHurryUpRequested)
                {
                    FinishTyping();
                    break;
                }
                await YarnTask.Yield();
            }

            while (!advanceRequested && !token.IsNextContentRequested && !token.IsHurryUpRequested)
                await YarnTask.Yield();
            advanceRequested = false;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            HideImmediate();
            DialogueFinished?.Invoke();
            return YarnTask.CompletedTask;
        }

        private void ShowLine(LocalizedLine line)
        {
            string speaker = line.CharacterName ?? string.Empty;
            string body = line.TextWithoutCharacterName.Text;
            string portraitSpeaker = speaker.Replace("（电话）", string.Empty).Replace("(电话)", string.Empty);
            DialogueCast.Entry entry = cast != null ? cast.Find(portraitSpeaker) : null;

            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.color = entry != null
                    ? entry.speakerColor
                    : new Color(0.56f, 0.82f, 0.86f, 1f);
            }

            fullBody = body ?? string.Empty;
            typeElapsed = 0f;
            if (bodyText != null)
            {
                bodyText.text = string.Empty;
                bool rich = fullBody.IndexOf('<') >= 0;
                typing = !rich && charsPerSecond > 0f && fullBody.Length > 0;
                if (!typing) bodyText.text = fullBody;
            }
            else typing = false;

            Sprite portrait = entry != null ? entry.portrait : null;
            bool hasPortrait = portrait != null;
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = hasPortrait;
                if (!portraitBaseCached)
                {
                    portraitBasePos = portraitImage.rectTransform.anchoredPosition;
                    portraitBaseCached = true;
                }
                // PreserveAspect 会把 sprite 等比缩放后居中，不同切图高度不同 → 脚离框底的距离也不一样。
                // 这里按真实绘制高度把立绘往下压，让每个人的脚都贴住框底，再叠加逐角色微调。
                float pillarbox = 0f;
                if (portrait != null && portrait.rect.height > 0f)
                {
                    var box = portraitImage.rectTransform.sizeDelta;
                    float aspect = portrait.rect.width / portrait.rect.height;
                    float drawnH = Mathf.Min(box.y, box.x / aspect);
                    pillarbox = (box.y - drawnH) * 0.5f;
                }
                float offsetY = entry != null ? entry.portraitOffsetY : 0f;
                portraitImage.rectTransform.anchoredPosition =
                    new Vector2(portraitBasePos.x, portraitBasePos.y - pillarbox + offsetY);
            }

            float indent = hasPortrait
                ? (entry != null && entry.textIndent > 0f ? entry.textIndent : textLeftWithPortrait)
                : textLeftWithoutPortrait;
            if (speakerText != null)
                speakerText.rectTransform.anchoredPosition = new Vector2(indent, speakerText.rectTransform.anchoredPosition.y);
            if (bodyText != null)
            {
                bodyText.rectTransform.anchoredPosition = new Vector2(indent, bodyText.rectTransform.anchoredPosition.y);
                var panel = bodyText.rectTransform.parent as RectTransform;
                if (panel != null)
                    bodyText.rectTransform.sizeDelta = new Vector2(
                        Mathf.Max(200f, panel.rect.width - indent - textRightMargin), bodyText.rectTransform.sizeDelta.y);
            }
        }
    }
}
