using UnityEngine;
using System.Collections;

public class BallThrower : MonoBehaviour
{
    [Header("Serve Area")]
    [SerializeField] private Vector2 serveAreaSize = new Vector2(4f, 3f);
    [SerializeField] private float serveAreaHeight = 0f;

    [Header("Child References")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private GameObject servingBat;

    [Header("Prefabs")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject servingBatPrefab;

    [Header("Serve Animation")]
    [SerializeField] private float serveAnimationTime = 1.5f;
    [SerializeField] private float ballDropHeight = 2f;
    [SerializeField] private Vector3 batOffset = new Vector3(1f, 0.5f, 1f);
    [SerializeField] private AnimationCurve ballDropCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private AnimationCurve batSwingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Ball Throw Settings")]
    [SerializeField] private Vector2 throwForceRange = new Vector2(8f, 12f);
    [SerializeField] private Vector2 throwHeightRange = new Vector2(2f, 4f);
    [SerializeField] private Vector2 throwAngleRange = new Vector2(-15f, 15f);

    [Header("Countdown Settings")]
    [SerializeField] private bool useCountdown = true;
    [SerializeField] private float countdownDelay = 3f;

    [Header("Cleanup")]
    [SerializeField] private float ballLifetime = 10f;

    private bool isServing = false;
    private GameObject currentBall;
    private GameObject currentServingBat;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isServing)
        {
            StartServe();
        }
    }

    private void StartServe()
    {
        isServing = true;
        CleanupPreviousServe();

        if (useCountdown)
        {
            StartCoroutine(CountdownAndServe());
        }
        else
        {
            BeginServeSequence();
        }
    }

    private void CleanupPreviousServe()
    {
        if (currentBall != null) Destroy(currentBall);
        if (currentServingBat != null) Destroy(currentServingBat);
    }

    private IEnumerator CountdownAndServe()
    {
        yield return new WaitForSeconds(countdownDelay);
        BeginServeSequence();
    }

    private void BeginServeSequence()
    {
        // Generate random serve point T within rectangular area
        Vector3 servePoint = GetRandomServePoint();

        // Calculate spawn positions
        Vector3 ballSpawnPoint = servePoint + Vector3.up * ballDropHeight; // Point P
        Vector3 batSpawnPoint = servePoint + batOffset; // Point B

        // Create ball and bat
        if (ballPrefab != null)
        {
            currentBall = Instantiate(ballPrefab, ballSpawnPoint, Quaternion.identity);
            // Disable ball physics initially
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null) ballRb.isKinematic = true;
        }

        if (servingBatPrefab != null)
        {
            currentServingBat = Instantiate(servingBatPrefab, batSpawnPoint, Quaternion.identity);
        }

        // Start serve animation
        StartCoroutine(ServeAnimation(ballSpawnPoint, batSpawnPoint, servePoint));
    }

    private IEnumerator ServeAnimation(Vector3 ballStart, Vector3 batStart, Vector3 servePoint)
    {
        float elapsedTime = 0f;

        while (elapsedTime < serveAnimationTime)
        {
            float progress = elapsedTime / serveAnimationTime;

            // Animate ball with gravity curve
            if (currentBall != null)
            {
                float ballCurveValue = ballDropCurve.Evaluate(progress);
                Vector3 ballPos = Vector3.Lerp(ballStart, servePoint, ballCurveValue);
                currentBall.transform.position = ballPos;
            }

            // Animate bat with swing curve
            if (currentServingBat != null)
            {
                float batCurveValue = batSwingCurve.Evaluate(progress);
                Vector3 batPos = Vector3.Lerp(batStart, servePoint, batCurveValue);

                // Add swing rotation
                float swingRotation = Mathf.Sin(progress * Mathf.PI) * 45f;
                currentServingBat.transform.position = batPos;
                currentServingBat.transform.rotation = Quaternion.Euler(-swingRotation, 0, 0);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Execute serve hit
        ExecuteServeHit(servePoint);
    }

    private void ExecuteServeHit(Vector3 hitPoint)
    {
        if (currentBall != null)
        {
            // Re-enable physics
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false;

                // Calculate throw force
                float throwForce = Random.Range(throwForceRange.x, throwForceRange.y);
                float throwHeight = Random.Range(throwHeightRange.x, throwHeightRange.y);
                float throwAngle = Random.Range(throwAngleRange.x, throwAngleRange.y);

                Vector3 forceDirection = new Vector3(
                    -throwForce, // Negative X (toward player)
                    throwHeight,
                    Mathf.Sin(throwAngle * Mathf.Deg2Rad) * throwForce * 0.3f
                );

                ballRb.AddForce(forceDirection, ForceMode.Impulse);

                // Auto cleanup
                Destroy(currentBall, ballLifetime);
            }
        }

        // Cleanup serving bat
        if (currentServingBat != null)
        {
            Destroy(currentServingBat, 1f);
        }

        isServing = false;
    }

    private Vector3 GetRandomServePoint()
    {
        float randomX = Random.Range(-serveAreaSize.x * 0.5f, serveAreaSize.x * 0.5f);
        float randomZ = Random.Range(-serveAreaSize.y * 0.5f, serveAreaSize.y * 0.5f);

        return transform.position + new Vector3(randomX, serveAreaHeight, randomZ);
    }

    // Public methods for runtime adjustment
    public void SetServeAreaSize(Vector2 size)
    {
        serveAreaSize = size;
    }

    public void SetUseCountdown(bool useIt)
    {
        useCountdown = useIt;
    }

    // Draw serve area gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + Vector3.up * serveAreaHeight;
        Vector3 size = new Vector3(serveAreaSize.x, 0.1f, serveAreaSize.y);

        Gizmos.DrawWireCube(center, size);

        // Draw corners
        Gizmos.color = Color.red;
        float halfX = serveAreaSize.x * 0.5f;
        float halfZ = serveAreaSize.y * 0.5f;

        Vector3[] corners = {
            center + new Vector3(-halfX, 0, -halfZ),
            center + new Vector3(halfX, 0, -halfZ),
            center + new Vector3(halfX, 0, halfZ),
            center + new Vector3(-halfX, 0, halfZ)
        };

        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawWireSphere(corner, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        // Show example serve points
        Gizmos.color = Color.cyan;
        Vector3 exampleServePoint = GetRandomServePoint();
        Vector3 ballStart = exampleServePoint + Vector3.up * ballDropHeight;
        Vector3 batStart = exampleServePoint + batOffset;

        Gizmos.DrawWireSphere(exampleServePoint, 0.15f);
        Gizmos.DrawLine(ballStart, exampleServePoint);
        Gizmos.DrawLine(batStart, exampleServePoint);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ballStart, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(batStart, 0.1f);
    }
}