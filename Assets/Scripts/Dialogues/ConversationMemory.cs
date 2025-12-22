using System.Collections.Generic;
using System.Text;

namespace ProjectSingularity.Dialogues
{
    /// <summary>
    /// 会話ログ管理（ステップ1では「直近Nターンをプロンプトに入れる」だけ）
    /// 要約や重要発言抽出は後で追加。
    /// </summary>
    public class ConversationMemory
    {
        private readonly List<DialogueTurn> _turns = new();
        public IReadOnlyList<DialogueTurn> Turns => _turns;

        public int maxTurnsForContext = 6;

        public void AddTurn(DialogueTurn turn) => _turns.Add(turn);

        public void Clear()
        {
            _turns.Clear();
        }

        public string BuildRecentTranscript()
        {
            int start = _turns.Count - maxTurnsForContext;
            if (start < 0) start = 0;

            var sb = new StringBuilder();
            for (int i = start; i < _turns.Count; i++)
            {
                sb.AppendLine($"プレイヤー: {_turns[i].playerText}");
                sb.AppendLine($"シグレ: {_turns[i].npcText}");
            }
            return sb.ToString().Trim();
        }
    }
}
