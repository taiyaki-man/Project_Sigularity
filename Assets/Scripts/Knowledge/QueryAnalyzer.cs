using System.Collections.Generic;

namespace ProjectSingularity.Dialogues.Knowledge
{
    public static class QueryAnalyzer
    {
        public static List<KnowledgeId> GuessRequestedKnowledge(string playerText)
        {
            // 空チェックと小文字化
            var t = (playerText ?? "").ToLowerInvariant();
            var result = new List<KnowledgeId>();

            // 例：主人の名前
            if (t.Contains("主人") && (t.Contains("名前") || t.Contains("なまえ")))
                result.Add(KnowledgeId.MasterName);

            if (t.Contains("主人") && (t.Contains("どんな") || t.Contains("人物") || t.Contains("プロフィール")))
                result.Add(KnowledgeId.MasterProfile);

            if (t.Contains("モヨリ") || t.Contains("モヨリソフト"))
                result.Add(KnowledgeId.CompanyMoyoriSoft);

            if (t.Contains("barth") || t.Contains("バース"))
                result.Add(KnowledgeId.BarthCorp);

            if (t.Contains("tomoni") || t.Contains("トモニ"))
                result.Add(KnowledgeId.TomoniApp);

            if (t.Contains("ここ") || t.Contains("取り調べ") || t.Contains("取調室"))
                result.Add(KnowledgeId.SceneInterrogationRoom);
            
            if (t.Contains("被害者") || t.Contains("殺された人") || t.Contains("犠牲者"))
                result.Add(KnowledgeId.Victim);

            return result;
        }
    }
}
