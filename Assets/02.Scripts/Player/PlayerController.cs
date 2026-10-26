using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Feedback Settings")]
    public Transform visuals;
    public GameObject[] parryVfxPrefabs;
    public Transform vfxSpawnPoint;

    public AudioSource audioSource;

    public AudioClip[] soundParrySuccessList;
    public AudioClip soundParryFail;

    public float parryWindow = 0.3f;

    [Tooltip("패링 유효 시간이 끝난 뒤 다시 패링할 수 없는 시간. 0이면 연타로 판정 시간을 이어 붙일 수 있습니다.")]
    public float parryRecovery = 0.35f;

    public bool IsParrying { get; private set; }
    public bool IsRecovering { get; private set; }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame) return;

        if (!IsParrying && !IsRecovering)
        {
            StartCoroutine(ParryActionRoutine());
            if (UIManager.Instance != null) UIManager.Instance.RecordParryPressed();
        }
        else if (IsRecovering)
        {
            if (UIManager.Instance != null) UIManager.Instance.RecordParryRejected();
        }
    }

    private IEnumerator ParryActionRoutine()
    {
        IsParrying = true;
        yield return new WaitForSeconds(parryWindow);
        IsParrying = false;

        // 연타 방지: 판정이 끝난 직후 바로 다시 패링하지 못하게 해서
        // 스페이스를 계속 누르는 것만으로 성공하는 상황을 막는다.
        if (parryRecovery > 0f)
        {
            IsRecovering = true;
            yield return new WaitForSeconds(parryRecovery);
            IsRecovering = false;
        }
    }

    public void PlayVisualFeedback()
    {
        if (parryVfxPrefabs != null && parryVfxPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, parryVfxPrefabs.Length);
            GameObject selectedPrefab = parryVfxPrefabs[randomIndex];

            if (selectedPrefab != null)
            {
                Vector3 spawnPos = (vfxSpawnPoint != null) ? vfxSpawnPoint.position : visuals.position;
                GameObject spawnedVfx = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                Destroy(spawnedVfx, 2f);
            }
        }

        StartCoroutine(FlashWhiteRoutine());
    }

    public void PlayAudioFeedback()
    {
        if (audioSource != null && soundParrySuccessList != null && soundParrySuccessList.Length > 0)
        {
            int randomIndex = Random.Range(0, soundParrySuccessList.Length);
            AudioClip selectedSound = soundParrySuccessList[randomIndex];

            if (selectedSound != null)
            {
                audioSource.PlayOneShot(selectedSound);
            }
        }
    }

    // 실패 사운드는 Sound 토글로만 제어한다. (Player Reaction 토글과 분리)
    public void PlayFailSound()
    {
        if (audioSource != null && soundParryFail != null)
        {
            audioSource.PlayOneShot(soundParryFail);
        }
    }

    // 실패 시 플레이어 반응(밀림 + 붉은 점멸). Player Reaction 토글로 제어한다.
    public void PlayFailFeedback()
    {
        StartCoroutine(KnockbackRoutine());
        StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        Vector3 originalPos = visuals.localPosition;
        Vector3 targetPos = originalPos + new Vector3(-0.5f, 0, 0);

        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            visuals.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            visuals.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visuals.localPosition = originalPos;
    }

    private IEnumerator FlashWhiteRoutine()
    {
        Renderer rend = visuals.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color originalColor = rend.material.color;
        rend.material.color = Color.white;

        yield return new WaitForSeconds(0.1f);
        rend.material.color = originalColor;
    }

    private IEnumerator FlashRedRoutine()
    {
        Renderer rend = visuals.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color originalColor = rend.material.color;
        rend.material.color = Color.red;

        yield return new WaitForSeconds(0.1f);
        rend.material.color = originalColor;
    }
}