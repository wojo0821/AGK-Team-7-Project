using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class PlayerPush : MonoBehaviourPun
{
    [Header("기본 밀치기 설정")]
    [SerializeField] private float basePushRange = 1.2f;
    [SerializeField] private float basePushDistance = 1f;
    [SerializeField] private float pushCooldown = 0.8f;
    [SerializeField] private float pushSpeed = 8f; // 밀쳐지는 이동 속도

    [Header("칼(무기) 장착 시 설정")]
    [SerializeField] private GameObject swordObject; // 플레이어 손에 붙은 칼 큐브
    [SerializeField] private float swordPushRange = 3.0f;   // 칼 장착 시 증가하는 범위
    [SerializeField] private float swordPushDistance = 3.5f; // 칼 장착 시 증가하는 밀쳐지는 거리

    [Header("막히는 지형 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionPadding = 0.02f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    private static readonly int PushTriggerHash = Animator.StringToHash("Push");
    private static readonly int IsPushedHash = Animator.StringToHash("IsPushed");

    private Rigidbody2D rb;
    private float nextPushTime;

    // 밀리는 중 상태 외부 공개 (이동 스크립트 제어용)
    private bool isBeingPushed;
    public bool IsBeingPushed => isBeingPushed;

    private float pushDirectionSign;
    private float remainingPushDistance;

    // 칼 장착 여부
    private bool hasSword = false;
    public bool HasSword => hasSword;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    // 현재 칼 소지 여부에 따른 밀치기 범위와 거리 반환
    public float CurrentPushRange => hasSword ? swordPushRange : basePushRange;
    public float CurrentPushDistance => hasSword ? swordPushDistance : basePushDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        UpdateSwordVisibility();
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        // F 키로 밀치기
        if (Keyboard.current == null ||
            !Keyboard.current.fKey.wasPressedThisFrame ||
            Time.time < nextPushTime)
        {
            return;
        }

        nextPushTime = Time.time + pushCooldown;
        TryPush();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || !isBeingPushed)
            return;

        // 한 프레임에 이동시킬 거리 계산
        float step = pushSpeed * Time.fixedDeltaTime;
        float actualMoveDistance = Mathf.Min(step, remainingPushDistance);

        if (actualMoveDistance <= 0f)
        {
            EndPush();
            return;
        }

        Vector2 moveDirection = Vector2.right * pushDirectionSign;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = obstacleLayer;
        filter.useTriggers = false;

        int hitCount = rb.Cast(
            moveDirection,
            filter,
            castHits,
            actualMoveDistance + collisionPadding
        );

        float allowedDistance = actualMoveDistance;

        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].distance > 0f)
            {
                allowedDistance = Mathf.Min(
                    allowedDistance,
                    castHits[i].distance - collisionPadding
                );
            }
        }

        if (allowedDistance > 0f)
        {
            rb.MovePosition(rb.position + moveDirection * allowedDistance);
        }

        remainingPushDistance -= actualMoveDistance;

        // 장애물에 막혔거나 계산된 거리를 모두 이동했으면 끝
        if (allowedDistance < actualMoveDistance - 0.0001f || remainingPushDistance <= 0f)
        {
            EndPush();
        }
    }

    private void EndPush()
    {
        isBeingPushed = false;
        remainingPushDistance = 0f;

        SetPushedAnimation(false);

        photonView.RPC(
            nameof(RPC_SetPushedAnimation),
            RpcTarget.Others,
            false
        );
    }

    private void TryPush()
    {
        // 1. 현재 미는 순간의 수치를 미리 완전 독립된 변수로 저장 (원인 1 차단)
        float targetRange = CurrentPushRange;
        float targetPushDistance = CurrentPushDistance;
        bool wasHoldingSword = hasSword;

        // 2. 범위를 내 스탯(targetRange)으로 측정
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, targetRange);

        PlayerPush closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            PlayerPush target = hit.GetComponentInParent<PlayerPush>();

            if (target == null || target == this)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                target.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = target;
            }
        }

        // 밀치기 동작 애니메이션 재생
        PlayPushAnimation();
        photonView.RPC(nameof(RPC_PlayPushAnimation), RpcTarget.Others);

        // 3. 타겟이 있으면 정확한 밀치기 수치 전달
        if (closestPlayer != null)
        {
            float direction = Mathf.Sign(
                closestPlayer.transform.position.x - transform.position.x
            );

            if (direction != 0f)
            {
                closestPlayer.photonView.RPC(
                    nameof(RPC_RequestPush),
                    closestPlayer.photonView.Owner,
                    direction,
                    targetPushDistance // 정확히 계산된 칼 거리가 넘어감
                );
            }
        }

        // 4. 밀치기 처리가 완벽히 끝난 후에 칼 해제 RPC 실행
        if (wasHoldingSword)
        {
            photonView.RPC(nameof(RPC_EquipSword), RpcTarget.All, false);
        }
    }

    private void UpdateSwordVisibility()
    {
        if (swordObject != null)
        {
            swordObject.SetActive(hasSword);
        }
    }

    private void PlayPushAnimation()
    {
        if (animator != null)
            animator.SetTrigger(PushTriggerHash);
    }

    private void SetPushedAnimation(bool isPushed)
    {
        if (animator != null)
            animator.SetBool(IsPushedHash, isPushed);
    }

    [PunRPC]
    public void RPC_EquipSword(bool equip)
    {
        hasSword = equip;
        UpdateSwordVisibility();
    }

    [PunRPC]
    private void RPC_PlayPushAnimation()
    {
        PlayPushAnimation();
    }

    [PunRPC]
    private void RPC_SetPushedAnimation(bool isPushed)
    {
        SetPushedAnimation(isPushed);
    }

    [PunRPC]
    private void RPC_RequestPush(float direction, float distance)
    {
        if (!photonView.IsMine)
            return;

        // 이전에 밀리던 것이 있더라도 즉시 끊고 새로운 수치(칼 밀치기 전체 거리)로 갱신
        pushDirectionSign = direction;
        remainingPushDistance = distance;
        isBeingPushed = true;

        SetPushedAnimation(true);
        photonView.RPC(nameof(RPC_SetPushedAnimation), RpcTarget.Others, true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = hasSword ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, CurrentPushRange);
    }
}