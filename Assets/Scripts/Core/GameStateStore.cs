using System;
using UnityEngine;

namespace WWG
{
    public static class GameStateStore
    {
        public const string SaveKey = "WWG_Save";
        private const string S0Key = "WWG_S0_ManualCheckpoint";
        private const string S1Key = "WWG_S1_Checkpoint";
        private const string LastChapterKey = "WWG_LastCheckpointChapter";

        [Serializable]
        private sealed class LegacyS0
        {
            public int phase;
            public int screen;
            public bool boardRead, chairRead, signRead, timerRead, guideRead;
        }

        [Serializable]
        private sealed class LegacyS1
        {
            public int phase;
            public int screen;
            public int san;
            public int health;
            public bool hasPhoto, hasBadge, curtainClosed, lampOn, chairPlaced;
            public bool shockApplied, glassRead;
        }

        public static bool HasCheckpoint() => PlayerPrefs.HasKey(SaveKey) ||
            PlayerPrefs.HasKey(S0Key) || PlayerPrefs.HasKey(S1Key);

        public static bool SaveCheckpoint(GameState state, out string error)
        {
            if (state == null) { error = "状态为空"; return false; }
            if (!state.Validate(out error)) return false;
            try
            {
                string json = JsonUtility.ToJson(state);
                var roundTrip = JsonUtility.FromJson<GameState>(json);
                if (roundTrip == null || !roundTrip.Validate(out error)) return false;
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = "检查点写入失败：" + ex.Message;
                return false;
            }
        }

        public static bool LoadCheckpoint(out GameState state, out string error)
        {
            state = null;
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                if (!TryMigrateLegacy(out error)) return false;
            }
            try
            {
                state = JsonUtility.FromJson<GameState>(PlayerPrefs.GetString(SaveKey));
                if (state == null) { error = "检查点内容为空"; return false; }
                state.EnsureCollections();
                if (!state.Validate(out error)) { state = null; return false; }
                state.Normalize();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                state = null;
                error = "检查点读取失败：" + ex.Message;
                return false;
            }
        }

        public static void DeleteCheckpoint()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private static bool TryMigrateLegacy(out string error)
        {
            int preferred = PlayerPrefs.GetInt(LastChapterKey, -1);
            bool hasS0 = PlayerPrefs.HasKey(S0Key);
            bool hasS1 = PlayerPrefs.HasKey(S1Key);
            if (!hasS0 && !hasS1) { error = "没有检查点"; return false; }

            string key = preferred == 1 && hasS1 ? S1Key :
                preferred == 0 && hasS0 ? S0Key : hasS1 ? S1Key : S0Key;
            try
            {
                string json = PlayerPrefs.GetString(key);
                if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"phase\"") ||
                    !json.Contains("\"screen\""))
                {
                    error = "旧检查点缺少阶段或地点";
                    return false;
                }
                GameState converted = key == S1Key ? ConvertS1(JsonUtility.FromJson<LegacyS1>(json)) :
                    ConvertS0(JsonUtility.FromJson<LegacyS0>(json));
                if (converted == null)
                {
                    error = "旧检查点无法迁移：内容无效";
                    return false;
                }
                if (!converted.Validate(out error))
                {
                    error = "旧检查点无法迁移：" + (error ?? "内容无效");
                    return false;
                }
                if (!SaveCheckpoint(converted, out error)) return false;
                // Keep the old key until a later explicit cleanup version.
                return true;
            }
            catch (Exception ex)
            {
                error = "旧检查点无法迁移：" + ex.Message;
                return false;
            }
        }

        private static GameState ConvertS0(LegacyS0 old)
        {
            if (old == null || old.phase < 0 || old.phase > 4 || old.screen < 0 || old.screen > 3)
                return null;
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.Prologue,
                stepId = "phase." + old.phase,
                locationId = new[] { StoryIds.Location.Exterior, StoryIds.Location.Corridor5F,
                    StoryIds.Location.ClubRoom, StoryIds.Location.BridgeOdd4F }[old.screen],
                locationVariant = old.phase == 4 ? StoryIds.Variant.Night : StoryIds.Variant.Day,
                timeId = old.phase == 4 ? "friday.2330" : "friday.day"
            };
            if (old.boardRead) state.SetFlag("flag.s0.board");
            if (old.chairRead) state.SetFlag("flag.s0.chair");
            if (old.signRead) state.SetFlag("flag.s0.sign");
            if (old.timerRead) state.SetFlag("flag.s0.timer");
            if (old.guideRead) state.SetFlag("flag.s0.guide");
            return state;
        }

        private static GameState ConvertS1(LegacyS1 old)
        {
            if (old == null || old.phase < 0 || old.phase > 4 || old.screen < 0 || old.screen > 2 ||
                old.san < 0 || old.san > 100 || old.health < 0 || old.health > 3)
                return null;
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.NightPhoto,
                stepId = old.phase == 4 ? "bridgeReached" : "phase." + old.phase,
                locationId = new[] { StoryIds.Location.ClubRoom, StoryIds.Location.Corridor5F,
                    StoryIds.Location.BridgeOdd4F }[old.screen],
                locationVariant = StoryIds.Variant.Night,
                timeId = old.phase >= 4 ? "saturday.0010" : old.phase >= 2 ? "saturday.0000" : "friday.2330",
                san = old.san,
                health = old.health
            };
            if (old.hasPhoto) state.AddItem(StoryIds.Item.Photo);
            if (old.hasBadge) state.AddItem(StoryIds.Item.Badge);
            if (old.curtainClosed) state.SetFlag("flag.p1.curtain");
            if (old.lampOn) state.SetFlag("flag.p1.lamp");
            if (old.chairPlaced) state.SetFlag("flag.p1.chair");
            if (old.phase >= 2) state.SetFlag(StoryIds.Flag.P1Complete);
            if (old.shockApplied) state.SetFlag(StoryIds.Flag.P1Shock);
            if (old.glassRead) state.SetFlag("flag.s1.glassRead");
            return state;
        }
    }
}
