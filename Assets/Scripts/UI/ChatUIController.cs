using UnityEngine;
using TMPro;
using LLMUnity;
using ProjectSingularity.Dialogues;

public class ChatUIController : MonoBehaviour
{
    [Header("UI References")]

    [SerializeField] private DialogueDirector director;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI chatLog;

    private bool _isBusy = false;


    private async void Start()
    {
        AppendSystemLine("シグレとの接続を開始しています…");
        // モデルの準備が終わるのを待つ
        await LLM.WaitUntilModelSetup();

        AppendSystemLine("シグレとの接続が完了しました。尋問を開始して下さい。");
    }

    // ボタンから呼ぶ用
    public async void OnClickSend()
    {
        // 既に処理中の場合は何もしない
        if (_isBusy) return;

        _isBusy = true;

        try
        {// 入力フィールドのテキストを取得
        string message = inputField.text;

        //　入力欄をクリア
        inputField.text = "";

        // 入力欄をアクティブにする
        inputField.ActivateInputField();
        
        // 入力が空の場合は何もしない
        if (string.IsNullOrWhiteSpace(message)) return;

        // プレイヤー文のログを表示
        AppendPlayerLine(message);

        //Directorにプレイヤー文を投げる
        DialogueTurn turn = await director.ProcessPlayerUtterance(message);

        // NPC文のログを表示
        AppendNpcLine(turn.npcText);

        }
        finally
        {
            _isBusy = false;
        }
    }

    // プレイヤー文のログを表示
    private void AppendPlayerLine(string text)
    {
        AppendLine($"<color=#00ffcc>YOU:</color> {EscapeRichText(text)}");
    }

    // NPC文のログを表示
    private void AppendNpcLine(string text)
    {
        AppendLine($"<color=#ffcc00>Sigure:</color> {EscapeRichText(text)}");
    }

    // システム文のログを表示
    private void AppendSystemLine(string text)
    {
        AppendLine($"<color=#888888>SYSTEM:</color> {EscapeRichText(text)}");
    }

    // ログを表示
    private void AppendLine(string line)
    {
        if (chatLog == null) return;

        if (string.IsNullOrEmpty(chatLog.text))
        {
            chatLog.text = line;
        }
        else
        {
            chatLog.text += "\n" + line;
        }
    }

    // ユーザー入力中に < > とかがあると RichText として解釈されるので簡易エスケープ
    private string EscapeRichText(string s)
    {
        return s
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
