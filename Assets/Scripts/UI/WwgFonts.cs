using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 全项目统一字体入口。优先使用随包的开源字体（霞鹜文楷，SIL OFL 1.1，可商用），
    /// 随包资源缺失时才回退到系统字体，避免打包到没有微软雅黑的机器上整屏方块。
    /// 换字体只改这里的两个资源名，或直接替换 Assets/Resources/Fonts 下的 ttf。
    /// </summary>
    public static class WwgFonts
    {
        public const string BodyResource = "Fonts/LXGWWenKai-Regular";
        public const string StrongResource = "Fonts/LXGWWenKai-Medium";

        private static Font body;
        private static Font strong;
        private static Font fallback;
        private static bool bodyLoaded;
        private static bool strongLoaded;

        /// <summary>正文、对白、备忘录与常规 UI 文字。</summary>
        public static Font Body
        {
            get
            {
                if (!bodyLoaded)
                {
                    body = Resources.Load<Font>(BodyResource);
                    bodyLoaded = true;
                }
                return body != null ? body : Fallback;
            }
        }

        /// <summary>标题与需要压过正文的强调文字，使用随包字体的 Medium 字重。</summary>
        public static Font Strong
        {
            get
            {
                if (!strongLoaded)
                {
                    strong = Resources.Load<Font>(StrongResource);
                    strongLoaded = true;
                }
                return strong != null ? strong : Body;
            }
        }

        /// <summary>没有随包字体时的兜底字体。</summary>
        public static Font Fallback
        {
            get
            {
                if (fallback == null)
                    fallback = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
                return fallback;
            }
        }

        private static bool sceneHookInstalled;

        /// <summary>
        /// 场景加载后把随包字体套到场景所有 Text 上，兜住场景里序列化下来的旧字体
        /// （如 Title.unity 中烘进场景的「Microsoft YaHei」动态字体，换台没装该字体的
        /// 机器就会缺字）。各章控制器随后仍可把标题换成 Strong。
        /// 将来接入 GameRuntime 时，这段统一注入由它接管。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            if (sceneHookInstalled) return;
            sceneHookInstalled = true;
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    Apply(root.transform);
            };
        }

        /// <summary>把字体套到 root 下所有 Text（含未激活），供各章控制器在 Start 里调用。</summary>
        public static void Apply(Transform root, Font font = null)
        {
            if (root == null) return;
            font = font != null ? font : Body;
            if (font == null) return;
            foreach (Text text in root.GetComponentsInChildren<Text>(true)) text.font = font;
        }
    }
}
