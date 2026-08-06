using UnityEngine;

public class Spike : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn playerRespawn =
            other.GetComponentInParent<PlayerRespawn>();

        if (playerRespawn != null)
        {
            playerRespawn.Respawn();
        }
    }
}