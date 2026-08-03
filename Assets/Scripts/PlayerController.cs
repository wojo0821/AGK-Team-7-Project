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

    
    [Header("Debug Status")]
    [SerializeField] private Vector2 movementInput;
    private PhotonView view;
    
    [Header("Jump")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float jumpForce = 6f;
    private bool isGrounded;
    private float gravityScale;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (view.IsMine)
        {
            movementInput = context.ReadValue<Vector2>();
        }
        if (movementInput.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (movementInput.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, LayerMask.GetMask("Ground"));
        if (view.IsMine)
        {
            if (context.started && isGrounded)
            {
                rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                rb.gravityScale = gravityScale * 0.5f;
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
                speed *= 1.5f;
            }
            else if (context.canceled)
            {
                speed /= 1.5f;
            }
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(movementInput.x * speed, rb.linearVelocity.y);
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetInteger("X", (int)movementInput.x);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
    }
}
