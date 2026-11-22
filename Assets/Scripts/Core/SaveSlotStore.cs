using System;
using UnityEngine;

namespace WWG
{
    [Serializable]
    public sealed class SaveSlotSnapshot
    {
        public int version = 1;
        public string chapter;
        public string locationLabel;
        public string gameTime;
        public string savedAt;
        public string payload;
    }

    /// <summary>Three player-facing slots wrapping the existing chapter checkpoint formats.</summary>
    public static class SaveSlotStore
    {
        public const int SlotCount = 3;
        private const string Prefix = "WWG_SaveSlot_";
        private const string MigrationKey = "WWG_SaveSlotsMigrated";

        public static SaveSlotSnapshot Read(int index)
        {
            if (index < 0 || index >= SlotCount || !PlayerPrefs.HasKey(Prefix + index)) return null;
            try
            {
                SaveSlotSnapshot slot = JsonUtility.FromJson<SaveSlotSnapshot>(PlayerPrefs.GetString(Prefix + index));
                return IsValid(slot) ? slot : null;
            }
            catch (Exception) { return null; }
        }

        public static bool Write(int index, SaveSlotSnapshot snapshot, out string error)
        {
            if (index < 0 || index >= SlotCount) { error = "存档槽位无效"; return false; }
            if (!IsValid(snapshot)) { error = "当前进度无法保存"; return false; }
            if (string.IsNullOrWhiteSpace(snapshot.gameTime)) snapshot.gameTime = GetGameTime(snapshot);
            snapshot.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            PlayerPrefs.SetString(Prefix + index, JsonUtility.ToJson(snapshot));
            PlayerPrefs.Save();
            error = null;
            return true;
        }

        public static void Delete(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            PlayerPrefs.DeleteKey(Prefix + index);
            PlayerPrefs.Save();
        }

        public static bool Load(int index, out string error)
        {
            SaveSlotSnapshot slot = Read(index);
            if (slot == null) { error = "这个槽位没有可读取的存档"; return false; }
            return LoadSnapshot(slot, out error);
        }

        private static readonly string[] StateChapters =
        {
            StoryIds.Chapter.BridgeRoute, StoryIds.Chapter.ServerRecovery, StoryIds.Chapter.Finale,
            StoryIds.Chapter.Replay, StoryIds.Chapter.SecondBlackout, StoryIds.Chapter.Reunion,
            StoryIds.Chapter.Packaging
        };

        private static GameState loadedState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoadedState() => loadedState = null;

        internal static GameState TakeLoadedState(string chapter)
        {
            if (loadedState == null || loadedState.chapterId != chapter) return null;
            GameState state = loadedState;
            loadedState = null;
            return state;
        }

        private static SaveSlotSnapshot ReadLegacyCheckpoint(int chapter)
        {
            string key = chapter == 0 ? "WWG_S0_ManualCheckpoint" : "WWG_S1_Checkpoint";
            string label = "S" + chapter;
            var slot = new SaveSlotSnapshot { chapter = label, locationLabel = label + " · 最近检查点",
                payload = PlayerPrefs.GetString(key) };
            return IsValid(slot) ? slot : null;
        }

        public static SaveSlotSnapshot ReadLatestCheckpoint()
        {
            int chapter = PlayerPrefs.GetInt("WWG_LastCheckpointChapter", -1);
            if (chapter == 0 || chapter == 1)
            {
                var preferred = ReadLegacyCheckpoint(chapter);
                if (preferred != null) return preferred;
            }

            string payload = PlayerPrefs.GetString(GameStateStore.SaveKey);
            try
            {
                var state = JsonUtility.FromJson<GameState>(payload);
                int index = state == null ? -1 : Array.IndexOf(StateChapters, state.chapterId);
                if (index >= 0 && state.Validate(out _) && StoryIds.LocationScene(state.chapterId, state.locationId) != null)
                {
                    string label = "S" + (index + 2);
                    return new SaveSlotSnapshot { chapter = label, locationLabel = label + " · 最近检查点", payload = payload };
                }
            }
            catch (ArgumentException) { }

            return ReadLegacyCheckpoint(1) ?? ReadLegacyCheckpoint(0);
        }

        public static bool LoadLatestCheckpoint(out string error)
        {
            SaveSlotSnapshot latest = ReadLatestCheckpoint();
            if (latest == null) { error = "没有可读取的最近检查点"; return false; }
            return LoadSnapshot(latest, out error);
        }

        private static bool LoadSnapshot(SaveSlotSnapshot slot, out string error)
        {
            if (slot.chapter == "S0")
            {
                PlayerPrefs.SetString("WWG_S0_ManualCheckpoint", slot.payload);
                PlayerPrefs.SetInt("WWG_S0_LoadManual", 1);
                PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 0);
                PlayerPrefs.Save();
                return LoadScene(GameFlow.FirstGameplayScene, out error);
            }
            if (slot.chapter == "S1")
            {
                PlayerPrefs.SetString("WWG_S1_Checkpoint", slot.payload);
                PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 1);
                PlayerPrefs.Save();
                return LoadScene(GameFlow.S1Scene, out error);
            }
            GameState state;
            try { state = JsonUtility.FromJson<GameState>(slot.payload); }
            catch (Exception ex) { error = "存档损坏：" + ex.Message; return false; }
            if (state == null) { error = "存档内容为空"; return false; }
            if (!state.Validate(out error)) return false;
            string path = StoryIds.LocationScene(state.chapterId, state.locationId);
            string scene = System.IO.Path.GetFileNameWithoutExtension(path);
            if (scene == null || !Application.CanStreamedLevelBeLoaded(scene))
            {
                error = "地点场景未加入 Build Settings：" + state.locationId;
                return false;
            }

            state.Normalize();
            if (!GameStateStore.SaveCheckpoint(state, out error)) return false;
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", Array.IndexOf(StateChapters, state.chapterId) + 2);
            PlayerPrefs.Save();
            loadedState = state;
            if (LoadScene(scene, out error)) return true;
            loadedState = null;
            return false;
        }

        public static void ImportLegacyCheckpointOnce()
        {
            if (PlayerPrefs.GetInt(MigrationKey, 0) == 1) return;
            PlayerPrefs.SetInt(MigrationKey, 1);
            for (int i = 0; i < SlotCount; i++)
                if (Read(i) != null) { PlayerPrefs.Save(); return; }
            SaveSlotSnapshot legacy = ReadLatestCheckpoint();
            if (legacy != null) legacy.locationLabel = legacy.chapter + " · 旧检查点";
            if (legacy != null && IsValid(legacy)) Write(0, legacy, out _);
            PlayerPrefs.Save();
        }

        private static bool IsValid(SaveSlotSnapshot slot)
        {
            if (slot == null || slot.version != 1 || string.IsNullOrWhiteSpace(slot.payload) ||
                string.IsNullOrWhiteSpace(slot.locationLabel)) return false;
            if (slot.chapter == "S0" || slot.chapter == "S1")
                return slot.payload.Contains("\"phase\"") && slot.payload.Contains("\"screen\"");
            if (slot.chapter == null || slot.chapter.Length != 2 || slot.chapter[0] != 'S') return false;
            int index = slot.chapter[1] - '2';
            if (index < 0 || index >= StateChapters.Length) return false;
            try
            {
                GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                return state != null && state.chapterId == StateChapters[index] && state.Validate(out _) &&
                    StoryIds.LocationScene(state.chapterId, state.locationId) != null;
            }
            catch (Exception) { return false; }
        }

        public static string GetGameTime(SaveSlotSnapshot slot)
        {
            if (slot == null) return "时间未记录";
            if (!string.IsNullOrWhiteSpace(slot.gameTime)) return slot.gameTime;
            if (!string.IsNullOrWhiteSpace(slot.locationLabel))
            {
                string[] parts = slot.locationLabel.Split('·');
                if (parts.Length >= 3 && parts[1].Trim().StartsWith("周")) return parts[1].Trim();
            }
            try
            {
                if (slot.chapter == "S4")
                {
                    GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                    return state.HasFlag(StoryIds.Flag.S4TangTalked) ? "周六 16:00" :
                        state.HasFlag(StoryIds.Flag.S4Rested) ? "周六 10:00" : "周六 02:00";
                }
                if (slot.chapter == "S3")
                {
                    GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                    return state.HasFlag(StoryIds.Flag.P3Copied) ? "周六 01:45" :
                        state.HasFlag(StoryIds.Flag.P3Recovered) ? "周六 01:35" : "周六 01:00";
                }
                if (slot.chapter == "S2")
                {
                    GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                    return state.HasFlag(StoryIds.Flag.P3Copied) ? "周六 01:45" :
                        state.timeId == "saturday.0050" ? "周六 00:50" : "周六 00:10";
                }
                if (slot.chapter == "S5" || slot.chapter == "S6" || slot.chapter == "S7" || slot.chapter == "S8")
                {
                    GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                    switch (state.timeId)
                    {
                        case "saturday.1605": return "周六 16:05";
                        case "saturday.2329": return "周六 23:29";
                        case "saturday.2330": return "周六 23:30";
                        case "sunday.0020": return "周日 00:20";
                        case "sunday.1000": return "周日 10:00";
                        case "sunday.1600": return "周日 16:00";
                        default: return "时间未记录";
                    }
                }
                LegacyTime time = JsonUtility.FromJson<LegacyTime>(slot.payload);
                if (slot.chapter == "S1") return time.phase >= 4 ? "周六 00:10" :
                    time.phase >= 2 ? "周六 00:00" : "周五 23:30";
                return time.phase >= 4 ? "周五 23:30" : time.phase >= 2 ? "周五 16:10" :
                    time.phase == 1 ? "周五 15:55" : "周五 15:40";
            }
            catch (Exception) { return "时间未记录"; }
        }

        [Serializable]
        private sealed class LegacyTime { public int phase; }

        private static bool LoadScene(string scene, out string error)
        {
            if (GameFlow.LoadLocation(scene)) { error = null; return true; }
            error = "地点场景未加入 Build Settings：" + scene;
            return false;
        }
    }
}
