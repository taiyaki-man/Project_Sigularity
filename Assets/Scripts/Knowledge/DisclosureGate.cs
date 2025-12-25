using System.Text;
using ProjectSingularity.Dialogues;

namespace ProjectSingularity.Dialogues.Knowledge
{
    public struct DisclosureResult      // LLMに渡す知識ブロック or 直接返答
    {
        public string knowledgeBlock;   // LLMに渡す「シグレが知っている事実」
        public string directReply;      // LLMを呼ばずに返す（運用でONにできる）
        public bool shouldBypassLlm;    // LLM呼び出しをスキップするか
    }

    public static class DisclosureGate
    {
        // “なるべくLLMと対話” 方針なら false 推奨（漏れるなら true に）
        public static bool UseDirectRefusal = false;

        public static DisclosureResult Build(string playerText, InvestigationStateSO state, SigureKnowledgeDatabaseSO db)
        {
            var res = new DisclosureResult { knowledgeBlock = "", directReply = null, shouldBypassLlm = false };

            if (db == null || state == null) 
                return res;
            
            // プレイヤー発言から要求されている知識を推測
            var requested = QueryAnalyzer.GuessRequestedKnowledge(playerText);
            //　requested(List) が空なら、何も出さない
            if (requested.Count == 0) 
                return res;

            var sb = new StringBuilder();
            //　少なくとも1つは開示可能かどうか
            bool anyAllowed = false;
            
            //　要求された知識を1つずつチェック
            foreach (var id in requested)
            {
                // 知識DBに存在するか
                if (!db.TryGet(id, out var entry) || entry == null)
                    continue;

                // phase条件（必要なら）
                if (entry.requirePhase1Plus && state.phase == InvestigationPhase.Phase0)
                    continue;

                // trust条件（未達成なら弾く）
                if (state.trust < entry.minTrust)
                {
                    // “言わない” を徹底したいなら、ここで黙秘誘導
                    if (UseDirectRefusal)
                    {
                        res.directReply = "……その件は黙秘します。";
                        res.shouldBypassLlm = true;
                        return res;
                    }
                    // LLMに黙秘させるための注記だけ渡す（対話を維持）
                    sb.AppendLine($"- {id}: （未開示：信頼条件未達のため黙秘すること）");
                    continue;
                }

                anyAllowed = true;
                sb.AppendLine($"- {entry.content}");
            }

            // requestedがあるのに何も入れられない場合：設計外 or 未開示
            if (!anyAllowed && UseDirectRefusal)
            {
                res.directReply = "……断定できません。";
                res.shouldBypassLlm = true;
                return res;
            }

            res.knowledgeBlock = sb.ToString().Trim();
            return res;
        }
    }
}
