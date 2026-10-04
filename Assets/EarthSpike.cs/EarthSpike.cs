
using UnityEngine;

public class EarthSpike : MonoBehaviour
{
    [Header("DAMAGE")]
    public int damage = 40;

    [Header("LIFETIME")]
    public float lifetime = 2f;

    private bool canDamage = true;


    private void Start()
    {
        // Шип исчезает через указанное время
        Destroy(gameObject, lifetime);
    }


    // =========================================
    // TRIGGER
    // =========================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDamage)
            return;

        Player2D player = other.GetComponent<Player2D>();

        // Если Player2D находится на родителе
        if (player == null)
        {
            player = other.GetComponentInParent<Player2D>();
        }

        if (player != null)
        {
            player.TakeDamage(damage);

            canDamage = false;

            Debug.Log(
                " ШИП ПОПАЛ В ИГРОКА! Урон: " +
                damage
            );
        }
    }


    // =========================================
    // COLLISION
    // =========================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canDamage)
            return;

        Player2D player =
            collision.gameObject.GetComponent<Player2D>();

        // Если Player2D находится на родителе
        if (player == null)
        {
            player =
                collision.gameObject
                .GetComponentInParent<Player2D>();
        }

        if (player != null)
        {
            player.TakeDamage(damage);

            canDamage = false;

            Debug.Log(
                " ШИП ПОПАЛ В ИГРОКА! Урон: " +
                damage
            );
        }
    }
}

