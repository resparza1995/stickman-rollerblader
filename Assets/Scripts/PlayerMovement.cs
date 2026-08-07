using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls player movement, physics, slope alignment, rail grinding, halfpipe launches, and trick executions.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    public float moveSpeed = 5f;

    public float horizontalMovement;

    public float jumpForce = 5f;
    public float rampJumpForce = 12f;
    public float fallMultiplier = 2.5f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    public bool isGrounded;

    private Animator animator;
    private TricksSystem.TrickController trickController;

    [Header("Grind Settings")]
    public bool isGrinding;
    public int currentGrindType = 1; // 1: Royal, 2: Savannah, 3: Soul
    public ParticleSystem grindSparksEffect;

    [Header("Slope Settings")]
    public float slopeRotationSpeed = 25f;
    private Vector2 slopeNormal;
    private bool isOnSlope;
    private bool isHalfpipe;
    private bool isVerticalAir;

    private bool canRampBoost;
    private Vector2 currentRampBoostImpulse;
    private float rampBoostTimer;
    private float initialGravityScale;
    private float jumpCooldownTimer;
    [Header("Air Rotation Settings")]
    public float spinDuration = 0.5f;
    private bool isSpinning;
    private Coroutine spinCoroutine;

    [Header("Speed Visual Effects")]
    public ParticleSystem speedEffect;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        trickController = GetComponent<TricksSystem.TrickController>();
        if (rb != null)
        {
            initialGravityScale = rb.gravityScale;
        }

        if (speedEffect == null)
        {
            Transform found = transform.Find("SpeedEffect");
            if (found != null) speedEffect = found.GetComponent<ParticleSystem>();
        }

        SetupGrindSparks();
    }

    private void SetupGrindSparks()
    {
        if (grindSparksEffect != null) return;
        Transform found = transform.Find("GrindSparks");
        if (found == null) found = transform.Find("GrindEffect");
        if (found != null) grindSparksEffect = found.GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Checks if player movement and actions are currently locked by the CountdownManager.
    /// </summary>
    public bool IsMovementLocked()
    {
        return UISystem.CountdownManager.Instance != null && UISystem.CountdownManager.Instance.IsCountingDown;
    }

    /// <summary>
    /// Per-frame update for input checks, cooldown timers, and animator parameter synchronization.
    /// </summary>
    void Update()
    {
        if (IsMovementLocked())
        {
            horizontalMovement = 0f;
            if (animator != null)
            {
                animator.SetFloat("Horizontal", 0f);
            }
            return;
        }

        checkFlip();
        CheckGrindInput();
        
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
            isGrounded = false;
            isOnSlope = false;
            isGrinding = false;
        }
        else
        {
            UpdateGroundedState();
        }

        UpdateRampBoostTimer();
        UpdateSpeedEffect();
        UpdateGrindSparksEffect();
        if (animator != null)
        {
            animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsGrinding", isGrinding);
        }
    }

    /// <summary>
    /// Enables golden spark particle emission at the skates while grinding on rails.
    /// </summary>
    private void UpdateGrindSparksEffect()
    {
        if (grindSparksEffect == null) return;

        var emission = grindSparksEffect.emission;
        if (isGrinding)
        {
            if (!emission.enabled)
            {
                emission.enabled = true;
                if (!grindSparksEffect.isPlaying) grindSparksEffect.Play();
            }
        }
        else
        {
            if (emission.enabled)
            {
                emission.enabled = false;
            }
        }
    }

    /// <summary>
    /// Controls emission of the user's SpeedEffect particle system based on skater movement and velocity.
    /// </summary>
    private void UpdateSpeedEffect()
    {
        if (speedEffect == null || rb == null) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        bool isMovingFast = (isGrounded && Mathf.Abs(horizontalMovement) > 0.1f) || isGrinding || currentSpeed >= 4f;
        var emission = speedEffect.emission;

        if (isMovingFast && currentSpeed > 0.5f)
        {
            if (!emission.enabled)
            {
                emission.enabled = true;
                if (!speedEffect.isPlaying) speedEffect.Play();
            }
            float t = Mathf.Clamp01((currentSpeed - 3f) / 10f);
            emission.rateOverTime = Mathf.Lerp(15f, 50f, t);
        }
        else
        {
            if (emission.enabled)
            {
                emission.enabled = false;
            }
        }
    }

    /// <summary>
    /// Reads direct number key input (1, 2, 3 or Numpad 1, 2, 3) to switch grind stances.
    /// </summary>
    private void CheckGrindInput()
    {
        if (Keyboard.current == null || IsMovementLocked()) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            SetGrindStance(1, "Royal");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            SetGrindStance(2, "Savannah");
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            SetGrindStance(3, "Soul");
        }
    }

    /// <summary>
    /// Updates the active grind stance (1: Royal, 2: Savannah, 3: Soul) and triggers animator state and score execution when grinding.
    /// </summary>
    private void SetGrindStance(int type, string trickName)
    {
        currentGrindType = type;
        if (!isGrinding) return;

        if (animator != null)
        {
            animator.SetTrigger(trickName);
            animator.Play(trickName);
        }

        bool executed = false;
        if (trickController != null)
        {
            executed = trickController.TryExecuteTrick(trickName, TricksSystem.TrickType.Grind);
        }

        if (!executed && UISystem.ScoreManager.Instance != null)
        {
            int pts = type == 1 ? 200 : (type == 2 ? 250 : 300);
            UISystem.ScoreManager.Instance.AddScore(trickName + " Grind", pts);
        }
    }

    private void LateUpdate()
    {
        UpdateSlopeRotation();
    }

    /// <summary>
    /// Interpolates character rotation smoothly to match slope or rail normal inclination.
    /// </summary>
    private void UpdateSlopeRotation()
    {
        if (isSpinning) return;

        if (isGrounded && (isOnSlope || isGrinding))
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

    /// <summary>
    /// Executes physics velocity updates for normal movement, slope sliding, zero-gravity grinding, and dynamic fall gravity.
    /// </summary>
    void FixedUpdate()
    {
        rb.angularVelocity = 0f;

        if (isGrinding)
        {
            rb.gravityScale = 0f;
            float moveDir = horizontalMovement != 0 ? horizontalMovement : (transform.localScale.x > 0 ? 1f : -1f);
            if (isOnSlope)
            {
                Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x);
                rb.linearVelocity = slopeDirection * (moveDir * moveSpeed);
            }
            else
            {
                rb.linearVelocity = new Vector2(moveDir * moveSpeed, 0f);
            }
        }
        else if (isGrounded && isOnSlope)
        {
            rb.gravityScale = 0f;
            Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;
            float projectedSpeed = Vector2.Dot(rb.linearVelocity, slopeDirection);

            float speed;
            if (horizontalMovement != 0)
            {
                float directionSign = Mathf.Sign(horizontalMovement);
                float speedMag = Mathf.Max(moveSpeed, Mathf.Abs(projectedSpeed));
                speed = directionSign * speedMag;
            }
            else
            {
                speed = projectedSpeed;
            }

            rb.linearVelocity = slopeDirection * speed;
        }
        else if (isVerticalAir)
        {
            rb.gravityScale = initialGravityScale;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            rb.gravityScale = initialGravityScale;
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        }

        if (!isGrounded && !isGrinding && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    private System.Collections.Generic.List<ContactPoint2D> contactsList = new System.Collections.Generic.List<ContactPoint2D>();

    public void UpdateGroundedState()
    {
        bool grounded = false;
        bool slope = false;
        bool halfpipe = false;
        bool grinding = false;
        Vector2 bestNormal = Vector2.up;

        if (rb != null)
        {
            int count = rb.GetContacts(contactsList);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D cp = contactsList[i];
                Vector2 n = cp.normal;
                if (n.y < 0) n = -n;

                if (n.y > 0.05f)
                {
                    grounded = true;
                    if (cp.collider != null)
                    {
                        if (cp.collider.CompareTag("Halfpipe"))
                        {
                            halfpipe = true;
                        }
                        if (cp.collider.CompareTag("Rail") || cp.collider.GetComponent<ObstaclesSystem.IRailObstacle>() != null)
                        {
                            grinding = true;
                        }
                    }

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
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (hit != null)
            {
                grounded = true;
                if (hit.CompareTag("Halfpipe"))
                {
                    halfpipe = true;
                }
                if (hit.CompareTag("Rail") || hit.GetComponent<ObstaclesSystem.IRailObstacle>() != null)
                {
                    grinding = true;
                }
            }
        }

        if (!isGrinding && grinding)
        {
            string trickName = currentGrindType == 2 ? "Savannah" : (currentGrindType == 3 ? "Soul" : "Royal");
            if (animator != null)
            {
                animator.SetTrigger(trickName);
                animator.Play(trickName);
            }
            if (trickController == null)
            {
                trickController = GetComponent<TricksSystem.TrickController>();
            }
            if (trickController != null)
            {
                trickController.TryExecuteTrick(trickName, TricksSystem.TrickType.Grind);
            }
        }

        isGrounded = grounded;
        isOnSlope = slope;
        isHalfpipe = halfpipe;
        isGrinding = grinding;
        slopeNormal = bestNormal;

        if (isGrounded)
        {
            isVerticalAir = false;
        }
    }

    private void CheckSlope()
    {
        // Unified into UpdateGroundedState using rb.GetContacts
    }

    public void Move(InputAction.CallbackContext context) 
    {
        if (IsMovementLocked())
        {
            horizontalMovement = 0f;
            return;
        }
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context) 
    {
        if (!context.performed || IsMovementLocked())
            return;

        if (canRampBoost)
        {
            canRampBoost = false;
            rampBoostTimer = 0f;
            isGrounded = false;
            isGrinding = false;
            if (animator != null)
            {
                animator.SetTrigger("Jump");
                animator.Play("Jump");
            }

            float facingDirection = transform.localScale.x > 0 ? 1f : -1f;
            Vector2 boostVelocity = new Vector2(currentRampBoostImpulse.x * facingDirection, currentRampBoostImpulse.y);
            rb.linearVelocity = boostVelocity;
            return;
        }

        if (!isGrounded && !isGrinding)
            return;

        bool wasSlope = isOnSlope;
        bool wasHalfpipe = isHalfpipe;

        isGrounded = false;
        isOnSlope = false;
        isGrinding = false;
        if (animator != null)
        {
            animator.SetTrigger("Jump");
            animator.Play("Jump");
        }

        if (wasSlope && wasHalfpipe)
        {
            isVerticalAir = true;
            jumpCooldownTimer = 0.35f;
            float launchY = Mathf.Max(rampJumpForce, rb.linearVelocity.y + rampJumpForce * 0.4f);
            rb.linearVelocity = new Vector2(0f, launchY);
        }
        else if (wasSlope)
        {
            jumpCooldownTimer = 0.2f;
            float launchY = Mathf.Max(rampJumpForce, rb.linearVelocity.y + rampJumpForce * 0.5f);
            float launchX = horizontalMovement * (moveSpeed * 0.25f);
            rb.linearVelocity = new Vector2(launchX, launchY);
        }
        else
        {
            jumpCooldownTimer = 0.15f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void Trick(InputAction.CallbackContext context)
    {
        if (!context.performed || IsMovementLocked())
            return;

        string controlName = context.control != null ? context.control.name : "";

        if (isGrinding)
        {
            if (controlName.Equals("1", System.StringComparison.OrdinalIgnoreCase)
             || controlName.Equals("digit1", System.StringComparison.OrdinalIgnoreCase)
             || controlName.Equals("numpad1", System.StringComparison.OrdinalIgnoreCase))
            {
                SetGrindStance(1, "Royal");
            }
            else if (controlName.Equals("2", System.StringComparison.OrdinalIgnoreCase)
                  || controlName.Equals("digit2", System.StringComparison.OrdinalIgnoreCase)
                  || controlName.Equals("numpad2", System.StringComparison.OrdinalIgnoreCase))
            {
                SetGrindStance(2, "Savannah");
            }
            else if (controlName.Equals("3", System.StringComparison.OrdinalIgnoreCase)
                  || controlName.Equals("digit3", System.StringComparison.OrdinalIgnoreCase)
                  || controlName.Equals("numpad3", System.StringComparison.OrdinalIgnoreCase))
            {
                SetGrindStance(3, "Soul");
            }
            return;
        }

        if (!isGrounded)
        {
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

            string airTrickName = "360";
            int basePoints = 300;

            if (controlName.Equals("upArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                airTrickName = "Backflip";
                basePoints = 450;
                spinCoroutine = StartCoroutine(PerformZFlip(1f));
            }
            else if (controlName.Equals("downArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                airTrickName = "Frontflip";
                basePoints = 450;
                spinCoroutine = StartCoroutine(PerformZFlip(-1f));
            }
            else if (controlName.Equals("leftArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                airTrickName = "360";
                basePoints = 300;
                spinCoroutine = StartCoroutine(PerformYSpin(1f));
            }
            else if (controlName.Equals("rightArrow", System.StringComparison.OrdinalIgnoreCase))
            {
                airTrickName = "360";
                basePoints = 300;
                spinCoroutine = StartCoroutine(PerformYSpin(-1f));
            }

            bool executed = false;
            if (trickController != null)
            {
                executed = trickController.TryExecuteTrick(controlName, TricksSystem.TrickType.Air);
            }

            if (!executed && UISystem.ScoreManager.Instance != null)
            {
                UISystem.ScoreManager.Instance.AddScore(airTrickName, basePoints);
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
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.TRS(groundCheck.position, groundCheck.rotation, Vector3.one);
            Gizmos.DrawWireSphere(Vector3.zero, groundCheckRadius);
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
