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
        
        Debug.Log("BallRayTracer initialized. Table bounds: A=" + cornerA + " B=" + cornerB + " C=" + cornerC + " D=" + cornerD);
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

        // Debug print all input parameters
        Debug.Log("=== BALL RAY TRACE STARTED ===");
        Debug.Log("Start Position: " + startPos.ToString("F3"));
        Debug.Log("Initial Direction: " + initialDirection.ToString("F3"));
        Debug.Log("Initial Speed: " + initialSpeed.ToString("F2"));
        Debug.Log("Bat Direction: " + batDir.ToString("F3"));
        Debug.Log("Bat Angle: " + batAngleVal.ToString("F2"));
        Debug.Log("Bat Speed: " + batSpeedVal.ToString("F2"));
        Debug.Log("Bat Force: " + batForceVal.ToString("F2"));

        if (!useRayTracing)
        {
            Debug.Log("Ray tracing disabled, using physics instead");
            return;
        }

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
            Debug.Log("Starting ball movement with " + tracedPath.Count + " path points");
        }
        else
        {
            Debug.LogWarning("No path points calculated! Ball will not move.");
        }
    }

    private void CalculateRayPath(Vector3 startPos, Vector3 direction, float speed)
    {
        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        int bounceCount = 0;

        tracedPath.Add(currentPos);
        Debug.Log("Path calculation started from: " + currentPos.ToString("F3") + " in direction: " + currentDir.ToString("F3"));

        while (bounceCount < maxBounces)
        {
            // Check intersection with virtual table plane
            Vector3 hitPoint;
            Vector3 hitNormal;
            bool hitTable = false;
            bool hitNet = false;

            // Check intersection with table plane (Y = cornerA.y)
            if (IntersectWithTablePlane(currentPos, currentDir, out hitPoint, out hitNormal))
            {
                Debug.Log("Table plane intersection found at: " + hitPoint.ToString("F3"));
                
                // Check if hit point is within table bounds
                if (IsPointInTableBounds(hitPoint))
                {
                    hitTable = true;
                    Debug.Log("Hit point is within table bounds");
                    
                    // Check if ball crossed the net (middle of table)
                    float netZ = (cornerA.z + cornerD.z) * 0.5f;
                    if (Mathf.Abs(hitPoint.z - netZ) < 0.1f && hitPoint.y <= netHeight)
                    {
                        hitNet = true;
                        Debug.Log("Ball hit the net at: " + hitPoint.ToString("F3"));
                        break; // Ball hit the net, stop
                    }
                }
                else
                {
                    Debug.Log("Hit point is outside table bounds");
                }
            }

            // If no table hit, check for net collision
            if (!hitTable && IntersectWithNetPlane(currentPos, currentDir, out hitPoint, out hitNormal))
            {
                hitNet = true;
                Debug.Log("Ball hit the net plane at: " + hitPoint.ToString("F3"));
                break; // Ball hit the net, stop
            }

            if (hitTable)
            {
                // Add intermediate points for smooth movement
                AddIntermediatePoints(currentPos, hitPoint);
                tracedPath.Add(hitPoint);
                debugPoints.Add(hitPoint);

                // Calculate bounce direction (reflect off table surface)
                Vector3 bounceDir = Vector3.Reflect(currentDir, Vector3.up);
                Debug.Log("Bounce #" + (bounceCount + 1) + " - Original dir: " + currentDir.ToString("F3") + " Bounce dir: " + bounceDir.ToString("F3"));

                // Apply spin if enabled
                if (enableSpin && Random.value < spinProbability)
                {
                    bounceDir = ApplySpin(bounceDir, Vector3.up);
                    Debug.Log("Spin applied. New direction: " + bounceDir.ToString("F3"));
                }

                // Apply gravity influence
                bounceDir.y -= gravityInfluence * (bounceCount + 1) * 0.1f;
                bounceDir = bounceDir.normalized;

                // Update for next iteration
                currentPos = hitPoint + Vector3.up * 0.01f; // Small offset above table
                currentDir = bounceDir;
                bounceCount++;
                
                Debug.Log("Next iteration starting from: " + currentPos.ToString("F3") + " direction: " + currentDir.ToString("F3"));
            }
            else
            {
                // No hit found, add final point in direction
                Vector3 finalPoint = currentPos + currentDir * 10f;
                AddIntermediatePoints(currentPos, finalPoint);
                tracedPath.Add(finalPoint);
                Debug.Log("No more intersections. Final point: " + finalPoint.ToString("F3"));
                break;
            }
        }
        
        Debug.Log("Path calculation completed. Total points: " + tracedPath.Count + " Bounces: " + bounceCount);
    }

    private bool IntersectWithTablePlane(Vector3 rayStart, Vector3 rayDir, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.up;

        // Table plane is at Y = cornerA.y
        float planeY = cornerA.y;
        
        // Check if ray is going towards the plane
        if (Mathf.Abs(rayDir.y) < 0.001f)
        {
            Debug.Log("Ray is parallel to table plane");
            return false; // Ray parallel to plane
        }
        
        // Calculate intersection distance
        float t = (planeY - rayStart.y) / rayDir.y;
        
        // Check if intersection is in front of ray
        if (t <= 0)
        {
            Debug.Log("Table plane intersection is behind ray start");
            return false;
        }
        
        // Calculate hit point
        hitPoint = rayStart + rayDir * t;
        Debug.Log("Table plane intersection calculated: t=" + t.ToString("F3") + " hitPoint=" + hitPoint.ToString("F3"));
        return true;
    }

    private bool IntersectWithNetPlane(Vector3 rayStart, Vector3 rayDir, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.forward;

        // Net is at the middle Z of the table
        float netZ = (cornerA.z + cornerD.z) * 0.5f;
        
        // Check if ray is going towards the net plane
        if (Mathf.Abs(rayDir.z) < 0.001f) return false; // Ray parallel to net
        
        // Calculate intersection distance
        float t = (netZ - rayStart.z) / rayDir.z;
        
        // Check if intersection is in front of ray
        if (t <= 0) return false;
        
        // Calculate hit point
        hitPoint = rayStart + rayDir * t;
        
        // Check if hit point is within net bounds (X and Y)
        bool withinX = hitPoint.x >= cornerD.x && hitPoint.x <= cornerC.x;
        bool withinY = hitPoint.y >= cornerA.y && hitPoint.y <= (cornerA.y + netHeight);
        
        return withinX && withinY;
    }

    private bool IsPointInTableBounds(Vector3 point)
    {
        // Check if point is within the rectangular table bounds
        bool withinX = point.x >= cornerD.x && point.x <= cornerC.x;
        bool withinZ = point.z >= cornerD.z && point.z <= cornerA.z;
        
        Debug.Log("Bounds check for point " + point.ToString("F3") + ": X=" + withinX + " Z=" + withinZ);
        return withinX && withinZ;
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
        
        Debug.Log("Added " + (pointCount - 1) + " intermediate points between " + start.ToString("F3") + " and " + end.ToString("F3"));
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
            Debug.Log("Ball finished following path");
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
            
            if (currentPathIndex < tracedPath.Count)
            {
                Debug.Log("Ball reached waypoint " + currentPathIndex + "/" + tracedPath.Count + " at position: " + targetPos.ToString("F3"));
            }
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

        // Draw virtual table plane (semi-transparent)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 tableCenter = (cornerA + cornerB + cornerC + cornerD) / 4f;
        Vector3 tableSize = new Vector3(
            Vector3.Distance(cornerD, cornerC),
            0.01f,
            Vector3.Distance(cornerD, cornerA)
        );
        Gizmos.DrawCube(tableCenter, tableSize);

        // Draw net line and plane
        Vector3 netStart = Vector3.Lerp(cornerA, cornerD, 0.5f);
        Vector3 netEnd = Vector3.Lerp(cornerB, cornerC, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(netStart, netEnd);
        Gizmos.DrawLine(netStart + Vector3.up * netHeight, netEnd + Vector3.up * netHeight);
        
        // Draw net plane (semi-transparent)
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Vector3 netCenter = (netStart + netEnd) / 2f + Vector3.up * (netHeight / 2f);
        Vector3 netSize = new Vector3(Vector3.Distance(netStart, netEnd), netHeight, 0.02f);
        Gizmos.DrawCube(netCenter, netSize);

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

        // Draw corner points
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(cornerA, 0.1f);
        Gizmos.DrawWireSphere(cornerB, 0.1f);
        Gizmos.DrawWireSphere(cornerC, 0.1f);
        Gizmos.DrawWireSphere(cornerD, 0.1f);
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Ball Ray Tracer Debug");
        GUILayout.Label($"Point of Contact: {pointOfContact}");
        GUILayout.Label($"Bat Direction: {batDirection}");
        GUILayout.Label($"Bat Angle: {batAngle:F2}�");
        GUILayout.Label($"Bat Speed: {batSpeed:F2}");
        GUILayout.Label($"Bat Force: {batForce:F2}");
        GUILayout.Label($"Path Points: {tracedPath.Count}");
        GUILayout.Label($"Following Path: {isFollowingPath}");
        GUILayout.Label($"Current Index: {currentPathIndex}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}