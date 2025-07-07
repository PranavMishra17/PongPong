using UnityEngine;

public class TableTennisBall : MonoBehaviour
{
    [Header("Ball Physics")]
    [SerializeField] private float bounceForce = 0.8f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float minSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip hitSound;

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool canPlaySound = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Ensure we don't get stuck (only if not kinematic)
        if (rb != null && !rb.isKinematic && rb.velocity.magnitude < minSpeed)
        {
            rb.velocity = rb.velocity.normalized * minSpeed;
        }
    }

    private void FixedUpdate()
    {
        // Limit max speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // Ensure minimum speed
        if (rb.velocity.magnitude < minSpeed && rb.velocity.magnitude > 0.1f)
        {
            rb.velocity = rb.velocity.normalized * minSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            HandleTableBounce(collision);
        }
        else if (collision.gameObject.CompareTag("Bat"))
        {
            HandleBatHit(collision);
        }
    }

    private void HandleTableBounce(Collision collision)
    {
        // Apply bounce force
        Vector3 bounceDirection = Vector3.Reflect(rb.velocity.normalized, collision.contacts[0].normal);
        rb.velocity = bounceDirection * rb.velocity.magnitude * bounceForce;

        PlaySound(bounceSound);
    }

    private void HandleBatHit(Collision collision)
    {
        // Get bat velocity for added force
        TableTennisBat bat = collision.gameObject.GetComponent<TableTennisBat>();

        // Reflect with additional force
        Vector3 hitDirection = Vector3.Reflect(rb.velocity.normalized, collision.contacts[0].normal);
        float hitForce = rb.velocity.magnitude + 3f; // Add some force from bat

        rb.velocity = hitDirection * hitForce;

        PlaySound(hitSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null && canPlaySound)
        {
            audioSource.PlayOneShot(clip);
            StartCoroutine(SoundCooldown());
        }
    }

    private System.Collections.IEnumerator SoundCooldown()
    {
        canPlaySound = false;
        yield return new WaitForSeconds(0.1f);
        canPlaySound = true;
    }
}