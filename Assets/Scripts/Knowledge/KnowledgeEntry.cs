using System;
using UnityEngine;

namespace ProjectSingularity.Dialogues.Knowledge
{
    [Serializable]
    public class KnowledgeEntry
    {
        public KnowledgeId id;

        [TextArea(2, 6)]
        public string content;     // LLMに渡す「事実テキスト」

        [Range(0, 100)]
        public int minTrust = 0;   // このtrust以上で開示

        public bool requirePhase1Plus = false; // 例：Phase1以降で開示など（必要なら）
    }
}
