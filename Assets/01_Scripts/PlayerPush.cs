using Photon.Pun;
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

    private Rigidbody2D rb;
    private float nextPushTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 내 플레이어만 F 입력 처리
        if (!photonView.IsMine)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame &&
            Time.time >= nextPushTime)
        {
            nextPushTime = Time.time + pushCooldown;
            TryPush();
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

            float distance =
                Vector2.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = target;
            }
        }

        if (closestPlayer == null)
            return;

        float horizontalDirection =
            Mathf.Sign(closestPlayer.transform.position.x - transform.position.x);

        if (horizontalDirection == 0f)
            return;

        closestPlayer.photonView.RPC(
            nameof(RPC_ApplyPush),
            closestPlayer.photonView.Owner,
            horizontalDirection
        );
    }

    [PunRPC]
    private void RPC_ApplyPush(float horizontalDirection)
    {
        if (!photonView.IsMine)
            return;

        Vector2 nextPosition = rb.position +
            Vector2.right * horizontalDirection * pushDistance;

        rb.position = nextPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRange);
    }
}