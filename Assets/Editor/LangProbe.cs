using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 打开 Edit > Preferences，并读回编辑器的当前语言状态，用来判断
/// “pref 写了但界面没变”到底卡在哪一层。
/// </summary>
[InitializeOnLoad]
public static class LangProbe
{
    private const string SessionKey = "WG.LangProbe.v4";

    static LangProbe()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            Type type = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.LocalizationDatabase");
            PropertyInfo current = type.GetProperty("currentEditorLanguage", F);
            MethodInfo availM = type.GetMethod("GetAvailableEditorLanguages", F);

            var available = (SystemLanguage[])availM.Invoke(null, null);
            object before = current.GetValue(null);
            Debug.Log("[Lang2] before = " + before);
            Debug.Log("[Lang2] pref = '" + EditorPrefs.GetString("editorLanguage", "<unset>") + "'");
            Debug.Log("[Lang2] available = " + string.Join(",", available.Select(x => x.ToString())));

            foreach (SystemLanguage lang in available)
            {
                if (lang != (SystemLanguage)before) { current.SetValue(null, lang); break; }
            }

            Debug.Log("[Lang2] after setter = " + current.GetValue(null));
            Debug.Log("[Lang2] pref now = '" + EditorPrefs.GetString("editorLanguage", "<unset>") + "'");

            bool opened = EditorApplication.ExecuteMenuItem("Edit/Preferences...");
            Debug.Log("[Lang2] open preferences = " + opened);
        }
        catch (Exception e)
        {
            Debug.LogError("[Lang2] " + e);
        }
    }
}
