using System.Collections.Generic;
using UnityEngine;

namespace WWG
{
    /// <summary>S0 场景音频总控：多条可独立控制的 BGM 轨道 + 一次性音效。</summary>
    public sealed class S0Audio : MonoBehaviour
    {
        public const string BgmVoice = "Audio/BGM/大楼人声";
        public const string BgmCricket = "Audio/BGM/蟋蟀";
        public const string BgmClubRoom = "Audio/BGM/活动室";

        public const string SfxBoxDrop = "Audio/SFX/盒子掉落";
        public const string SfxPage = "Audio/SFX/翻页";
        public const string SfxCurtain = "Audio/SFX/拉窗帘";
        public const string SfxMoveChair = "Audio/SFX/搬动椅子";
        public const string SfxLightOn = "Audio/SFX/开灯";
        public const string SfxLightOff = "Audio/SFX/关灯";

        private sealed class Layer
        {
            public AudioSource src;
            public float gain;
            public float target;
            public float fadeSpeed;
        }

        public static S0Audio Instance { get; private set; }

        private readonly Dictionary<string, Layer> layers = new Dictionary<string, Layer>();
        private AudioSource sfx;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("[S0Audio] 音频宿主已创建并跨场景保活");
            var s = new GameObject("Sfx").AddComponent<AudioSource>();
            s.transform.SetParent(transform, false);
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            sfx = s;
        }

        public static S0Audio Ensure(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[S0Audio]");
            return go.AddComponent<S0Audio>();
        }

        private Layer GetLayer(string key)
        {
            Layer l;
            if (layers.TryGetValue(key, out l)) return l;
            var go = new GameObject("Bgm_" + key).AddComponent<AudioSource>();
            go.transform.SetParent(transform, false);
            go.loop = true;
            go.playOnAwake = false;
            go.spatialBlend = 0f;
            l = new Layer { src = go, gain = 0f, target = 0f, fadeSpeed = 0.8f };
            layers[key] = l;
            return l;
        }

        /// <summary>播放/切换某条 BGM 轨道。</summary>
        public void Play(string key, string resPath, float volume = 0.6f, float fadeSeconds = 1.2f)
        {
            if (string.IsNullOrEmpty(resPath)) return;
            var clip = Resources.Load<AudioClip>(resPath);
            if (clip == null) { Debug.LogWarning("[S0Audio] 缺少音频：" + resPath); return; }
            var l = GetLayer(key);
            if (l.src.clip != clip)
            {
                l.src.clip = clip; l.src.loop = true; l.gain = 0f; l.src.volume = 0f; l.src.Play();
            }
            else if (!l.src.isPlaying)
            {
                l.src.loop = true; l.src.Play();
            }
            l.target = volume;
            l.fadeSpeed = fadeSeconds > 0.01f ? 1f / fadeSeconds : 100f;
        }

        /// <summary>淡出并停止某条轨道。</summary>
        public void Stop(string key, float fadeSeconds = 1.2f)
        {
            Layer l;
            if (!layers.TryGetValue(key, out l)) return;
            l.target = 0f;
            l.fadeSpeed = fadeSeconds > 0.01f ? 1f / fadeSeconds : 100f;
            l.src.loop = false;
        }

        /// <summary>立即设某轨道音量（用于进楼压低人声）。</summary>
        public void SetVolume(string key, float volume, float fadeSeconds = 1.5f)
        {
            var l = GetLayer(key);
            if (volume > 0f && l.src.clip != null && !l.src.isPlaying)
            {
                l.src.loop = true; l.gain = 0f; l.src.volume = 0f; l.src.Play();
            }
            l.target = volume;
            l.fadeSpeed = fadeSeconds > 0.01f ? 1f / fadeSeconds : 100f;
        }

        /// <summary>立刻停止某条轨道（不淡出）。</summary>
        public void StopNow(string key)
        {
            Layer l;
            if (!layers.TryGetValue(key, out l)) return;
            if (l.src == null) return;
            l.target = 0f;
            l.gain = 0f;
            l.src.volume = 0f;
            l.src.Stop();
        }

        public void PlaySfx(string resPath, float volume = 0.9f)
        {
            if (string.IsNullOrEmpty(resPath)) return;
            var clip = Resources.Load<AudioClip>(resPath);
            if (clip == null) { Debug.LogWarning("[S0Audio] 缺少音频：" + resPath); return; }
            sfx.PlayOneShot(clip, volume * WwgAudioSettings.SfxVolume);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float music = WwgAudioSettings.MusicVolume;
            foreach (var kv in layers)
            {
                var l = kv.Value;
                if (l.src == null) continue;
                l.gain = Mathf.MoveTowards(l.gain, l.target, l.fadeSpeed * dt);
                l.src.volume = l.gain * music;
                if (l.target <= 0f && l.gain <= 0.001f && l.src.isPlaying) l.src.Stop();
            }
        }
    }
}
