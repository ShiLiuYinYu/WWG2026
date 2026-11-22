using UnityEngine;

namespace WWG
{
    /// <summary>
    /// UI 音效的全局出入口。音频资源放在 Assets/Resources/Audio/SE 下，
    /// 第一次调用时创建常驻 AudioSource，切场景也不会被销毁。
    /// 素材来源见 Document：RPG Maker VX Ace RTP 的 Cursor1 / Sword1 / Sword3。
    /// </summary>
    public static class UISound
    {
        public const string CursorPath = "Audio/SE/选择-更换(Cursor1)";
        public const string OkPath = "Audio/SE/选择-确定(Sword1)";
        public const string CancelPath = "Audio/SE/选择-取消(Sword3)";
        public const string BuzzerPath = "Audio/SE/无效-buzzer";

        /// <summary>光标音最小间隔，防止快速划过多个按钮时连响成一团。</summary>
        private const float CursorCooldown = 0.045f;

        private static AudioSource _source;
        private static AudioClip _cursor;
        private static AudioClip _ok;
        private static AudioClip _cancel;
        private static AudioClip _buzzer;
        private static float _lastCursorAt = -99f;

        public static bool Ready
        {
            get { Ensure(); return _source != null; }
        }

        private static void Ensure()
        {
            if (_source != null) return;

            var go = new GameObject("[UISound]");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;

            _cursor = Resources.Load<AudioClip>(CursorPath);
            _ok = Resources.Load<AudioClip>(OkPath);
            _cancel = Resources.Load<AudioClip>(CancelPath);
            _buzzer = Resources.Load<AudioClip>(BuzzerPath);

            if (_cursor == null) Debug.LogWarning("[UISound] 找不到 " + CursorPath);
            if (_ok == null) Debug.LogWarning("[UISound] 找不到 " + OkPath);
            if (_cancel == null) Debug.LogWarning("[UISound] 找不到 " + CancelPath);
        }

        private static void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            Ensure();
            if (_source == null) return;
            _source.PlayOneShot(clip, volume * WwgAudioSettings.SfxVolume);
        }

        /// <summary>光标在选项间移动（鼠标移上按钮）。</summary>
        public static void Cursor()
        {
            if (Time.unscaledTime - _lastCursorAt < CursorCooldown) return;
            _lastCursorAt = Time.unscaledTime;
            Ensure();
            Play(_cursor, 0.55f);
        }

        /// <summary>确认 / 进入。</summary>
        public static void Ok()
        {
            Ensure();
            Play(_ok, 0.7f);
        }

        /// <summary>返回 / 取消。存档系统上线后接到对应按钮。</summary>
        public static void Cancel()
        {
            Ensure();
            Play(_cancel, 0.7f);
        }

        /// <summary>操作无效。</summary>
        public static void Buzzer()
        {
            Ensure();
            Play(_buzzer, 0.6f);
        }
    }
}
