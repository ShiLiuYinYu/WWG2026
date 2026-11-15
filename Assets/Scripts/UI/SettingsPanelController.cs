using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>主界面用的设置面板：主音量 + 文字速度，独立于 GameplayOverlay。</summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private Slider volumeSlider;
        private Slider textSpeedSlider;
        private Text volumeLabel;
        private Text textSpeedLabel;

        public static SettingsPanelController Open(Transform canvas)
        {
            var prefab = Resources.Load<SettingsPanelController>("UI/SettingsPanel");
            if (prefab == null)
            {
                Debug.LogWarning("[Settings] 找不到 Resources/UI/SettingsPanel");
                return null;
            }

            SettingsPanelController inst = Instantiate(prefab, canvas, false);
            inst.name = "SettingsPanel";
            var rt = inst.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            inst.gameObject.SetActive(true);
            inst.EnsureShade(canvas);
            inst.Bind();
            return inst;
        }

        private GameObject shade;
        private Transform canvasRoot;

        private void EnsureShade(Transform canvas)
        {
            canvasRoot = canvas;
            var go = new GameObject("Shade", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(transform.GetSiblingIndex());
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.78f);
            go.GetComponent<Button>().onClick.AddListener(Close);
            shade = go;
            transform.SetAsLastSibling();
        }

        private void Bind()
        {
            volumeSlider = Find<Slider>("Volume");
            textSpeedSlider = Find<Slider>("Text speed");
            volumeLabel = Find<Text>("Volume value");
            textSpeedLabel = Find<Text>("Text speed value");

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.SetValueWithoutNotify(AudioListener.volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
            if (textSpeedSlider != null)
            {
                textSpeedSlider.minValue = 5f;
                textSpeedSlider.maxValue = 80f;
                textSpeedSlider.wholeNumbers = true;
                textSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("WWG_TextSpeed", 30f));
                textSpeedSlider.onValueChanged.AddListener(SetTextSpeed);
            }
            var back = Find<Button>("Back");
            if (back != null) back.onClick.AddListener(Close);

            UpdateVolumeLabel();
            UpdateTextSpeedLabel();
        }

        private T Find<T>(string path) where T : Component
        {
            Transform t = transform.Find(path);
            return t == null ? null : t.GetComponent<T>();
        }

        private void SetVolume(float v)
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("WWG_MasterVolume", v);
            PlayerPrefs.Save();
            UpdateVolumeLabel();
        }

        private void SetTextSpeed(float v)
        {
            PlayerPrefs.SetFloat("WWG_TextSpeed", v);
            PlayerPrefs.Save();
            UpdateTextSpeedLabel();
        }

        private void UpdateVolumeLabel()
        {
            if (volumeLabel != null)
                volumeLabel.text = "主音量  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        }

        private void UpdateTextSpeedLabel()
        {
            if (textSpeedLabel == null) return;
            float v = PlayerPrefs.GetFloat("WWG_TextSpeed", 30f);
            textSpeedLabel.text = v <= 5f ? "文字速度  逐字" : "文字速度  " + Mathf.RoundToInt(v) + " 字/秒";
        }

        public void Close()
        {
            if (shade != null) Destroy(shade);
            Destroy(gameObject);
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }
    }
}
