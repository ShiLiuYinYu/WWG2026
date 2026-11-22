using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 按钮的悬停和激活音效。标题与共用菜单可另外启用原有的悬停缩放。
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class TitleButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("悬停放大")]
        [Tooltip("鼠标移上去时相对原始尺寸的倍率")]
        [SerializeField] private float hoverScale = 1.08f;

        [Tooltip("放大/还原的过渡时长（秒）")]
        [SerializeField] private float tweenTime = 0.12f;

        [Header("音效")]
        [SerializeField] private bool playHoverSound = true;
        [SerializeField] private bool playClickSound = true;

        [Tooltip("点击时播哪一种：Ok = 选择-确定，Cancel = 选择-取消（返回/关闭类按钮用），Buzzer = 无效")]
        [SerializeField] private ClickSound clickSound = ClickSound.Ok;

        public enum ClickSound { Ok, Cancel, Buzzer, None }

        private Button _button;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _tween;
        private bool animateHover = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneSounds()
        {
            SceneManager.sceneLoaded -= InstallSceneSounds;
            SceneManager.sceneLoaded += InstallSceneSounds;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                InstallSceneSounds(SceneManager.GetSceneAt(i), LoadSceneMode.Single);
        }

        private static void InstallSceneSounds(Scene scene, LoadSceneMode mode)
        {
            if (!scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
                Install(root);
        }

        internal static void Install(GameObject root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                var presenter = button.GetComponentInParent<WwgDialoguePresenter>(true);
                if (presenter != null && presenter.dialogueRoot != null &&
                    button.transform.IsChildOf(presenter.dialogueRoot.transform))
                    continue;
                Attach(button);
            }
        }

        internal static TitleButtonFx Attach(Button button, bool animate = false)
        {
            if (!button.TryGetComponent(out TitleButtonFx fx))
            {
                fx = button.gameObject.AddComponent<TitleButtonFx>();
                fx.animateHover = animate;
                string name = button.name;
                fx.clickSound = name.StartsWith("Close", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Back", StringComparison.OrdinalIgnoreCase) ||
                    name is "Cancel" or "No" or "Title" or "Quit" or "关闭" or "取消" or "屏幕关闭热点"
                    ? ClickSound.Cancel : ClickSound.Ok;
            }
            else if (animate) fx.animateHover = true;

            fx.BindClick();
            return fx;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
            BindClick();
        }

        private void BindClick()
        {
            if (_button == null) return;
            _button.onClick.RemoveListener(PlayClick);
            _button.onClick.AddListener(PlayClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(PlayClick);
        }

        private void OnDisable()
        {
            if (_tween != null) { StopCoroutine(_tween); _tween = null; }
            if (animateHover) transform.localScale = _baseScale;
        }

        private bool Interactable
        {
            get { return _button != null && _button.isActiveAndEnabled && _button.IsInteractable(); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable) return;
            if (playHoverSound) UISound.Cursor();
            if (animateHover) TweenTo(_baseScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (animateHover) TweenTo(_baseScale);
        }

        private void PlayClick()
        {
            if (!playClickSound) return;
            switch (clickSound)
            {
                case ClickSound.Cancel: UISound.Cancel(); break;
                case ClickSound.Buzzer: UISound.Buzzer(); break;
                case ClickSound.None: break;
                default: UISound.Ok(); break;
            }
        }

        /// <summary>运行期改点击音，接线脚本用。</summary>
        public void SetClickSound(ClickSound sound) { clickSound = sound; }
        internal void SetHoverSound(bool enabled) => playHoverSound = enabled;

        private void TweenTo(Vector3 target)
        {
            if (_tween != null) StopCoroutine(_tween);
            if (!isActiveAndEnabled)
            {
                transform.localScale = target;
                return;
            }
            _tween = StartCoroutine(TweenRoutine(target));
        }

        private IEnumerator TweenRoutine(Vector3 target)
        {
            Vector3 from = transform.localScale;
            if (tweenTime <= 0f)
            {
                transform.localScale = target;
                _tween = null;
                yield break;
            }

            float t = 0f;
            while (t < tweenTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / tweenTime);
                k = 1f - (1f - k) * (1f - k); // ease out
                transform.localScale = Vector3.LerpUnclamped(from, target, k);
                yield return null;
            }

            transform.localScale = target;
            _tween = null;
        }
    }
}
