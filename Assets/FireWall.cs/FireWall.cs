using UnityEngine;
using System.Collections.Generic;

public class FireWall : MonoBehaviour
{
    [Header("SETTINGS")]
    public float speed = 2f;
    public int damage = 35;
    public float lifetime = 7f;

    private Transform target;
    private float directionX = 1f;
    private bool initialized;

    private readonly HashSet<Player2D> damagedPlayers =
        new HashSet<Player2D>();

    public void SetTarget(
        Transform newTarget,
        int newDamage,
        float newSpeed,
        float newLifetime)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;
        lifetime = newLifetime;

        if (target != null)
        {
            directionX = Mathf.Sign(
                target.position.x - transform.position.x
            );

            if (directionX == 0)
                directionX = 1f;
        }

        initialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!initialized)
            return;

        // Стена движется горизонтально в сторону игрока.
        transform.position += new Vector3(
            directionX * speed * Time.deltaTime,
            0f,
            0f
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player2D playerScript =
            other.GetComponentInParent<Player2D>();

        if (playerScript == null)
            return;

        // Эта стена наносит урон игроку только один раз.
        if (damagedPlayers.Add(playerScript))
            playerScript.TakeDamage(damage);
    }
}