using UnityEngine;
using System.Collections;

public class BossMageAI : MonoBehaviour
{
    [Header("BOSS HP")]
    public int maxHealth = 1000;
    public int currentHealth = 1000;

    [Header("PLAYER")]
    public Transform player;
    public float attackRange = 15f;
    public float farDistance = 12f;

    [Header("TELEPORT")]
    public Transform[] teleportPoints;
    public bool teleportBeforeAttack = true;
    public GameObject teleportEffect;
    public float teleportEffectLifetime = 2f;

    [Header("FIREBALL MACHINE GUN")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public int fireballDamage = 20;
    public int burstFireballCount = 7;
    public float burstFireballDelay = 0.2f;

    [Header("FIRE DRAGON HEAD")]
    public GameObject dragonHeadPrefab;
    public Transform dragonHeadSpawnPoint;
    public int dragonHeadDamage = 60;
    public float dragonHeadSpeed = 5f;
    public float dragonHeadLifetime = 6f;

    [Header("FIRE WALL")]
    public GameObject fireWallPrefab;
    public Transform fireWallSpawnPoint;
    public int fireWallDamage = 35;
    public float fireWallSpeed = 2f;
    public float fireWallLifetime = 7f;

    [Header("LIGHTNING")]
    public GameObject lightningPrefab;
    public int lightningDamage = 30;
    public float lightningWarningTime = 0.8f;
    public float lightningHeight = 5f;

    [Header("EARTH SPIKES")]
    public GameObject earthSpikePrefab;
    public int earthSpikeDamage = 40;
    public int totalSpikes = 20;
    public float spikeDistance = 1.5f;
    public float spikeDelay = 0.15f;
    public float spikeLifetime = 2f;
    public float spikeHeight = 0f;

    [Header("TWO SPIKE TRAP")]
    public Transform bigSpikePoint1;
    public Transform bigSpikePoint2;
    public float bigSpikeLifetime = 4f;

    [Header("FIRE CIRCLE")]
    public GameObject fireCirclePrefab;
    public int fireCircleDamage = 40;
    public float fireCircleWarningTime = 1f;
    [Range(0f, 1f)]
    public float fireCircleChance = 0.2f;

    [Header("ATTACK SETTINGS")]
    public float attackCooldown = 2f;
    public float warningTime = 0.5f;
    public float comboDelay = 0.2f;

    [Header("ATTACK CHANCES")]
    [Range(0f, 1f)]
    public float spikeChance = 0.4f;
    [Range(0f, 1f)]
    public float bigSpikeTrapChance = 0.2f;
    [Range(0f, 1f)]
    public float fireWallChance = 0.35f;

    [Header("WARNING VISUAL")]
    public SpriteRenderer warningGlow;
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    private bool isAttacking;
    private bool isDead;
    private int lastTeleportIndex = -1;

    private void Start()
    {
        currentHealth = maxHealth;

        if (warningGlow != null)
            warningGlow.color = normalColor;
    }

    private void Update()
    {
        if (isDead || isAttacking || player == null)
            return;

        if (CanStartAttack())
            StartCoroutine(StartAttack());
    }

    private bool CanStartAttack()
    {
        if (player == null)
            return false;

        if (IsPlayerGrounded())
            return true;

        return Vector2.Distance(transform.position, player.position)
               <= attackRange;
    }

    private IEnumerator StartAttack()
    {
        isAttacking = true;

        // Определяем дальность ДО телепортации.
        bool playerWasFar = player != null &&
            Vector2.Distance(transform.position, player.position)
            >= farDistance;

        if (teleportBeforeAttack)
            TeleportToPoint();

        if (isDead || player == null)
        {
            isAttacking = false;
            yield break;
        }

        if (playerWasFar)
        {
            // На расстоянии босс выбирает одну из двух
            // дальнобойных огненных способностей.
            if (Random.value < fireWallChance)
                yield return StartCoroutine(FireWallAttack());
            else
                yield return StartCoroutine(DragonHeadAttack());
        }
        else
        {
            bool grounded = IsPlayerGrounded();
            float roll = Random.value;

            if (grounded && roll < bigSpikeTrapChance)
            {
                yield return StartCoroutine(BigSpikeTrapAttack());
            }
            else if (grounded &&
                     roll < bigSpikeTrapChance + spikeChance)
            {
                yield return StartCoroutine(SpikeCombo());
            }
            else if (roll < bigSpikeTrapChance + spikeChance
                     + fireCircleChance)
            {
                yield return StartCoroutine(FireCircleAttack());
            }
            else if (IsPlayerAboveBoss())
            {
                yield return StartCoroutine(LightningAttack());
            }
            else
            {
                // Одиночного огненного шара больше нет.
                yield return StartCoroutine(FireballBurstAttack());
            }
        }

        SetWarning(false);

        if (!isDead)
            yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    private void TeleportToPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0)
            return;

        System.Collections.Generic.List<int> validPoints =
            new System.Collections.Generic.List<int>();

        for (int i = 0; i < teleportPoints.Length; i++)
        {
            if (teleportPoints[i] != null && i != lastTeleportIndex)
                validPoints.Add(i);
        }

        // Если доступна только предыдущая точка,
        // разрешаем телепортироваться туда.
        if (validPoints.Count == 0)
        {
            for (int i = 0; i < teleportPoints.Length; i++)
            {
                if (teleportPoints[i] != null)
                    validPoints.Add(i);
            }
        }

        if (validPoints.Count == 0)
            return;

        int chosenIndex = validPoints[
            Random.Range(0, validPoints.Count)
        ];

        Vector3 oldPosition = transform.position;

        if (teleportEffect != null)
        {
            GameObject effect = Instantiate(
                teleportEffect, oldPosition, Quaternion.identity
            );
            Destroy(effect, teleportEffectLifetime);
        }

        transform.position = teleportPoints[chosenIndex].position;
        lastTeleportIndex = chosenIndex;

        if (teleportEffect != null)
        {
            GameObject effect = Instantiate(
                teleportEffect, transform.position, Quaternion.identity
            );
            Destroy(effect, teleportEffectLifetime);
        }
    }

    // ПУЛЕМЁТ: только серия огненных шаров.
    private IEnumerator FireballBurstAttack()
    {
        SetWarning(true);
        yield return new WaitForSeconds(warningTime);
        SetWarning(false);

        for (int i = 0; i < burstFireballCount; i++)
        {
            if (player == null || isDead)
                yield break;

            if (fireballPrefab != null && firePoint != null)
            {
                GameObject fb = Instantiate(
                    fireballPrefab,
                    firePoint.position,
                    Quaternion.identity
                );

                Fireball2D fs = fb.GetComponent<Fireball2D>();

                if (fs != null)
                {
                    fs.damage = fireballDamage;

                    Vector2 direction =
                        ((Vector2)player.position -
                         (Vector2)firePoint.position).normalized;

                    fs.SetDirection(direction);
                }
            }

            if (i < burstFireballCount - 1)
                yield return new WaitForSeconds(burstFireballDelay);
        }
    }

    // ГОЛОВА ОГНЕННОГО ДРАКОНА.
    private IEnumerator DragonHeadAttack()
    {
        if (player == null || dragonHeadPrefab == null)
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(warningTime);
        SetWarning(false);

        if (player == null)
            yield break;

        Vector3 spawnPosition = dragonHeadSpawnPoint != null
            ? dragonHeadSpawnPoint.position
            : transform.position;

        GameObject head = Instantiate(
            dragonHeadPrefab,
            spawnPosition,
            Quaternion.identity
        );

        FireDragonHead headScript =
            head.GetComponent<FireDragonHead>();

        if (headScript != null)
        {
            headScript.SetTarget(
                player,
                dragonHeadDamage,
                dragonHeadSpeed,
                dragonHeadLifetime
            );
        }
    }

    // ДЛИННАЯ СТЕНА ОГНЯ.
    private IEnumerator FireWallAttack()
    {
        if (player == null || fireWallPrefab == null)
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(warningTime);
        SetWarning(false);

        if (player == null)
            yield break;

        Vector3 spawnPosition = fireWallSpawnPoint != null
            ? fireWallSpawnPoint.position
            : transform.position;

        GameObject wall = Instantiate(
            fireWallPrefab,
            spawnPosition,
            Quaternion.identity
        );

        FireWall wallScript = wall.GetComponent<FireWall>();

        if (wallScript != null)
        {
            wallScript.SetTarget(
                player,
                fireWallDamage,
                fireWallSpeed,
                fireWallLifetime
            );
        }
    }

    private IEnumerator FireCircleAttack()
    {
        if (player == null || fireCirclePrefab == null)
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(fireCircleWarningTime);
        SetWarning(false);

        if (player == null)
            yield break;

        GameObject circle = Instantiate(
            fireCirclePrefab,
            player.position,
            Quaternion.identity
        );

        FireCircle circleScript = circle.GetComponent<FireCircle>();

        if (circleScript != null)
        {
            circleScript.damage = fireCircleDamage;
            circleScript.warningTime = fireCircleWarningTime;
        }
    }

    private IEnumerator SpikeCombo()
    {
        yield return StartCoroutine(EarthSpikeRoutine());

        if (isDead)
            yield break;

        yield return new WaitForSeconds(comboDelay);
        yield return StartCoroutine(FireballBurstAttack());

        if (isDead)
            yield break;

        yield return new WaitForSeconds(comboDelay);

        if (IsPlayerAboveBoss())
            yield return StartCoroutine(LightningAttack());
    }

    private IEnumerator EarthSpikeRoutine()
    {
        if (player == null || earthSpikePrefab == null)
            yield break;

        if (!IsPlayerGrounded())
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(warningTime);
        SetWarning(false);

        if (player == null || !IsPlayerGrounded())
            yield break;

        float direction = Mathf.Sign(
            player.position.x - transform.position.x
        );

        if (direction == 0)
            direction = 1;

        for (int i = 0; i < totalSpikes; i++)
        {
            if (isDead)
                yield break;

            Vector3 position = new Vector3(
                transform.position.x + i * spikeDistance * direction,
                transform.position.y + spikeHeight,
                transform.position.z
            );

            GameObject spike = Instantiate(
                earthSpikePrefab,
                position,
                Quaternion.identity
            );

            EarthSpike spikeScript = spike.GetComponent<EarthSpike>();

            if (spikeScript != null)
                spikeScript.damage = earthSpikeDamage;

            // Размер шипов не меняем.
            Destroy(spike, spikeLifetime);

            if (i < totalSpikes - 1)
                yield return new WaitForSeconds(spikeDelay);
        }
    }

    private IEnumerator BigSpikeTrapAttack()
    {
        if (earthSpikePrefab == null ||
            bigSpikePoint1 == null ||
            bigSpikePoint2 == null)
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(warningTime);
        SetWarning(false);

        GameObject spike1 = Instantiate(
            earthSpikePrefab,
            bigSpikePoint1.position,
            Quaternion.identity
        );

        GameObject spike2 = Instantiate(
            earthSpikePrefab,
            bigSpikePoint2.position,
            Quaternion.identity
        );

        EarthSpike e1 = spike1.GetComponent<EarthSpike>();
        EarthSpike e2 = spike2.GetComponent<EarthSpike>();

        if (e1 != null)
            e1.damage = earthSpikeDamage;

        if (e2 != null)
            e2.damage = earthSpikeDamage;

        // Оба шипа стандартного размера.
        Destroy(spike1, bigSpikeLifetime);
        Destroy(spike2, bigSpikeLifetime);
    }

    private IEnumerator LightningAttack()
    {
        if (!IsPlayerAboveBoss() || lightningPrefab == null)
            yield break;

        SetWarning(true);
        yield return new WaitForSeconds(lightningWarningTime);
        SetWarning(false);

        if (!IsPlayerAboveBoss() || player == null || isDead)
            yield break;

        Vector3 position = new Vector3(
            player.position.x,
            player.position.y + lightningHeight,
            player.position.z
        );

        GameObject lightning = Instantiate(
            lightningPrefab,
            position,
            Quaternion.identity
        );

        Lightning2D ls = lightning.GetComponent<Lightning2D>();

        if (ls != null)
        {
            ls.damage = lightningDamage;
            ls.SetTarget(player);
        }
    }

    private bool IsPlayerGrounded()
    {
        if (player == null)
            return false;

        Player2D ps = player.GetComponent<Player2D>();

        if (ps == null || ps.groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            ps.groundCheck.position,
            ps.groundCheckRadius,
            ps.groundLayer
        );
    }

    private bool IsPlayerAboveBoss()
    {
        return player != null &&
               player.position.y > transform.position.y + 1f;
    }

    private void SetWarning(bool active)
    {
        if (warningGlow != null)
            warningGlow.color = active ? warningColor : normalColor;
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;

            StopAllCoroutines();
            SetWarning(false);
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;

        if (teleportPoints != null)
        {
            foreach (Transform point in teleportPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.5f);
            }
        }

        if (bigSpikePoint1 != null)
            Gizmos.DrawWireSphere(bigSpikePoint1.position, 0.4f);

        if (bigSpikePoint2 != null)
            Gizmos.DrawWireSphere(bigSpikePoint2.position, 0.4f);
    }
}