namespace WWG
{
    public static class StoryIds
    {
        public static class Chapter
        {
            public const string Prologue = "Prologue";
            public const string NightPhoto = "NightPhoto";
            public const string BridgeRoute = "BridgeRoute";
            public const string ServerRecovery = "ServerRecovery";
            public const string Finale = "Finale";
            public const string Replay = "Replay";
            public const string SecondBlackout = "SecondBlackout";
            public const string Reunion = "Reunion";
            public const string Packaging = "Packaging";
        }

        public static class Location
        {
            public const string Exterior = "Exterior";
            public const string Corridor5F = "Corridor5F";
            public const string ClubRoom = "ClubRoom";
            public const string BridgeOdd4F = "BridgeOdd4F";
            public const string BridgeSpan4F = "BridgeSpan4F";
            public const string BridgeEven4F = "BridgeEven4F";
            public const string ServerRoom3F = "ServerRoom3F";
        }

        public static class Variant
        {
            public const string Day = "day";
            public const string Night = "night";
            public const string Emergency = "emergency";
        }

        public static class Item
        {
            public const string Photo = "photo";
            public const string Badge = "badge";
            public const string MachineNote = "machineNote";
            public const string TeamSheet = "teamSheet";
            public const string OldProjectUsb = "oldProjectUsb";
        }

        public static class Choice
        {
            public const string RouteLight = "RouteLight";
            public const string RouteDark = "RouteDark";
            public const string ViewTogether = "ViewTogether";
            public const string ViewAlone = "ViewAlone";
            public const string EndingLetter = "EndingLetter";
            public const string EndingSeal = "EndingSeal";
        }

        public static class Flag
        {
            public const string P1Complete = "flag.p1.complete";
            public const string P1Shock = "flag.p1.shock";
            public const string P2OddFarRead = "flag.p2.oddFarRead";
            public const string P2EvenFarRead = "flag.p2.evenFarRead";
            public const string P2CabinetOpen = "flag.p2.cabinetOpen";
            public const string P2EmergencyLightOn = "flag.p2.emergencyLightOn";
            public const string P2GlassRead = "flag.p2.glassRead";
            public const string P2OddNearRead = "flag.p2.oddNearRead";
            public const string P2EvenNearRead = "flag.p2.evenNearRead";
            public const string P2DrawerTaken = "flag.p2.drawerTaken";
            public const string P3SketchRead = "flag.p3.sketchRead";
            public const string P3Recovered = "flag.p3.recovered";
            public const string P3MemoHeard = "flag.p3.memoHeard";
            public const string P3Copied = "flag.p3.copied";
            public const string S4BadgeArchived = "flag.s4.badgeArchived";
            public const string S4Rested = "flag.s4.rested";
            public const string S4YuanTalked = "flag.s4.yuanTalked";
            public const string S4XuTalked = "flag.s4.xuTalked";
            public const string S4TangTalked = "flag.s4.tangTalked";
            public const string S4YuanWitness = "flag.s4.yuanWitness";
            public const string S4XuWitness = "flag.s4.xuWitness";
            public const string S4UsbMounted = "flag.s4.usbMounted";
            public const string S4Finished = "flag.s4.finished";

            public const string P4BadgeRead = "flag.p4.badgeRead";
            public const string P4FrameRead = "flag.p4.frameRead";
            public const string P4FrameFound = "flag.p4.frameFound";
            public const string P4LogRead = "flag.p4.logRead";
            public const string P4LogFound = "flag.p4.logFound";
            public const string P4Complete = "flag.p4.complete";
            public const string P4RuleSeen = "flag.p4.ruleSeen";
            public const string S5Finished = "flag.s5.finished";

            public const string P5LightRead = "flag.p5.lightRead";
            public const string P5GlassMark = "flag.p5.glassMark";
            public const string P5SheetPlaced = "flag.p5.sheetPlaced";
            public const string P5PaperRule = "flag.p5.paperRule";
            public const string P5BackupRule = "flag.p5.backupRule";
            public const string P5Complete = "flag.p5.complete";
            public const string S6Finished = "flag.s6.finished";

            public const string P6NameVerified = "flag.p6.nameVerified";
            public const string P6BadgeChecked = "flag.p6.badgeChecked";
            public const string P6NameSelected = "flag.p6.nameSelected";
            public const string P6ReviewAtSecondEvent = "flag.p6.reviewAtSecondEvent";
            public const string P6ReviewAuthor = "flag.p6.reviewAuthor";
            public const string P6ReviewPlayer = "flag.p6.reviewPlayer";
            public const string P6NameRestored = "flag.p6.nameRestored";
            public const string P6TeamConfirmed = "flag.p6.teamConfirmed";
            public const string P6ReviewDone = "flag.p6.reviewDone";
            public const string P6Verified = "flag.p6.verified";
            public const string P6Complete = "flag.p6.complete";
            public const string S7Finished = "flag.s7.finished";

            public const string S8MaterialsSeen = "flag.s8.materialsSeen";
            public const string S8LetterAttached = "flag.s8.letterAttached";
            public const string S8Previewed = "flag.s8.previewed";
            public const string S8ArchiveCopy = "flag.s8.archiveCopy";
            public const string S8Archived = "flag.s8.archived";
            public const string S8Finished = "flag.s8.finished";
        }

        /// <summary>True once the route-specific reunion rule of S6 was recorded.</summary>
        public static bool HasReunionRule(GameState state) =>
            state != null && (state.HasFlag(Flag.P5PaperRule) || state.HasFlag(Flag.P5BackupRule));

        public static bool IsChapter(string id) => id == Chapter.Prologue || id == Chapter.NightPhoto ||
            id == Chapter.BridgeRoute || id == Chapter.ServerRecovery || id == Chapter.Finale ||
            id == Chapter.Replay || id == Chapter.SecondBlackout || id == Chapter.Reunion ||
            id == Chapter.Packaging;

        public static bool IsLocation(string id) => id == Location.Exterior || id == Location.Corridor5F ||
            id == Location.ClubRoom || id == Location.BridgeOdd4F || id == Location.BridgeSpan4F ||
            id == Location.BridgeEven4F || id == Location.ServerRoom3F;

        public static bool IsVariant(string id) => id == Variant.Day || id == Variant.Night ||
            id == Variant.Emergency;

        public static string LocationScene(string chapterId, string locationId)
        {
            if (chapterId == Chapter.Prologue)
            {
                switch (locationId)
                {
                    case Location.Exterior: return "Assets/Scenes/S0.unity";
                    case Location.Corridor5F: return "Assets/Scenes/S0/S0_Corridor.unity";
                    case Location.ClubRoom: return "Assets/Scenes/S0/S0_ClubRoom.unity";
                    case Location.BridgeOdd4F: return "Assets/Scenes/S0/S0_Bridge.unity";
                }
            }
            else if (chapterId == Chapter.NightPhoto)
            {
                switch (locationId)
                {
                    case Location.ClubRoom: return "Assets/Scenes/S1.unity";
                    case Location.Corridor5F: return "Assets/Scenes/S1/S1_Corridor.unity";
                    case Location.BridgeOdd4F: return "Assets/Scenes/S1/S1_BridgeEntrance.unity";
                }
            }
            else if (chapterId == Chapter.BridgeRoute)
            {
                switch (locationId)
                {
                    case Location.BridgeOdd4F: return "Assets/Scenes/S2.unity";
                    case Location.BridgeSpan4F: return "Assets/Scenes/S2/S2_BridgeSpan4F.unity";
                    case Location.BridgeEven4F: return "Assets/Scenes/S2/S2_BridgeEven4F.unity";
                    case Location.ClubRoom: return "Assets/Scenes/S2/S2_ClubRoom.unity";
                }
            }
            else if (chapterId == Chapter.ServerRecovery && locationId == Location.ServerRoom3F)
                return "Assets/Scenes/S3.unity";
            else if (chapterId == Chapter.Finale && locationId == Location.ClubRoom)
                return "Assets/Scenes/S4.unity";
            else if (chapterId == Chapter.Replay && locationId == Location.ClubRoom)
                return "Assets/Scenes/S5.unity";
            else if (chapterId == Chapter.SecondBlackout)
            {
                switch (locationId)
                {
                    case Location.Corridor5F: return "Assets/Scenes/S6/S6_Corridor.unity";
                    case Location.BridgeOdd4F: return "Assets/Scenes/S6/S6_BridgeOdd4F.unity";
                    case Location.BridgeSpan4F: return "Assets/Scenes/S6/S6_BridgeSpan4F.unity";
                    case Location.BridgeEven4F: return "Assets/Scenes/S6/S6_BridgeEven4F.unity";
                    case Location.ServerRoom3F: return "Assets/Scenes/S6/S6_ServerRoom3F.unity";
                }
            }
            else if (chapterId == Chapter.Reunion && locationId == Location.ClubRoom)
                return "Assets/Scenes/S7.unity";
            else if (chapterId == Chapter.Packaging && locationId == Location.ClubRoom)
                return "Assets/Scenes/S8.unity";
            return null;
        }
    }
}
