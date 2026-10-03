
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

    [Header("ATTACK SETTINGS")]
    public float attackCooldown = 2f;
    private bool attacking;
    private bool dead;

    [Header("🔥 FIREBALL")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public int fireballDamage = 30;

    [Header("🌍 EARTH SPIKES")]
    public GameObject earthSpikePrefab;
    public Transform earthSpikePoint;
    public int earthDamage = 40;

    public float spikeDistance = 1.5f;
    public float spikeDelay = 0.3f;
    public int totalSpikes = 13;

    [Header("⚡ LIGHTNING")]
    public GameObject lightningPrefab;
    public Transform lightningPoint;
    public int lightningDamage = 50;

    [Header("GROUND")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;


    private void Start()
    {
        currentHealth = maxHealth;

        // Создаём EarthSpikePoint автоматически
        if (earthSpikePoint == null)
        {
            GameObject point = new GameObject("EarthSpikePoint");

            point.transform.SetParent(transform);

            point.transform.localPosition = new Vector3(
                1.5f,
                -0.5f,
                0f
            );

            earthSpikePoint = point.transform;
        }

        StartCoroutine(BossAI());
    }


    // =========================================
    // 💥 УРОН БОССУ
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
    // 💀 СМЕРТЬ
    // =========================================

    private void Die()
    {
        dead = true;

        StopAllCoroutines();

        Debug.Log("💀 БОСС УМЕР!");

        Destroy(gameObject);
    }


    // =========================================
    // 🤖 ИИ
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
    // ⚔️ ВЫБОР АТАКИ
    // =========================================

    private IEnumerator Attack()
    {
        attacking = true;

        bool grounded = IsPlayerGrounded();
        bool aboveBoss = IsPlayerAboveBoss();

        // =========================================
        // ⚡ ИГРОК В ВОЗДУХЕ
        // =========================================

        if (!grounded && aboveBoss)
        {
            // Босс сам выбирает:
            // 0 = молния
            // 1 = огненный шар

            int randomAttack = Random.Range(0, 2);

            if (randomAttack == 0)
            {
                LightningAttack();
            }
            else
            {
                FireballAttack();
            }
        }

        // =========================================
        // 🌍 ИГРОК НА ЗЕМЛЕ
        // =========================================

        else if (grounded)
        {
            // Босс сам выбирает:
            // 0 = земные шипы
            // 1 = огненный шар

            int randomAttack = Random.Range(0, 2);

            if (randomAttack == 0)
            {
                // 13 шипов по 2
                yield return StartCoroutine(EarthSpikeAttack());
            }
            else
            {
                // Огненный шар
                FireballAttack();
            }
        }

        // =========================================
        // 🔥 В ДРУГИХ СЛУЧАЯХ
        // =========================================

        else
        {
            FireballAttack();
        }

        // =========================================
        // ⏱ ЗАДЕРЖКА 3 СЕКУНДЫ
        // =========================================

        yield return new WaitForSeconds(3f);

        attacking = false;
    }


    // =========================================
    // 🔥 ОГНЕННЫЙ ШАР
    // =========================================

    private void FireballAttack()
    {
        if (fireballPrefab == null || player == null)
            return;

        Vector2 spawnPosition =
            firePoint != null
            ? firePoint.position
            : transform.position;

        GameObject fireball = Instantiate(
            fireballPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Fireball2D script =
            fireball.GetComponent<Fireball2D>();

        if (script != null)
        {
            Vector2 direction =
                (player.position -
                 fireball.transform.position).normalized;

            script.SetDirection(direction);

            script.damage = fireballDamage;
        }

        Debug.Log("🔥 Босс выпустил огненный шар!");
    }


    // =========================================
    // 🌍 ЗЕМНЫЕ ШИПЫ
    // =========================================

    private IEnumerator EarthSpikeAttack()
    {
        if (earthSpikePrefab == null)
        {
            Debug.LogError(
                "🌍 Earth Spike Prefab не назначен!"
            );

            yield break;
        }

        if (player == null)
            yield break;


        // Определяем сторону игрока
        float direction =
            player.position.x > transform.position.x
            ? 1f
            : -1f;


        // Перемещаем Point перед боссом
        earthSpikePoint.position = new Vector3(
            transform.position.x +
            direction * 1.5f,

            transform.position.y - 0.5f,

            transform.position.z
        );


        // Создаём 13 шипов
        // По 2 одновременно
        for (int i = 0; i < totalSpikes; i += 2)
        {
            // Первый шип
            Vector2 position1 = new Vector2(
                earthSpikePoint.position.x +
                direction * (i * spikeDistance),

                earthSpikePoint.position.y
            );

            CreateEarthSpike(position1);


            // Второй шип
            if (i + 1 < totalSpikes)
            {
                Vector2 position2 = new Vector2(
                    earthSpikePoint.position.x +
                    direction * ((i + 1) * spikeDistance),

                    earthSpikePoint.position.y
                );

                CreateEarthSpike(position2);
            }


            Debug.Log(
                "🌍 Шипы: " +
                Mathf.Min(i + 2, totalSpikes) +
                "/" +
                totalSpikes
            );


            // Ждём перед следующими 2 шипами
            yield return new WaitForSeconds(spikeDelay);
        }

        Debug.Log("🌍 Все 13 шипов появились!");
    }


    // =========================================
    // 🌍 СОЗДАНИЕ ШИПА
    // =========================================

    private void CreateEarthSpike(Vector2 position)
    {
        GameObject spike = Instantiate(
            earthSpikePrefab,
            position,
            Quaternion.identity
        );

        EarthSpike spikeScript =
            spike.GetComponent<EarthSpike>();

        if (spikeScript != null)
        {
            spikeScript.damage = earthDamage;
        }
        else
        {
            Debug.LogError(
                "🌍 На EarthSpike Prefab нет EarthSpike.cs!"
            );
        }
    }


    // =========================================
    // ⚡ МОЛНИЯ
    // =========================================

    private void LightningAttack()
    {
        if (lightningPrefab == null)
        {
            Debug.LogWarning(
                "⚡ Lightning Prefab не назначен!"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                "⚡ Player не назначен!"
            );

            return;
        }


        float lightningY;

        if (lightningPoint != null)
        {
            lightningY =
                lightningPoint.position.y;
        }
        else
        {
            lightningY =
                player.position.y + 5f;
        }


        Vector2 spawnPosition = new Vector2(
            player.position.x,
            lightningY
        );


        GameObject lightning = Instantiate(
            lightningPrefab,
            spawnPosition,
            Quaternion.identity
        );


        Lightning2D lightningScript =
            lightning.GetComponent<Lightning2D>();


        if (lightningScript != null)
        {
            lightningScript.damage =
                lightningDamage;

            lightningScript.SetTarget(player);

            Debug.Log(
                "⚡ Молния создана над игроком!"
            );
        }
        else
        {
            Debug.LogError(
                "⚡ Lightning Prefab НЕ содержит Lightning2D!"
            );
        }
    }


    // =========================================
    // 👤 ПРОВЕРКА ЗЕМЛИ
    // =========================================

    private bool IsPlayerGrounded()
    {
        if (player == null)
            return false;

        Collider2D col =
            player.GetComponent<Collider2D>();

        if (col == null)
            return false;

        Vector2 origin =
            col.bounds.center;

        float distance =
            col.bounds.extents.y +
            groundCheckDistance;


        RaycastHit2D hit =
            Physics2D.Raycast(
                origin,
                Vector2.down,
                distance,
                groundLayer
            );


        return hit.collider != null;
    }


    // =========================================
    // 👆 ИГРОК ВЫШЕ БОССА?
    // =========================================

    private bool IsPlayerAboveBoss()
    {
        if (player == null)
            return false;

        return player.position.y >
               transform.position.y + 0.5f;
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


        // 🔥 FirePoint
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawSphere(
                firePoint.position,
                0.2f
            );
        }


        // ⚡ LightningPoint
        if (lightningPoint != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawSphere(
                lightningPoint.position,
                0.25f
            );
        }


        // 🌍 EarthSpikePoint
        if (earthSpikePoint != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawSphere(
                earthSpikePoint.position,
                0.25f
            );
        }
    }
}