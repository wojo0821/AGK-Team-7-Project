using Photon.Pun;
using UnityEngine;

public class GunItem : MonoBehaviourPun
{
    private bool isPickedUp = false;

    private void OnTriggerStay2D(Collider2D other)
    {
        // 이미 누군가 주운 아이템이면 중복 실행 방지
        if (isPickedUp) return;

        PlayerPush player = other.GetComponentInParent<PlayerPush>();

        // 내 캐릭터이고 빈 손일 때만 획득 가능
        if (player != null && player.photonView.IsMine && player.CurrentWeapon == PlayerPush.WeaponType.None)
        {
            isPickedUp = true;

            // 1. 플레이어에게 총 장착
            player.photonView.RPC(nameof(PlayerPush.RPC_EquipGun), RpcTarget.All, true);

            // 2. 모든 클라이언트(나중에 들어오는 유저 포함)에서 아이템 숨기기
            photonView.RPC(nameof(RPC_HideItem), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void RPC_HideItem()
    {
        gameObject.SetActive(false);
    }
}