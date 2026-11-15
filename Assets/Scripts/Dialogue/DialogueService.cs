using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace WWG
{
    /// <summary>Shared dialogue runtime. One host instance, one dialogue at a time.</summary>
    public sealed class DialogueService : MonoBehaviour
    {
        private const string HostResourcePath = "Dialogue/WwgDialogueHost";
        private const string DefaultLocale = "zh-CN";

        [SerializeField] private YarnProject yarnProject;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private WwgDialoguePresenter presenter;

        private readonly List<string> completedNodes = new List<string>();
        private WwgDocumentPanel documentPanel;
        private bool initialized;
        private bool running;
        private bool stopping;
        private bool completionPending;
        private string startNode;
        private string pendingCompletedNode;
        private IDialogueContext pendingCompletedContext;
        private bool startNodeOnce;

        public static DialogueService Instance { get; private set; }
        public bool IsRunning => running || (runner != null && runner.IsDialogueRunning);
        public IDialogueContext Context { get; private set; }
        public string CurrentNode => presenter != null ? presenter.CurrentNode : null;

        public event Action<string> NodeCompleted;
        public event Action<string> DialogueCompleted;

        private void Awake()
        {
            Instance = this;
            ApplySystemFont();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static DialogueService Ensure()
        {
            if (Instance != null)
            {
                Instance.Initialize();
                return Instance;
            }

            DialogueService existing = FindFirstObjectByType<DialogueService>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                existing.ApplySystemFont();
                existing.Initialize();
                return existing;
            }

            GameObject prefab = Resources.Load<GameObject>(HostResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[Dialogue] 找不到对话宿主：Resources/" + HostResourcePath);
                return null;
            }

            GameObject host = Instantiate(prefab);
            host.name = "WwgDialogueHost";
            DontDestroyOnLoad(host);
            Instance = host.GetComponent<DialogueService>();
            if (Instance == null) Instance = host.AddComponent<DialogueService>();
            Instance.ApplySystemFont();
            Instance.Initialize();
            return Instance;
        }

        public bool Play(string nodeId, IDialogueContext context, bool once = false)
        {
            Initialize();
            if (!initialized)
            {
                Debug.LogWarning("[Dialogue] 对话服务尚未完成初始化，无法播放：" + nodeId);
                return false;
            }
            if (IsRunning)
            {
                Debug.LogWarning("[Dialogue] 已有对话进行中，忽略节点：" + nodeId);
                return false;
            }
            if (context == null)
            {
                Debug.LogWarning("[Dialogue] 缺少章节上下文，无法播放：" + nodeId);
                return false;
            }
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                Debug.LogWarning("[Dialogue] 节点名为空。");
                return false;
            }
            if (!NodeExists(nodeId))
            {
                Debug.LogWarning("[Dialogue] 节点不存在：" + nodeId);
                return false;
            }
            if (once && context.HasSeenDialogue(nodeId)) return false;

            completionPending = false;
            pendingCompletedNode = null;
            pendingCompletedContext = null;
            Context = context;
            startNode = nodeId;
            startNodeOnce = once;
            completedNodes.Clear();
            stopping = false;
            running = true;
            if (documentPanel != null) documentPanel.Hide();
            if (runner.VariableStorage != null) runner.VariableStorage.Clear();
            StartDialogueAsync(nodeId).Forget();
            return true;
        }

        public void Advance()
        {
            if (!IsRunning) return;
            if (presenter != null) presenter.RequestAdvance();
            else if (runner != null) runner.RequestNextLine();
        }

        public void Stop()
        {
            completionPending = false;
            pendingCompletedNode = null;
            pendingCompletedContext = null;
            if (runner != null && runner.IsDialogueRunning)
            {
                stopping = true;
                StopAsync().Forget();
            }
            else
            {
                stopping = false;
                running = false;
                Context = null;
            }
            if (presenter != null) presenter.HideImmediate();
            if (documentPanel != null) documentPanel.Hide();
        }

        public static void Shutdown()
        {
            if (Instance == null) return;
            Instance.Stop();
            Instance.running = false;
            Instance.stopping = false;
            Instance.Context = null;
            Instance.startNode = null;
            Instance.startNodeOnce = false;
        }

        private void Initialize()
        {
            if (initialized) return;
            if (runner == null) runner = GetComponent<DialogueRunner>();
            if (presenter == null) presenter = GetComponentInChildren<WwgDialoguePresenter>(true);
            if (documentPanel == null) documentPanel = GetComponentInChildren<WwgDocumentPanel>(true);
            if (runner == null || presenter == null || yarnProject == null)
            {
                Debug.LogError("[Dialogue] 对话宿主缺少 DialogueRunner、WwgDialoguePresenter 或 YarnProject。");
                return;
            }

            runner.DialoguePresenters = new DialoguePresenterBase[] { presenter };
            runner.SetProject(yarnProject);
            if (runner.LineProvider is BuiltinLocalisedLineProvider builtin)
            {
                builtin.LocaleCode = DefaultLocale;
                builtin.AssetLocaleCode = DefaultLocale;
            }

            DialogueCommands commands = GetComponent<DialogueCommands>();
            if (commands == null) commands = gameObject.AddComponent<DialogueCommands>();
            commands.Register(runner);

            presenter.NodeExited += HandleNodeExited;
            presenter.DialogueFinished += HandleDialogueFinished;
            initialized = true;
        }

        private async YarnTask StartDialogueAsync(string nodeId)
        {
            try
            {
                await runner.StartDialogue(nodeId);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                running = false;
                stopping = false;
                Context = null;
                startNode = null;
                startNodeOnce = false;
            }
        }

        private async YarnTask StopAsync()
        {
            try
            {
                await runner.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                running = false;
                stopping = false;
                Context = null;
            }
        }

        private void HandleNodeExited(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return;
            if (!completedNodes.Contains(nodeName)) completedNodes.Add(nodeName);
            NodeCompleted?.Invoke(nodeName);
        }

        private void HandleDialogueFinished()
        {
            if (stopping)
            {
                stopping = false;
                running = false;
                completionPending = false;
                Context = null;
                startNode = null;
                startNodeOnce = false;
                completedNodes.Clear();
                return;
            }

            running = false;
            startNodeOnce = false;
            if (ContextAlive(Context))
            {
                foreach (string node in completedNodes) Context.MarkDialogueSeen(node);
                completedNodes.Clear();
                Context.RefreshHud();
            }

            if (completionPending) return;
            completionPending = true;
            pendingCompletedNode = startNode;
            pendingCompletedContext = Context;
            StartCoroutine(NotifyCompletionNextFrame());
        }

        private System.Collections.IEnumerator NotifyCompletionNextFrame()
        {
            yield return null;
            if (!completionPending) yield break;
            completionPending = false;
            string completedNode = pendingCompletedNode;
            IDialogueContext finishedContext = pendingCompletedContext;
            pendingCompletedNode = null;
            pendingCompletedContext = null;
            if (!ContextAlive(finishedContext) || string.IsNullOrEmpty(completedNode)) yield break;
            DialogueCompleted?.Invoke(completedNode);
        }

        private static bool ContextAlive(IDialogueContext context)
        {
            if (context == null) return false;
            return !(context is UnityEngine.Object unityObject) || unityObject != null;
        }

        private bool NodeExists(string nodeId)
        {
            if (yarnProject == null) return false;
            try
            {
                string[] nodes = yarnProject.NodeNames;
                if (nodes == null) return false;
                foreach (string node in nodes)
                    if (node == nodeId) return true;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                return false;
            }
        }

        private void ApplySystemFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
            if (font == null) return;
            foreach (UnityEngine.UI.Text text in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                text.font = font;
        }
    }
}
