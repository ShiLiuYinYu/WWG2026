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

        public static SaveSlotSnapshot ReadLatestCheckpoint()
        {
            int chapter = PlayerPrefs.GetInt("WWG_LastCheckpointChapter", -1);
            string[] order = chapter == 0 ? new[] { "S0", "S1", "S2", "S3", "S4" } :
                chapter == 1 ? new[] { "S1", "S0", "S2", "S3", "S4" } :
                chapter == 3 ? new[] { "S3", "S4", "S2", "S1", "S0" } :
                chapter == 4 ? new[] { "S4", "S3", "S2", "S1", "S0" } :
                new[] { "S2", "S3", "S4", "S1", "S0" };
            foreach (string current in order)
            {
                string key = current == "S2" || current == "S3" || current == "S4" ? GameStateStore.SaveKey :
                    current == "S1" ? "WWG_S1_Checkpoint" : "WWG_S0_ManualCheckpoint";
                if (!PlayerPrefs.HasKey(key)) continue;
                var slot = new SaveSlotSnapshot { chapter = current, locationLabel = current + " · 最近检查点",
                    payload = PlayerPrefs.GetString(key) };
                if (IsValid(slot)) return slot;
            }
            return null;
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
            if (!GameStateStore.SaveCheckpoint(state, out error)) return false;
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", state.chapterId == StoryIds.Chapter.Finale ? 4 :
                state.chapterId == StoryIds.Chapter.ServerRecovery ? 3 : 2);
            PlayerPrefs.Save();
            string scene = state.chapterId == StoryIds.Chapter.Finale ? GameFlow.S4Scene :
                state.locationId == StoryIds.Location.ClubRoom ? "ClubRoom" : state.locationId;
            return LoadScene(scene, out error);
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
            if (slot.chapter != "S2" && slot.chapter != "S3" && slot.chapter != "S4") return false;
            try
            {
                GameState state = JsonUtility.FromJson<GameState>(slot.payload);
                return state != null && state.chapterId ==
                    (slot.chapter == "S4" ? StoryIds.Chapter.Finale :
                        slot.chapter == "S3" ? StoryIds.Chapter.ServerRecovery : StoryIds.Chapter.BridgeRoute) &&
                    state.Validate(out _);
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
