using UnityEngine;

public class CameraBatFollow : MonoBehaviour
{
    [Header("Bat Reference")]
    [SerializeField] private GameObject batGameObject;
    [SerializeField] private TableTennisBat batScript;

    [Header("Camera Movement Settings")]
    [SerializeField] private float movementFollowPercentage = 0.8f;
    [SerializeField] private float movementSmoothTime = 0.2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Rotation Settings")]
    [SerializeField] private float maxCameraRotation = 15f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Private variables
    private float targetYRotation;
    private float currentYRotation;
    private float rotationVelocity;

    private Vector3 targetPosition;
    private Vector3 currentPositionVelocity;
    private Vector3 originalRotation;
    private Vector3 originalPosition;

    private void Start()
    {
        // Get bat script component if not assigned
        if (batGameObject != null && batScript == null)
        {
            batScript = batGameObject.GetComponent<TableTennisBat>();
        }

        // Store original camera rotation and position
        originalRotation = transform.localEulerAngles;
        originalPosition = transform.localPosition;
        currentYRotation = originalRotation.y;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        if (batScript != null && batGameObject != null)
        {
            UpdateCameraMovement();
            UpdateCameraRotation();
        }
    }

    private void UpdateCameraMovement()
    {
        // Get bat's current Z position
        float batZ = batGameObject.transform.localPosition.z;

        // Calculate target camera Z position (80% of bat movement)
        float targetZ = originalPosition.z + (batZ * movementFollowPercentage);

        // Apply movement curve
        float batZMin = -15f;
        float batZMax = 15f;
        float normalizedBatZ = Mathf.InverseLerp(batZMin, batZMax, Mathf.Abs(batZ));
        float curveValue = movementCurve.Evaluate(normalizedBatZ);

        // Update target position
        targetPosition = new Vector3(originalPosition.x, originalPosition.y, targetZ);

        // Smooth movement with curve influence
        float adjustedSmoothTime = movementSmoothTime * (1f - curveValue * 0.3f);
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref currentPositionVelocity,
            adjustedSmoothTime
        );
    }

    private void UpdateCameraRotation()
    {
        // Get bat's current Z position
        float batZ = batGameObject.transform.localPosition.z;

        // Get bat's Z constraints
        float batZMin = -15f;
        float batZMax = 15f;

        // Normalize bat Z position (0 to 1)
        float normalizedBatZ = Mathf.InverseLerp(batZMin, batZMax, batZ);

        // Apply rotation curve
        float curveValue = rotationCurve.Evaluate(normalizedBatZ);

        // Calculate target camera rotation (OPPOSITE direction for realistic head movement)
        // When bat goes right (+z), camera turns left (-y rotation) toward center
        targetYRotation = originalRotation.y + Mathf.Lerp(-maxCameraRotation, maxCameraRotation, curveValue);

        // Smooth rotation
        currentYRotation = Mathf.SmoothDamp(
            currentYRotation,
            targetYRotation,
            ref rotationVelocity,
            rotationSmoothTime
        );

        // Apply rotation to camera
        transform.localRotation = Quaternion.Euler(
            originalRotation.x,
            currentYRotation,
            originalRotation.z
        );
    }

    // Public methods for runtime adjustment
    public void SetBatReference(GameObject bat)
    {
        batGameObject = bat;
        if (bat != null)
        {
            batScript = bat.GetComponent<TableTennisBat>();
        }
    }

    public void SetMovementFollowPercentage(float percentage)
    {
        movementFollowPercentage = Mathf.Clamp01(percentage);
    }

    public void SetMaxRotation(float maxRotation)
    {
        maxCameraRotation = maxRotation;
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Draw movement range
            Gizmos.color = Color.cyan;
            Vector3 leftPos = originalPosition + Vector3.forward * (-15f * movementFollowPercentage);
            Vector3 rightPos = originalPosition + Vector3.forward * (15f * movementFollowPercentage);
            Gizmos.DrawLine(leftPos, rightPos);
            Gizmos.DrawWireCube(leftPos, Vector3.one * 0.3f);
            Gizmos.DrawWireCube(rightPos, Vector3.one * 0.3f);

            // Draw rotation range
            Gizmos.color = Color.magenta;
            Vector3 leftRotation = Quaternion.Euler(0, -maxCameraRotation, 0) * transform.forward;
            Vector3 rightRotation = Quaternion.Euler(0, maxCameraRotation, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftRotation * 3f);
            Gizmos.DrawRay(transform.position, rightRotation * 3f);
        }
    }
}