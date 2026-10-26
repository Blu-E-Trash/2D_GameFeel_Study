using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform visuals; // 기울임 연출을 위해 자식 Visuals 할당
    public GameObject attackIndicator; // 느낌표 등 예비 동작 UI
    public float telegraphTime = 0.8f; // 공격 전조 시간

    [Tooltip("Random Telegraph가 켜져 있을 때 전조 시간 = telegraphTime × (1 ± 이 비율)")]
    [Range(0f, 0.5f)] public float telegraphRandomRange = 0.25f;

    private bool isAttacking = false;

    private void Start()
    {
        attackIndicator.SetActive(false);
        UIManager.Instance.OnAttackTriggered += StartAttack;
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnAttackTriggered -= StartAttack;
    }

    private void StartAttack()
    {
        if (!isAttacking) StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        attackIndicator.SetActive(true);

        // 공격 예비 동작 대기 (Random Telegraph: 리듬을 외워서 맞추지 못하도록 전조 길이를 흔든다)
        float telegraph = telegraphTime;
        if (UIManager.Instance.UseRandomTelegraph)
            telegraph = telegraphTime * Random.Range(1f - telegraphRandomRange, 1f + telegraphRandomRange);
        yield return new WaitForSeconds(telegraph);
        attackIndicator.SetActive(false);

        // 공격 찌르기 모션
        Vector3 originalPos = visuals.localPosition;
        // 플레이어 방향(좌측, x축 -방향)으로 3만큼 순간 이동
        visuals.localPosition = new Vector3(originalPos.x - 3f, originalPos.y, originalPos.z);

        // 공격 판정
        PlayerController player = FindAnyObjectByType<PlayerController>();
        bool parrySuccess = player != null && player.IsParrying;
        UIManager.Instance.RecordParryResult(parrySuccess);

        if (parrySuccess)
        {
            // 패링 성공
            UIManager.Instance.ShowSystemMessage("Parrying Success!", Color.green);
            FeedbackManager.Instance.TriggerParryFeedback();

            if (UIManager.Instance.UseReaction)
                StartCoroutine(ApplyReactionRoutine());
        }
        else
        {
            // 패링 실패
            UIManager.Instance.ShowSystemMessage("Parrying Fail...", Color.red);

            if (player != null)
            {
                // 실패 사운드는 Sound 토글로, 밀림·색상 변경은 Player Reaction 토글로 각각 제어
                if (UIManager.Instance.UseAudio) player.PlayFailSound();
                if (UIManager.Instance.UsePlayerReaction) player.PlayFailFeedback();
            }
        }

        yield return new WaitForSeconds(0.2f); // 공격 자세 유지 시간
        visuals.localPosition = originalPos; // 원래 자리로 복귀

        yield return new WaitForSeconds(0.3f); // 공격 후딜레이
        isAttacking = false;
    }

    private IEnumerator ApplyReactionRoutine()
    {
        // 에셋 없이 Z축을 기울여 뒤로 밀리는 자세 붕괴 구현
        float duration = 0.15f;
        float elapsed = 0f;
        Quaternion originalRot = visuals.localRotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, -30f); // 30도 젖혀짐

        while (elapsed < duration)
        {
            visuals.localRotation = Quaternion.Slerp(originalRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f); // 스턴 유지 시간

        elapsed = 0f;
        while (elapsed < duration)
        {
            visuals.localRotation = Quaternion.Slerp(targetRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visuals.localRotation = originalRot;
    }
}