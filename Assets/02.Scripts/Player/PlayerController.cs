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
    public bool IsParrying { get; private set; }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !IsParrying)
        {
            StartCoroutine(ParryActionRoutine());
        }
    }

    private IEnumerator ParryActionRoutine()
    {
        IsParrying = true;
        yield return new WaitForSeconds(parryWindow);
        IsParrying = false;
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

    public void PlayFailFeedback()
    {
        if (UIManager.Instance.UseAudio && audioSource != null && soundParryFail != null)
        {
            audioSource.PlayOneShot(soundParryFail);
        }

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