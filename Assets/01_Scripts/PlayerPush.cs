using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class PlayerPush : MonoBehaviourPun
{
    public enum WeaponType
    {
        None,
        Sword,
        Gun
    }

    [Header("기본 밀치기 설정")]
    [SerializeField] private float basePushRange = 1.2f;
    [SerializeField] private float basePushDistance = 1f;
    [SerializeField] private float pushCooldown = 0.8f;
    [SerializeField] private float pushSpeed = 8f;

    [Header("무기 손 위치 및 프리팹 설정")]
    [SerializeField] private Transform handPoint;      // 손 위치 기준점 (Hierarchy의 HandPoint)
    [SerializeField] private GameObject swordPrefab;   // Project 창의 칼 프리팹
    [SerializeField] private GameObject gunPrefab;     // Project 창의 총 프리팹

    [Header("칼(무기) 설정")]
    [SerializeField] private float swordPushRange = 3.0f;
    [SerializeField] private float swordPushDistance = 3.5f;

    [Header("총(무기) 설정")]
    [SerializeField] private float gunPushDistance = 1.0f;

    [Header("막히는 지형 레이어")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionPadding = 0.02f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    private static readonly int PushTriggerHash = Animator.StringToHash("Push");
    private static readonly int IsPushedHash = Animator.StringToHash("IsPushed");

    private Rigidbody2D rb;
    private float nextPushTime;

    private bool isBeingPushed;
    public bool IsBeingPushed => isBeingPushed;

    private float pushDirectionSign;
    private float remainingPushDistance;

    private WeaponType currentWeapon = WeaponType.None;
    public WeaponType CurrentWeapon => currentWeapon;

    public bool HasSword => currentWeapon == WeaponType.Sword;
    public bool HasGun => currentWeapon == WeaponType.Gun;

    // 현재 손에 동적으로 생성되어 쥐어지고 있는 무기 오브젝트 참조
    private GameObject currentSpawnedWeapon;
    private Transform gunFirePoint;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private float facingDirection = 1f;

    public float CurrentPushRange
    {
        get
        {
            switch (currentWeapon)
            {
                case WeaponType.Sword: return swordPushRange;
                default: return basePushRange;
            }
        }
    }

    public float CurrentPushDistance
    {
        get
        {
            switch (currentWeapon)
            {
                case WeaponType.Gun: return basePushDistance;
                case WeaponType.Sword: return swordPushDistance;
                default: return basePushDistance;
            }
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (handPoint == null)
            handPoint = transform;
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        if (transform.localScale.x != 0)
        {
            facingDirection = Mathf.Sign(transform.localScale.x);
        }

        if (Keyboard.current == null ||
            !Keyboard.current.fKey.wasPressedThisFrame ||
            Time.time < nextPushTime)
        {
            return;
        }

        nextPushTime = Time.time + pushCooldown;
        TryPush();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || !isBeingPushed)
            return;

        float step = pushSpeed * Time.fixedDeltaTime;
        float actualMoveDistance = Mathf.Min(step, remainingPushDistance);

        if (actualMoveDistance <= 0f)
        {
            EndPush();
            return;
        }

        Vector2 moveDirection = Vector2.right * pushDirectionSign;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = obstacleLayer;
        filter.useTriggers = false;

        int hitCount = rb.Cast(
            moveDirection,
            filter,
            castHits,
            actualMoveDistance + collisionPadding
        );

        float allowedDistance = actualMoveDistance;

        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].distance > 0f)
            {
                allowedDistance = Mathf.Min(
                    allowedDistance,
                    castHits[i].distance - collisionPadding
                );
            }
        }

        if (allowedDistance > 0f)
        {
            rb.MovePosition(rb.position + moveDirection * allowedDistance);
        }

        remainingPushDistance -= actualMoveDistance;

        if (allowedDistance < actualMoveDistance - 0.0001f || remainingPushDistance <= 0f)
        {
            EndPush();
        }
    }

    private void EndPush()
    {
        isBeingPushed = false;
        remainingPushDistance = 0f;

        SetPushedAnimation(false);

        photonView.RPC(
            nameof(RPC_SetPushedAnimation),
            RpcTarget.Others,
            false
        );
    }

    private void TryPush()
    {
        WeaponType usedWeapon = currentWeapon;

        PlayPushAnimation();
        photonView.RPC(nameof(RPC_PlayPushAnimation), RpcTarget.Others);

        if (usedWeapon == WeaponType.Gun)
        {
            Vector3 spawnPos = (gunFirePoint != null) ? gunFirePoint.position : handPoint.position;

            photonView.RPC(
                nameof(RPC_SpawnBullet),
                RpcTarget.All,
                spawnPos,
                facingDirection,
                basePushDistance
            );
        }
        else
        {
            float targetRange = CurrentPushRange;
            float targetPushDistance = CurrentPushDistance;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, targetRange);

            PlayerPush closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                PlayerPush target = hit.GetComponentInParent<PlayerPush>();

                if (target == null || target == this)
                    continue;

                float distance = Vector2.Distance(
                    transform.position,
                    target.transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = target;
                }
            }

            if (closestPlayer != null)
            {
                float direction = Mathf.Sign(
                    closestPlayer.transform.position.x - transform.position.x
                );

                if (direction != 0f)
                {
                    closestPlayer.photonView.RPC(
                        nameof(RPC_RequestPush),
                        closestPlayer.photonView.Owner,
                        direction,
                        targetPushDistance
                    );
                }
            }
        }

        // 사용 후 무기 해제
        if (usedWeapon != WeaponType.None)
        {
            photonView.RPC(nameof(RPC_EquipWeapon), RpcTarget.All, (int)WeaponType.None);
        }
    }

    [PunRPC]
    private void RPC_SpawnBullet(Vector3 spawnPosition, float direction, float pushDist)
    {
        GameObject bulletObj = new GameObject("GunBullet");
        bulletObj.transform.position = spawnPosition;
        bulletObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        SpriteRenderer sr = bulletObj.AddComponent<SpriteRenderer>();
        Texture2D texture = new Texture2D(16, 16);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                texture.SetPixel(x, y, Color.white);
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        sr.color = Color.yellow;
        sr.sortingOrder = 10;

        CircleCollider2D col2d = bulletObj.AddComponent<CircleCollider2D>();
        col2d.isTrigger = true;
        col2d.radius = 0.5f;

        Bullet bullet = bulletObj.AddComponent<Bullet>();
        bullet.Init(direction, pushDist, photonView);
    }

    // 프리팹을 활용한 손 무기 생성/파괴 처리
    private void UpdateWeaponVisibility()
    {
        // 기존 손에 들고 있던 무기 파괴
        if (currentSpawnedWeapon != null)
        {
            Destroy(currentSpawnedWeapon);
            currentSpawnedWeapon = null;
            gunFirePoint = null;
        }

        GameObject prefabToSpawn = null;

        if (currentWeapon == WeaponType.Sword)
            prefabToSpawn = swordPrefab;
        else if (currentWeapon == WeaponType.Gun)
            prefabToSpawn = gunPrefab;

        // 새 무기 프리팹 생성 후 handPoint 하위에 배치
        if (prefabToSpawn != null)
        {
            currentSpawnedWeapon = Instantiate(prefabToSpawn, handPoint.position, handPoint.rotation, handPoint);

            // 총인 경우 FirePoint 자동 탐색
            if (currentWeapon == WeaponType.Gun)
            {
                Transform foundPoint = currentSpawnedWeapon.transform.Find("FirePoint");
                gunFirePoint = (foundPoint != null) ? foundPoint : currentSpawnedWeapon.transform;
            }
        }
    }

    private void PlayPushAnimation()
    {
        if (animator != null)
            animator.SetTrigger(PushTriggerHash);
    }

    private void SetPushedAnimation(bool isPushed)
    {
        if (animator != null)
            animator.SetBool(IsPushedHash, isPushed);
    }

    [PunRPC]
    public void RPC_EquipWeapon(int weaponTypeIndex)
    {
        currentWeapon = (WeaponType)weaponTypeIndex;
        UpdateWeaponVisibility();
    }

    [PunRPC]
    public void RPC_EquipSword(bool equip)
    {
        RPC_EquipWeapon(equip ? (int)WeaponType.Sword : (int)WeaponType.None);
    }

    [PunRPC]
    public void RPC_EquipGun(bool equip)
    {
        RPC_EquipWeapon(equip ? (int)WeaponType.Gun : (int)WeaponType.None);
    }

    [PunRPC]
    private void RPC_PlayPushAnimation()
    {
        PlayPushAnimation();
    }

    [PunRPC]
    private void RPC_SetPushedAnimation(bool isPushed)
    {
        SetPushedAnimation(isPushed);
    }

    [PunRPC]
    private void RPC_RequestPush(float direction, float distance)
    {
        if (!photonView.IsMine)
            return;

        pushDirectionSign = direction;
        remainingPushDistance = distance;
        isBeingPushed = true;

        SetPushedAnimation(true);
        photonView.RPC(nameof(RPC_SetPushedAnimation), RpcTarget.Others, true);
    }

    private void OnDrawGizmosSelected()
    {
        Color gizmoColor = Color.yellow;
        if (currentWeapon == WeaponType.Sword) gizmoColor = Color.red;
        else if (currentWeapon == WeaponType.Gun) gizmoColor = Color.cyan;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, CurrentPushRange);
    }
}