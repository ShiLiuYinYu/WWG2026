using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 主菜单按钮的接线与控制。挂在 Title Canvas 上。
    /// 只负责“按钮被点了之后做什么”，不碰任何 RectTransform，排版改动安全。
    /// 后续章节重构时：改 GameFlow.FirstGameplayScene，或挂 onStartGame 做转场演出，
    /// 这里的按钮逻辑不用动。
    /// </summary>
    public class TitleMenuController : MonoBehaviour
    {
        [Header("按钮")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("开始游戏")]
        [Tooltip("点击后停顿多久再切场景。0 = 立刻切。")]
        [SerializeField] private float startDelay = 0.5f;

        [Tooltip("黑幕淡入时长（秒）。0 = 不做淡入。")]
        [SerializeField] private float fadeDuration = 0.6f;

        [Tooltip("切场景之前触发，留给转场演出 / 存档初始化等流程。")]
        [SerializeField] private UnityEngine.Events.UnityEvent onStartGame;

        private bool _starting;
        private Image _fade;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartGame);
            if (loadButton != null) loadButton.onClick.AddListener(OnLoadGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartGame);
            if (loadButton != null) loadButton.onClick.RemoveListener(OnLoadGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        public void OnStartGame()
        {
            if (_starting) return;
            _starting = true;
            StartCoroutine(StartRoutine());
        }

        private IEnumerator StartRoutine()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group != null) group.interactable = false;

            onStartGame?.Invoke();
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            yield return FadeToBlack();

            if (!GameFlow.StartNewGame())
            {
                _starting = false;
                if (group != null) group.interactable = true;
                yield return FadeFromBlack();
            }
        }

        private IEnumerator FadeToBlack()
        {
            if (fadeDuration <= 0f) yield break;
            EnsureFade();
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                Color c = _fade.color;
                c.a = Mathf.Clamp01(t / fadeDuration);
                _fade.color = c;
                yield return null;
            }
            Color f = _fade.color;
            f.a = 1f;
            _fade.color = f;
        }

        private IEnumerator FadeFromBlack()
        {
            if (_fade == null) yield break;
            float t = 0f;
            float dur = Mathf.Max(0.01f, fadeDuration);
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                Color c = _fade.color;
                c.a = 1f - Mathf.Clamp01(t / dur);
                _fade.color = c;
                yield return null;
            }
            _fade.gameObject.SetActive(false);
        }

        /// <summary>黑幕用一个代码生成的全屏 Image，不加场景对象，排版权重为零。</summary>
        private void EnsureFade()
        {
            if (_fade != null)
            {
                _fade.gameObject.SetActive(true);
                return;
            }

            var go = new GameObject("Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            _fade = go.GetComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = true;
        }

        public void OnLoadGame()
        {
            // 存档系统还没做，先占位。
            Debug.Log("[Title] 读取存档：尚未实现");
        }

        public void OnSettings()
        {
            // 设置面板还没做，先占位。
            Debug.Log("[Title] 设置：尚未实现");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>供编辑器接线脚本使用，运行期请走 GameFlow。</summary>
        public void EditorAssign(Button start, Button load, Button settings, Button quit)
        {
            startButton = start;
            loadButton = load;
            settingsButton = settings;
            quitButton = quit;
        }
    }
}
