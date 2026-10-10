
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireCircle : MonoBehaviour
{
    [Header("DAMAGE")]
    public int damage = 40;
    public float radius = 2.5f;

    [Header("TIMING")]
    public float warningTime = 1f;
    public float explosionLifetime = 0.3f;

    [Header("VISUAL")]
    public GameObject explosionEffect;
    public SpriteRenderer circleRenderer;
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    private bool exploded = false;

    private void Start()
    {
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        if (circleRenderer != null)
            circleRenderer.color = warningColor;

        // Игроку даётся время покинуть круг.
        yield return new WaitForSeconds(warningTime);

        if (exploded)
            yield break;

        exploded = true;

        // Ищем все объекты внутри радиуса взрыва.
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, radius);

        // Не наносим урон одному игроку несколько раз
        // за один взрыв, даже если у него несколько коллайдеров.
        HashSet<Player2D> damagedPlayers = new HashSet<Player2D>();

        foreach (Collider2D hit in hits)
        {
            Player2D playerScript =
                hit.GetComponentInParent<Player2D>();

            if (playerScript != null &&
                damagedPlayers.Add(playerScript))
            {
                playerScript.TakeDamage(damage);
                Debug.Log("Огненный круг нанёс игроку " +
                          damage + " урона!");
            }
        }

        if (explosionEffect != null)
        {
            Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );
        }

        if (circleRenderer != null)
            circleRenderer.color = normalColor;

        Destroy(gameObject, explosionLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}


