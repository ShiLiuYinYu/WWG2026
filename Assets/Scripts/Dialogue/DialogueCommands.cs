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
            runner.AddCommandHandler<string, float>("bgm", PlayBgm);
            runner.AddCommandHandler<string, float>("bgm_stop", StopBgm);
            runner.AddCommandHandler<string, float>("bgm_vol", SetBgmVolume);
            runner.AddCommandHandler<string, float>("sfx", PlaySfx);
        }

        private static IDialogueContext Context =>
            DialogueService.Instance != null ? DialogueService.Instance.Context : null;

        private static string NodeName =>
            DialogueService.Instance != null ? DialogueService.Instance.CurrentNode : null;

        private static void Warn(string command, string detail)
        {
            Debug.LogWarning("[Dialogue] 命令 " + command + " 失败（节点 " + (NodeName ?? "?") + "）：" + detail);
        }

        private static S0Audio Audio
        {
            get
            {
                var a = S0Audio.Instance;
                if (a == null) a = S0Audio.Ensure(Object.FindFirstObjectByType<DialogueService>()?.transform);
                return a;
            }
        }

        /// <summary>把短 key 映射到 Resources 下的音频路径，剧本里只写 key.</summary>
        private static string ResolveAudio(string key)
        {
            switch (key)
            {
                case "voice": return S0Audio.BgmVoice;
                case "cricket": return S0Audio.BgmCricket;
                case "room": return S0Audio.BgmClubRoom;
                case "box": return S0Audio.SfxBoxDrop;
                case "page": return S0Audio.SfxPage;
                case "curtain": return S0Audio.SfxCurtain;
                case "chair": return S0Audio.SfxMoveChair;
                case "lighton": return S0Audio.SfxLightOn;
                case "lightoff": return S0Audio.SfxLightOff;
                default: return key;
            }
        }

        private void PlayBgm(string key, float volume)
        {
            var a = Audio;
            if (a == null) { Warn("bgm", "没有音频宿主"); return; }
            a.Play(key, ResolveAudio(key), volume <= 0f ? 0.5f : volume);
        }

        private void StopBgm(string key, float fade)
        {
            var a = Audio;
            if (a == null) { Warn("bgm_stop", "没有音频宿主"); return; }
            a.Stop(key, fade <= 0f ? 1.2f : fade);
        }

        private void SetBgmVolume(string key, float volume)
        {
            var a = Audio;
            if (a == null) { Warn("bgm_vol", "没有音频宿主"); return; }
            a.SetVolume(key, volume);
        }

        private void PlaySfx(string key, float volume)
        {
            var a = Audio;
            if (a == null) { Warn("sfx", "没有音频宿主"); return; }
            a.PlaySfx(ResolveAudio(key), volume <= 0f ? 0.9f : volume);
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
