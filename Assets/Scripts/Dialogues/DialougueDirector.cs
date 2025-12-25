using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using LLMUnity; // LLMUnityの名前空間
using ProjectSingularity.Dialogues.Knowledge;

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
        [SerializeField] private InvestigationStateSO investigationState; 

        [Header("Knowledge DataBase")]
        [SerializeField] private SigureKnowledgeDatabaseSO knowledgeDatabase;

        [Header("Debug Pronpt")]
        [SerializeField] private bool dumpPronptToFile = false;
        [SerializeField] private bool dumpResponsesToFile = true;
        [SerializeField] private bool includeTranscriptInDump = true;

        public string LastPrompt { get; private set; }
        public string LastResponseRaw { get; private set; }
        public string LastDumpPath { get; private set; }

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
            // if (IsNameQuestion(playerText))
            // {
            //     var turn0 = new DialogueTurn(playerText, "……シグレです。");
            //     memory.AddTurn(turn0);
            //     return turn0;
            // }

            string transcript = memory.BuildRecentTranscript();

            //質問→開示情報の決定
            var disclosure = DisclosureGate.Build(playerText, investigationState, knowledgeDatabase);
            // 直接返答モードなら、LLM呼び出しをスキップ
            if (disclosure.shouldBypassLlm)
            {
                var turnBypass = new DialogueTurn(playerText, disclosure.directReply);
                memory.AddTurn(turnBypass);
                return turnBypass;
            }
            
            // プロンプトの組み立て
            string prompt = promptBuilder.BuildPrompt(playerText, transcript, investigationState, disclosure.knowledgeBlock);
            LastPrompt = prompt;

            // デバッグ用：プロンプトをファイルに保存
            string turnDumpPath = null;
            if (dumpPronptToFile || dumpResponsesToFile)
            {
                turnDumpPath = CreateTurnDumpFile(playerText);
                LastDumpPath = turnDumpPath;
                WriteTurnDumpHeader(turnDumpPath, playerText, transcript, prompt);
            }


            // LLM呼び出し
            string rawResponse = null;
            string npcText;
            try
            {
                // LLMUnityの呼び出し（プロジェクトの使い方によりメソッド名が違う場合あり）
                rawResponse = await llmCharacter.Complete(prompt);


                // ① echo（プロンプト全文）が返ってきた場合は除去
                var processed = StripEcho(rawResponse, prompt);

                // ② 仕上げ（ロールや空白を整える）
                processed = PostProcess(processed);

                npcText = processed;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueDirector] LLM call failed: {e}");
                rawResponse = null;
                npcText = "……通信ではなく内部処理の問題のようですね。少し、困りました。";
            }

            // ③ 最終レスポンスの保存
            LastResponseRaw = rawResponse;

            // デバッグ用：レスポンスの生データをファイルに保存
            if (!string.IsNullOrEmpty(turnDumpPath) && (dumpPronptToFile || dumpResponsesToFile))
            {
                AppendTurnDumpResponse(turnDumpPath, rawResponse, npcText);
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

            // 「\n会話例」「会話例」「\n重要」や「重要」が出たらそこで打ち切り
            i = text.IndexOf("\n会話例");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("会話例");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("\n重要");
            if (i >= 0) text = text.Substring(0, i).Trim();
            i = text.IndexOf("重要");
            if (i >= 0) text = text.Substring(0, i).Trim();

            // 「（」が出たらそこで打ち切り
            int p = text.IndexOf('（');
            if (p >= 0) text = text.Substring(0, p).Trim();

            return text;
        }

        private string CreateTurnDumpFile(string playerText)
        {
            string dir = Path.Combine(Application.persistentDataPath, "PromptDumps");
            Directory.CreateDirectory(dir);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string safeHead = SanitizeFileName(Shorten(playerText, 18));
            return Path.Combine(dir, $"{stamp}_{safeHead}_turn.txt");
        }

        private void WriteTurnDumpHeader(string path, string playerText, string transcript, string prompt)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Timestamp ===");
                sb.AppendLine(DateTime.Now.ToString("O"));
                sb.AppendLine();

                sb.AppendLine("=== Player ===");
                sb.AppendLine(playerText ?? "");
                sb.AppendLine();

                if (includeTranscriptInDump)
                {
                    sb.AppendLine("=== Transcript (recent) ===");
                    sb.AppendLine(transcript ?? "");
                    sb.AppendLine();
                }

                sb.AppendLine("=== Prompt ===");
                sb.AppendLine(prompt ?? "");
                sb.AppendLine();

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[TurnDump] Created: {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TurnDump] Create failed: {e.Message}");
            }
        }

        private void AppendTurnDumpResponse(string path, string rawResponse, string processedResponse)
        {
            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("=== Response (raw) ===");
                sb.AppendLine(rawResponse ?? "");
                sb.AppendLine();

                sb.AppendLine("=== Response (post-processed) ===");
                sb.AppendLine(processedResponse ?? "");
                sb.AppendLine();

                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[TurnDump] Appended response: {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TurnDump] Append failed: {e.Message}");
            }
        }


        private static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "empty";
            s = s.Replace("\n", " ").Replace("\r", " ").Trim();
            return s.Length <= max ? s : s.Substring(0, max);
        }

        private static string SanitizeFileName(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

    }
}
