using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 让 Unity 端 MCP 桥在编辑器加载时自行拉起（默认只监听本地回环）。
/// 会话内只执行一次，不污染 EditorPrefs 里的传输设置。
/// </summary>
[InitializeOnLoad]
public static class McpBridgeBoot
{
    private const string SessionKey = "McpBridgeBoot.Done";

    static McpBridgeBoot()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        EditorApplication.delayCall += Boot;
    }

    private static void Boot()
    {
        SessionState.SetBool(SessionKey, true);
        try
        {
            if (!StdioBridgeHost.IsRunning)
                StdioBridgeHost.StartAutoConnect();

            Debug.Log($"[McpBridgeBoot] Unity 端 MCP 桥 running={StdioBridgeHost.IsRunning} port={StdioBridgeHost.GetCurrentPort()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[McpBridgeBoot] 启动失败：" + ex.Message);
        }
    }
}
