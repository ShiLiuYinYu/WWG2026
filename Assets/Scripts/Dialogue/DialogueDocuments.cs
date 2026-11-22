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
                               "队友便签：周六早上来的时候机器还开着，林未也没回消息。她昨晚十一点半说来导备份。要是看到这张纸，先回五楼找我们。",
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
                        body = "林未（录音）：……能录上吗。走廊刚熄灯。我跟他们说下来备份，因为工程里我的署名突然空了。刷新了两次，还是空的。\n\n" +
                               "下午打印的参考图，刚才再看，靠窗那把空椅子上坐着我。可拍照的时候，我明明站在桌边。\n\n" +
                               "工牌我留在椅垫下了，上面有通往借用单的线索。要是我没回去，这儿还有我自己的备份。你们别拿照片做素材，也别坐那把椅子。\n\n" +
                               "（录音末尾：门把手传来一声轻响，录音戛然而止。）",
                        residue = false
                    }
                },
                {
                    "p4.frames",
                    new Entry
                    {
                        id = "p4.frames",
                        title = "四人的目击记录 · 三张帧卡",
                        body = "社团自己的记录，不是监控。三张帧卡是我们各自看到的那一刻：\n\n" +
                               "第一张：熄灯后，椅子的位置。四把椅子，桌面上空着。\n" +
                               "第二张：许圆画的纸团落在插线板左侧，桌角还压着名单抄件。\n" +
                               "第三张：名单抄件特写。第五格只有编号 05，姓名一栏空着。\n\n" +
                               "对照房间草图：插线板在桌子左侧，纸团在它旁边。",
                        residue = false
                    }
                },
                {
                    "p4.log",
                    new Entry
                    {
                        id = "p4.log",
                        title = "离线预览日志 · 状态快照",
                        body = "离线预览不联网，只记录本机工程的状态变化。\n\n" +
                               "快照：椅子 4 把 / 已登记状态 4。\n" +
                               "日志 A：椅子 5 把 / 已登记状态 4。\n" +
                               "日志 B：椅子 5 把 / 已登记状态 5。\n\n" +
                               "两条状态日志的先后，需要回放这一段才能确认。",
                        residue = false
                    }
                },
                {
                    "p4.badge",
                    new Entry
                    {
                        id = "p4.badge",
                        title = "纪念盒 · 工牌记录",
                        body = "纪念盒里是协会留下的旧工牌。\n\n" +
                               "编号 05：林未。原工程署名。\n" +
                               "编号 06 起：本队四人。\n\n" +
                               "工牌记录里 05 仍归在原格，没有划掉，也没有新名字补上。",
                        residue = false
                    }
                },
                {
                    "p4.rule",
                    new Entry
                    {
                        id = "p4.rule",
                        title = "旧测试说明 · 未核实的归队规则",
                        body = "「公共灯熄灭后，按归队表验一次。」\n\n" +
                               "这是林未以前测试时写的，只有这一句。归队表在哪、验哪一栏，她没写完。\n\n" +
                               "要等第二次熄灯后，去她去过的地方核对。",
                        residue = false
                    }
                },
                {
                    "p5.guide",
                    new Entry
                    {
                        id = "p5.guide",
                        title = "导览展板 · 露出的旧展位号",
                        body = "应急灯亮起后，导览展板边缘露出被新贴纸盖住的旧展位号：\n\n" +
                               "4F-02 「旧工程资料柜」\n\n" +
                               "柜门内侧压着一折页，是当年留给归队核对的表。",
                        residue = false
                    }
                },
                {
                    "p5.sheet",
                    new Entry
                    {
                        id = "p5.sheet",
                        title = "柜门内侧折页 · 归队确认栏",
                        body = "折页上是去年的归队确认表，四列：编号 / 姓名 / 到场 / 备注。\n\n" +
                               "05 一行的备注栏写着：\n「原作者留在原格。现队到齐后各记各的，空格可保留为待确认。」\n\n" +
                               "其余几列空白。纸边有铅笔痕，是许圆以前抄过的笔迹。",
                        residue = false
                    }
                },
                {
                    "p5.glass",
                    new Entry
                    {
                        id = "p5.glass",
                        title = "残影 · 连廊中段玻璃",
                        body = "玻璃倒影里浮出两行旧字：\n\n「第二次门响，选归队页。」\n\n" +
                               "倒影里的走廊是去年的样子，连廊尽头还有一盏没熄的灯。现实中没有那盏灯。",
                        residue = true
                    }
                },
                {
                    "p5.backup",
                    new Entry
                    {
                        id = "p5.backup",
                        title = "只读测试时间轴 · 三张音效事件卡",
                        body = "靠窗第 3 台的只读测试时间轴，有三张事件卡：\n\n" +
                               "① 门响 / 离队：门把手响，编号 05 记录离场。\n" +
                               "② 门响 / 归队：门把手响，编号 05 记录回到原格。\n" +
                               "③ 椅脚声 / 结算：椅脚轻响，名单结算。\n\n" +
                               "只看这三张卡还不能确定；归队页在第二张之后。",
                        residue = false
                    }
                },
                {
                    "p5.rulePaper",
                    new Entry
                    {
                        id = "p5.rulePaper",
                        title = "备忘录 · 纸质归队规则",
                        body = "许圆抄下的归队规则：\n\n" +
                               "「原作者留在原格。现队到齐后各记各的，空格可保留为待确认。」\n\n" +
                               "归队确认时，05 单列待确认，本队四人各记各的。",
                        residue = false
                    }
                },
                {
                    "p5.ruleBackup",
                    new Entry
                    {
                        id = "p5.ruleBackup",
                        title = "备忘录 · 备份中的归队规则",
                        body = "从只读测试时间轴里读到的归队页：\n\n" +
                               "归队人数：4（现队）+ 1（05，待确认）。\n" +
                               "05 不占现队名额，保留原名，状态记为待确认。\n\n" +
                               "归队确认时按这一页核对。",
                        residue = false
                    }
                },
                {
                    "p6.roster",
                    new Entry
                    {
                        id = "p6.roster",
                        title = "名单副本 · 第五格",
                        body = "名单副本第一页：\n\n编号 01 袁祁 / 02 许圆 / 03 唐澈 / 04 你\n" +
                               "编号 05 —— 姓名：林未（被覆盖为空白）\n\n" +
                               "第四格后面有被擦掉的笔迹，纸上还留着凹痕。",
                        residue = false
                    }
                },
                {
                    "p6.confirm",
                    new Entry
                    {
                        id = "p6.confirm",
                        title = "归队确认页",
                        body = "归队确认页（可写入）：\n\n" +
                               "01 袁祁 —— 已归队\n02 许圆 —— 已归队\n03 唐澈 —— 已归队\n04 你 —— 已归队\n" +
                               "05 林未 —— 原署名 · 待确认\n\n" +
                               "修改会先写进本机备份，原始文件仍在旧工程 U 盘里。",
                        residue = false
                    }
                },
                {
                    "p6.pause",
                    new Entry
                    {
                        id = "p6.pause",
                        title = "暂停帧 · 05 与主角两行",
                        body = "把暂停帧停在这一刻：\n\n" +
                               "05 那一行的状态是「待确认」，姓名尚未写回。\n" +
                               "你这一行的状态是「已归队」。\n\n" +
                               "两行分开记，谁也不占谁的位置。",
                        residue = false
                    }
                },
                {
                    "s8.letter",
                    new Entry
                    {
                        id = "s8.letter",
                        title = "署名更正页",
                        body = "旧工程署名更正\n\n" +
                               "原署名：05（未登记姓名）\n更正为：林未\n来源：本机备份与纪念盒工牌记录一致\n\n" +
                               "附在本队自愿展示的说明页之后，单独成页。",
                        residue = false
                    }
                },
                {
                    "s8.archive",
                    new Entry
                    {
                        id = "s8.archive",
                        title = "协会内部存档",
                        body = "协会内部存档不含对外展示材料。\n\n" +
                               "留存副本与纸面记录归入同一档案，索引为：\n05 林未 · 原工程署名 · 待确认。\n\n" +
                               "归档后不再改动原始工程文件。",
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
