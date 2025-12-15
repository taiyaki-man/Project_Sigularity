using UnityEngine;
using LLMUnity;

public class SimpleChatTest : MonoBehaviour
{
    [SerializeField] private LLMCharacter llmCharacter;

    private async void Start()
    {
        // ① まず LLM サーバーとモデルの準備完了を待つ
        await LLM.WaitUntilModelSetup();

        // ② 必要ならキャラをウォームアップ（任意）
        // await llmCharacter.Warmup();

        string message = "こんにちは。自己紹介をしてください。";
        string reply = await llmCharacter.Chat(message);

        Debug.Log($"AI: {reply}");
    }
}
