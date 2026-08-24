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
    [SerializeField] private float maxVolume = 0.3f; // 볼륨 기본값을 0.3으로 낮춤 (원하는대로 조절 가능)

    [Header("3D Sound Settings")]
    [SerializeField] private float minDistance = 2f;  // 이 거리 안에서는 소리가 최대 크기(maxVolume)로 들림
    [SerializeField] private float maxDistance = 20f; // 이 거리보다 멀어지면 소리가 들리지 않음

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

            // --- 3D 사운드(거리 감소) 핵심 설정 ---
            audioSource.spatialBlend = 1.0f; // 1.0 = 완전한 3D 사운드 적용
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // 거리에 따라 자연스럽게 감소
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
        }
    }

    // 인스펙터에서 값을 수정했을 때 바로 적용되도록 처리
    private void OnValidate()
    {
        if (audioSource != null)
        {
            audioSource.volume = maxVolume;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
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