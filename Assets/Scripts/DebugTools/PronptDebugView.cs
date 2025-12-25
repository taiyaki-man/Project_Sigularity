using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectSingularity.Dialogues;

public class PromptDebugView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogueDirector director;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private RectTransform contentRect;   // Viewport/Content
    [SerializeField] private ScrollRect scrollRect;        // Scroll View の ScrollRect（任意だけど推奨）
    [SerializeField] private TMP_Text metaText;

    [Header("Behavior")]
    [SerializeField] private bool visibleOnStart = false;
    [SerializeField] private GameObject panelRoot; // DebugPanel か Scroll View の親
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;

    [Header("Layout")]
    [SerializeField] private float extraPaddingY = 24f;    // 少し余白
    [SerializeField] private bool autoScrollToBottom = false;

    private string lastShownPrompt = null;

    void Start()
    {
        SetVisible(visibleOnStart);
        RefreshIfChanged(force: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!isVisible);

        if (!isVisible) return;

        RefreshIfChanged(force:false);
    }

    private bool isVisible;

    private void SetVisible(bool v)
    {
        isVisible = v;
        if (panelRoot != null) panelRoot.SetActive(v);
        else
        {
            // panelRoot未設定なら、このコンポーネント直下のScrollView等を探して切る等も可
            // ただしまずはInspectorで設定がおすすめ
        }
    }


    private void RefreshIfChanged(bool force)
    {
        if (director == null || promptText == null || contentRect == null) return;

        var p = director.LastPrompt ?? "";

        if (!force && p == lastShownPrompt) return; // ★変化がないなら再レイアウトしない
        lastShownPrompt = p;

        promptText.text = string.IsNullOrEmpty(p) ? "(no prompt yet)" : p;

        // TMPのPreferredHeightを確定
        promptText.ForceMeshUpdate();

        // ★ Text自体の高さを preferredHeight に合わせる（これが効く）
        float textH = promptText.preferredHeight + extraPaddingY;
        promptText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textH);

        // ★ Content も同じ高さにする
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textH);

        // レイアウト更新
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(promptText.rectTransform);

        if (autoScrollToBottom && scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;

        if (metaText != null)
        {
            metaText.text =
                $"DumpPath: {director.LastDumpPath}\n" +
                $"PromptChars: {(p?.Length ?? 0)}\n" +
                $"ContentH: {textH:0}";
        }

        if (scrollRect != null && scrollRect.viewport != null)
        {
            Debug.Log($"[PromptDebug] ViewportH={scrollRect.viewport.rect.height:F1}  ContentH={contentRect.rect.height:F1}  TextPrefH={promptText.preferredHeight:F1}");
        }

    }

    public void CopyPromptToClipboard()
    {
        if (director == null) return;
        GUIUtility.systemCopyBuffer = director.LastPrompt ?? "";
    }

}
