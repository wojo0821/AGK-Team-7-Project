using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 checkpointPosition;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        checkpointPosition = transform.position; // 처음 시작 위치
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
        Debug.Log("체크포인트 저장: " + position);
    }

    public void Respawn()
    {
        transform.position = checkpointPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}