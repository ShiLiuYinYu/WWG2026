using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Reusable uGUI panel for document/notice text opened by &lt;&lt;show_info&gt;&gt;.</summary>
    public sealed class WwgDocumentPanel : MonoBehaviour
    {
        public GameObject panelRoot;
        public Text titleText;
        public Text bodyText;
        public Button closeButton;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            Hide();
        }

        public IEnumerator Show(string documentId)
        {
            if (!DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry document))
            {
                Debug.LogWarning("[Dialogue] 缺少文档 ID：" + documentId);
                yield break;
            }
            if (titleText != null)
                titleText.text = document.residue ? "【残影】" + document.title : document.title;
            if (bodyText != null) bodyText.text = document.body;
            if (panelRoot != null) panelRoot.SetActive(true);
            while (IsOpen) yield return null;
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Hide() => Close();
    }
}
