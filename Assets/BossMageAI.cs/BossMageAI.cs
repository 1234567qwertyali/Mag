
using UnityEngine;
using System.Collections;

public class BossMageAI : MonoBehaviour
{
    [Header("BOSS HP")]
    public int maxHealth = 1000;
    private int currentHealth;

    [Header("PLAYER")]
    public Transform player;
    public float attackRange = 15f;

    [Header("ATTACK POINTS")]
    public Transform[] attackPoints;

    [Header("ATTACK SETTINGS")]
    public float attackCooldown = 2f;
    private bool attacking;
    private bool dead;

    [Header("🔥 FIREBALL")]
    public GameObject fireballPrefab;

    // Точка, откуда вылетает огненный шар
    public Transform firePoint;

    public int fireballDamage = 30;


    [Header("EARTH SPIKES")]
    public GameObject earthSpikePrefab;
    public int earthDamage = 40;


    [Header("LIGHTNING")]
    public GameObject lightningPrefab;
    public int lightningDamage = 50;


    private void Start()
    {
        currentHealth = maxHealth;

        StartCoroutine(BossAI());
    }


    // =========================================
    // БОСС ПОЛУЧАЕТ УРОН
    // =========================================

    public void TakeDamage(int damage)
    {
        if (dead)
            return;

        currentHealth -= damage;

        Debug.Log(
            "Босс получил " + damage +
            " урона. HP: " +
            currentHealth + "/" + maxHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // =========================================
    // СМЕРТЬ
    // =========================================

    private void Die()
    {
        dead = true;

        StopAllCoroutines();

        Debug.Log("БОСС УМЕР!");

        Destroy(gameObject);
    }


    // =========================================
    // ИИ
    // =========================================

    private IEnumerator BossAI()
    {
        while (!dead)
        {
            if (player != null && !attacking)
            {
                float distance = Vector2.Distance(
                    transform.position,
                    player.position
                );

                if (distance <= attackRange)
                {
                    StartCoroutine(Attack());
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }


    // =========================================
    // ВЫБОР АТАКИ
    // =========================================

    private IEnumerator Attack()
    {
        attacking = true;

        int randomAttack = Random.Range(0, 3);

        switch (randomAttack)
        {
            case 0:
                FireballAttack();
                break;

            case 1:
                EarthSpikeAttack();
                break;

            case 2:
                LightningAttack();
                break;
        }

        yield return new WaitForSeconds(attackCooldown);

        attacking = false;
    }


    // =========================================
    // 🔥 ОГНЕННЫЙ ШАР
    // =========================================

    private void FireballAttack()
    {
        if (fireballPrefab == null)
        {
            Debug.LogWarning(
                "🔥 Не назначен Fireball Prefab!"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                "🔥 Босс не знает, где находится Player!"
            );

            return;
        }


        // =========================================
        // ТОЧКА СОЗДАНИЯ
        // =========================================

        Vector2 spawnPosition;

        if (firePoint != null)
        {
            spawnPosition = firePoint.position;
        }
        else
        {
            spawnPosition = transform.position;
        }


        // =========================================
        // СОЗДАЁМ ОГНЕННЫЙ ШАР
        // =========================================

        GameObject fireball = Instantiate(
            fireballPrefab,
            spawnPosition,
            Quaternion.identity
        );


        // =========================================
        // ПОЛУЧАЕМ СКРИПТ
        // =========================================

        Fireball2D fireballScript =
            fireball.GetComponent<Fireball2D>();


        if (fireballScript != null)
        {
            // =========================================
            // НАПРАВЛЕНИЕ К ИГРОКУ
            // =========================================

            Vector2 direction =
                (player.position - fireball.transform.position)
                .normalized;


            // =========================================
            // ПЕРЕДАЁМ НАПРАВЛЕНИЕ
            // =========================================

            fireballScript.SetDirection(direction);


            // =========================================
            // ПЕРЕДАЁМ УРОН
            // =========================================

            fireballScript.damage = fireballDamage;


            Debug.Log(
                "🔥 Босс выпустил прямой огненный шар!"
            );
        }
        else
        {
            Debug.LogError(
                "🔥 На Fireball Prefab нет компонента Fireball2D!"
            );
        }
    }


    // =========================================
    // 🌍 ШИПЫ ЗЕМЛИ
    // =========================================

    private void EarthSpikeAttack()
    {
        if (earthSpikePrefab == null)
        {
            Debug.LogWarning(
                "Не назначен Earth Spike Prefab!"
            );

            return;
        }

        Vector2 target = GetAttackPoint();


        GameObject spike = Instantiate(
            earthSpikePrefab,
            target,
            Quaternion.identity
        );


        Debug.Log(
            "🌍 Босс создал шип земли!"
        );
    }


    // =========================================
    // ⚡ МОЛНИЯ
    // =========================================

    private void LightningAttack()
    {
        if (lightningPrefab == null)
        {
            Debug.LogWarning(
                "Не назначен Lightning Prefab!"
            );

            return;
        }

        Vector2 target = GetAttackPoint();


        GameObject lightning = Instantiate(
            lightningPrefab,
            target,
            Quaternion.identity
        );


        Debug.Log(
            "⚡ Босс вызвал молнию!"
        );
    }


    // =========================================
    // ТОЧКА АТАКИ
    // =========================================

    private Vector2 GetAttackPoint()
    {
        if (player == null)
        {
            return transform.position;
        }


        if (attackPoints == null ||
            attackPoints.Length == 0)
        {
            return player.position;
        }


        int randomPoint =
            Random.Range(
                0,
                attackPoints.Length
            );


        return attackPoints[randomPoint].position;
    }


    // =========================================
    // GIZMOS
    // =========================================

    private void OnDrawGizmosSelected()
    {
        // Радиус атаки
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );


        // =========================================
        // FIRE POINT
        // =========================================

        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawSphere(
                firePoint.position,
                0.2f
            );

            Gizmos.DrawLine(
                transform.position,
                firePoint.position
            );
        }


        // =========================================
        // ТОЧКИ АТАК
        // =========================================

        if (attackPoints != null)
        {
            Gizmos.color = Color.yellow;

            foreach (Transform point in attackPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(
                        point.position,
                        0.25f
                    );
                }
            }
        }
    }
}