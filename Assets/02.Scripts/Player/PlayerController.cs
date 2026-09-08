using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform visuals;
    public ParticleSystem vfxParry;
    public AudioSource audioSource;
    public float parryWindow = 0.3f; // 패링 지속 시간 (테스트 시 조절 가능)

    public bool IsParrying { get; private set; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !IsParrying)
        {
            StartCoroutine(ParryActionRoutine());
            Debug.Log("패링 시작");
        }
    }

    private IEnumerator ParryActionRoutine()
    {
        IsParrying = true;
        // 필요시 방어 자세 애니메이션 재생
        yield return new WaitForSeconds(parryWindow);
        IsParrying = false;
    }

    public void PlayVisualFeedback()
    {
        if (vfxParry != null) vfxParry.Play();
        StartCoroutine(FlashWhiteRoutine());
    }

    public void PlayAudioFeedback()
    {
        if (audioSource != null) audioSource.Play();
    }

    private IEnumerator FlashWhiteRoutine()
    {
        Renderer rend = visuals.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color originalColor = rend.material.color;
        rend.material.color = Color.white; // 순간적인 화이트 플래시 타격감

        yield return new WaitForSeconds(0.1f);
        rend.material.color = originalColor;
    }

    public void PlayFailFeedback()
    {
        StartCoroutine(KnockbackRoutine());
        StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        Vector3 originalPos = visuals.localPosition;
        Vector3 targetPos = originalPos + new Vector3(-0.5f, 0, 0); // 좌측으로 0.5만큼 밀림

        float elapsed = 0f;
        float duration = 0.1f;

        // 뒤로 밀리기
        while (elapsed < duration)
        {
            visuals.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f); // 잠시 경직

        // 원래 위치로 복귀
        elapsed = 0f;
        while (elapsed < duration)
        {
            visuals.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visuals.localPosition = originalPos;
    }

    private IEnumerator FlashRedRoutine()
    {
        Renderer rend = visuals.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color originalColor = rend.material.color;
        rend.material.color = Color.red; // 피격 시 붉은색

        yield return new WaitForSeconds(0.1f);
        rend.material.color = originalColor;
    }
}