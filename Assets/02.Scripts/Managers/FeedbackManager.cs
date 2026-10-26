using System.Collections;
using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Tooltip("패링 성공 시 히트스톱 길이(초). 이전 버전은 0.5초 고정이었다. UI 슬라이더로 조절된다.")]
    public float hitStopDuration = 0.1f;

    private Camera mainCamera;
    private Vector3 originalCameraPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;
        if (mainCamera != null) originalCameraPos = mainCamera.transform.localPosition;
    }

    public void TriggerParryFeedback()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();

        // 1. 로컬 피드백 (시각/청각)
        if (player != null)
        {
            if (UIManager.Instance.UseVisual) player.PlayVisualFeedback();
            if (UIManager.Instance.UseAudio) player.PlayAudioFeedback();
        }

        // 2. 글로벌 피드백 (시간/화면)
        if (UIManager.Instance.UseHitStop && hitStopDuration > 0f)
            StartCoroutine(HitStopRoutine(hitStopDuration));

        if (UIManager.Instance.UseCamShake)
            StartCoroutine(CameraShakeRoutine(0.15f, 0.3f)); // 0.15초간 0.3의 강도로 진동
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.01f; // 0으로 주면 멈추는 로직이 꼬일 수 있어 0.01로 극단적 감속 처리
        yield return new WaitForSecondsRealtime(duration); // TimeScale의 영향을 받지 않는 타이머
        Time.timeScale = 1f;
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // HitStop(TimeScale=0) 상황에서도 쉐이크가 동작하도록 unscaledDeltaTime 사용
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.localPosition = new Vector3(originalCameraPos.x + x, originalCameraPos.y + y, originalCameraPos.z);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalCameraPos;
    }
}