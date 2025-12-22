using UnityEngine;

namespace InvestigationAI
{
    /// <summary>
    /// 捜査全体の「数値パラメータ」を持つだけのシンプルなクラス。
    /// 後でセーブ/ロードや更新ロジックを足す。
    /// </summary>
    public class InvestigationGameState : MonoBehaviour
    {
        [Header("Relationship & Tension (0〜1)")]
        [Range(0f, 1f)] public float trust = 0.5f;       // NPC→プレイヤー信頼
        [Range(0f, 1f)] public float suspicion = 0.5f;   // NPC→プレイヤー疑念
        [Range(0f, 1f)] public float tension = 0.3f;     // シーンの緊張度

        [Header("Case Progress (0〜1)")]
        [Range(0f, 1f)] public float clueProgress = 0f;  // 事件の解明進行度

        // シンプルな履歴要約のプレースホルダ
        public string GetHistorySummary()
        {
            // TODO: 実際には会話ログから作る
            return "取り調べは序盤。プレイヤーは事件の概要を聞き出そうとしている。";
        }
    }
}
