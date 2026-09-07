using System;
using UnityEngine;
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
    public Button buttonAttack;

    // 공격 명령을 외부로 알리는 이벤트
    public event Action OnAttackTriggered;

    // 다른 스크립트에서 UI 상태를 즉시 읽어갈 수 있도록 프로퍼티화
    public bool UseVisual => toggleVisual.isOn;
    public bool UseAudio => toggleAudio.isOn;
    public bool UseHitStop => toggleHitStop.isOn;
    public bool UseCamShake => toggleCamShake.isOn;
    public bool UseReaction => toggleReaction.isOn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        buttonAttack.onClick.AddListener(() => OnAttackTriggered?.Invoke());
    }
}