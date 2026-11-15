using System;
using System.Collections.Generic;

namespace WWG
{
    /// <summary>Long-form documents referenced from Yarn with &lt;&lt;show_info&gt;&gt;.</summary>
    public static class DialogueDocuments
    {
        public sealed class Entry
        {
            public string id;
            public string title;
            public string body;
            public bool residue;
        }

        private static readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal)
            {
                {
                    "test.document",
                    new Entry
                    {
                        id = "test.document",
                        title = "接入测试文档",
                        body = "这是 DialogueDocuments 静态表中的测试条目。\n\n" +
                               "长展板、便签和任务记录应存放在这里，由 <<show_info>> 按 ID 打开，" +
                               "而不是直接写进 Yarn 行。",
                        residue = false
                    }
                },
                {
                    "s0.directory",
                    new Entry
                    {
                        id = "s0.directory",
                        title = "门口楼层展板",
                        body = "实验楼导览｜单数栋 / 偶数栋\n\n" +
                               "1F  电气与电子工程\n2F  机器人工程\n3F  计算机工程\n" +
                               "4F  外国语学院（跨栋连廊）\n5F  数智设计学院\n\n" +
                               "【物业通知】工作日 23:00；周五、周六 23:30，走廊、楼梯及连廊等公共区域照明自动断电。\n" +
                               "实验室与社团活动室的内部插座独立供电，夜间不断电。",
                        residue = false
                    }
                },
                {
                    "s0.timer",
                    new Entry
                    {
                        id = "s0.timer",
                        title = "照明定时箱",
                        body = "周五、周六 23:30 自动切断走廊与楼梯照明。请勿私自扳动开关。\n\n" +
                               "从活动室门口到楼梯口只有十几步，但走廊没有外窗。",
                        residue = false
                    }
                },
                {
                    "s0.floorPlaque",
                    new Entry
                    {
                        id = "s0.floorPlaque",
                        title = "单数栋 5F · 数智设计学院",
                        body = "← 游戏开发协会活动室\n↓ 4F 连廊 / 3F 机房\n\n其余实验室门禁关闭。",
                        residue = false
                    }
                },
                {
                    "s0.guide",
                    new Entry
                    {
                        id = "s0.guide",
                        title = "双栋导览练习稿｜社团旧资料",
                        body = "去年社团留下的平面练习稿。\n\n单数栋 —— 4F 玻璃连廊 —— 偶数栋\n\n" +
                               "红笔批注：巡场顺序，先去单数栋，再去偶数栋；从连廊口向外走。\n\n" +
                               "两端墙上各有近、远两张海报。远端海报被新招募贴纸盖住一层。",
                        residue = false
                    }
                },
                {
                    "s0.bridgeSwitch",
                    new Entry
                    {
                        id = "s0.bridgeSwitch",
                        title = "连廊应急照明",
                        body = "墙上有个独立的『连廊应急照明』开关。现在是白天，还不用打开。",
                        residue = false
                    }
                }
            };

        public static bool TryGet(string id, out Entry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && entries.TryGetValue(id, out entry);
        }

        public static string GetBody(string id) => TryGet(id, out Entry entry) ? entry.body : string.Empty;

        public static IEnumerable<string> Ids
        {
            get { return entries.Keys; }
        }
    }
}
