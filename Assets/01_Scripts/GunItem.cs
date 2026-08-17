using Photon.Pun;
using UnityEngine;

public class GunItem : MonoBehaviourPun
{
    private bool isPickedUp = false;
    [SerializeField] private float pickupCooldown = 1.0f; // 드롭 후 대기시간 1초로 설정
    private float spawnTime;

    public int ItemId { get; set; } = -1;

    private void Start()
    {
        if (ItemId == -1)
        {
            Vector3 pos = transform.position;
            ItemId = (int)(pos.x * 1000) ^ (int)(pos.y * 1000) ^ (int)(pos.z * 1000);
        }
    }

    private void OnEnable()
    {
        isPickedUp = false;
        spawnTime = Time.time;
    }

    public void ResetPickupCooldown()
    {
        spawnTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryPickup(other);
    private void OnTriggerStay2D(Collider2D other) => TryPickup(other);

    private void TryPickup(Collider2D other)
    {
        // 쿨다운 시간(1초) 동안은 습득 불가
        if (isPickedUp || Time.time < spawnTime + pickupCooldown) return;

        PlayerPush player = other.GetComponentInParent<PlayerPush>();

        if (player != null && player.photonView.IsMine)
        {
            if (player.CurrentWeapon == PlayerPush.WeaponType.Gun) return;

            isPickedUp = true; // 중복 습득 방지

            player.photonView.RPC(nameof(PlayerPush.RPC_EquipGun), RpcTarget.All, true);

            if (ItemId != -1)
            {
                player.photonView.RPC(nameof(PlayerPush.RPC_DestroyItemByID), RpcTarget.All, ItemId);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}