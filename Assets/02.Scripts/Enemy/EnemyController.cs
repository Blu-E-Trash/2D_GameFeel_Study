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

        // 공격 예비 동작 대기
        yield return new WaitForSeconds(telegraphTime);
        attackIndicator.SetActive(false);

        // 공격 찌르기 모션
        Vector3 originalPos = visuals.localPosition;
        // 플레이어 방향(좌측, x축 -방향)으로 3만큼 순간 이동
        visuals.localPosition = new Vector3(originalPos.x - 3f, originalPos.y, originalPos.z);

        // 공격 판정
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.IsParrying)
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

            // 토글이 켜져 있을 때만 플레이어 밀림 및 색상 변경 연출 실행
            if (player != null && UIManager.Instance.UsePlayerReaction)
            {
                player.PlayFailFeedback();
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