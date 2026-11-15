using System.Collections;
using UnityEngine;
using Yarn.Unity;

namespace WWG
{
    /// <summary>Explicit Yarn command registration. Commands only request work from the chapter.</summary>
    public sealed class DialogueCommands : MonoBehaviour
    {
        private bool registered;

        public void Register(DialogueRunner runner)
        {
            if (registered || runner == null) return;
            registered = true;
            runner.AddCommandHandler<string>("set_flag", SetFlag);
            runner.AddCommandHandler<string>("clear_flag", ClearFlag);
            runner.AddCommandHandler<string>("give_item", GiveItem);
            runner.AddCommandHandler<string, int>("apply_shock", ApplyShock);
            runner.AddCommandHandler<string>("choose_route", ChooseRoute);
            runner.AddCommandHandler<string>("choose_view", ChooseView);
            runner.AddCommandHandler("show_info", (System.Func<string, IEnumerator>)ShowInfo);
            runner.AddCommandHandler("show_photo", (System.Func<IEnumerator>)ShowPhoto);
            runner.AddCommandHandler("checkpoint", Checkpoint);
            runner.AddCommandHandler<string, string>("go_to", GoTo);
            runner.AddCommandHandler("refresh_hud", RefreshHud);
        }

        private static IDialogueContext Context =>
            DialogueService.Instance != null ? DialogueService.Instance.Context : null;

        private static string NodeName =>
            DialogueService.Instance != null ? DialogueService.Instance.CurrentNode : null;

        private static void Warn(string command, string detail)
        {
            Debug.LogWarning("[Dialogue] 命令 " + command + " 失败（节点 " + (NodeName ?? "?") + "）：" + detail);
        }

        private void SetFlag(string id)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("set_flag", "没有章节上下文"); return; }
            context.SetFlag(id);
            context.RefreshHud();
        }

        private void ClearFlag(string id)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("clear_flag", "没有章节上下文"); return; }
            context.ClearFlag(id);
            context.RefreshHud();
        }

        private void GiveItem(string id)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("give_item", "没有章节上下文"); return; }
            context.AddItem(id);
            context.RefreshHud();
        }

        private void ApplyShock(string id, int sanDelta)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("apply_shock", "没有章节上下文"); return; }
            context.ApplyShock(id, sanDelta);
            context.RefreshHud();
        }

        private void ChooseRoute(string choice)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("choose_route", "没有章节上下文"); return; }
            context.ChooseRoute(choice);
            context.RefreshHud();
        }

        private void ChooseView(string choice)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("choose_view", "没有章节上下文"); return; }
            context.ChooseView(choice);
            context.RefreshHud();
        }

        private IEnumerator ShowInfo(string documentId)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("show_info", "没有章节上下文"); return Empty(); }
            IEnumerator routine = context.ShowDocument(documentId);
            if (routine == null) { Warn("show_info", "文档未打开：" + documentId); return Empty(); }
            return routine;
        }

        private IEnumerator ShowPhoto()
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("show_photo", "没有章节上下文"); return Empty(); }
            return context.ShowPhoto() ?? Empty();
        }

        private void Checkpoint()
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("checkpoint", "没有章节上下文"); return; }
            context.RequestCheckpoint();
        }

        private void GoTo(string locationId, string variant)
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("go_to", "没有章节上下文"); return; }
            context.RequestGoTo(locationId, variant);
        }

        private void RefreshHud()
        {
            IDialogueContext context = Context;
            if (context == null) { Warn("refresh_hud", "没有章节上下文"); return; }
            context.RefreshHud();
        }

        private static IEnumerator Empty()
        {
            yield break;
        }
    }
}
