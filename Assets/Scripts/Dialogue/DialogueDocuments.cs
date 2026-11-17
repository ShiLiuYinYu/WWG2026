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
                },
                {
                    "s1.photoInfo",
                    new Entry
                    {
                        id = "s1.photoInfo",
                        title = "去年的活动室照片",
                        body = "这是社团留下的旧照片。照片记录的是活动室里窗帘、桌灯和第五把椅子的位置；可放大查看。",
                        residue = false
                    }
                },
                {
                    "s1.badgeInfo",
                    new Entry
                    {
                        id = "s1.badgeInfo",
                        title = "林未的旧工牌",
                        body = "背面写着：玻璃两边，各存一半。4F。\n\n这是调查线索，工牌会留在物品栏中。",
                        residue = false
                    }
                },
                {
                    "s1.badgeDetail",
                    new Entry
                    {
                        id = "s1.badgeDetail",
                        title = "社团旧资料 · 林未的工牌",
                        body = "48H GameJam 参赛证\n姓名：林未    编号：05\n\n" +
                               "背面手写：玻璃两边，各存一半。4F。\n\n关键物品 · 不可丢弃",
                        residue = false
                    }
                },
                {
                    "s1.glassResidue",
                    new Entry
                    {
                        id = "s1.glassResidue",
                        title = "残影 · 门玻璃",
                        body = "水雾般的旧字一闪而过：\n\n「不要让她独自过连廊……」\n\n" +
                               "这行字只在玻璃反光里出现，门牌和现实墙面没有变化。",
                        residue = false
                    }
                },
                {
                    "s2.drawer",
                    new Entry
                    {
                        id = "s2.drawer",
                        title = "社团自留资料 · 柜内纸张",
                        body = "机房借用便签：林未，周五 23:30，单数栋 3F 机房靠窗第 3 台。\n\n" +
                               "去年的分工表残页：第五格被撕去，只剩编号 05。\n\n两件关键物品已入背包。",
                        residue = false
                    }
                },
                {
                    "s3.sketch",
                    new Entry
                    {
                        id = "s3.sketch",
                        title = "社团自留资料 · 手绘关卡草图与便签",
                        body = "草图上画着 5F 活动室、4F 连廊和 3F 机房。\n\n" +
                               "便签：周六早上来看看，机器还开着，但林未人不在机房，发消息也没回。她昨晚十一点半说下来导个备份，怎么一晚上没见人？我们先回五楼合工程了嗷。",
                        residue = false
                    }
                },
                {
                    "s3.archive",
                    new Entry
                    {
                        id = "s3.archive",
                        title = "社团本地归档提取工具 v1.2",
                        body = "历史暂存包须核对【登记人】【机位编号】【封存时间】三项索引。\n\n选错不会解压。纸面登记记录在背包里；可在工具界面打开背包查看。",
                        residue = false
                    }
                },
                {
                    "s3.memo",
                    new Entry
                    {
                        id = "s3.memo",
                        title = "社团自留资料 · 林未的录音备忘全文",
                        body = "林未（录音）：……喂？能录上吗。现在是周五晚上十一点半，外头走廊刚熄灯。我借口说下三楼导备份，其实是不敢在五楼待了。\n\n" +
                               "刚才熄灯前，我拍了张活动室的参考图。洗出来后，靠窗那把空着的第五张椅子上，明明坐着一个人。我凑近了看才发现，上面坐着的是我自己……可照片是我站在桌边亲手拍的。\n\n" +
                               "我去四楼连廊看过了，现在在三楼机房把工程锁死。如果明天早上我没回五楼，你们谁也别拿那张照片当素材，还有……千万别去坐那把椅子。\n\n" +
                               "（录音末尾：门把手传来一声轻响，录音戛然而止。）",
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
