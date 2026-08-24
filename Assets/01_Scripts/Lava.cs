using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class Lava : MonoBehaviourPun, IPunObservable
{
    [Header("Lava Components")]
    [SerializeField] private SpriteRenderer bottomSprite;

    [Header("Lava Audio Settings")]
    [SerializeField] private AudioClip lavaSound;
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.3f; // 용암 소리 크기 조절

    private AudioSource audioSource;

    [Header("Rise Settings")]
    [SerializeField] private float riseSpeed = 0.5f;
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private bool isRising = false;

    private float startPosY;
    private float initialBottomHeight;

    private Vector3 networkPosition;
    private float networkSizeY;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null) return;

        if (lavaSound != null)
        {
            audioSource.clip = lavaSound;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = maxVolume;

            // 용암 전체 영역에서 화면 어디서든 균일하게 들리도록 완전 2D 처리
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnValidate()
    {
        if (audioSource != null)
        {
            audioSource.volume = maxVolume;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        startPosY = transform.position.y;
        networkPosition = transform.position;

        if (bottomSprite != null)
        {
            initialBottomHeight = bottomSprite.size.y;
            networkSizeY = initialBottomHeight;
        }

        UpdateAudioState();
    }

    public void SetRiseSpeed(float newSpeed)
    {
        riseSpeed = newSpeed;
    }

    public void StartLava()
    {
        if (isRising) return;
        isRising = true;
        UpdateAudioState();
    }

    private void UpdateAudioState()
    {
        if (audioSource == null || lavaSound == null) return;

        if (isRising)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void Update()
    {
        if (!isRising) return;

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

            bool prevRising = isRising;
            isRising = (bool)stream.ReceiveNext();

            if (prevRising != isRising)
            {
                UpdateAudioState();
            }
        }
    }
}