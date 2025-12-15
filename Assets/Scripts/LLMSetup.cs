using UnityEngine;
using LLMUnity;

public class SimpleLLMSetup : MonoBehaviour
{
    private LLM llm;
    private LLMCharacter character;

    async void Start()
    {
        // Awake が即走って落ちるのを避けるため一旦無効化
        gameObject.SetActive(false);

        // LLM コンポーネントを追加
        llm = gameObject.AddComponent<LLM>();

        // ★ LLM Model Manager に登録されている「ファイル名」を指定
        // 例: "llama-3.2-1b-instruct-q4_k_m.gguf"
        llm.SetModel("llama-3.2-1b-instruct-q4_k_m.gguf");

        // チャットテンプレートがうまく自動判定されない場合は明示的に
        // llm.SetTemplate("llama-3.2-instruct"); // テンプレ名は実際のInspector表示に合わせて下さい

        // CPU のみで動かす
        llm.numGPULayers = 0;

        // スレッド数（とりあえず自動設定）
        llm.numThreads = -1;

        // LLMCharacter 追加
        character = gameObject.AddComponent<LLMCharacter>();
        character.llm = llm;
        character.SetPrompt("You are a helpful AI assistant.");
        character.AIName = "AI";
        character.playerName = "Player";

        // GameObject を再度有効化して LLM の Awake を動かす
        gameObject.SetActive(true);

        // モデル初期化完了を待機
        await LLM.WaitUntilModelSetup();

        // テストで1回だけメッセージ送る
        string reply = await character.Chat("テストです。自己紹介してください。");
        Debug.Log("[LLM reply] " + reply);
    }
}
