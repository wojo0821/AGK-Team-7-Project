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

    [Header("막히는 지형 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionPadding = 0.02f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private float nextPushTime;

    private bool hasPendingPush;
    private float pendingDirection;

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
        if (!photonView.IsMine || !hasPendingPush)
            return;

        hasPendingPush = false;

        Vector2 moveDirection = Vector2.right * pendingDirection;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = obstacleLayer;
        filter.useTriggers = false;

        int hitCount = rb.Cast(
            moveDirection,
            filter,
            castHits,
            pushDistance + collisionPadding
        );

        float allowedDistance = pushDistance;

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

        PlayPushAnimation();

        photonView.RPC(
            nameof(RPC_PlayPushAnimation),
            RpcTarget.Others
        );

        closestPlayer.photonView.RPC(
            nameof(RPC_RequestPush),
            closestPlayer.photonView.Owner,
            direction
        );
    }

    private void PlayPushAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Push");
    }

    [PunRPC]
    private void RPC_PlayPushAnimation()
    {
        PlayPushAnimation();
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