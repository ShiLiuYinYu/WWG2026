using System;
using System.IO;
using UnityEditor;

namespace WWG.Editor
{
    /// <summary>
    /// 无界面触发打包：编辑器刷新（域重载）后如果发现 Temp/wwg-build-request.txt，
    /// 就调 WindowsPlayerBuild.Build() 跑一次完整构建，并把结果写进
    /// Temp/wwg-build-result.txt。这样构建不依赖菜单点击，也不会弹模态对话框。
    /// 请求文件用后即删，所以同一次刷新只会打包一次。
    /// </summary>
    [InitializeOnLoad]
    internal static class WindowsPlayerBuildAutoRun
    {
        private const string RequestPath = "Temp/wwg-build-request.txt";
        private const string ResultPath = "Temp/wwg-build-result.txt";

        static WindowsPlayerBuildAutoRun()
        {
            EditorApplication.delayCall += TryRun;
        }

        private static void TryRun()
        {
            if (!File.Exists(RequestPath)) return;

            try
            {
                File.Delete(RequestPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(ResultPath, "error 删除请求文件失败: " + ex);
                return;
            }

            try
            {
                File.WriteAllText(ResultPath, "running " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                string zip = WindowsPlayerBuild.Build();
                File.WriteAllText(ResultPath,
                    "ok " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + zip + " " + new FileInfo(zip).Length);
            }
            catch (Exception ex)
            {
                File.WriteAllText(ResultPath, "error " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + ex);
            }
        }
    }
}
