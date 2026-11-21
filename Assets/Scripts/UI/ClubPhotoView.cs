using System;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    public sealed class ClubPhotoView : MonoBehaviour
    {
        public const float RevealedShadowOpacity = 0.72f;

        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private Image shadow;
        [SerializeField] private GameObject lampHint;
        [SerializeField] private GameObject tapeHint;

        public bool IsOpen => gameObject.activeSelf;
        public event Action Closed;

        public void Open(float shadowOpacity, bool showHints = false)
        {
            front.SetActive(true);
            back.SetActive(false);
            SetClues(shadowOpacity, showHints);
            gameObject.SetActive(true);
        }

        public void SetClues(float shadowOpacity, bool showHints)
        {
            Color color = shadow.color;
            color.a = shadowOpacity;
            shadow.color = color;
            lampHint.SetActive(showHints);
            tapeHint.SetActive(showHints);
        }

        public void Flip()
        {
            bool showBack = front.activeSelf;
            front.SetActive(!showBack);
            back.SetActive(showBack);
            if (S0Audio.Instance != null) S0Audio.Instance.PlaySfx(S0Audio.SfxPage, 0.85f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (S0Audio.Instance != null) S0Audio.Instance.PlaySfx(S0Audio.SfxPage, 0.55f);
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
