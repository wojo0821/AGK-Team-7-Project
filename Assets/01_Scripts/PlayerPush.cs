using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class PlayerPush : MonoBehaviourPun
{
    [Header("밀치기 설정")]
    [SerializeField] private float pushRange = 1.2f;
    [SerializeField] private float pushDistance = 1f;
    [SerializeField] private float pushCooldown = 0.8f;
    [SerializeField] private float pushSpeed = 6f; // 초당 이동 속도 (클수록 빨리 밀림)

    [Header("막히는 지형 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionPadding = 0.02f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    private static readonly int PushTriggerHash = Animator.StringToHash("Push");
    private static readonly int IsPushedHash = Animator.StringToHash("IsPushed");

    private Rigidbody2D rb;
    private float nextPushTime;

    private bool hasPendingPush;
    private float pendingDirection;

    // 밀리는 중 상태 (여러 FixedUpdate에 걸쳐 조금씩 이동)
    private bool isBeingPushed;
    private float pushDirectionSign;
    private float remainingPushDistance;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

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
        if (!photonView.IsMine)
            return;

        // 새로 요청된 밀치기가 있으면 이동 상태 시작 (밀리는 사람 본인 클라이언트에서 실행됨)
        if (hasPendingPush)
        {
            hasPendingPush = false;

            isBeingPushed = true;
            pushDirectionSign = pendingDirection;
            remainingPushDistance = pushDistance;

            SetPushedAnimation(true);

            photonView.RPC(
                nameof(RPC_SetPushedAnimation),
                RpcTarget.Others,
                true
            );
        }

        if (!isBeingPushed)
            return;

        // 이번 프레임에 이동할 거리 (속도 * 시간), 남은 거리를 넘지 않도록 클램프
        float step = Mathf.Min(pushSpeed * Time.fixedDeltaTime, remainingPushDistance);

        if (step <= 0f)
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
            step + collisionPadding
        );

        float allowedDistance = step;

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
            rb.MovePosition(
                rb.position + moveDirection * allowedDistance
            );
        }

        remainingPushDistance -= step;

        // 장애물에 막혔거나 남은 거리를 다 이동했으면 종료
        if (allowedDistance < step - 0.0001f || remainingPushDistance <= 0f)
        {
            EndPush();
        }
    }

    private void EndPush()
    {
        isBeingPushed = false;
        SetPushedAnimation(false);

        photonView.RPC(
            nameof(RPC_SetPushedAnimation),
            RpcTarget.Others,
            false
        );
    }

    private void TryPush()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, pushRange);

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

        if (closestPlayer == null)
            return;

        float direction = Mathf.Sign(
            closestPlayer.transform.position.x - transform.position.x
        );

        if (direction == 0f)
            return;

        // 미는 사람(나): 기존 그대로 트리거 애니메이션 재생
        PlayPushAnimation();

        photonView.RPC(
            nameof(RPC_PlayPushAnimation),
            RpcTarget.Others
        );

        // 밀리는 사람: 이동 + 새로 추가된 반복 애니메이션 요청
        closestPlayer.photonView.RPC(
            nameof(RPC_RequestPush),
            closestPlayer.photonView.Owner,
            direction
        );
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
    private void RPC_RequestPush(float direction)
    {
        if (!photonView.IsMine)
            return;

        pendingDirection = direction;
        hasPendingPush = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRange);
    }
}