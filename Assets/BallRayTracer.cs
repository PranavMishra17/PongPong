using UnityEngine;
using System.Collections.Generic;

public class BallRayTracer : MonoBehaviour
{
    [Header("Table Geometry")]
    [SerializeField] private Vector3 cornerA = new Vector3(-7.5f, 0f, 7.5f); // Far left
    [SerializeField] private Vector3 cornerB = new Vector3(7.5f, 0f, 7.5f);  // Far right  
    [SerializeField] private Vector3 cornerC = new Vector3(7.5f, 0f, -7.5f); // Near right
    [SerializeField] private Vector3 cornerD = new Vector3(-7.5f, 0f, -7.5f); // Near left
    [SerializeField] private float netHeight = 0.15f;

    [Header("Ray Tracing Settings")]
    [SerializeField] private bool useRayTracing = true;
    [SerializeField] private float ballSpeed = 8f;
    [SerializeField] private int maxBounces = 3;
    [SerializeField] private float gravityInfluence = 0.5f;

    [Header("Spin Settings")]
    [SerializeField] private bool enableSpin = false;
    [SerializeField] private Vector2 spinAngleRange = new Vector2(5f, 15f);
    [SerializeField] private float spinProbability = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showDebugInfo = true;

    // Private variables
    private List<Vector3> tracedPath = new List<Vector3>();
    private List<Vector3> debugPoints = new List<Vector3>();
    private int currentPathIndex = 0;
    private bool isFollowingPath = false;
    private Rigidbody ballRb;

    // Debug info
    private Vector3 pointOfContact;
    private Vector3 batDirection;
    private float batAngle;
    private float batSpeed;
    private float batForce;

    private void Start()
    {
        ballRb = GetComponent<Rigidbody>();
        if (ballRb != null && useRayTracing)
        {
            ballRb.isKinematic = true; // Disable physics when using ray tracing
        }
    }

    private void Update()
    {
        if (isFollowingPath && useRayTracing)
        {
            FollowTracedPath();
        }
    }

    public void StartRayTrace(Vector3 startPos, Vector3 initialDirection, float initialSpeed, Vector3 batDir = default, float batAngleVal = 0f, float batSpeedVal = 0f, float batForceVal = 0f)
    {
        // Store debug info
        pointOfContact = startPos;
        batDirection = batDir;
        batAngle = batAngleVal;
        batSpeed = batSpeedVal;
        batForce = batForceVal;

        if (!useRayTracing) return;

        // Clear previous path
        tracedPath.Clear();
        debugPoints.Clear();
        currentPathIndex = 0;

        // Calculate complete path
        CalculateRayPath(startPos, initialDirection, initialSpeed);

        // Start following the path
        if (tracedPath.Count > 0)
        {
            isFollowingPath = true;
            transform.position = tracedPath[0];
        }
    }

    private void CalculateRayPath(Vector3 startPos, Vector3 direction, float speed)
    {
        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        int bounceCount = 0;

        tracedPath.Add(currentPos);

        while (bounceCount < maxBounces)
        {
            // Find intersection with table or net
            RaycastHit hit;
            if (Physics.Raycast(currentPos, currentDir, out hit, 50f))
            {
                if (hit.collider.CompareTag("Table") || hit.collider.CompareTag("Net"))
                {
                    // Add intersection point to path
                    Vector3 hitPoint = hit.point;

                    // Add intermediate points for smooth movement
                    AddIntermediatePoints(currentPos, hitPoint);
                    tracedPath.Add(hitPoint);
                    debugPoints.Add(hitPoint);

                    // Calculate bounce direction
                    Vector3 bounceDir = Vector3.Reflect(currentDir, hit.normal);

                    // Apply spin if enabled
                    if (enableSpin && Random.value < spinProbability)
                    {
                        bounceDir = ApplySpin(bounceDir, hit.normal);
                    }

                    // Apply gravity influence
                    bounceDir.y -= gravityInfluence * (bounceCount + 1) * 0.1f;
                    bounceDir = bounceDir.normalized;

                    // Update for next iteration
                    currentPos = hitPoint + bounceDir * 0.01f; // Small offset to avoid re-hitting
                    currentDir = bounceDir;
                    bounceCount++;

                    // Check if ball crossed net
                    if (hit.collider.CompareTag("Net"))
                    {
                        break;
                    }
                }
                else
                {
                    break; // Hit something else, stop tracing
                }
            }
            else
            {
                // No hit found, add final point in direction
                Vector3 finalPoint = currentPos + currentDir * 10f;
                AddIntermediatePoints(currentPos, finalPoint);
                tracedPath.Add(finalPoint);
                break;
            }
        }
    }

    private void AddIntermediatePoints(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        int pointCount = Mathf.CeilToInt(distance / 0.2f); // Point every 0.2 units

        for (int i = 1; i < pointCount; i++)
        {
            float t = (float)i / pointCount;
            Vector3 intermediatePoint = Vector3.Lerp(start, end, t);

            // Add slight gravity curve
            intermediatePoint.y -= gravityInfluence * t * t * 0.5f;

            tracedPath.Add(intermediatePoint);
        }
    }

    private Vector3 ApplySpin(Vector3 bounceDirection, Vector3 surfaceNormal)
    {
        // Calculate spin angle
        float spinAngle = Random.Range(spinAngleRange.x, spinAngleRange.y);
        if (Random.value < 0.5f) spinAngle = -spinAngle; // Random direction

        // Apply spin rotation around surface normal
        Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, surfaceNormal);
        return spinRotation * bounceDirection;
    }

    private void FollowTracedPath()
    {
        if (currentPathIndex >= tracedPath.Count)
        {
            isFollowingPath = false;
            return;
        }

        Vector3 targetPos = tracedPath[currentPathIndex];
        float moveDistance = ballSpeed * Time.deltaTime;

        // Move towards target position
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 newPosition = transform.position + direction * moveDistance;

        // Check if we've reached the target
        if (Vector3.Distance(transform.position, targetPos) <= moveDistance)
        {
            transform.position = targetPos;
            currentPathIndex++;
        }
        else
        {
            transform.position = newPosition;
        }
    }

    // Public methods
    public void SetUseRayTracing(bool use)
    {
        useRayTracing = use;
        if (ballRb != null)
        {
            ballRb.isKinematic = use;
        }
    }

    public void SetTableCorners(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        cornerA = a;
        cornerB = b;
        cornerC = c;
        cornerD = d;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugRays) return;

        // Draw table outline
        Gizmos.color = Color.green;
        Gizmos.DrawLine(cornerA, cornerB);
        Gizmos.DrawLine(cornerB, cornerC);
        Gizmos.DrawLine(cornerC, cornerD);
        Gizmos.DrawLine(cornerD, cornerA);

        // Draw net line
        Vector3 netStart = Vector3.Lerp(cornerA, cornerD, 0.5f);
        Vector3 netEnd = Vector3.Lerp(cornerB, cornerC, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(netStart, netEnd);
        Gizmos.DrawLine(netStart + Vector3.up * netHeight, netEnd + Vector3.up * netHeight);

        // Draw traced path
        if (tracedPath.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < tracedPath.Count - 1; i++)
            {
                Gizmos.DrawLine(tracedPath[i], tracedPath[i + 1]);
            }
        }

        // Draw bounce points
        Gizmos.color = Color.red;
        foreach (Vector3 point in debugPoints)
        {
            Gizmos.DrawWireSphere(point, 0.1f);
        }

        // Draw point of contact
        if (pointOfContact != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(pointOfContact, 0.15f);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Ball Ray Tracer Debug");
        GUILayout.Label($"Point of Contact: {pointOfContact}");
        GUILayout.Label($"Bat Direction: {batDirection}");
        GUILayout.Label($"Bat Angle: {batAngle:F2}°");
        GUILayout.Label($"Bat Speed: {batSpeed:F2}");
        GUILayout.Label($"Bat Force: {batForce:F2}");
        GUILayout.Label($"Path Points: {tracedPath.Count}");
        GUILayout.Label($"Following Path: {isFollowingPath}");
        GUILayout.Label($"Current Index: {currentPathIndex}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}