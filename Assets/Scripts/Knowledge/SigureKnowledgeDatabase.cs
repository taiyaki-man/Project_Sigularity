using System.Collections.Generic;
using UnityEngine;

namespace ProjectSingularity.Dialogues.Knowledge
{
    [CreateAssetMenu(
        menuName = "ProjectSingularity/Dialogue/Sigure Knowledge Database",
        fileName = "SigureKnowledgeDatabase")]
    public class SigureKnowledgeDatabaseSO : ScriptableObject
    {
        public List<KnowledgeEntry> entries = new();

        public bool TryGet(KnowledgeId id, out KnowledgeEntry entry)
        {
            foreach (var e in entries)
            {
                if (e.id == id)
                {
                    entry = e;
                    return true;
                }
            }
            entry = null;
            return false;
        }
    }
}
