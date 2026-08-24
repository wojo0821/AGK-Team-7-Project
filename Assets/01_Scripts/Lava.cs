using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class Lava : MonoBehaviourPun, IPunObservable
{
    [Header("Lava Components")]
    [SerializeField] private SpriteRenderer bottomSprite; // Pivot: Top, Draw Mode: Tiled

    [Header("Rise Settings")]
    [SerializeField] private float riseSpeed = 0.5f;
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private bool isRising = false;

    private float startPosY;
    private float initialBottomHeight;

    private Vector3 networkPosition;
    private float networkSizeY;

    private void Start()
    {
        startPosY = transform.position.y;
        networkPosition = transform.position;

        if (bottomSprite != null)
        {
            initialBottomHeight = bottomSprite.size.y;
            networkSizeY = initialBottomHeight;
        }
    }

    public void SetRiseSpeed(float newSpeed)
    {
        riseSpeed = newSpeed;
    }

    public void StartLava()
    {
        isRising = true;
    }

    private void Update()
    {
        if (!isRising) return;

        // 1. 방장: GameManager가 조절하는 riseSpeed로 직접 올라감
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            if (transform.position.y < maxHeight)
            {
                float moveAmount = riseSpeed * Time.deltaTime;
                transform.position += Vector3.up * moveAmount;

                if (bottomSprite != null)
                {
                    float totalRise = transform.position.y - startPosY;
                    Vector2 newSize = bottomSprite.size;
                    newSize.y = initialBottomHeight + totalRise;
                    bottomSprite.size = newSize;
                }
            }
        }
        else
        {
            // 2. 다른 플레이어: 방장의 위치와 텍스쳐 크기를 그대로 따라감
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 25f);
            if (bottomSprite != null)
            {
                Vector2 s = bottomSprite.size;
                s.y = Mathf.Lerp(s.y, networkSizeY, Time.deltaTime * 25f);
                bottomSprite.size = s;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(bottomSprite != null ? bottomSprite.size.y : 0f);
            stream.SendNext(isRising);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkSizeY = (float)stream.ReceiveNext();
            isRising = (bool)stream.ReceiveNext();
        }
    }
}
