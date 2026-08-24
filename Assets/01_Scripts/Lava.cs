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
    [SerializeField] private float maxVolume = 0.5f; // 최고 소리 크기

    [Header("3D Distance Audio Settings")]
    [SerializeField] private float minDistance = 2f;   // 소리가 최대로 들리는 최소 거리
    [SerializeField] private float maxDistance = 25f;  // 소리가 들리지 않게 되는 최대 거리

    private AudioSource audioSource;
    private Transform localPlayerTransform; // 내 로컬 플레이어 위치

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

            // 3D 감쇠(거리에 따른 소리 변화) 활성화
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
        }
    }

    private void OnValidate()
    {
        if (audioSource != null)
        {
            audioSource.volume = maxVolume;
            audioSource.spatialBlend = 1.0f;
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
        // 내 클라이언트 플레이어 기준 3D 오디오 위치 업데이트
        Update3DAudioPosition();

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

    private void Update3DAudioPosition()
    {
        if (audioSource == null) return;

        // 내 로컬 플레이어(IsMine == true)의 Transform 탐색
        if (localPlayerTransform == null)
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    localPlayerTransform = player.transform;
                    break;
                }
            }

            // 플레이어를 찾지 못했으면 메인 카메라 위치로 대체
            if (localPlayerTransform == null && Camera.main != null)
            {
                localPlayerTransform = Camera.main.transform;
            }
        }

        if (localPlayerTransform != null)
        {
            float halfWidth = 50f;
            if (bottomSprite != null)
            {
                halfWidth = bottomSprite.bounds.extents.x;
            }

            float minX = transform.position.x - halfWidth;
            float maxX = transform.position.x + halfWidth;

            // 내 로컬 플레이어의 X 위치를 용암 폭 안으로 고정하여 가장 가까운 X 위치에서 소리가 발생하도록 조절
            float closestX = Mathf.Clamp(localPlayerTransform.position.x, minX, maxX);

            // AudioSource의 연산 위치 조정
            audioSource.transform.position = new Vector3(closestX, transform.position.y, transform.position.z);
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