using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform visuals; // 기울임 연출을 위해 자식 Visuals 할당
    public GameObject attackIndicator; // 느낌표 등 예비 동작 UI
    public float telegraphTime = 0.8f; // 공격 전조 시간

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

        // 1. 공격 예비 동작 대기
        yield return new WaitForSeconds(telegraphTime);
        attackIndicator.SetActive(false);

        // 2. 공격 판정 (단순화를 위해 Player를 직접 찾아 상태 확인)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.IsParrying)
        {
            // 패링 성공
            FeedbackManager.Instance.TriggerParryFeedback();

            // UI에 적 반응 옵션이 켜져 있다면 자세 붕괴 코루틴 실행
            if (UIManager.Instance.UseReaction)
                StartCoroutine(ApplyReactionRoutine());
        }
        else
        {
            Debug.Log("패링 실패! 일반 피격 처리");
        }

        yield return new WaitForSeconds(0.5f); // 공격 후딜레이
        isAttacking = false;
    }

    private IEnumerator ApplyReactionRoutine()
    {
        // 에셋 없이 Z축을 기울여 뒤로 밀리는 자세 붕괴(스턴) 구현
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