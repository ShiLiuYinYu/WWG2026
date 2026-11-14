using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把 Unity 编辑器界面语言切换成简体中文（Unity 6.x）。
/// UnityEditor.LocalizationDatabase 是 internal，只能反射调用。
/// </summary>
[InitializeOnLoad]
public static class SetEditorChinese
{
    private const string SessionKey = "SetEditorChinese.v3";

    static SetEditorChinese()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        SessionState.SetBool(SessionKey, true);
        try
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            Type type = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.LocalizationDatabase");
            if (type == null)
            {
                Debug.LogError("[Lang] 找不到 UnityEditor.LocalizationDatabase");
                return;
            }

            MethodInfo getAvail = type.GetMethod("GetAvailableEditorLanguages", F);
            PropertyInfo current = type.GetProperty("currentEditorLanguage", F);

            var available = (SystemLanguage[])getAvail.Invoke(null, null);
            object now = current.GetValue(null);
            Debug.Log("[Lang] current = " + now);
            if (now is SystemLanguage cur && (cur == SystemLanguage.ChineseSimplified || cur == SystemLanguage.Chinese))
            {
                Debug.Log("[Lang] 已经是简体中文，无需再改");
                return;
            }
            Debug.Log("[Lang] available = " + string.Join(", ", available.Select(l => l + "(" + (int)l + ")")));

            SystemLanguage target = SystemLanguage.Unknown;
            foreach (SystemLanguage lang in available)
            {
                if (lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.Chinese)
                {
                    target = lang;
                    break;
                }
            }

            if (target == SystemLanguage.Unknown)
            {
                Debug.LogWarning("[Lang] 可用列表里没有简体中文");
                return;
            }

            current.SetValue(null, target);
            Debug.Log("[Lang] 已设置 current = " + target + "，重启编辑器后界面生效");
        }
        catch (Exception e)
        {
            Debug.LogError("[Lang] " + e);
        }
    }
}
