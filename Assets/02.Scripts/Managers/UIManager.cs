using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Text 컴포넌트 사용을 위해 추가

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

    [Header("System Message")]
    public Text systemMessageText; // 상태를 띄울 텍스트 UI
    public GameObject systemMessagePanel;

    public event Action OnAttackTriggered;

    public bool UseVisual => toggleVisual.isOn;
    public bool UseAudio => toggleAudio.isOn;
    public bool UseHitStop => toggleHitStop.isOn;
    public bool UseCamShake => toggleCamShake.isOn;
    public bool UseReaction => toggleReaction.isOn;
    public bool UsePlayerReaction => togglePlayerReaction.isOn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        buttonAttack.onClick.AddListener(() => OnAttackTriggered?.Invoke());
        if (systemMessagePanel != null) systemMessagePanel.gameObject.SetActive(false);
    }

    // 시스템 메시지 출력 함수
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