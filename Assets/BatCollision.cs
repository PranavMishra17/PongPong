using UnityEngine;

public class BatCollision : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private float hitForceMultiplier = 1.5f;
    [SerializeField] private LayerMask ballLayer = -1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            HandleBallHit(other);
        }
    }

    private void HandleBallHit(Collider ballCollider)
    {
        Rigidbody ballRb = ballCollider.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // Calculate hit direction based on bat orientation
            Vector3 hitDirection = transform.forward;

            // Add some randomness for realism
            hitDirection += new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(0.1f, 0.3f),
                0
            );

            // Apply force
            float hitForce = ballRb.velocity.magnitude * hitForceMultiplier + 2f;
            ballRb.velocity = hitDirection.normalized * hitForce;
        }
    }
}