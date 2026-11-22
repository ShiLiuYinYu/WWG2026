using System;
using System.Collections.Generic;

namespace WWG
{
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string chapterId = StoryIds.Chapter.Prologue;
        public string stepId = "start";
        public string locationId = StoryIds.Location.Exterior;
        public string locationVariant = StoryIds.Variant.Day;
        public string timeId = "friday.1540";
        public int san = 100;
        public int sanMax = 100;
        public int health = 3;
        public List<string> flags = new List<string>();
        public List<string> inventory = new List<string>();
        public List<string> seenDialogue = new List<string>();
        public string routeChoice;
        public string viewChoice;
        public string endingChoice;

        public bool HasFlag(string id) => Contains(flags, id);
        public bool HasItem(string id) => Contains(inventory, id);
        public bool HasSeenDialogue(string id) => Contains(seenDialogue, id);

        public bool SetFlag(string id) => AddUnique(flags, id);
        public bool ClearFlag(string id) => flags != null && !string.IsNullOrWhiteSpace(id) && flags.Remove(id);
        public bool AddItem(string id) => AddUnique(inventory, id);
        public bool MarkDialogueSeen(string id) => AddUnique(seenDialogue, id);

        public bool ChooseRoute(string choice)
        {
            if (choice != StoryIds.Choice.RouteLight && choice != StoryIds.Choice.RouteDark)
                return false;
            if (!string.IsNullOrEmpty(routeChoice)) return routeChoice == choice;
            routeChoice = choice;
            return true;
        }

        public bool ChooseView(string choice)
        {
            if (choice != StoryIds.Choice.ViewTogether && choice != StoryIds.Choice.ViewAlone)
                return false;
            if (!string.IsNullOrEmpty(viewChoice)) return viewChoice == choice;
            viewChoice = choice;
            return true;
        }

        public bool ChooseEnding(string choice)
        {
            if (choice != StoryIds.Choice.EndingLetter && choice != StoryIds.Choice.EndingSeal)
                return false;
            if (!string.IsNullOrEmpty(endingChoice)) return endingChoice == choice;
            endingChoice = choice;
            return true;
        }

        public void Normalize()
        {
            EnsureCollections();
            flags = Clean(flags);
            inventory = Clean(inventory);
            seenDialogue = Clean(seenDialogue);
            if (sanMax < 100) sanMax = 100;
            if (sanMax > 120) sanMax = 120;
            if (san < 0) san = 0;
            if (san > sanMax) san = sanMax;
            if (health < 0) health = 0;
            if (health > 3) health = 3;
        }

        public void EnsureCollections()
        {
            if (flags == null) flags = new List<string>();
            if (inventory == null) inventory = new List<string>();
            if (seenDialogue == null) seenDialogue = new List<string>();
        }

        public bool Validate(out string error)
        {
            if (version != CurrentVersion) { error = "存档版本不兼容"; return false; }
            if (!StoryIds.IsChapter(chapterId)) { error = "未知章节：" + chapterId; return false; }
            if (string.IsNullOrWhiteSpace(stepId)) { error = "章节步骤为空"; return false; }
            if (!StoryIds.IsLocation(locationId)) { error = "未知地点：" + locationId; return false; }
            if (!StoryIds.IsVariant(locationVariant)) { error = "未知地点变体：" + locationVariant; return false; }
            if (sanMax != 100 && sanMax != 120) { error = "SAN 上限无效"; return false; }
            if (san < 0 || san > sanMax || health < 0 || health > 3)
            {
                error = "SAN 或生命超出范围";
                return false;
            }
            if (!string.IsNullOrEmpty(routeChoice) &&
                routeChoice != StoryIds.Choice.RouteLight && routeChoice != StoryIds.Choice.RouteDark)
            {
                error = "路线选择无效";
                return false;
            }
            if (!string.IsNullOrEmpty(viewChoice) &&
                viewChoice != StoryIds.Choice.ViewTogether && viewChoice != StoryIds.Choice.ViewAlone)
            {
                error = "结尾选择无效";
                return false;
            }
            if (!string.IsNullOrEmpty(endingChoice) &&
                endingChoice != StoryIds.Choice.EndingLetter && endingChoice != StoryIds.Choice.EndingSeal)
            {
                error = "封包选择无效";
                return false;
            }
            if (flags == null || inventory == null || seenDialogue == null)
            {
                error = "进度列表缺失";
                return false;
            }
            error = null;
            return true;
        }

        private static bool Contains(List<string> values, string id) =>
            values != null && !string.IsNullOrWhiteSpace(id) && values.Contains(id);

        private static bool AddUnique(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id) || values.Contains(id)) return false;
            values.Add(id);
            return true;
        }

        private static List<string> Clean(List<string> values)
        {
            var result = new List<string>();
            if (values == null) return result;
            foreach (string id in values)
                if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id)) result.Add(id);
            return result;
        }
    }
}
