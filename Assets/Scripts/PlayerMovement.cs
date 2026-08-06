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
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.15f);
    public LayerMask groundLayer;
    public float slopeCheckRadius = 0.2f;
    public bool isGrounded;

    private Animator animator;
    private TricksSystem.TrickController trickController;

    [Header("Slope Settings")]
    public float slopeRotationSpeed = 25f;
    private Vector2 slopeNormal;
    private bool isOnSlope;

    private bool canRampBoost;
    private Vector2 currentRampBoostImpulse;
    private float rampBoostTimer;
    private float initialGravityScale;
    private float jumpCooldownTimer;
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
            initialGravityScale = rb.gravityScale;
        }
    }

    void Update()
    {
        checkFlip();
        
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
            isGrounded = false;
            isOnSlope = false;
        }
        else
        {
            UpdateGroundedState();
        }

        UpdateRampBoostTimer();
        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void LateUpdate()
    {
        UpdateSlopeRotation();
    }


    private void UpdateSlopeRotation()
    {
        if (isSpinning) return;

        if (isGrounded && isOnSlope)
        {
            float targetAngle = Mathf.Atan2(-slopeNormal.x, slopeNormal.y) * Mathf.Rad2Deg;
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
        rb.angularVelocity = 0f;

        if (isGrounded && isOnSlope)
        {
            rb.gravityScale = 0f;
            Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x);
            rb.linearVelocity = slopeDirection * (horizontalMovement * moveSpeed);
        }
        else
        {
            rb.gravityScale = initialGravityScale;
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        }

        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    private System.Collections.Generic.List<ContactPoint2D> contactsList = new System.Collections.Generic.List<ContactPoint2D>();

    public void UpdateGroundedState()
    {
        bool grounded = false;
        bool slope = false;
        Vector2 bestNormal = Vector2.up;

        if (rb != null)
        {
            int count = rb.GetContacts(contactsList);
            for (int i = 0; i < count; i++)
            {
                Vector2 n = contactsList[i].normal;
                if (n.y < 0) n = -n;

                if (n.y > 0.05f)
                {
                    grounded = true;
                    if (Mathf.Abs(n.x) > 0.01f)
                    {
                        slope = true;
                        bestNormal = n;
                        break;
                    }
                    else
                    {
                        bestNormal = n;
                    }
                }
            }
        }

        if (!grounded && groundCheck != null)
        {
            grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer)
                    || Physics2D.OverlapCircle(groundCheck.position, slopeCheckRadius, groundLayer);
        }

        isGrounded = grounded;
        isOnSlope = slope;
        slopeNormal = bestNormal;
    }

    private void CheckSlope()
    {
        // Unified into UpdateGroundedState using rb.GetContacts
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
        isOnSlope = false;
        jumpCooldownTimer = 0.2f;
        animator.SetTrigger("Jump");

        if (isOnSlope)
        {
            float upVelocity = Mathf.Max(jumpForce, rb.linearVelocity.y + jumpForce * 0.5f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, upVelocity);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void Trick(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!isGrounded)
        {
            string controlName = context.control != null ? context.control.name : "";

            bool isArrowKey = controlName.Equals("upArrow", System.StringComparison.OrdinalIgnoreCase)
                           || controlName.Equals("downArrow", System.StringComparison.OrdinalIgnoreCase)
                           || controlName.Equals("leftArrow", System.StringComparison.OrdinalIgnoreCase)
                           || controlName.Equals("rightArrow", System.StringComparison.OrdinalIgnoreCase);

            if (!isArrowKey) return;

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
            Gizmos.matrix = Matrix4x4.TRS(groundCheck.position, groundCheck.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, groundCheckSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Vector3.zero, slopeCheckRadius);
            Gizmos.matrix = Matrix4x4.identity;
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
