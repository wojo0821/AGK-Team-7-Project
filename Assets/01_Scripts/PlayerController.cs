using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float speed = 14f;

    [Header("Camera Zoom")]
    [SerializeField] private Unity.Cinemachine.CinemachineCamera vcam;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float zoomSmoothSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 18f;
    private float targetZoom;
    private float currentZoom;


    [Header("Debug Status")]
    [SerializeField] private Vector2 movementInput;
    private PhotonView view;

    [Header("Jump")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.2f, 0.2f);
    [SerializeField] private float jumpForce = 6f;
    private bool isGrounded;
    private float gravityScale;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;

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
            vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Target.TrackingTarget = this.transform;

                targetZoom = vcam.Lens.OrthographicSize;
                currentZoom = targetZoom;
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
    public void OnZoom(InputAction.CallbackContext context)
    {
        if (!view.IsMine || vcam == null) return;

        if (context.performed)
        {
            Vector2 scroll = context.ReadValue<Vector2>();

            if (scroll.y > 0)
            {
                targetZoom -= zoomSpeed;
            }
            else if (scroll.y < 0)
            {
                targetZoom += zoomSpeed;
            }

            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
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
        if (vcam != null)
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSmoothSpeed);
            var lens = vcam.Lens;
            lens.OrthographicSize = currentZoom;
            vcam.Lens = lens;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
