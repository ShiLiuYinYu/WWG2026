using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 主菜单按钮的鼠标反馈：移上去播光标音 + 轻微放大，移开还原，点击播确定音。
    /// 只改 localScale，不动 RectTransform 的尺寸和位置，手调好的排版不会被撕。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TitleButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("悬停放大")]
        [Tooltip("鼠标移上去时相对原始尺寸的倍率")]
        [SerializeField] private float hoverScale = 1.08f;

        [Tooltip("放大/还原的过渡时长（秒）")]
        [SerializeField] private float tweenTime = 0.12f;

        [Header("音效")]
        [SerializeField] private bool playHoverSound = true;
        [SerializeField] private bool playClickSound = true;

        private Button _button;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _tween;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (_tween != null) { StopCoroutine(_tween); _tween = null; }
            transform.localScale = _baseScale;
        }

        private bool Interactable
        {
            get { return _button == null || _button.IsInteractable(); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable) return;
            if (playHoverSound) UISound.Cursor();
            TweenTo(_baseScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TweenTo(_baseScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Interactable) return;
            if (playClickSound) UISound.Ok();
        }

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
