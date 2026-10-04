
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
    public float attackCooldown = 3f;
    public float warningTime = 1f;

    [Header("🔥 FIREBALL")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public int fireballDamage = 30;

    [Header("⚡ LIGHTNING")]
    public GameObject lightningPrefab;
    public int lightningDamage = 50;

    // Время предупреждения перед молнией
    public float lightningWarningTime = 3f;

    // Высота появления молнии
    public float lightningHeight = 6f;

    [Header("🌍 EARTH SPIKES")]
    public GameObject earthSpikePrefab;
    public int earthDamage = 40;

    public int totalSpikes = 20;
    public float spikeDistance = 1.5f;
    public float spikeDelay = 0.15f;
    public float spikeLifetime = 4f;

    public float firstSpikeDistance = 1.5f;
    public float spikeHeight = -1f;

    [Header("🌍 SPIKE FREQUENCY")]
    public int spikeEveryNAttacks = 4;

    private int attackCounter = 0;

    [Header("⚠️ ATTACK WARNING")]
    public SpriteRenderer warningGlow;

    public Color fireColor = Color.red;
    public Color lightningColor = Color.blue;
    public Color spikeColor = new Color(0.45f, 0.2f, 0.05f);

    private bool attacking;
    private bool dead;


    private void Start()
    {
        currentHealth = maxHealth;

        if (warningGlow != null)
            warningGlow.enabled = false;

        StartCoroutine(BossAI());
    }


    // =========================================================
    // BOSS AI
    // =========================================================

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


    // =========================================================
    // ВЫБОР АТАКИ
    // =========================================================

    private IEnumerator Attack()
    {
        attacking = true;

        attackCounter++;


        bool playerOnGround =
            IsPlayerOnGround();


        // Игрок выше босса
        bool playerAboveBoss =
            player.position.y >
            transform.position.y + 0.5f;


        int attackType;


        // =====================================================
        // ИГРОК ВЫШЕ БОССА
        // =====================================================

        if (playerAboveBoss)
        {
        

            int random =
                Random.Range(0, 2);

            if (random == 0)
            {
                attackType = 0; // 🔥 Огонь
            }
            else
            {
                attackType = 1; // ⚡ Молния
            }
        }


        // =====================================================
        // ИГРОК НА ЗЕМЛЕ
        // =====================================================

        else if (playerOnGround)
        {
          
            int random =
                Random.Range(0, 2);

            if (random == 0)
            {
                attackType = 0; // 🔥 Огонь
            }
            else
            {
                attackType = 2; // 🌍 Шипы
            }
        }


        // =====================================================
        // ИГРОК НЕ НА ЗЕМЛЕ И НЕ ВЫШЕ БОССА
        // =====================================================

        else
        {
        

            attackType =
                Random.Range(0, 3);
        }


        // =====================================================
        // 🔥 FIREBALL
        // =====================================================

        if (attackType == 0)
        {
            ShowAttackWarning(
                fireColor
            );

            yield return new WaitForSeconds(
                warningTime
            );

            HideAttackWarning();

            if (!dead)
            {
                FireballAttack();
            }
        }


        // =====================================================
        // ⚡ LIGHTNING
        // =====================================================

        else if (attackType == 1)
        {
            // Перед молнией предупреждение

            ShowAttackWarning(
                lightningColor
            );


            yield return new WaitForSeconds(
                lightningWarningTime
            );


            HideAttackWarning();


            if (!dead)
            {
                // Проверяем, что игрок всё ещё
                // находится выше босса

                bool stillAbove =
                    player.position.y >
                    transform.position.y + 0.5f;


                if (stillAbove)
                {
                    LightningAttack();
                }
            }
        }


        // =====================================================
        // 🌍 EARTH SPIKES
        // =====================================================

        else if (attackType == 2)
        {
            ShowAttackWarning(
                spikeColor
            );


            yield return new WaitForSeconds(
                warningTime
            );


            HideAttackWarning();


            if (!dead)
            {
                yield return StartCoroutine(
                    EarthSpikeAttack()
                );
            }
        }


        yield return new WaitForSeconds(
            attackCooldown
        );


        attacking = false;
    }


    // =========================================================
    // ПРОВЕРКА ЗЕМЛИ
    // =========================================================

    private bool IsPlayerOnGround()
    {
        if (player == null)
            return false;


        Player2D playerScript =
            player.GetComponent<Player2D>();


        if (playerScript == null)
            return false;


        if (playerScript.groundCheck == null)
            return false;


        Collider2D hit =
            Physics2D.OverlapCircle(
                playerScript.groundCheck.position,
                playerScript.groundCheckRadius,
                playerScript.groundLayer
            );


        return hit != null;
    }


    // =========================================================
    // WARNING
    // =========================================================

    private void ShowAttackWarning(Color color)
    {
        if (warningGlow == null)
            return;

        warningGlow.color = color;
        warningGlow.enabled = true;
    }


    private void HideAttackWarning()
    {
        if (warningGlow != null)
            warningGlow.enabled = false;
    }


    // =========================================================
    // 🔥 FIREBALL
    // =========================================================

    private void FireballAttack()
    {
        if (fireballPrefab == null)
        {
            Debug.LogWarning(
                "🔥 Fireball Prefab не назначен!"
            );

            return;
        }


        Vector3 spawnPosition =
            firePoint != null
            ? firePoint.position
            : transform.position;


        GameObject fireball =
            Instantiate(
                fireballPrefab,
                spawnPosition,
                Quaternion.identity
            );


        Fireball2D script =
            fireball.GetComponent<Fireball2D>();


        if (script != null && player != null)
        {
            Vector2 direction =
                (
                    player.position -
                    fireball.transform.position
                ).normalized;


            script.SetDirection(
                direction
            );


            script.damage =
                fireballDamage;
        }


        Debug.Log(
            "🔥 БОСС ИСПОЛЬЗОВАЛ ОГОНЬ!"
        );
    }


    // =========================================================
    // ⚡ LIGHTNING
    // =========================================================

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
            return;


        Vector3 spawnPosition =
            new Vector3(
                player.position.x,
                player.position.y + lightningHeight,
                0f
            );


        GameObject lightning =
            Instantiate(
                lightningPrefab,
                spawnPosition,
                Quaternion.identity
            );


        Lightning2D script =
            lightning.GetComponent<Lightning2D>();


        if (script != null)
        {
            script.damage =
                lightningDamage;

            script.SetTarget(
                player
            );
        }


        Debug.Log(
            "⚡ БОСС УДАРИЛ МОЛНИЕЙ!"
        );
    }


    // =========================================================
    // 🌍 EARTH SPIKES
    // =========================================================

    private IEnumerator EarthSpikeAttack()
    {
        if (earthSpikePrefab == null)
        {
            Debug.LogError(
                "❌ Earth Spike Prefab НЕ НАЗНАЧЕН!"
            );

            yield break;
        }


        float startX =
            transform.position.x +
            firstSpikeDistance;


        float startY =
            transform.position.y +
            spikeHeight;


        for (int i = 0;
             i < totalSpikes;
             i++)
        {
            if (dead)
                yield break;


            float x =
                startX -
                (i * spikeDistance);


            Vector3 spawnPosition =
                new Vector3(
                    x,
                    startY,
                    0f
                );


            GameObject spike =
                Instantiate(
                    earthSpikePrefab,
                    spawnPosition,
                    Quaternion.identity
                );


            spike.transform.rotation =
                Quaternion.identity;


            EarthSpike spikeScript =
                spike.GetComponent<EarthSpike>();


            if (spikeScript != null)
            {
                spikeScript.damage =
                    earthDamage;
            }


            Destroy(
                spike,
                spikeLifetime
            );


            yield return new WaitForSeconds(
                spikeDelay
            );
        }


        Debug.Log(
            "🌍 ШИПЫ СОЗДАНЫ!"
        );
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (dead)
            return;


        currentHealth -= damage;


        Debug.Log(
            "💥 Босс получил " +
            damage +
            " урона. HP: " +
            currentHealth +
            "/" +
            maxHealth
        );


        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        dead = true;

        StopAllCoroutines();


        if (warningGlow != null)
        {
            warningGlow.enabled = false;
        }


        Debug.Log(
            "💀 БОСС УМЕР!"
        );


        Destroy(gameObject);
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );


        Gizmos.color = spikeColor;


        float startX =
            transform.position.x +
            firstSpikeDistance;


        float startY =
            transform.position.y +
            spikeHeight;


        for (int i = 0;
             i < totalSpikes;
             i++)
        {
            float x =
                startX -
                (i * spikeDistance);


            Vector3 position =
                new Vector3(
                    x,
                    startY,
                    0f
                );


            Gizmos.DrawWireCube(
                position,
                new Vector3(
                    0.5f,
                    1f,
                    0.1f
                )
            );
        }
    }
}