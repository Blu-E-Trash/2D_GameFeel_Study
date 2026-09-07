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
}