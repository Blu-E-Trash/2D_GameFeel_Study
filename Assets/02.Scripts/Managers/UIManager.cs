using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Controls")]
    public Toggle toggleVisual;
    public Toggle toggleAudio;
    public Toggle toggleHitStop;
    public Toggle toggleCamShake;
    public Toggle toggleReaction;
    public Toggle togglePlayerReaction;
    public Button buttonAttack;
    public Button ExitButton;

    [Header("System Message")]
    public Text systemMessageText;
    public GameObject systemMessagePanel;

    [Header("Test - Target")]
    public PlayerController player;

    [Header("Test - Parry Window")]
    public Slider sliderParryWindow;
    public Text labelParryWindow;
    public Button presetFiora;       // 리그 오브 레전드 피오라 「응수」 0.75초
    public Button presetAdversary;   // 메이플스토리 최초의 대적자 0.50초
    public Button presetSekiro;      // 세키로 약 0.2초 (비공식)
    public Button presetSolateria;   // 솔라테리아 약 0.16~0.2초 (비공식) → 중간값 0.18초

    [Header("Test - Hit Stop")]
    public Slider sliderHitStop;
    public Text labelHitStop;

    [Header("Test - Attack Pattern")]
    public Toggle toggleRandomTelegraph;
    public Toggle toggleAutoAttack;
    [Tooltip("자동 공격 간격(초) 최소/최대. 공격 1회(전조 + 공격 + 후딜) 보다 길어야 한다.")]
    public float autoAttackIntervalMin = 2.0f;
    public float autoAttackIntervalMax = 3.0f;

    [Header("Test - Stats")]
    public Text statsText;
    public Button buttonResetStats;

    public event Action OnAttackTriggered;

    public bool UseVisual => toggleVisual.isOn;
    public bool UseAudio => toggleAudio.isOn;
    public bool UseHitStop => toggleHitStop.isOn;
    public bool UseCamShake => toggleCamShake.isOn;
    public bool UseReaction => toggleReaction.isOn;
    public bool UsePlayerReaction => togglePlayerReaction.isOn;
    public bool UseRandomTelegraph => toggleRandomTelegraph != null && toggleRandomTelegraph.isOn;
    public bool UseAutoAttack => toggleAutoAttack != null && toggleAutoAttack.isOn;

    // --- 테스트 기록 ---
    private const int RecentCount = 10;
    private const float LateInputWindow = 0.5f;   // 실패한 타격 뒤 이 시간 안에 누르면 '늦음'으로 기록
    private const float EarlyInputWindow = 1.0f;  // 타격 전 이 시간 안에 누른 입력만 '이름'으로 기록

    private int attempts;
    private int successes;
    private int streak;
    private int bestStreak;
    private readonly Queue<bool> recent = new Queue<bool>();
    private string lastResultText = "-";

    private float lastPressTime = -999f;
    private float lastRejectedPressTime = -999f;
    private float lastFailedHitTime = -999f;
    private float autoAttackTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        buttonAttack.onClick.AddListener(() => OnAttackTriggered?.Invoke());

        ExitButton.onClick.AddListener(() => Application.Quit());

        if (systemMessagePanel != null) systemMessagePanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        // 슬라이더 초기값 = 현재 스크립트 값 (범위는 인스펙터의 Min/Max Value)
        if (sliderParryWindow != null && player != null) sliderParryWindow.value = player.parryWindow;
        if (sliderHitStop != null && FeedbackManager.Instance != null) sliderHitStop.value = FeedbackManager.Instance.hitStopDuration;

        if (sliderParryWindow != null) sliderParryWindow.onValueChanged.AddListener(delegate { OnTuningChanged(); });
        if (sliderHitStop != null) sliderHitStop.onValueChanged.AddListener(delegate { OnTuningChanged(); });

        if (presetFiora != null) presetFiora.onClick.AddListener(() => SetParryWindow(0.75f));
        if (presetAdversary != null) presetAdversary.onClick.AddListener(() => SetParryWindow(0.50f));
        if (presetSekiro != null) presetSekiro.onClick.AddListener(() => SetParryWindow(0.20f));
        if (presetSolateria != null) presetSolateria.onClick.AddListener(() => SetParryWindow(0.18f));

        if (buttonResetStats != null) buttonResetStats.onClick.AddListener(ResetStats);

        ResetAutoAttackTimer();
        OnTuningChanged();
        RefreshStatsText();
    }

    private void Update()
    {
        // [A] 키로 공격 시작 (마우스로 버튼을 누르지 않고 반복 테스트)
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            OnAttackTriggered?.Invoke();
        }

        // 자동 공격: 일정하지 않은 간격으로 반복 (적이 공격 중이면 무시됨)
        if (UseAutoAttack)
        {
            autoAttackTimer -= Time.deltaTime;
            if (autoAttackTimer <= 0f)
            {
                OnAttackTriggered?.Invoke();
                ResetAutoAttackTimer();
            }
        }
    }

    private void ResetAutoAttackTimer()
    {
        autoAttackTimer = UnityEngine.Random.Range(autoAttackIntervalMin, autoAttackIntervalMax);
    }

    // --- 슬라이더 / 프리셋 ---
    private void SetParryWindow(float seconds)
    {
        if (sliderParryWindow != null) sliderParryWindow.value = seconds; // onValueChanged → OnTuningChanged
        else if (player != null) player.parryWindow = seconds;
        OnTuningChanged();
    }

    private void OnTuningChanged()
    {
        if (player != null && sliderParryWindow != null) player.parryWindow = sliderParryWindow.value;
        if (FeedbackManager.Instance != null && sliderHitStop != null) FeedbackManager.Instance.hitStopDuration = sliderHitStop.value;

        if (labelParryWindow != null && player != null) labelParryWindow.text = $"Parry Window {player.parryWindow:F2}s";
        if (labelHitStop != null && FeedbackManager.Instance != null) labelHitStop.text = $"Hit Stop {FeedbackManager.Instance.hitStopDuration:F2}s";
    }

    // --- 테스트 기록 ---
    // 플레이어가 패링을 '실제로' 시작했을 때 (경직 중 입력은 제외)
    public void RecordParryPressed()
    {
        float now = Time.time;
        lastPressTime = now;

        // 직전 타격이 실패였고 그 직후에 누른 것이라면 '늦은 입력'으로 기록
        float sinceFailedHit = now - lastFailedHitTime;
        if (sinceFailedHit >= 0f && sinceFailedHit <= LateInputWindow)
        {
            lastResultText = $"Fail, late {sinceFailedHit * 1000f:F0}ms";
            lastFailedHitTime = -999f;
            RefreshStatsText();
        }
    }

    // 패링 후 경직(연타 방지) 중에 눌러서 무시된 입력
    public void RecordParryRejected()
    {
        lastRejectedPressTime = Time.time;
    }

    // 적의 타격 순간에 호출
    public void RecordParryResult(bool success)
    {
        float now = Time.time;
        float sincePress = now - lastPressTime;
        float window = player != null ? player.parryWindow : 0f;

        attempts++;
        recent.Enqueue(success);
        while (recent.Count > RecentCount) recent.Dequeue();

        if (success)
        {
            successes++;
            streak++;
            bestStreak = Mathf.Max(bestStreak, streak);
            lastResultText = $"Success (+{sincePress * 1000f:F0}ms / {window * 1000f:F0}ms)";
            lastFailedHitTime = -999f;
        }
        else
        {
            streak = 0;
            float sinceRejected = now - lastRejectedPressTime;
            if (sinceRejected >= 0f && sinceRejected <= window + 0.1f && sinceRejected < sincePress)
            {
                lastResultText = "Fail, pressed in recovery";
                lastFailedHitTime = -999f;
            }
            else if (sincePress >= 0f && sincePress <= EarlyInputWindow)
            {
                lastResultText = $"Fail, early {(sincePress - window) * 1000f:F0}ms";
                lastFailedHitTime = -999f;
            }
            else
            {
                lastResultText = "Fail, no input";
                lastFailedHitTime = now; // 곧바로 누르면 '늦음'으로 갱신
            }
        }

        RefreshStatsText();
    }

    private void ResetStats()
    {
        attempts = 0;
        successes = 0;
        streak = 0;
        bestStreak = 0;
        recent.Clear();
        lastResultText = "-";
        lastFailedHitTime = -999f;
        RefreshStatsText();
    }

    private void RefreshStatsText()
    {
        if (statsText == null) return;

        float rate = attempts > 0 ? (float)successes / attempts * 100f : 0f;

        StringBuilder recentStr = new StringBuilder();
        foreach (bool r in recent) recentStr.Append(r ? "O " : "X ");

        statsText.text = $"Attempts {attempts}   Success {successes} ({rate:F0}%)\n" +
                         $"Streak {streak}   Best {bestStreak}\n" +
                         $"Recent {recentStr}\n" +
                         $"Last: {lastResultText}";
    }

    // --- 기존 시스템 메시지 ---
    public void ShowSystemMessage(string message, Color color)
    {
        if (systemMessageText == null) return;

        systemMessageText.text = message;
        systemMessageText.color = color;
        systemMessagePanel.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(HideMessageRoutine());
    }

    private IEnumerator HideMessageRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        systemMessagePanel.gameObject.SetActive(false);
    }
}
