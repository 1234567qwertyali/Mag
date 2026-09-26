using UnityEngine;

public class Fireball2D : MonoBehaviour
{
    [Header("FIREBALL")]
    public float speed = 8f;
    public int damage = 30;
    public float lifeTime = 5f;

    // Цель огненного шара
    private Transform target;

    // =========================================
    // ПОЛУЧИТЬ ЦЕЛЬ
    // =========================================

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // =========================================
    // СОЗДАНИЕ
    // =========================================

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // =========================================
    // ПОЛЁТ К ИГРОКУ
    // =========================================

    private void Update()
    {
        if (target == null)
            return;

        Vector2 direction =
            (target.position - transform.position).normalized;

        transform.position +=
            (Vector3)direction * speed * Time.deltaTime;

        // Поворачиваем шар в сторону игрока
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0, 0, angle);
    }

    // =========================================
    // ПОПАДАНИЕ В ИГРОКА
    // =========================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player2D player =
            collision.GetComponent<Player2D>();

        if (player != null)
        {
            player.TakeDamage(damage);

            Debug.Log(
                "🔥 Огненный шар попал в игрока! Урон: "
                + damage
            );

            Destroy(gameObject);
        }
    }
}