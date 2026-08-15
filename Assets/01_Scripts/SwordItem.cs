using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PhotonView))]
public class SwordItem : MonoBehaviourPun
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 서버/마스터 클라이언트에서만 획득 판정 처리 (중복 획득 방지)
        if (!PhotonNetwork.IsMasterClient)
            return;

        PlayerPush player = other.GetComponentInParent<PlayerPush>();

        // 플레이어이고 아직 칼을 가지고 있지 않은 경우에만 주움
        if (player != null && !player.HasSword)
        {
            // 플레이어에게 칼 획득 처리 (RPC)
            player.photonView.RPC(nameof(PlayerPush.RPC_EquipSword), RpcTarget.All, true);

            // 아이템 오브젝트 네트워크 삭제
            PhotonNetwork.Destroy(gameObject);
        }
    }
}