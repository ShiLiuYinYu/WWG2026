#if UNITY_EDITOR
using UnityEngine;

namespace WWG
{
    /// <summary>Editor-only, in-memory starting states for playing a chapter scene directly.</summary>
    public static class ChapterPreviewStates
    {
        public static GameState S2(string sceneName)
        {
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.BridgeRoute,
                stepId = "bridgeReached",
                locationId = sceneName == "S2_BridgeSpan4F" ? StoryIds.Location.BridgeSpan4F :
                    sceneName == "S2_BridgeEven4F" ? StoryIds.Location.BridgeEven4F :
                    sceneName == "S2_ClubRoom" ? StoryIds.Location.ClubRoom : StoryIds.Location.BridgeOdd4F,
                locationVariant = StoryIds.Variant.Night,
                timeId = "saturday.0010",
                san = 75,
                health = 3
            };
            state.SetFlag(StoryIds.Flag.P1Complete);
            state.SetFlag(StoryIds.Flag.P1Shock);
            state.AddItem(StoryIds.Item.Photo);
            state.AddItem(StoryIds.Item.Badge);
            if (sceneName == "S2_BridgeEven4F")
            {
                state.ChooseRoute(StoryIds.Choice.RouteLight);
                state.SetFlag(StoryIds.Flag.P2EmergencyLightOn);
            }
            else if (sceneName == "S2_ClubRoom")
            {
                state.ChooseRoute(StoryIds.Choice.RouteLight);
                state.SetFlag(StoryIds.Flag.P2DrawerTaken);
                state.AddItem(StoryIds.Item.MachineNote);
                state.AddItem(StoryIds.Item.TeamSheet);
                state.stepId = "p2Complete";
                state.timeId = "saturday.0100";
            }
            return state;
        }

        public static GameState S3()
        {
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.ServerRecovery,
                stepId = "p3Start",
                locationId = StoryIds.Location.ServerRoom3F,
                locationVariant = StoryIds.Variant.Night,
                timeId = "saturday.0100",
                san = 75,
                health = 3
            };
            state.SetFlag(StoryIds.Flag.P1Complete);
            state.SetFlag(StoryIds.Flag.P2DrawerTaken);
            state.AddItem(StoryIds.Item.Photo);
            state.AddItem(StoryIds.Item.Badge);
            state.AddItem(StoryIds.Item.MachineNote);
            state.AddItem(StoryIds.Item.TeamSheet);
            state.ChooseRoute(StoryIds.Choice.RouteLight);
            return state;
        }

        public static GameState S4(string route = StoryIds.Choice.RouteLight)
        {
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.Finale,
                stepId = "arrival",
                locationId = StoryIds.Location.ClubRoom,
                locationVariant = StoryIds.Variant.Night,
                timeId = "saturday.0200",
                san = 65,
                health = 2
            };
            state.SetFlag(StoryIds.Flag.P1Complete);
            state.SetFlag(StoryIds.Flag.P2DrawerTaken);
            state.SetFlag(StoryIds.Flag.P3Recovered);
            state.SetFlag(StoryIds.Flag.P3MemoHeard);
            state.SetFlag(StoryIds.Flag.P3Copied);
            state.AddItem(StoryIds.Item.Photo);
            state.AddItem(StoryIds.Item.Badge);
            state.AddItem(StoryIds.Item.MachineNote);
            state.AddItem(StoryIds.Item.TeamSheet);
            state.AddItem(StoryIds.Item.OldProjectUsb);
            state.ChooseRoute(route);
            return state;
        }

        /// <summary>S5 复盘：接续 S4 任一 24H 结尾。</summary>
        public static GameState S5(string route = StoryIds.Choice.RouteLight,
            string view = StoryIds.Choice.ViewTogether)
        {
            var state = new GameState
            {
                chapterId = StoryIds.Chapter.Replay,
                stepId = "s5Start",
                locationId = StoryIds.Location.ClubRoom,
                locationVariant = StoryIds.Variant.Day,
                timeId = "saturday.1605",
                san = 55,
                health = 2
            };
            state.SetFlag(StoryIds.Flag.S4Finished);
            state.SetFlag(StoryIds.Flag.S4BadgeArchived);
            state.SetFlag(StoryIds.Flag.S4Rested);
            state.SetFlag(StoryIds.Flag.S4UsbMounted);
            state.SetFlag(StoryIds.Flag.P1Complete);
            state.SetFlag(StoryIds.Flag.P3Recovered);
            state.SetFlag(StoryIds.Flag.P3MemoHeard);
            state.SetFlag(StoryIds.Flag.P3Copied);
            state.AddItem(StoryIds.Item.OldProjectUsb);
            state.AddItem(StoryIds.Item.TeamSheet);
            state.AddItem(StoryIds.Item.Badge);
            state.ChooseRoute(route);
            state.ChooseView(view);
            return state;
        }

        /// <summary>S6 第二次熄灯：从 5F 活动室出发；sceneName 决定当前地点。</summary>
        public static GameState S6(string sceneName, string route = StoryIds.Choice.RouteLight,
            string view = StoryIds.Choice.ViewTogether)
        {
            var state = S5(route, view);
            state.chapterId = StoryIds.Chapter.SecondBlackout;
            state.stepId = "s6Start";
            state.locationVariant = StoryIds.Variant.Night;
            state.timeId = "saturday.2330";
            state.san = Mathf.Max(60, state.san);
            state.SetFlag(StoryIds.Flag.P4Complete);
            state.SetFlag(StoryIds.Flag.P4RuleSeen);
            state.SetFlag(StoryIds.Flag.S5Finished);
            state.locationId = sceneName == "S6_Corridor" ? StoryIds.Location.Corridor5F :
                sceneName == "S6_BridgeSpan4F" ? StoryIds.Location.BridgeSpan4F :
                sceneName == "S6_BridgeEven4F" ? StoryIds.Location.BridgeEven4F :
                sceneName == "S6_ServerRoom3F" ? StoryIds.Location.ServerRoom3F :
                StoryIds.Location.BridgeOdd4F;
            return state;
        }

        /// <summary>S7 归队确认：已完成路线取证。</summary>
        public static GameState S7(string route = StoryIds.Choice.RouteLight,
            string view = StoryIds.Choice.ViewTogether)
        {
            var state = S5(route, view);
            state.chapterId = StoryIds.Chapter.Reunion;
            state.stepId = "s7Start";
            state.locationVariant = StoryIds.Variant.Night;
            state.timeId = "sunday.0020";
            state.san = Mathf.Max(55, state.san);
            state.SetFlag(StoryIds.Flag.P4Complete);
            state.SetFlag(StoryIds.Flag.S5Finished);
            state.SetFlag(StoryIds.Flag.S6Finished);
            if (route == StoryIds.Choice.RouteDark)
            {
                state.SetFlag(StoryIds.Flag.P5GlassMark);
                state.SetFlag(StoryIds.Flag.P5BackupRule);
            }
            else
            {
                state.SetFlag(StoryIds.Flag.P5LightRead);
                state.SetFlag(StoryIds.Flag.P5PaperRule);
            }
            state.SetFlag(StoryIds.Flag.P5Complete);
            return state;
        }

        /// <summary>S8 封包：周日白天；ending 决定已选材料。</summary>
        public static GameState S8(string route = StoryIds.Choice.RouteLight,
            string view = StoryIds.Choice.ViewTogether, string ending = null)
        {
            var state = S7(route, view);
            state.chapterId = StoryIds.Chapter.Packaging;
            state.stepId = "s8Start";
            state.locationVariant = StoryIds.Variant.Day;
            state.timeId = "sunday.1000";
            state.san = Mathf.Max(70, state.san);
            state.health = 3;
            state.SetFlag(StoryIds.Flag.P6NameRestored);
            state.SetFlag(StoryIds.Flag.P6TeamConfirmed);
            state.SetFlag(StoryIds.Flag.P6Verified);
            state.SetFlag(StoryIds.Flag.P6Complete);
            state.SetFlag(StoryIds.Flag.S7Finished);
            state.SetFlag(StoryIds.Flag.S8MaterialsSeen);
            if (!string.IsNullOrEmpty(ending)) state.ChooseEnding(ending);
            return state;
        }
    }
}
#endif
