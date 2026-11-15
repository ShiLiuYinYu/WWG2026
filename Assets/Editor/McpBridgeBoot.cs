using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 让 Unity 端 MCP 桥在编辑器加载 / 每次域重载后自动拉起。
/// StdioBridgeReloadHandler 编译前会 Stop 桥，且只恢复 TransportManager 自己起的桥，
/// 所以这里自己盯着补一次。用 update 计时而不是 delayCall，失焦也能跑。
/// 注意 StdioBridgeHost.IsRunning 不可靠（起了 listener 也可能报 false），
/// 因此限制总尝试次数，避免无限 Stop/Start 抖动。
/// </summary>
[InitializeOnLoad]
public static class McpBridgeBoot
{
    private static double _nextAt;
    private static int _tries;

    static McpBridgeBoot()
    {
        _nextAt = 0d;
        _tries = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (_tries >= 12) return;
        if (EditorApplication.timeSinceStartup < _nextAt) return;
        _nextAt = EditorApplication.timeSinceStartup + 2.0;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (StdioBridgeHost.IsRunning) return;

        _tries++;
        try
        {
            StdioBridgeHost.StartAutoConnect();
            Debug.Log($"[McpBridgeBoot] 桥已拉起 running={StdioBridgeHost.IsRunning} port={StdioBridgeHost.GetCurrentPort()} try={_tries}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[McpBridgeBoot] 拉起失败：" + ex.Message);
        }
    }
}
