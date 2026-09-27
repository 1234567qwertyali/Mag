using UnityEngine;

public class Lightning2D : MonoBehaviour
{
    [Header("LIGHTNING")]
    public int damage = 50;
    public float lifeTime = 0.5f;

    private bool hitPlayer = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitPlayer)
            return;

        Player2D player = collision.GetComponent<Player2D>();

        if (player != null)
        {
            hitPlayer = true;

            player.TakeDamage(damage);

            Debug.Log(
                "⚡ Молния попала в игрока! Урон: " + damage
            );

            Destroy(gameObject);
        }
    }
}