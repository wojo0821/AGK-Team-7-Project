using Photon.Pun;
using UnityEngine;

public class Bullet : MonoBehaviourPun
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    private float directionSign = 1f;
    private float pushDistance = 1f; // 기본 밀치기와 동일한 수치
    private PhotonView ownerPhotonView;

    public void Init(float direction, float pushDist, PhotonView owner)
    {
        directionSign = direction;
        pushDistance = pushDist;
        ownerPhotonView = owner;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 오른쪽/왼쪽으로 직선 이동
        transform.Translate(Vector3.right * directionSign * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 발사한 본인은 무시
        if (ownerPhotonView != null && (other.gameObject == ownerPhotonView.gameObject || other.transform.IsChildOf(ownerPhotonView.transform)))
            return;

        PlayerPush targetPlayer = other.GetComponentInParent<PlayerPush>();

        if (targetPlayer != null)
        {
            if (ownerPhotonView != null && ownerPhotonView.IsMine)
            {
                // 타겟 플레이어 밀치기 (기본 밀치기 거리가 적용됨)
                targetPlayer.photonView.RPC(
                    "RPC_RequestPush",
                    targetPlayer.photonView.Owner,
                    directionSign,
                    pushDistance
                );
            }

            Destroy(gameObject);
            return;
        }

        // 벽이나 지형 장애물 충돌 시 제거
        if (((1 << other.gameObject.layer) & LayerMask.GetMask("Default", "Obstacle", "Ground")) != 0)
        {
            Destroy(gameObject);
        }
    }
}