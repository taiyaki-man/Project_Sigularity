using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using LLMUnity; // LLMUnityの名前空間

namespace ProjectSingularity.Dialogues
{
    /// <summary>
    /// UIからの入力を受け取り、Promptを作り、LLMに投げ、結果を返す司令塔。
    /// ステップ1はここまで。後で Evidence/Phase/ScriptPayload を追加する。
    /// </summary>
    public class DialogueDirector : MonoBehaviour
    {
        [Header("LLMUnity")]
        [SerializeField] private LLMCharacter llmCharacter; // シーン上のLLMCharacter(シグレ)を割り当て

        [Header("Context")]
        [SerializeField] private int maxTurnsForContext = 6;

        [Header("Investigation State")]
        [SerializeField] private InvestigationState investigationState; 

        private ConversationMemory memory;
        private PromptBuilder promptBuilder;

        private void Awake()
        {
            memory = new ConversationMemory { maxTurnsForContext = maxTurnsForContext };
            promptBuilder = new PromptBuilder();

            if (llmCharacter == null)
                Debug.LogError("[DialogueDirector] LLMCharacter is not assigned.");
        }

        private void Start()
        {
            ResetConversation();

            if (llmCharacter == null)
                return;

            // 以前の会話が混ざらないように（後述）
            llmCharacter.ClearChat();

            // stop を初期化（重複防止）
            llmCharacter.stop = llmCharacter.stop.Distinct().ToList();

            // 「次の話者」や「見出し」を出したら停止
            llmCharacter.stop.Add("\n刑事:");
            llmCharacter.stop.Add("刑事:");
            llmCharacter.stop.Add("\n【");     // 「【前回の会話】」等を止める
            llmCharacter.stop.Add("【");
            llmCharacter.stop.Add("（");  
            llmCharacter.stop.Add("\n（");   
        }

        public async Task<DialogueTurn> ProcessPlayerUtterance(string playerText)
        {
            if (llmCharacter == null)
                return new DialogueTurn(playerText, "（エラー：LLMCharacter が未設定です）");

            playerText = (playerText ?? string.Empty).Trim();
            if (playerText.Length == 0)
                return new DialogueTurn(playerText, "……何か仰ってくださいませんか。");

            // 名前を尋ねる質問だけは固定応答とする（小型モデルの安定性向上のため）
            if (IsNameQuestion(playerText))
            {
                var turn0 = new DialogueTurn(playerText, "シグレです。確認は以上でしょうか。");
                memory.AddTurn(turn0);
                return turn0;
            }

            string transcript = memory.BuildRecentTranscript();
            string prompt = promptBuilder.BuildPrompt(playerText, transcript, investigationState);

            string npcText;
            try
            {
                // LLMUnityの呼び出し（プロジェクトの使い方によりメソッド名が違う場合あり）
                // 多くのケースで llmCharacter.Chat(prompt) / llmCharacter.Complete(prompt) のような関数になります。
                npcText = await llmCharacter.Complete(prompt);


                // ① echo（プロンプト全文）が返ってきた場合は除去
                npcText = StripEcho(npcText, prompt);

                // ② 仕上げ（ロールや空白を整える）
                npcText = PostProcess(npcText);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueDirector] LLM call failed: {e}");
                npcText = "……通信ではなく内部処理の問題のようですね。少し、困りました。";
            }

            var turn = new DialogueTurn(playerText, npcText);
            memory.AddTurn(turn);
            return turn;
        }

        public void ResetConversation()
        {
            if (llmCharacter != null)
            // LLMUnity側の履歴を消す
            llmCharacter.ClearChat();

            // 自作の履歴も消す
            memory.Clear();
        }

        private static string StripEcho(string output, string prompt)
        {
            if (string.IsNullOrEmpty(output)) return output;

            // まず完全一致で先頭がpromptなら切り落とす（最優先）
            if (!string.IsNullOrEmpty(prompt) && output.StartsWith(prompt))
                output = output.Substring(prompt.Length);

            // それでも会話履歴ごと返ってきている場合、最後の「シグレ:」以降だけ採用
            int last = output.LastIndexOf("シグレ:");
            if (last >= 0)
                output = output.Substring(last + "シグレ:".Length);

            return output.Trim();
        }

        private static bool IsNameQuestion(string s)
        {
            s = (s ?? string.Empty).ToLowerInvariant();
            return s.Contains("名前") || s.Contains("なまえ")
                   || s.Contains("name") || s.Contains("ネーム");
        }


        private string PostProcess(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "……。";

            // 余計な前後空白を削る、改行を詰める（最小）
            text = text.Trim();

            // 「シグレ:」などを返してきた場合に除去
            if (text.StartsWith("シグレ:"))
                text = text.Substring("シグレ:".Length).Trim();


            //　「\n刑事:」や「刑事:」「\n刑事：」「刑事：」が出たらそこで打ち切り
            int i = text.IndexOf("\n刑事:");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("刑事:");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("\n刑事：");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("刑事：");
            if (i >= 0) text = text.Substring(0, i).Trim();

            // 「\nシグレ:」や「シグレ:」「\nシグレ：」「シグレ：」が出たらそこで打ち切り
            i = text.IndexOf("\nシグレ:");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("シグレ:");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("\nシグレ：");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("シグレ：");
            if (i >= 0) text = text.Substring(0, i).Trim();

            // 「（」が出たらそこで打ち切り
            int p = text.IndexOf('（');
            if (p >= 0) text = text.Substring(0, p).Trim();

            return text;
        }
    }
}
