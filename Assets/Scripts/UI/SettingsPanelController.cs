using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 全游戏共用的设置窗口：窗口框用选项框，按钮用选项按钮，
    /// 内容为音乐、音效两条音量滑条，底部关闭按钮。
    /// 标题页与章节内的暂停菜单都从这里打开同一个预制体。
    /// </summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private static SettingsPanelController current;
        private static int closedByEscapeFrame = -1;

        private Slider musicSlider;
        private Slider sfxSlider;
        private Text musicValue;
        private Text sfxValue;
        private GameObject shade;

        /// <summary>窗口是否已经打开。</summary>
        public static bool IsOpen
        {
            get { return current != null; }
        }

        /// <summary>本帧是否由窗口自己按 Esc 关闭，避免同时触发暂停菜单的 Esc。</summary>
        public static bool ConsumedEscapeThisFrame
        {
            get { return closedByEscapeFrame == Time.frameCount; }
        }

        /// <summary>在指定 Canvas 下打开设置窗口，已经打开时直接返回现有实例。</summary>
        public static SettingsPanelController Open(Transform canvas)
        {
            if (current != null) return current;
            if (canvas == null) return null;

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
            inst.BuildShade();
            inst.Bind();
            current = inst;
            return inst;
        }

        /// <summary>关闭当前窗口（没有打开时什么也不做）。</summary>
        public static void CloseCurrent()
        {
            if (current != null) current.Close();
        }

        private void BuildShade()
        {
            var go = new GameObject("Settings shade", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform.parent, false);
            rt.SetSiblingIndex(transform.GetSiblingIndex());
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.72f);
            Canvas.ForceUpdateCanvases();
            go.GetComponent<Button>().onClick.AddListener(Close);
            shade = go;
            transform.SetAsLastSibling();
        }

        private void Bind()
        {
            musicSlider = Find<Slider>("Slider Music");
            sfxSlider = Find<Slider>("Slider Sfx");
            musicValue = Find<Text>("Value Music");
            sfxValue = Find<Text>("Value Sfx");

            BindSlider(musicSlider, WwgAudioSettings.MusicVolume, WwgAudioSettings.SetMusicVolume, UpdateMusicValue);
            BindSlider(sfxSlider, WwgAudioSettings.SfxVolume, WwgAudioSettings.SetSfxVolume, UpdateSfxValue);

            var close = Find<Button>("Close");
            if (close != null)
            {
                close.onClick.AddListener(Close);
                var fx = close.GetComponent<TitleButtonFx>();
                if (fx == null) fx = close.gameObject.AddComponent<TitleButtonFx>();
                fx.SetClickSound(TitleButtonFx.ClickSound.Cancel);
            }
            else Debug.LogWarning("[Settings] 设置窗口内找不到 Close 按钮");

            UpdateMusicValue();
            UpdateSfxValue();
        }

        private static void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> setter,
            System.Action refresh)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                refresh();
            });
        }

        private T Find<T>(string path) where T : Component
        {
            Transform t = transform.Find(path);
            return t == null ? null : t.GetComponent<T>();
        }

        private void UpdateMusicValue()
        {
            if (musicValue != null)
                musicValue.text = Mathf.RoundToInt(WwgAudioSettings.MusicVolume * 100f) + "%";
        }

        private void UpdateSfxValue()
        {
            if (sfxValue != null)
                sfxValue.text = Mathf.RoundToInt(WwgAudioSettings.SfxVolume * 100f) + "%";
        }

        public void Close()
        {
            WwgAudioSettings.Save();
            if (current == this) current = null;
            if (shade != null) Destroy(shade);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                closedByEscapeFrame = Time.frameCount;
                Close();
            }
        }
    }
}
