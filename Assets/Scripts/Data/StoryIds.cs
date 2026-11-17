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
        }

        public static bool IsChapter(string id) => id == Chapter.Prologue || id == Chapter.NightPhoto ||
            id == Chapter.BridgeRoute || id == Chapter.ServerRecovery || id == Chapter.Finale;

        public static bool IsLocation(string id) => id == Location.Exterior || id == Location.Corridor5F ||
            id == Location.ClubRoom || id == Location.BridgeOdd4F || id == Location.BridgeSpan4F ||
            id == Location.BridgeEven4F || id == Location.ServerRoom3F;

        public static bool IsVariant(string id) => id == Variant.Day || id == Variant.Night ||
            id == Variant.Emergency;

        public static string LocationScene(string locationId) => IsLocation(locationId)
            ? "Assets/Scenes/Locations/" + locationId + ".unity"
            : null;
    }
}
