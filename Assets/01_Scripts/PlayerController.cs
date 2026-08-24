using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float speed = 14f;

    [Header("Debug Status")]
    [SerializeField] private Vector2 movementInput;
    private PhotonView view;

    [Header("Jump")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.2f, 0.2f);
    [SerializeField] private float jumpForce = 6f;
    private bool isGrounded;
    private float gravityScale;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound; // 점프 사운드

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (!view.IsMine)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void Start()
    {
        // 시네마 카메라 따라오게 (Cinemachine 3.x 대응)
        if (view.IsMine)
        {
            var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Target.TrackingTarget = this.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerController] CinemachineCamera를 찾지 못했습니다.");
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (view.IsMine)
        {
            movementInput = context.ReadValue<Vector2>();

            if (movementInput.x > 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (movementInput.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, LayerMask.GetMask("Ground"));

        if (view.IsMine)
        {
            if (context.started && isGrounded)
            {
                rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                rb.gravityScale = gravityScale * 0.6f;

                // 점프 사운드 재생 (자신 및 네트워크 플레이어 전체 동기화)
                PlayJumpSound();
                view.RPC(nameof(RPC_PlayJumpSound), RpcTarget.Others);
            }
            else if (context.canceled)
            {
                rb.gravityScale = gravityScale;
            }
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (view.IsMine)
        {
            if (context.started)
            {
                speed += 2f;
            }
            else if (context.canceled)
            {
                speed -= 2f;
            }
        }
    }

    private void PlayJumpSound()
    {
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    [PunRPC]
    private void RPC_PlayJumpSound()
    {
        PlayJumpSound();
    }

    private void FixedUpdate()
    {
        if (!view.IsMine) return;
        rb.linearVelocity = new Vector2(movementInput.x * speed, rb.linearVelocity.y);
    }

    private void Update()
    {
        if (!view.IsMine) return;

        if (animator != null)
        {
            animator.SetInteger("X", (int)movementInput.x);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}