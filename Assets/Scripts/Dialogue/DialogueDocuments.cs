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
                }
            };

        public static bool TryGet(string id, out Entry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && entries.TryGetValue(id, out entry);
        }

        public static IEnumerable<string> Ids
        {
            get { return entries.Keys; }
        }
    }
}
