
using UnityEngine;

public class Lightning2D : MonoBehaviour
{
    public int damage = 50;
    public float lifeTime = 0.5f;

    private bool hasHit = false;

    // Этот метод вызывается BossMageAI
    public void SetTarget(Transform target)
    {
        if (target == null)
            return;

        Player2D player =
            target.GetComponent<Player2D>();

        if (player != null && !hasHit)
        {
            player.TakeDamage(damage);

            hasHit = true;

            Debug.Log(
                "⚡ МОЛНИЯ ПОПАЛА! УРОН: " + damage
            );
        }

        Destroy(gameObject, lifeTime);
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}

