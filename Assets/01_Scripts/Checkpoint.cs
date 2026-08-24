using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite activatedSprite;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip checkpointSound; // 체크포인트 활성화 사운드

    private bool activated;

    private void Reset()
    {
        respawnPoint = transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        PlayerRespawn playerRespawn =
            other.GetComponentInParent<PlayerRespawn>();

        if (playerRespawn == null)
            return;

        activated = true;

        // 체크포인트 사운드 재생
        if (checkpointSound != null)
        {
            AudioSource.PlayClipAtPoint(checkpointSound, transform.position);
        }

        if (flagRenderer != null)
            flagRenderer.color = new Color(0.75f, 1f, 0.75f, 1f);

        playerRespawn.SetCheckpoint(respawnPoint.position);

        if (flagRenderer != null && activatedSprite != null)
            flagRenderer.sprite = activatedSprite;
    }
}