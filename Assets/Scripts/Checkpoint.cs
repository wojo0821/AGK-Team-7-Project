using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite activatedSprite;

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
        playerRespawn.SetCheckpoint(respawnPoint.position);

        if (flagRenderer != null && activatedSprite != null)
            flagRenderer.sprite = activatedSprite;
    }
}