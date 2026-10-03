using UnityEngine;

public class EarthSpike : MonoBehaviour
{
    public int damage = 40;
    public float lifeTime = 2f;

    private bool hasHit = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        Player2D player = other.GetComponent<Player2D>();

        if (player != null)
        {
            player.TakeDamage(damage);
            hasHit = true;

            Debug.Log("🌍 Шип попал! Урон: " + damage);
        }
    }
}