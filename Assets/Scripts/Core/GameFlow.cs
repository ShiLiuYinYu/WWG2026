using UnityEngine.SceneManagement;

namespace WWG
{
    /// <summary>
    /// 全局流程入口。主菜单不直接写死场景名，所有“从哪开始”都走这里，
    /// 之后 S0 以及后续章节要重构（改名、拆场景、加过渡演出）只改这一个文件。
    /// </summary>
    public static class GameFlow
    {
        /// <summary>新游戏第一个可玩场景。重构 S0 时改这里即可。</summary>
        public static string FirstGameplayScene = "S0";

        /// <summary>开始新游戏。返回 false 表示目标场景不在 Build Settings 里。</summary>
        public static bool StartNewGame()
        {
            if (!SceneManager.GetSceneByName(FirstGameplayScene).IsValid()
                && !ApplicationCanLoad(FirstGameplayScene))
            {
                UnityEngine.Debug.LogError($"[GameFlow] Build Settings 里没有场景：{FirstGameplayScene}");
                return false;
            }

            SceneManager.LoadScene(FirstGameplayScene);
            return true;
        }

        private static bool ApplicationCanLoad(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return true;
            }
            return false;
        }
    }
}
