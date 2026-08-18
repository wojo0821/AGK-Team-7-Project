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
    [SerializeField] private GameObject swordPrefab;   // 손에 장착될 칼 프리팹
    [SerializeField] private GameObject gunPrefab;     // 손에 장착될 총 프리팹

    [Header("바닥 드롭용 프리팹 설정 (인스펙터 할당 가능)")]
    [SerializeField] private GameObject swordDropPrefab; // 바닥 드롭용 칼 프리팹
    [SerializeField] private GameObject gunDropPrefab;   // 바닥 드롭용 총 프리팹
    [SerializeField] private Vector3 weaponDropOffset = Vector3.zero; // 플레이어 몸쪽 오프셋

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

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame ||
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

        if (usedWeapon != WeaponType.None)
        {
            photonView.RPC(nameof(RPC_EquipWeaponDirect), RpcTarget.All, (int)WeaponType.None);
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

    private void DropWeapon(WeaponType weaponToDrop)
    {
        if (weaponToDrop == WeaponType.None) return;

        // 발바닥(transform.position) 대신 손/몸통 위치(handPoint.position)를 기준으로 드롭
        Vector3 basePosition = (handPoint != null) ? handPoint.position : transform.position;
        Vector3 dropPos = basePosition + weaponDropOffset;
        int targetWeapon = (int)weaponToDrop;

        int uniqueItemId = (int)(Time.time * 1000f) + Random.Range(1, 100000);

        photonView.RPC(nameof(RPC_SpawnDropItem), RpcTarget.All, targetWeapon, dropPos, uniqueItemId);
    }

    [PunRPC]
    private void RPC_SpawnDropItem(int weaponType, Vector3 position, int itemId)
    {
        GameObject dropPrefab = null;

        if (weaponType == (int)WeaponType.Sword) dropPrefab = swordDropPrefab;
        else if (weaponType == (int)WeaponType.Gun) dropPrefab = gunDropPrefab;

        if (dropPrefab != null)
        {
            GameObject spawnedItem = Instantiate(dropPrefab, position, Quaternion.identity);

            SwordItem sword = spawnedItem.GetComponent<SwordItem>();
            if (sword != null)
            {
                sword.ItemId = itemId;
                sword.ResetPickupCooldown();
            }

            GunItem gun = spawnedItem.GetComponent<GunItem>();
            if (gun != null)
            {
                gun.ItemId = itemId;
                gun.ResetPickupCooldown();
            }
        }
    }

    [PunRPC]
    public void RPC_DestroyItemByID(int itemId)
    {
        SwordItem[] swords = FindObjectsOfType<SwordItem>();
        foreach (var s in swords)
        {
            if (s.ItemId == itemId)
            {
                Destroy(s.gameObject);
                return;
            }
        }

        GunItem[] guns = FindObjectsOfType<GunItem>();
        foreach (var g in guns)
        {
            if (g.ItemId == itemId)
            {
                Destroy(g.gameObject);
                return;
            }
        }
    }

    private void UpdateWeaponVisibility()
    {
        if (currentSpawnedWeapon != null)
            Destroy(currentSpawnedWeapon);

        currentSpawnedWeapon = null;
        gunFirePoint = null;

        GameObject prefabToSpawn = null;

        if (currentWeapon == WeaponType.Sword)
            prefabToSpawn = swordPrefab;
        else if (currentWeapon == WeaponType.Gun)
            prefabToSpawn = gunPrefab;

        if (prefabToSpawn != null)
        {
            currentSpawnedWeapon = Instantiate(prefabToSpawn, handPoint.position, Quaternion.identity, handPoint);

            if (currentWeapon == WeaponType.Sword)
            {
                // 칼: Z축 -140도 회전
                currentSpawnedWeapon.transform.localRotation = Quaternion.Euler(0f, 0f, -140f);
            }
            else if (currentWeapon == WeaponType.Gun)
            {
                // 총: Y축 -180도 회전
                currentSpawnedWeapon.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);

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
        WeaponType newWeapon = (WeaponType)weaponTypeIndex;

        if (currentWeapon != WeaponType.None && newWeapon != WeaponType.None && currentWeapon != newWeapon)
        {
            if (photonView.IsMine)
            {
                DropWeapon(currentWeapon);
            }
        }

        currentWeapon = newWeapon;
        UpdateWeaponVisibility();
    }

    [PunRPC]
    public void RPC_EquipWeaponDirect(int weaponTypeIndex)
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