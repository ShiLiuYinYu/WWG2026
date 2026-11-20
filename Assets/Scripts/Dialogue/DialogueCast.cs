using System;
using System.Collections.Generic;
using UnityEngine;

namespace WWG
{
    /// <summary>Speaker-name to portrait/colour/indent mapping used by the shared presenter.</summary>
    [CreateAssetMenu(menuName = "WWG/Dialogue Cast", fileName = "WwgDialogueCast")]
    public sealed class DialogueCast : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string speaker;
            public Sprite portrait;
            public Color speakerColor = new Color(0.56f, 0.82f, 0.86f, 1f);
            [Tooltip("Portrait layouts default to 290px; set above zero to override.")]
            public float textIndent;
            [Tooltip("立绘竖向微调：正值上移、负值下沉。立绘画布里留白多、人物脚踩不到底时用负值补。")]
            public float portraitOffsetY;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(string speaker)
        {
            if (entries == null || string.IsNullOrEmpty(speaker)) return null;
            foreach (Entry entry in entries)
                if (entry != null && entry.speaker == speaker) return entry;
            return null;
        }
    }
}
