using System.Collections;
using UnityEngine;

namespace WWG
{
    /// <summary>Development-only context used by WWG/Dev/Play Dialogue Test.</summary>
    public sealed class DialogueDebugContext : MonoBehaviour, IDialogueContext
    {
        private readonly GameState state = new GameState();
        private WwgDocumentPanel documentPanel;
        private string pendingGoTo;

        public int San => state.san;
        public int Health => state.health;
        public string Route => state.routeChoice;
        public string View => state.viewChoice;

        private void Awake()
        {
            state.EnsureCollections();
            documentPanel = GetComponentInChildren<WwgDocumentPanel>(true);
        }

        public bool HasFlag(string id) => state.HasFlag(id);
        public bool HasItem(string id) => state.HasItem(id);
        public bool HasSeenDialogue(string nodeId) => state.HasSeenDialogue(nodeId);
        public void MarkDialogueSeen(string nodeId) => state.MarkDialogueSeen(nodeId);

        public void SetFlag(string id)
        {
            state.SetFlag(id);
            Debug.Log("[DialogueTest] set_flag " + id);
        }

        public void ClearFlag(string id)
        {
            state.ClearFlag(id);
            Debug.Log("[DialogueTest] clear_flag " + id);
        }

        public void AddItem(string id)
        {
            state.AddItem(id);
            Debug.Log("[DialogueTest] give_item " + id);
        }

        public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
        {
            string guard = "flag." + id + ".shock";
            if (state.HasFlag(guard)) return;
            state.SetFlag(guard);
            state.san = Mathf.Max(0, state.san + sanDelta);
            state.health = Mathf.Clamp(state.health + healthDelta, 0, 3);
            Debug.Log("[DialogueTest] apply_shock " + id + " SAN " + sanDelta + " 生命 " + healthDelta);
        }

        public void ChooseRoute(string choice)
        {
            bool changed = state.ChooseRoute(choice);
            Debug.Log("[DialogueTest] choose_route " + choice + " -> " + changed);
        }

        public void ChooseView(string choice)
        {
            bool changed = state.ChooseView(choice);
            Debug.Log("[DialogueTest] choose_view " + choice + " -> " + changed);
        }

        public IEnumerator ShowDocument(string documentId)
        {
            if (documentPanel == null) documentPanel = GetComponentInChildren<WwgDocumentPanel>(true);
            if (documentPanel == null)
            {
                Debug.LogWarning("[DialogueTest] 缺少文档面板：" + documentId);
                return null;
            }
            return documentPanel.Show(documentId);
        }

        public IEnumerator ShowPhoto()
        {
            Debug.Log("[DialogueTest] show_photo（测试环境无照片面板）");
            return null;
        }

        public void RequestCheckpoint()
        {
            Debug.Log("[DialogueTest] checkpoint 请求（测试环境不写存档）");
        }

        public void RequestGoTo(string locationId, string variant)
        {
            pendingGoTo = locationId + "/" + variant;
            Debug.Log("[DialogueTest] go_to 排队：" + pendingGoTo);
        }

        public void RefreshHud()
        {
        }
    }
}
