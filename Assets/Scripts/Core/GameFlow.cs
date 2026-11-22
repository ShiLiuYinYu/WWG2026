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
        public const string TitleScene = "Title";
        public const string FirstGameplayScene = "S0";
        public const string S1Scene = "S1";
        public const string S2StartScene = "S2";
        public const string S3Scene = "S3";
        public const string S4Scene = "S4";
        public const string S5Scene = "S5";
        public const string S6CorridorScene = "S6_Corridor";
        public const string S6Odd4FScene = "S6_BridgeOdd4F";
        public const string S6SpanScene = "S6_BridgeSpan4F";
        public const string S6EvenScene = "S6_BridgeEven4F";
        public const string S6ServerScene = "S6_ServerRoom3F";
        public const string S7Scene = "S7";
        public const string S8Scene = "S8";

        private static readonly string[] S0Locations = { "S0", "S0_Corridor", "S0_ClubRoom", "S0_Bridge" };
        private static readonly string[] S1Locations = { "S1", "S1_Corridor", "S1_BridgeEntrance" };

        public static string S0Location(int index) => S0Locations[index];
        public static string S1Location(int index) => S1Locations[index];
        public static int S0LocationIndex(string name) => System.Array.IndexOf(S0Locations, name);
        public static int S1LocationIndex(string name) => System.Array.IndexOf(S1Locations, name);

        public static string S2Location(string locationId)
        {
            string path = StoryIds.LocationScene(StoryIds.Chapter.BridgeRoute, locationId);
            return path == null ? null : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static string S6Location(string locationId)
        {
            string path = StoryIds.LocationScene(StoryIds.Chapter.SecondBlackout, locationId);
            return path == null ? null : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>开始新游戏。返回 false 表示目标场景不在 Build Settings 里。</summary>
        public static bool StartNewGame()
        {
            if (!ApplicationCanLoad(FirstGameplayScene))
            {
                UnityEngine.Debug.LogError($"[GameFlow] Build Settings 里没有场景：{FirstGameplayScene}");
                return false;
            }

            SceneManager.LoadScene(FirstGameplayScene);
            return true;
        }

        public static bool LoadLocation(string sceneName)
        {
            if (!ApplicationCanLoad(sceneName))
            {
                UnityEngine.Debug.LogError($"[GameFlow] Build Settings 里没有场景：{sceneName}");
                return false;
            }
            SceneManager.LoadScene(sceneName);
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
