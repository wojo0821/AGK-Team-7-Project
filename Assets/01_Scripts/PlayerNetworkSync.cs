using Photon.Pun;
using UnityEngine;
public class PlayerNetworkSync : MonoBehaviourPun, IPunObservable
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 networkPosition;
    private Vector2 networkVelocity;
    private bool networkFlipX;
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float teleportDistance = 3.0f; // 오차가 너무 크면 순간이동
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    private void Update()
    {
        if (photonView.IsMine) return;
        // 원격 플레이어: 네트워크 위치 + 속도 기반 추정 보간
        transform.position = Vector2.Lerp(transform.position, networkPosition, Time.deltaTime * smoothSpeed);
        // 거리가 너무 벌어졌을 때만 즉시 텔레포트
        if (Vector2.Distance(transform.position, networkPosition) > teleportDistance)
        {
            transform.position = networkPosition;
        }
        // 좌우 반전 동기화
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = networkFlipX;
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 데이터 전송
            stream.SendNext((Vector2)transform.position);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(spriteRenderer != null ? spriteRenderer.flipX : false);
        }
        else
        {
            // 상대방 데이터 수신
            networkPosition = (Vector2)stream.ReceiveNext();
            networkVelocity = (Vector2)stream.ReceiveNext();
            networkFlipX = (bool)stream.ReceiveNext();
            // 네트워크 지연 시간(Lag) 보상 (속도 * 지연시간만큼 미리 이동 예측)
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * lag;
        }
    }
}