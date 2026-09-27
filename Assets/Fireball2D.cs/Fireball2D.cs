using UnityEngine;

public class Fireball2D : MonoBehaviour
{
    [Header("FIREBALL")]
    public float speed = 8f;
    public int damage = 30;
    public float lifeTime = 5f;

    // Направление полёта
    private Vector2 direction;

    // =========================================
    // УСТАНОВИТЬ НАПРАВЛЕНИЕ
    // =========================================

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;

        // Поворачиваем огненный шар
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0,
            0,
            angle
        );
    }

    // =========================================
    // СОЗДАНИЕ
    // =========================================

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // =========================================
    // ПОЛЁТ ПО ПРЯМОЙ
    // =========================================

    private void Update()
    {
        transform.position +=
            (Vector3)direction *
            speed *
            Time.deltaTime;
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