using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WWG.Editor
{
    /// <summary>
    /// Windows 独立版打包：构建到 Builds/Windows，再把可运行内容压成工程根目录的
    /// 「&lt;Product Name&gt;_Windows_v&lt;bundleVersion&gt;.zip」。可执行文件名与压缩包名都
    /// 取自 Player Settings，不再沿用工程目录名。菜单 WWG/Build/Windows Player 供手工使用；
    /// 关掉编辑器后也可以用
    /// Unity.exe -quit -batchMode -projectPath . -executeMethod WWG.Editor.WindowsPlayerBuild.BuildFromCommandLine
    /// 做命令行构建。
    /// </summary>
    public static class WindowsPlayerBuild
    {
        private const string OutputDirectory = "Builds/Windows";

        /// <summary>Burst 的调试符号目录后缀，玩家不需要；Unity 会带上产品名前缀。</summary>
        private const string BurstDebugSuffix = "_BurstDebugInformation_DoNotShip";

        /// <summary>Player Settings 里的产品名，用来命名 exe 与压缩包。</summary>
        private static string ProductName
        {
            get
            {
                string name = PlayerSettings.productName;
                return string.IsNullOrWhiteSpace(name) ? "WWG" : name.Trim();
            }
        }

        private static string ExecutableName => ProductName + ".exe";

        private static string ZipFileName => $"{ProductName}_Windows_v{PlayerSettings.bundleVersion}.zip";

        [MenuItem("WWG/Build/Windows Player (Build and Zip)", priority = 60)]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("打包", "编辑器还在编译或导入，等它完成后再打包。", "好");
                return;
            }

            try
            {
                string zip = Build();
                EditorUtility.DisplayDialog("打包完成",
                    $"构建目录：{OutputDirectory}\n压缩包：{zip}\n大小：{new FileInfo(zip).Length / 1024 / 1024} MB", "好");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Build] 打包失败：{ex}");
                EditorUtility.DisplayDialog("打包失败", ex.Message + "\n\n详见 Console。", "好");
            }
        }

        /// <summary>命令行构建入口，失败时返回码非 0。</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Build] 打包失败：{ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>构建 Windows 播放器并压缩，返回压缩包路径。</summary>
        public static string Build()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (Directory.Exists(OutputDirectory)) Directory.Delete(OutputDirectory, true);
            Directory.CreateDirectory(OutputDirectory);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("Build Settings 里没有启用中的场景。");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            UnityEngine.Debug.Log($"[Build] 开始构建 {scenes.Length} 个场景 → {options.locationPathName}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                string errors = string.Join("\n", report.steps
                    .SelectMany(step => step.messages)
                    .Where(message => message.type == LogType.Error || message.type == LogType.Exception)
                    .Select(message => message.content)
                    .Take(10));
                throw new InvalidOperationException($"构建结果 {summary.result}，错误 {summary.totalErrors} 个。\n{errors}");
            }

            UnityEngine.Debug.Log($"[Build] 构建成功，用时 {summary.totalTime}，输出 {summary.totalSize / 1024 / 1024} MB");

            string zip = Path.Combine(root, ZipFileName);
            if (File.Exists(zip)) File.Delete(zip);
            int entries = Compress(OutputDirectory, zip);
            UnityEngine.Debug.Log($"[Build] 已打包 {entries} 个文件 → {zip}");
            return zip;
        }

        /// <summary>把目录内容压到压缩包根部（不含外层文件夹），与之前分发的结构一致。</summary>
        private static int Compress(string sourceDirectory, string zipPath)
        {
            string source = Path.GetFullPath(sourceDirectory);
            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                .Where(file => !IsExcluded(source, file))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            using (FileStream stream = File.Create(zipPath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    string relative = Path.GetRelativePath(source, file).Replace('\\', '/');
                    ZipArchiveEntry entry = archive.CreateEntry("./" + relative, System.IO.Compression.CompressionLevel.Optimal);
                    using (Stream input = File.OpenRead(file))
                    using (Stream output = entry.Open())
                        input.CopyTo(output);
                }
            }

            return files.Length;
        }

        /// <summary>按路径分段排除 Burst 调试目录；Unity 会把它命名成「&lt;产品名&gt;_BurstDebugInformation_DoNotShip」。</summary>
        private static bool IsExcluded(string source, string file)
        {
            string relative = Path.GetRelativePath(source, file).Replace('\\', '/');
            return relative
                .Split('/')
                .Any(segment => segment.EndsWith(BurstDebugSuffix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
