using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using WWG;

/// <summary>Launches the shared dialogue service in a throwaway empty scene.</summary>
public static class DialogueDevelopment
{
    [MenuItem("WWG/Dev/Play Dialogue Test")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        new GameObject("Dialogue debug bootstrap").AddComponent<DialogueDebugBootstrap>();
        EditorApplication.isPlaying = true;
    }
}
