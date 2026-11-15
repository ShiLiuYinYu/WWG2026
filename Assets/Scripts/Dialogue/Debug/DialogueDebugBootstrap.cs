using UnityEngine;

namespace WWG
{
    /// <summary>Spawns the shared host and plays the base test node in an empty scene.</summary>
    public sealed class DialogueDebugBootstrap : MonoBehaviour
    {
        private void Start()
        {
            DialogueService service = DialogueService.Ensure();
            if (service == null)
            {
                Debug.LogError("[DialogueTest] 无法创建对话服务。");
                return;
            }

            DialogueDebugContext context = service.GetComponent<DialogueDebugContext>();
            if (context == null) context = service.gameObject.AddComponent<DialogueDebugContext>();
            if (!service.Play("Test_Hello", context))
                Debug.LogError("[DialogueTest] Test_Hello 未能开始。");
        }
    }
}
