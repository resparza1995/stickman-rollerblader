using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    public float moveSpeed = 5f;

    public float horizontalMovement;

    public float jumpForce = 5f;
    public float fallMultiplier = 2.5f;

    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(1.8f, 0.1f);
    public LayerMask groundLayer;
    public bool isGrounded;

    private Animator animator;
    private TricksSystem.TrickController trickController;

    [Header("Slope Settings")]
    public float slopeRotationSpeed = 15f;
    private Vector2 slopeNormal;
    private bool isOnSlope;

    private bool canRampBoost;
    private Vector2 currentRampBoostImpulse;
    private float rampBoostTimer;
    [Header("Air Rotation Settings")]
    public float spinDuration = 0.5f;
    private bool isSpinning;
    private Coroutine spinCoroutine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        trickController = GetComponent<TricksSystem.TrickController>();
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
        UpdateSlopeRotation();
        UpdateRampBoostTimer();
        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);
    }


    private void UpdateSlopeRotation()
    {
        if (isSpinning) return;

        if (isGrounded && isOnSlope)
        {
            float targetAngle = Mathf.Atan2(slopeNormal.y, slopeNormal.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * slopeRotationSpeed);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * slopeRotationSpeed);
        }
    }

    private void UpdateRampBoostTimer()
    {
        if (rampBoostTimer > 0f)
        {
            rampBoostTimer -= Time.deltaTime;
            if (rampBoostTimer <= 0f)
            {
                canRampBoost = false;
            }
        }
    }

    public void EnableRampBoost(Vector2 impulse, float duration)
    {
        canRampBoost = true;
        currentRampBoostImpulse = impulse;
        rampBoostTimer = duration;
    }

    void FixedUpdate()
    {
        // Only apply slope movement if grounded, on slope, and NOT ascending from a jump
        if (isGrounded && isOnSlope && rb.linearVelocity.y <= 0.1f)
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

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
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
        if (!context.performed)
            return;

        if (canRampBoost)
        {
            canRampBoost = false;
            rampBoostTimer = 0f;
            isGrounded = false;
            animator.SetTrigger("Jump");

            float facingDirection = transform.localScale.x > 0 ? 1f : -1f;
            Vector2 boostVelocity = new Vector2(currentRampBoostImpulse.x * facingDirection, currentRampBoostImpulse.y);
            rb.linearVelocity = boostVelocity;
            return;
        }

        if (!isGrounded)
            return;

        isGrounded = false;
        animator.SetTrigger("Jump");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public void Trick(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!isGrounded)
        {
            string controlName = context.control != null ? context.control.name : "Trick";

            if (trickController == null)
            {
                trickController = GetComponent<TricksSystem.TrickController>();
            }

            animator.SetTrigger("AirRotate");

            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
            }

            if (controlName.Equals("upArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                // Backflip (Z rotation +360)
                spinCoroutine = StartCoroutine(PerformZFlip(1f));
            }
            else if (controlName.Equals("downArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                // Frontflip (Z rotation -360)
                spinCoroutine = StartCoroutine(PerformZFlip(-1f));
            }
            else if (controlName.Equals("leftArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                // 360 Spin (Y rotation 360)
                spinCoroutine = StartCoroutine(PerformYSpin(1f));
            }
            else if (controlName.Equals("rightArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                // 360 Spin (Y rotation -360)
                spinCoroutine = StartCoroutine(PerformYSpin(-1f));
            }
            else
            {
                spinCoroutine = StartCoroutine(PerformYSpin(1f));
            }

            if (trickController != null)
            {
                bool executed = trickController.TryExecuteTrick(controlName, TricksSystem.TrickType.Air);
                if (!executed)
                {
                    trickController.TryExecuteTrick("AirRotate", TricksSystem.TrickType.Air);
                }
            }
        }
    }

    private System.Collections.IEnumerator PerformZFlip(float direction)
    {
        isSpinning = true;
        float elapsed = 0f;
        float startZAngle = transform.eulerAngles.z;

        while (elapsed < spinDuration && !isGrounded)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / spinDuration);
            float currentAngle = Mathf.Lerp(0f, 360f * direction, progress);
            transform.rotation = Quaternion.Euler(0f, 0f, startZAngle + currentAngle);
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        isSpinning = false;
        spinCoroutine = null;
    }

    private System.Collections.IEnumerator PerformYSpin(float direction)
    {
        isSpinning = true;
        float elapsed = 0f;

        while (elapsed < spinDuration && !isGrounded)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / spinDuration);
            float currentAngle = Mathf.Lerp(0f, 360f * direction, progress);
            transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        isSpinning = false;
        spinCoroutine = null;
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
