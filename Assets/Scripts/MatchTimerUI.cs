using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a minimal HUD that shows:
///  • Countdown timer
///  • Current match number
///  • Alive counts for killers and survivors
///  • A brief "Match Over" flash when the round ends
/// </summary>
public class MatchTimerUI : MonoBehaviour
{
    private GameManager gm;

    // UI objects
    private Canvas          canvas;
    private TextMeshProUGUI timerLabel;
    private TextMeshProUGUI matchLabel;
    private TextMeshProUGUI statsLabel;
    private TextMeshProUGUI flashLabel;

    private float    flashTimer    = 0f;
    private const float FLASH_DURATION = 2f;
    private int      lastMatchNumber = -1;

    // ── lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        gm = GetComponent<GameManager>();
        if (gm == null) gm = FindFirstObjectByType<GameManager>();

        BuildUI();
    }

    void Update()
    {
        if (gm == null) return;

        // Timer countdown
        float timeLeft = Mathf.Max(0f, gm.matchTime - gm.currentTime);
        timerLabel.text = $"<b>{timeLeft:F1} s</b>";

        // Colour: green → yellow → red as time runs out
        float frac = timeLeft / gm.matchTime;
        timerLabel.color = frac > 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (frac - 0.5f) * 2f)
            : Color.Lerp(Color.red,    Color.yellow, frac * 2f);

        // Match number
        matchLabel.text = $"Match  #{gm.matchNumber + 1}";

        // Alive counts
        int killers = 0, survivors = 0, total = 0;
        if (gm.robots != null)
        {
            foreach (var kvp in gm.robots)
            {
                if (!kvp.Value.isAlive) continue;
                total++;
                if (kvp.Value.agentType == Constants.AGENT_TYPE_KILLER)   killers++;
                else if (kvp.Value.agentType == Constants.AGENT_TYPE_SURVIVOR) survivors++;
            }
        }
        statsLabel.text = $"<color=#FF5555>⬤ {killers} killers</color>   <color=#44DDEE>⬤ {survivors} survivors</color>";

        // Flash banner when a new match starts
        if (gm.matchNumber != lastMatchNumber && gm.matchNumber > 0)
        {
            lastMatchNumber     = gm.matchNumber;
            flashLabel.text     = $"Match {gm.matchNumber} complete!";
            flashLabel.enabled  = true;
            flashTimer          = FLASH_DURATION;
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(flashTimer / FLASH_DURATION);
            flashLabel.color = new Color(1f, 0.9f, 0.2f, alpha);
            if (flashTimer <= 0f) flashLabel.enabled = false;
        }
    }

    // ── UI builder ────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        // Canvas
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("Canvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler cs = cGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cGO.AddComponent<GraphicRaycaster>();
        }

        // Background panel at the top
        GameObject panel = new GameObject("HUDPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pRect = panel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0f, 1f);
        pRect.anchorMax = new Vector2(1f, 1f);
        pRect.pivot     = new Vector2(0.5f, 1f);
        pRect.sizeDelta = new Vector2(0f, 60f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);

        // Timer (top centre)
        timerLabel = CreateLabel("TimerLabel", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-120f, -60f), new Vector2(120f, 0f), 36);
        timerLabel.alignment = TextAlignmentOptions.Center;

        // Match number (top left)
        matchLabel = CreateLabel("MatchLabel", panel.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -60f), new Vector2(250f, 0f), 24);
        matchLabel.alignment = TextAlignmentOptions.TopLeft;
        matchLabel.color     = new Color(0.9f, 0.9f, 0.9f);

        // Stats (top right)
        statsLabel = CreateLabel("StatsLabel", panel.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-400f, -60f), new Vector2(0f, 0f), 22);
        statsLabel.alignment = TextAlignmentOptions.TopRight;

        // Flash banner (centre screen)
        flashLabel = CreateLabel("FlashLabel", canvas.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -30f), new Vector2(300f, 30f), 48);
        flashLabel.alignment = TextAlignmentOptions.Center;
        flashLabel.enabled   = false;
    }

    private TextMeshProUGUI CreateLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Top; // ✅ was TopCenter
        tmp.color     = Color.white;
        return tmp;    
        }
}