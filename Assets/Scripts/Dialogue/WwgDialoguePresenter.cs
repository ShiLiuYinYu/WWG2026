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
        public Text speakerText;
        public Text bodyText;
        public Image portraitImage;
        public DialogueCast cast;

        public event Action<string> NodeExited;
        public event Action DialogueFinished;

        public string CurrentNode { get; private set; }

        private bool advanceRequested;

        public void RequestAdvance() => advanceRequested = true;

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
            DialogueCast.Entry entry = cast != null ? cast.Find(speaker) : null;

            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.color = entry != null
                    ? entry.speakerColor
                    : new Color(0.56f, 0.82f, 0.86f, 1f);
            }
            if (bodyText != null) bodyText.text = body;

            Sprite portrait = entry != null ? entry.portrait : null;
            bool hasPortrait = portrait != null;
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = hasPortrait;
            }

            float indent = hasPortrait
                ? (entry != null && entry.textIndent > 0f ? entry.textIndent : 290f)
                : 36f;
            if (speakerText != null)
                speakerText.rectTransform.anchoredPosition = new Vector2(indent, -18f);
            if (bodyText != null)
            {
                bodyText.rectTransform.anchoredPosition = new Vector2(indent, -68f);
                bodyText.rectTransform.sizeDelta = new Vector2(hasPortrait ? 1480f : 1745f, 115f);
            }
        }
    }
}
