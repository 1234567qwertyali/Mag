
using UnityEngine;

public class FireRainProjectile : MonoBehaviour
{
    public float launchSpeed = 15f;
    public float fallSpeed = 12f;
    public int damage = 30;
    public float lifeTime = 20f;

    private Rigidbody2D rb;
    private bool falling = false;
    private bool hit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.up * launchSpeed;
        }

        Destroy(gameObject, lifeTime);
    }

    public void StartFalling(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        falling = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * fallSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || !falling) return;

        if (other.CompareTag("Player"))
        {
            hit = true;

            other.SendMessage(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );

            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
