#if UNITY_EDITOR
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
                state.SetFlag(StoryIds.Flag.P2GlassRead);
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
    }
}
#endif
