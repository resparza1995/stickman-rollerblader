using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    public float moveSpeed = 5f;

    public float horizontalMovement;

    public float jumpForce = 5f;

    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(1.8f, 0.1f);
    public LayerMask groundLayer;
    public bool isGrounded;

    private Animator animator;

    private Vector2 slopeNormal;
    private bool isOnSlope;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        checkFlip();
        UpdateGroundedState();
        CheckSlope();
        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetBool("IsGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        if (isGrounded && isOnSlope)
        {
            Vector2 slopeDirection = Vector2.Perpendicular(slopeNormal).normalized;
            if (slopeDirection.x < 0)
            {
                slopeDirection = -slopeDirection;
            }

            Vector2 moveVelocity = slopeDirection * (horizontalMovement * moveSpeed);
            rb.linearVelocity = moveVelocity;
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void CheckSlope()
    {
        if (groundCheck == null) return;

        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckSize.y * 2f + 0.2f, groundLayer);
        if (hit)
        {
            slopeNormal = hit.normal;
            isOnSlope = slopeNormal != Vector2.up && Mathf.Abs(slopeNormal.x) > 0.01f;
        }
        else
        {
            isOnSlope = false;
            slopeNormal = Vector2.up;
        }
    }

    public void Move(InputAction.CallbackContext context) 
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context) 
    {
        if (!context.performed || !isGrounded)
            return;

        animator.SetTrigger("Jump");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void OnDrawGizmosSelected() 
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

    public void UpdateGroundedState()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
    }

    public void checkFlip() 
    {
        if (horizontalMovement == 0) 
            return;

        bool movingRight = horizontalMovement > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight) 
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}
