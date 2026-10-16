using UnityEngine;

public class Fireball2D : MonoBehaviour
{
    [Header("STATS")]
    public int damage = 20;
    public float speed = 10f;
    public float lifetime = 5f;

    [Header("COLLISION")]
    [Tooltip("Слои, о которые шар разбивается (земля, стены). Игрок определяется по компоненту Player2D.")]
    public LayerMask obstacleLayers;

    [Header("HIT EFFECT")]
    public GameObject hitEffect;
    public float hitEffectLifetime = 1.5f;

    private Vector2 direction = Vector2.right;
    private bool hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Вызывается боссом сразу после создания шара.
    public void SetDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude < 0.0001f)
            return;

        direction = newDirection.normalized;
    }

    private void Update()
    {
        // Space.World: шар повёрнут носом по полёту, но летит строго по направлению.
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        // Не бьём самого босса.
        if (other.GetComponentInParent<BossMageAI>() != null)
            return;

        Player2D playerScript = other.GetComponentInParent<Player2D>();

        if (playerScript != null)
        {
            // Вызывает TakeDamage(int) на игроке, если такой метод есть.
            playerScript.gameObject.SendMessage(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );

            Hit();
            return;
        }

        // Земля и стены.
        if (obstacleLayers.value != 0 &&
            (obstacleLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Hit();
        }
    }

    private void Hit()
    {
        hasHit = true;

        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(fx, hitEffectLifetime);
        }

        Destroy(gameObject);
    }
}