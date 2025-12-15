using UnityEngine;
using TMPro;
using LLMUnity;

public class ChatUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI chatLog;

    [Header("LLM Character")]
    [SerializeField] private LLMCharacter npc;  // NPC_Aoi をセット

    private bool isBusy = false;

    private async void Start()
    {
        // モデルの準備が終わるのを待つ
        await LLM.WaitUntilModelSetup();

        AppendSystemLine("時雨との接続が完了しました。何でも聞いてみてください。");
    }

    // ボタンから呼ぶ用
    public async void OnClickSend()
    {
        if (isBusy) return;

        string message = inputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        isBusy = true;
        inputField.text = "";

        AppendPlayerLine(message);

        // シンプル版：まだ台本JSONは使わず、そのまま渡す
        string reply = await npc.Chat(message);

        AppendNpcLine(reply);
        isBusy = false;
    }

    private void AppendPlayerLine(string text)
    {
        AppendLine($"<color=#00ffcc>YOU:</color> {EscapeRichText(text)}");
    }

    private void AppendNpcLine(string text)
    {
        AppendLine($"<color=#ffcc00>AOI:</color> {EscapeRichText(text)}");
    }

    private void AppendSystemLine(string text)
    {
        AppendLine($"<color=#888888>SYSTEM:</color> {EscapeRichText(text)}");
    }

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
