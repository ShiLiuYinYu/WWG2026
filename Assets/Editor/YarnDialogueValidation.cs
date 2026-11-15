using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yarn.Unity;
using WWG;

/// <summary>Explicit validation for Yarn nodes, line IDs, commands and document references.</summary>
public static class YarnDialogueValidation
{
    private const string ProjectPath = "Assets/Yarn/WWG.yarnproject";
    private const string YarnFolder = "Assets/Yarn";

    private static readonly HashSet<string> KnownCommands = new HashSet<string>
    {
        "if", "elseif", "else", "endif", "jump", "stop", "declare", "set", "enum", "case",
        "endcase", "once", "endonce", "local", "call", "wait",
        "set_flag", "clear_flag", "give_item", "apply_shock", "choose_route", "choose_view",
        "show_info", "show_photo", "checkpoint", "go_to", "refresh_hud"
    };

    private static readonly Regex LineIdPattern = new Regex(@"#line:([A-Za-z0-9_]+)");
    private static readonly Regex CommandPattern = new Regex(@"<<\s*([A-Za-z_][A-Za-z0-9_]*)");
    private static readonly Regex ShowInfoPattern = new Regex(@"<<\s*show_info\s+""([^""]+)""");

    [MenuItem("WWG/Yarn/Validate Dialogue")]
    public static void Validate()
    {
        var problems = new List<string>();
        YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);
        if (project == null)
        {
            Debug.LogError("[Yarn] 找不到工程：" + ProjectPath);
            return;
        }
        if (project.Program == null) problems.Add("Yarn 工程没有编译后的 Program，请检查 Console。");
        else
        {
            foreach (string required in new[] { "Test_Hello", "Test_ValueBranch" })
                if (Array.IndexOf(project.NodeNames, required) < 0)
                    problems.Add("缺少节点：" + required);
        }

        var lineIds = new Dictionary<string, string>(StringComparer.Ordinal);
        int fileCount = 0;
        foreach (string path in Directory.GetFiles(YarnFolder, "*.yarn", SearchOption.AllDirectories))
        {
            fileCount++;
            string relative = path.Replace('\\', '/');
            string text = File.ReadAllText(path);
            foreach (Match match in LineIdPattern.Matches(text))
            {
                string id = match.Groups[1].Value;
                if (!Regex.IsMatch(id, "^[a-z0-9_]+$"))
                    problems.Add(relative + "：行 ID 含非法字符：" + id);
                if (lineIds.TryGetValue(id, out string owner))
                    problems.Add(relative + "：行 ID 重复 " + id + "（另见 " + owner + "）");
                else lineIds[id] = relative;
            }
            foreach (Match match in CommandPattern.Matches(text))
            {
                string command = match.Groups[1].Value;
                if (!KnownCommands.Contains(command))
                    problems.Add(relative + "：未注册的命令 <<" + command + ">>");
            }
            foreach (Match match in ShowInfoPattern.Matches(text))
            {
                string id = match.Groups[1].Value;
                if (!DialogueDocuments.TryGet(id, out _))
                    problems.Add(relative + "：show_info 引用了不存在的文档：" + id);
            }
        }

        if (fileCount == 0) problems.Add("Assets/Yarn 下没有 .yarn 文件。");
        if (problems.Count == 0)
            Debug.Log("[Yarn] 校验通过：" + fileCount + " 个文件，" + lineIds.Count + " 个行 ID。");
        else
            foreach (string problem in problems) Debug.LogError("[Yarn] " + problem);
    }
}
