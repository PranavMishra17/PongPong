using UnityEngine;

public class TableTennisCameraController : MonoBehaviour
{
    [Header("Breathing/Bobbing Settings")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobbingFrequency = 1.2f;
    [SerializeField] private float bobbingAmplitude = 0.03f;
    [SerializeField] private AnimationCurve bobbingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Movement Settings")]
    [SerializeField] private KeyCode forwardKey = KeyCode.W;
    [SerializeField] private KeyCode backwardKey = KeyCode.S;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode toggleBobbingKey = KeyCode.B;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 2.5f;
    [SerializeField] private float movementSmoothTime = 0.1f;
    [SerializeField] private float returnTocenterSpeed = 1.5f;
    [SerializeField] private float maxMoveDistance = 3f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Private variables
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private float bobbingTimer = 0f;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        HandleMovementInput();
        UpdateMovement();
        UpdateBobbing();
        HandleToggleInput();
    }

    private void HandleMovementInput()
    {
        float moveInput = 0f;

        // Get W/S input
        if (Input.GetKey(forwardKey)) moveInput += 1f;
        if (Input.GetKey(backwardKey)) moveInput -= 1f;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Check if sprinting
            bool isSprinting = Input.GetKey(sprintKey);
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

            // Calculate target position in X direction (left/right movement for table tennis)
            Vector3 moveDirection = transform.right * moveInput;
            Vector3 newTargetPosition = targetPosition + moveDirection * currentSpeed * Time.deltaTime;

            // Apply movement constraints in X direction
            Vector3 offsetFromOriginal = newTargetPosition - originalPosition;
            float distanceFromOriginal = Vector3.Dot(offsetFromOriginal, transform.right);

            // Clamp distance to maxMoveDistance
            if (Mathf.Abs(distanceFromOriginal) > maxMoveDistance)
            {
                distanceFromOriginal = Mathf.Sign(distanceFromOriginal) * maxMoveDistance;
                newTargetPosition = originalPosition + transform.right * distanceFromOriginal;
            }

            targetPosition = newTargetPosition;
        }
        else
        {
            // Spring back to center when no input
            targetPosition = Vector3.Lerp(targetPosition, originalPosition, returnTocenterSpeed * Time.deltaTime);
        }
    }

    private void UpdateMovement()
    {
        // Get distance from original position to calculate curve value
        Vector3 offsetFromOriginal = targetPosition - originalPosition;
        float distanceFromOriginal = Vector3.Dot(offsetFromOriginal, transform.right);
        float normalizedDistance = Mathf.Abs(distanceFromOriginal) / maxMoveDistance;

        // Apply movement curve for smooth acceleration/deceleration
        float curveValue = movementCurve.Evaluate(normalizedDistance);

        // Smooth movement to target position with curve-based easing
        Vector3 basePosition = Vector3.SmoothDamp(
            transform.localPosition - GetBobbingOffset(),
            targetPosition,
            ref currentVelocity,
            movementSmoothTime * (1f - curveValue * 0.5f) // Faster when further out
        );

        // Apply final position with bobbing
        transform.localPosition = basePosition + GetBobbingOffset();
    }

    private void UpdateBobbing()
    {
        if (enableBobbing)
        {
            bobbingTimer += Time.deltaTime * bobbingFrequency;

            if (bobbingTimer > Mathf.PI * 2)
            {
                bobbingTimer -= Mathf.PI * 2;
            }
        }
    }

    private Vector3 GetBobbingOffset()
    {
        if (!enableBobbing) return Vector3.zero;

        float sineValue = Mathf.Sin(bobbingTimer);
        float curveValue = bobbingCurve.Evaluate((sineValue + 1f) * 0.5f);
        float bobbingY = (curveValue - 0.5f) * 2f * bobbingAmplitude;

        return new Vector3(0, bobbingY, 0);
    }

    private void HandleToggleInput()
    {
        if (Input.GetKeyDown(toggleBobbingKey))
        {
            enableBobbing = !enableBobbing;
            if (!enableBobbing) bobbingTimer = 0f;
        }
    }

    // Public methods for runtime adjustment
    public void SetBobbingEnabled(bool enabled)
    {
        enableBobbing = enabled;
        if (!enabled) bobbingTimer = 0f;
    }

    public void SetBobbingFrequency(float frequency)
    {
        bobbingFrequency = frequency;
    }

    public void SetBobbingAmplitude(float amplitude)
    {
        bobbingAmplitude = amplitude;
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        Vector3 basePos = Application.isPlaying ? originalPosition : transform.localPosition;

        // Movement constraints in X direction (left/right for table tennis)
        Gizmos.color = Color.yellow;
        Vector3 rightConstraint = basePos + transform.right * maxMoveDistance;
        Vector3 leftConstraint = basePos - transform.right * maxMoveDistance;

        Gizmos.DrawLine(leftConstraint, rightConstraint);
        Gizmos.DrawWireCube(rightConstraint, Vector3.one * 0.2f);
        Gizmos.DrawWireCube(leftConstraint, Vector3.one * 0.2f);

        // Original position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(basePos, 0.1f);
    }
}