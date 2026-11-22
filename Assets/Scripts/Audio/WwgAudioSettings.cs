using UnityEngine;

namespace WWG
{
    /// <summary>
    /// 全局音量设置：音乐与音效各一条，保存在 PlayerPrefs。
    /// 不再使用 AudioListener.volume 做主音量，音频源在播放时各自乘上对应音量，
    /// 这样两条滑条互不干扰。旧版的 WWG_MasterVolume 只在存在且尚无新键时迁移成初值。
    /// </summary>
    public static class WwgAudioSettings
    {
        public const string MusicKey = "WWG_MusicVolume";
        public const string SfxKey = "WWG_SfxVolume";
        private const string LegacyMasterKey = "WWG_MasterVolume";
        public const float DefaultVolume = 0.8f;

        private static bool migrated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            AudioListener.volume = 1f;
            MigrateLegacyMaster();
        }

        public static float MusicVolume
        {
            get { MigrateLegacyMaster(); return Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultVolume)); }
        }

        public static float SfxVolume
        {
            get { MigrateLegacyMaster(); return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultVolume)); }
        }

        public static void SetMusicVolume(float value)
        {
            Set(MusicKey, value);
        }

        public static void SetSfxVolume(float value)
        {
            Set(SfxKey, value);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        private static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        }

        private static void MigrateLegacyMaster()
        {
            if (migrated) return;
            migrated = true;
            if (PlayerPrefs.HasKey(MusicKey) || PlayerPrefs.HasKey(SfxKey)) return;
            if (!PlayerPrefs.HasKey(LegacyMasterKey)) return;
            float legacy = Mathf.Clamp01(PlayerPrefs.GetFloat(LegacyMasterKey, DefaultVolume));
            PlayerPrefs.SetFloat(MusicKey, legacy);
            PlayerPrefs.SetFloat(SfxKey, legacy);
        }
    }
}
