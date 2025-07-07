using UnityEngine;

public class TableTennisBat : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float movementSmoothTime = 0.05f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Movement Constraints")]
    [SerializeField] private float zMin = -15f;
    [SerializeField] private float zMax = 15f;
    [SerializeField] private float yMin = -14f;
    [SerializeField] private float yMax = -10f;

    [Header("Rotation Settings")]
    [SerializeField] private float maxZRotation = 70f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Flip Settings")]
    [SerializeField] private float flipTriggerOffset = 0.1f;
    [SerializeField] private float flipRotationAmount = 180f;
    [SerializeField] private float flipDuration = 0.3f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Private variables
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3 startPosition;

    private float targetZRotation;
    private float currentZRotation;
    private float rotationVelocity;

    // Flip mechanics
    private float previousZ;
    private bool hasFlippedFromNegative = false;
    private bool hasFlippedFromPositive = false;
    private bool isFlipping = false;
    private float flipTimer = 0f;
    private float flipStartRotation;

    private void Start()
    {
        startPosition = transform.localPosition;
        targetPosition = startPosition;
        previousZ = startPosition.z;

        // Initialize rotation
        currentZRotation = 0f;
        targetZRotation = 0f;
    }

    private void Update()
    {
        HandleMouseInput();
        UpdateMovement();
        UpdateRotation();
        HandleFlipLogic();
    }

    private void HandleMouseInput()
    {
        if (!isFlipping) // Don't process input during flip
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Update target position
            Vector3 newTarget = targetPosition;
            newTarget.z -= mouseX; // Fix: Invert Z movement
            newTarget.y += mouseY; // Fix: Remove inversion

            // Apply constraints
            newTarget.z = Mathf.Clamp(newTarget.z, zMin, zMax);
            newTarget.y = Mathf.Clamp(newTarget.y, yMin, yMax);

            targetPosition = newTarget;
        }
    }

    private void UpdateMovement()
    {
        // Calculate movement progress for curve
        Vector3 moveDirection = targetPosition - transform.localPosition;
        float moveDistance = moveDirection.magnitude;
        float maxDistance = Vector3.Distance(new Vector3(0, yMin, zMin), new Vector3(0, yMax, zMax));
        float normalizedDistance = Mathf.Clamp01(moveDistance / maxDistance);

        // Apply movement curve
        float curveMultiplier = movementCurve.Evaluate(normalizedDistance);
        float adjustedSmoothTime = movementSmoothTime * (1f - curveMultiplier * 0.7f);

        // Smooth movement
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref currentVelocity,
            adjustedSmoothTime
        );
    }

    private void UpdateRotation()
    {
        if (!isFlipping)
        {
            // Calculate target Z rotation based on Z position
            float zNormalized = Mathf.InverseLerp(zMin, zMax, transform.localPosition.z);
            targetZRotation = Mathf.Lerp(-maxZRotation, maxZRotation, zNormalized);

            // Smooth rotation
            currentZRotation = Mathf.SmoothDamp(
                currentZRotation,
                targetZRotation,
                ref rotationVelocity,
                rotationSmoothTime
            );

            // Apply rotation (keeping Y at 90)
            transform.localRotation = Quaternion.Euler(0f, 90f, currentZRotation);
        }
        else
        {
            // Handle flip animation
            flipTimer += Time.deltaTime;
            float flipProgress = flipTimer / flipDuration;

            if (flipProgress >= 1f)
            {
                // Flip complete
                isFlipping = false;
                flipTimer = 0f;
            }
            else
            {
                // Animate flip on X axis
                float curveValue = flipCurve.Evaluate(flipProgress);
                float currentFlipRotation = flipStartRotation + (flipRotationAmount * curveValue);

                transform.localRotation = Quaternion.Euler(
                    currentFlipRotation,
                    90f,
                    currentZRotation
                );
            }
        }
    }

    private void HandleFlipLogic()
    {
        float currentZ = transform.localPosition.z;

        // Check for crossing z=0 with safeguards
        bool crossedToPositive = previousZ < -flipTriggerOffset && currentZ > flipTriggerOffset;
        bool crossedToNegative = previousZ > flipTriggerOffset && currentZ < -flipTriggerOffset;

        // Trigger flip if crossed and haven't flipped recently in this direction
        if (crossedToPositive && !hasFlippedFromNegative)
        {
            TriggerFlip();
            hasFlippedFromNegative = true;
            hasFlippedFromPositive = false;
        }
        else if (crossedToNegative && !hasFlippedFromPositive)
        {
            TriggerFlip();
            hasFlippedFromPositive = true;
            hasFlippedFromNegative = false;
        }

        // Reset flip flags when moving away from center
        if (Mathf.Abs(currentZ) > flipTriggerOffset * 3f)
        {
            if (currentZ > 0)
                hasFlippedFromNegative = false;
            else
                hasFlippedFromPositive = false;
        }

        previousZ = currentZ;
    }

    private void TriggerFlip()
    {
        if (!isFlipping)
        {
            isFlipping = true;
            flipTimer = 0f;
            flipStartRotation = 0f; // Start from current relative rotation
        }
    }

    // Public methods for runtime adjustment
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    public void SetMovementConstraints(float zMinVal, float zMaxVal, float yMinVal, float yMaxVal)
    {
        zMin = zMinVal;
        zMax = zMaxVal;
        yMin = yMinVal;
        yMax = yMaxVal;
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.localPosition;

        // Draw movement bounds
        Gizmos.color = Color.cyan;

        // Z bounds
        Vector3 leftBound = new Vector3(center.x, center.y, zMin);
        Vector3 rightBound = new Vector3(center.x, center.y, zMax);
        Gizmos.DrawLine(leftBound, rightBound);

        // Y bounds  
        Vector3 topBound = new Vector3(center.x, yMax, center.z);
        Vector3 bottomBound = new Vector3(center.x, yMin, center.z);
        Gizmos.DrawLine(topBound, bottomBound);

        // Draw flip trigger zones
        Gizmos.color = Color.red;
        Vector3 flipZonePos = new Vector3(center.x, center.y, flipTriggerOffset);
        Vector3 flipZoneNeg = new Vector3(center.x, center.y, -flipTriggerOffset);
        Gizmos.DrawWireSphere(flipZonePos, 0.2f);
        Gizmos.DrawWireSphere(flipZoneNeg, 0.2f);

        // Draw movement area
        Gizmos.color = Color.yellow;
        Vector3 size = new Vector3(0.1f, yMax - yMin, zMax - zMin);
        Vector3 boundsCenter = new Vector3(center.x, (yMax + yMin) * 0.5f, (zMax + zMin) * 0.5f);
        Gizmos.DrawWireCube(boundsCenter, size);
    }
}