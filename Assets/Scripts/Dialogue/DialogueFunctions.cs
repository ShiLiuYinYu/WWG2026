using Yarn.Unity;

namespace WWG
{
    /// <summary>Read-only Yarn functions backed by the active chapter context.</summary>
    public static class DialogueFunctions
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Actions.AddRegistrationMethod(Register);
        }

        public static void Register(IActionRegistration target, RegistrationType type)
        {
            if (target == null) return;
            if (type == RegistrationType.Compilation)
            {
                target.RegisterFunctionDeclaration("has_flag", typeof(bool), new[] { typeof(string) });
                target.RegisterFunctionDeclaration("has_item", typeof(bool), new[] { typeof(string) });
                target.RegisterFunctionDeclaration("seen", typeof(bool), new[] { typeof(string) });
                target.RegisterFunctionDeclaration("san", typeof(float), new System.Type[0]);
                target.RegisterFunctionDeclaration("health", typeof(float), new System.Type[0]);
                target.RegisterFunctionDeclaration("route", typeof(string), new System.Type[0]);
                target.RegisterFunctionDeclaration("view", typeof(string), new System.Type[0]);
                return;
            }

            target.AddFunction<string, bool>("has_flag", HasFlag);
            target.AddFunction<string, bool>("has_item", HasItem);
            target.AddFunction<string, bool>("seen", Seen);
            target.AddFunction<float>("san", San);
            target.AddFunction<float>("health", Health);
            target.AddFunction<string>("route", Route);
            target.AddFunction<string>("view", View);
        }

        private static IDialogueContext Context =>
            DialogueService.Instance != null ? DialogueService.Instance.Context : null;

        private static bool HasFlag(string id)
        {
            IDialogueContext context = Context;
            return context != null && context.HasFlag(id);
        }

        private static bool HasItem(string id)
        {
            IDialogueContext context = Context;
            return context != null && context.HasItem(id);
        }

        private static bool Seen(string nodeId)
        {
            IDialogueContext context = Context;
            return context != null && context.HasSeenDialogue(nodeId);
        }

        private static float San()
        {
            IDialogueContext context = Context;
            return context != null ? context.San : 0f;
        }

        private static float Health()
        {
            IDialogueContext context = Context;
            return context != null ? context.Health : 0f;
        }

        private static string Route()
        {
            IDialogueContext context = Context;
            return context != null ? context.Route ?? string.Empty : string.Empty;
        }

        private static string View()
        {
            IDialogueContext context = Context;
            return context != null ? context.View ?? string.Empty : string.Empty;
        }
    }
}
