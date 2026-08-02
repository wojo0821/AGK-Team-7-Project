using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMove : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float speed = 10f;

    private Rigidbody2D rb;
    [Header("Debug Status")]
    [SerializeField] private Vector2 movementInput;
    private PhotonView view;
    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
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
}
