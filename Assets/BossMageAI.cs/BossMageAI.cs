using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum BossSkill
{
    FireballBurst,
    DragonHead,
    FireWall,
    FireCircle,
    EarthSpikes,
    SpikeCombo,
    BigSpikeTrap,
    Lightning
}

[System.Serializable]
public class BossSkillColor
{
    public BossSkill skill;
    public Color color = Color.white;

    public BossSkillColor(BossSkill skill, Color color)
    {
        this.skill = skill;
        this.color = color;
    }
}

public class BossMageAI : MonoBehaviour
{
    [Header("BOSS HP")]
    public int maxHealth = 1000;
    public int currentHealth = 1000;
    public UnityEvent onDeath;

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

    [Header("ATTACK SETTINGS")]
    public float attackCooldown = 2f;
    public float warningTime = 0.5f;
    public float comboDelay = 0.2f;

    [Header("ATTACK WEIGHTS (вес = относительный шанс)")]
    [Tooltip("Вблизи, игрок на земле")]
    [Min(0f)] public float bigSpikeTrapChance = 0.2f;
    [Tooltip("Вблизи, игрок на земле")]
    [Min(0f)] public float spikeChance = 0.4f;
    [Tooltip("Вблизи")]
    [Min(0f)] public float fireCircleChance = 0.2f;
    [Tooltip("Вблизи, игрок выше босса")]
    [Min(0f)] public float lightningChance = 0.5f;
    [Tooltip("Вблизи, всегда доступен")]
    [Min(0f)] public float fireballChance = 0.3f;
    [Tooltip("Издалека: шанс стены огня, иначе голова дракона")]
    [Range(0f, 1f)] public float fireWallChance = 0.35f;

    [Header("WARNING VISUAL")]
    public SpriteRenderer warningGlow;
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    [Header("SKILL COLOR INDICATOR")]
    [Tooltip("Точка, в которой висит квадрат. Создай пустой дочерний объект над головой босса.")]
    public Transform indicatorPoint;
    [Tooltip("Квадрат. Если не назначен, будет создан автоматически в indicatorPoint.")]
    public SpriteRenderer skillIndicator;
    public float indicatorSize = 0.5f;
    public int indicatorSortingOrder = 20;
    [Tooltip("Если indicatorPoint не назначен: смещение над боссом")]
    public Vector3 fallbackOffset = new Vector3(0f, 2.5f, 0f);
    public Color idleColor = Color.gray;
    public float indicatorFadeTime = 0.6f;
    [Tooltip("Если пусто или чего-то не хватает, возьмутся цвета по умолчанию")]
    public List<BossSkillColor> skillColors = new List<BossSkillColor>();

    // ---------- Внутреннее состояние ----------

    private bool isAttacking;   // идёт вся атака, включая кулдаун
    private bool isCasting;     // идёт сам скил, без кулдауна
    private bool isDead;
    private int lastTeleportIndex = -1;

    private Player2D playerController;
    private Coroutine indicatorRoutine;
    private bool indicatorWasCreated;

    private readonly List<int> validTeleportPoints = new List<int>();
    private readonly List<KeyValuePair<BossSkill, float>> candidates =
        new List<KeyValuePair<BossSkill, float>>();
    private readonly Dictionary<BossSkill, Color> colorLookup =
        new Dictionary<BossSkill, Color>();

    // ---------- Unity ----------

    private void Reset()
    {
        skillColors = CreateDefaultColors();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        totalSpikes = Mathf.Max(1, totalSpikes);
        burstFireballCount = Mathf.Max(1, burstFireballCount);
        indicatorSize = Mathf.Max(0.05f, indicatorSize);
    }

    private void Awake()
    {
        BuildColorLookup();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        CachePlayer();
        SetWarning(false);
        SetupIndicator();
    }

    private void Update()
    {
        if (isDead || isAttacking || player == null)
            return;

        if (CanStartAttack())
            StartCoroutine(StartAttack());
    }

    private void LateUpdate()
    {
        FollowIndicatorPoint();
    }

    private void OnDestroy()
    {
        // Автоматически созданный квадрат не является ребёнком босса.
        if (indicatorWasCreated && skillIndicator != null)
            Destroy(skillIndicator.gameObject);
    }

    // ---------- Индикатор скилов (цветной квадрат) ----------

    private void SetupIndicator()
    {
        if (skillIndicator == null)
        {
            GameObject go = new GameObject("SkillIndicator");
            skillIndicator = go.AddComponent<SpriteRenderer>();

            Texture2D tex = Texture2D.whiteTexture;
            skillIndicator.sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width // 1 юнит = размер текстуры, значит квадрат 1x1
            );

            go.transform.localScale = new Vector3(indicatorSize, indicatorSize, 1f);
            indicatorWasCreated = true;
        }

        skillIndicator.sortingOrder = indicatorSortingOrder;
        skillIndicator.color = idleColor;

        FollowIndicatorPoint();
    }

    private void FollowIndicatorPoint()
    {
        if (skillIndicator == null)
            return;

        // Если квадрат создан кодом или стоит отдельно от точки, переносим его в точку.
        // Позиция берётся каждый кадр, поэтому поворот босса квадрат не ломает.
        if (indicatorPoint != null)
        {
            skillIndicator.transform.position = indicatorPoint.position;
        }
        else if (indicatorWasCreated)
        {
            skillIndicator.transform.position = transform.position + fallbackOffset;
        }
    }

    private void ShowSkillColor(BossSkill skill)
    {
        if (skillIndicator == null)
            return;

        if (indicatorRoutine != null)
            StopCoroutine(indicatorRoutine);

        indicatorRoutine = StartCoroutine(IndicatorRoutine(GetColor(skill)));
    }

    private IEnumerator IndicatorRoutine(Color color)
    {
        skillIndicator.color = color;

        // Цвет держится, пока скил (или комбо) выполняется.
        while (isCasting && !isDead)
            yield return null;

        // Плавно гаснем до серого.
        float t = 0f;
        float fade = Mathf.Max(0.01f, indicatorFadeTime);

        while (t < fade)
        {
            t += Time.deltaTime;
            skillIndicator.color = Color.Lerp(color, idleColor, t / fade);
            yield return null;
        }

        skillIndicator.color = idleColor;
        indicatorRoutine = null;
    }

    private static List<BossSkillColor> CreateDefaultColors()
    {
        return new List<BossSkillColor>
        {
            new BossSkillColor(BossSkill.FireballBurst, new Color(1f, 0.5f, 0f)),     // оранжевый
            new BossSkillColor(BossSkill.DragonHead,    new Color(0.8f, 0f, 0f)),     // тёмно-красный
            new BossSkillColor(BossSkill.FireWall,      new Color(1f, 0.9f, 0f)),     // жёлтый
            new BossSkillColor(BossSkill.FireCircle,    new Color(1f, 0f, 0.5f)),     // розовый
            new BossSkillColor(BossSkill.EarthSpikes,   new Color(0.55f, 0.3f, 0.1f)),// коричневый
            new BossSkillColor(BossSkill.SpikeCombo,    new Color(0.55f, 0.3f, 0.1f)),
            new BossSkillColor(BossSkill.BigSpikeTrap,  new Color(0.6f, 0f, 1f)),     // фиолетовый
            new BossSkillColor(BossSkill.Lightning,     new Color(0.3f, 0.8f, 1f))    // голубой
        };
    }

    private void BuildColorLookup()
    {
        colorLookup.Clear();

        // Сначала цвета по умолчанию, потом поверх них то, что задано в инспекторе.
        foreach (BossSkillColor c in CreateDefaultColors())
            colorLookup[c.skill] = c.color;

        if (skillColors == null)
            return;

        foreach (BossSkillColor c in skillColors)
        {
            if (c != null)
                colorLookup[c.skill] = c.color;
        }
    }

    private Color GetColor(BossSkill skill)
    {
        if (colorLookup.Count == 0)
            BuildColorLookup();

        return colorLookup.TryGetValue(skill, out Color color) ? color : Color.white;
    }

    // ---------- Кеширование ----------

    private void CachePlayer()
    {
        playerController = player != null
            ? player.GetComponent<Player2D>()
            : null;
    }

    // ---------- Выбор и запуск атаки ----------

    private bool CanStartAttack()
    {
        if (IsPlayerGrounded())
            return true;

        return (transform.position - player.position).sqrMagnitude
               <= attackRange * attackRange;
    }

    private IEnumerator StartAttack()
    {
        isAttacking = true;

        // Дальность определяем ДО телепортации.
        bool playerWasFar = (transform.position - player.position).sqrMagnitude
                            >= farDistance * farDistance;

        if (teleportBeforeAttack)
            TeleportToPoint();

        if (isDead || player == null)
        {
            isAttacking = false;
            yield break;
        }

        BossSkill chosen;
        bool hasSkill = playerWasFar
            ? TryChooseFarSkill(out chosen)
            : TryChooseNearSkill(out chosen);

        if (hasSkill)
        {
            isCasting = true;
            yield return StartCoroutine(RunSkill(chosen));
            isCasting = false;
        }

        SetWarning(false);

        if (!isDead)
            yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    private IEnumerator RunSkill(BossSkill skill)
    {
        switch (skill)
        {
            case BossSkill.FireballBurst: return FireballBurstAttack();
            case BossSkill.DragonHead: return DragonHeadAttack();
            case BossSkill.FireWall: return FireWallAttack();
            case BossSkill.FireCircle: return FireCircleAttack();
            case BossSkill.EarthSpikes: return EarthSpikeRoutine();
            case BossSkill.SpikeCombo: return SpikeCombo();
            case BossSkill.BigSpikeTrap: return BigSpikeTrapAttack();
            case BossSkill.Lightning: return LightningAttack();
            default: return EmptyRoutine();
        }
    }

    private static IEnumerator EmptyRoutine()
    {
        yield break;
    }

    // Издалека: стена огня или голова дракона.
    private bool TryChooseFarSkill(out BossSkill skill)
    {
        bool wantWall = Random.value < fireWallChance;
        BossSkill first = wantWall ? BossSkill.FireWall : BossSkill.DragonHead;
        BossSkill second = wantWall ? BossSkill.DragonHead : BossSkill.FireWall;

        if (HasAssets(first)) { skill = first; return true; }
        if (HasAssets(second)) { skill = second; return true; }

        skill = BossSkill.FireballBurst;
        return false;
    }

    // Вблизи: взвешенный выбор среди доступных скилов.
    private bool TryChooseNearSkill(out BossSkill skill)
    {
        candidates.Clear();

        if (IsPlayerGrounded())
        {
            AddCandidate(BossSkill.BigSpikeTrap, bigSpikeTrapChance);
            AddCandidate(BossSkill.SpikeCombo, spikeChance);
        }

        AddCandidate(BossSkill.FireCircle, fireCircleChance);

        if (IsPlayerAboveBoss())
            AddCandidate(BossSkill.Lightning, lightningChance);

        AddCandidate(BossSkill.FireballBurst, fireballChance);

        float total = 0f;
        foreach (var c in candidates)
            total += c.Value;

        if (candidates.Count == 0 || total <= 0f)
        {
            skill = BossSkill.FireballBurst;
            return false;
        }

        float roll = Random.value * total;

        foreach (var c in candidates)
        {
            roll -= c.Value;
            if (roll <= 0f)
            {
                skill = c.Key;
                return true;
            }
        }

        skill = candidates[candidates.Count - 1].Key;
        return true;
    }

    private void AddCandidate(BossSkill skill, float weight)
    {
        if (weight > 0f && HasAssets(skill))
            candidates.Add(new KeyValuePair<BossSkill, float>(skill, weight));
    }

    // Проверка, что для скила назначены все нужные префабы и точки.
    private bool HasAssets(BossSkill skill)
    {
        switch (skill)
        {
            case BossSkill.FireballBurst:
                return fireballPrefab != null && firePoint != null;
            case BossSkill.DragonHead:
                return dragonHeadPrefab != null;
            case BossSkill.FireWall:
                return fireWallPrefab != null;
            case BossSkill.FireCircle:
                return fireCirclePrefab != null;
            case BossSkill.EarthSpikes:
            case BossSkill.SpikeCombo:
                return earthSpikePrefab != null;
            case BossSkill.BigSpikeTrap:
                return earthSpikePrefab != null &&
                       bigSpikePoint1 != null &&
                       bigSpikePoint2 != null;
            case BossSkill.Lightning:
                return lightningPrefab != null;
            default:
                return true;
        }
    }

    // ---------- Телепорт ----------

    private void TeleportToPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0)
            return;

        validTeleportPoints.Clear();

        for (int i = 0; i < teleportPoints.Length; i++)
        {
            if (teleportPoints[i] != null && i != lastTeleportIndex)
                validTeleportPoints.Add(i);
        }

        // Если доступна только прошлая точка, разрешаем её.
        if (validTeleportPoints.Count == 0)
        {
            for (int i = 0; i < teleportPoints.Length; i++)
            {
                if (teleportPoints[i] != null)
                    validTeleportPoints.Add(i);
            }
        }

        if (validTeleportPoints.Count == 0)
            return;

        int chosenIndex = validTeleportPoints[
            Random.Range(0, validTeleportPoints.Count)
        ];

        SpawnEffect(teleportEffect, transform.position);
        transform.position = teleportPoints[chosenIndex].position;
        lastTeleportIndex = chosenIndex;
        SpawnEffect(teleportEffect, transform.position);
    }

    private void SpawnEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(fx, teleportEffectLifetime);
    }

    // ---------- Скилы ----------

    // Серия огненных шаров.
    private IEnumerator FireballBurstAttack()
    {
        if (!HasAssets(BossSkill.FireballBurst))
            yield break;

        ShowSkillColor(BossSkill.FireballBurst);
        yield return WarnRoutine(warningTime);

        for (int i = 0; i < burstFireballCount; i++)
        {
            if (player == null || isDead)
                yield break;

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

            if (i < burstFireballCount - 1)
                yield return new WaitForSeconds(burstFireballDelay);
        }
    }

    // Голова огненного дракона.
    private IEnumerator DragonHeadAttack()
    {
        if (player == null || !HasAssets(BossSkill.DragonHead))
            yield break;

        ShowSkillColor(BossSkill.DragonHead);
        yield return WarnRoutine(warningTime);

        if (player == null || isDead)
            yield break;

        Vector3 spawnPosition = dragonHeadSpawnPoint != null
            ? dragonHeadSpawnPoint.position
            : transform.position;

        GameObject head = Instantiate(
            dragonHeadPrefab, spawnPosition, Quaternion.identity
        );

        FireDragonHead script = head.GetComponent<FireDragonHead>();

        if (script != null)
        {
            script.SetTarget(
                player,
                dragonHeadDamage,
                dragonHeadSpeed,
                dragonHeadLifetime
            );
        }
    }

    // Длинная стена огня.
    private IEnumerator FireWallAttack()
    {
        if (player == null || !HasAssets(BossSkill.FireWall))
            yield break;

        ShowSkillColor(BossSkill.FireWall);
        yield return WarnRoutine(warningTime);

        if (player == null || isDead)
            yield break;

        Vector3 spawnPosition = fireWallSpawnPoint != null
            ? fireWallSpawnPoint.position
            : transform.position;

        GameObject wall = Instantiate(
            fireWallPrefab, spawnPosition, Quaternion.identity
        );

        FireWall script = wall.GetComponent<FireWall>();

        if (script != null)
        {
            script.SetTarget(
                player,
                fireWallDamage,
                fireWallSpeed,
                fireWallLifetime
            );
        }
    }

    // Огненный круг под игроком.
    private IEnumerator FireCircleAttack()
    {
        if (player == null || !HasAssets(BossSkill.FireCircle))
            yield break;

        ShowSkillColor(BossSkill.FireCircle);
        yield return WarnRoutine(fireCircleWarningTime);

        if (player == null || isDead)
            yield break;

        GameObject circle = Instantiate(
            fireCirclePrefab, player.position, Quaternion.identity
        );

        FireCircle script = circle.GetComponent<FireCircle>();

        if (script != null)
        {
            script.damage = fireCircleDamage;
            script.warningTime = fireCircleWarningTime;
        }
    }

    // Комбо: шипы, потом огненные шары, потом молния (если игрок выше).
    private IEnumerator SpikeCombo()
    {
        yield return EarthSpikeRoutine();
        if (isDead) yield break;

        yield return new WaitForSeconds(comboDelay);
        yield return FireballBurstAttack();
        if (isDead) yield break;

        yield return new WaitForSeconds(comboDelay);

        if (IsPlayerAboveBoss())
            yield return LightningAttack();
    }

    // Волна шипов в сторону игрока.
    private IEnumerator EarthSpikeRoutine()
    {
        if (player == null || !HasAssets(BossSkill.EarthSpikes))
            yield break;

        if (!IsPlayerGrounded())
            yield break;

        ShowSkillColor(BossSkill.EarthSpikes);
        yield return WarnRoutine(warningTime);

        if (player == null || isDead || !IsPlayerGrounded())
            yield break;

        float direction = Mathf.Sign(player.position.x - transform.position.x);
        if (direction == 0f)
            direction = 1f;

        Vector3 origin = transform.position;

        for (int i = 0; i < totalSpikes; i++)
        {
            if (isDead)
                yield break;

            Vector3 position = new Vector3(
                origin.x + i * spikeDistance * direction,
                origin.y + spikeHeight,
                origin.z
            );

            SpawnSpike(position, spikeLifetime);

            if (i < totalSpikes - 1)
                yield return new WaitForSeconds(spikeDelay);
        }
    }

    // Два больших шипа в заданных точках.
    private IEnumerator BigSpikeTrapAttack()
    {
        if (!HasAssets(BossSkill.BigSpikeTrap))
            yield break;

        ShowSkillColor(BossSkill.BigSpikeTrap);
        yield return WarnRoutine(warningTime);

        if (isDead)
            yield break;

        SpawnSpike(bigSpikePoint1.position, bigSpikeLifetime);
        SpawnSpike(bigSpikePoint2.position, bigSpikeLifetime);
    }

    private void SpawnSpike(Vector3 position, float lifetime)
    {
        GameObject spike = Instantiate(
            earthSpikePrefab, position, Quaternion.identity
        );

        EarthSpike script = spike.GetComponent<EarthSpike>();
        if (script != null)
            script.damage = earthSpikeDamage;

        Destroy(spike, lifetime);
    }

    // Молния сверху, если игрок выше босса.
    private IEnumerator LightningAttack()
    {
        if (!HasAssets(BossSkill.Lightning) || !IsPlayerAboveBoss())
            yield break;

        ShowSkillColor(BossSkill.Lightning);
        yield return WarnRoutine(lightningWarningTime);

        if (player == null || isDead || !IsPlayerAboveBoss())
            yield break;

        Vector3 position = new Vector3(
            player.position.x,
            player.position.y + lightningHeight,
            player.position.z
        );

        GameObject lightning = Instantiate(
            lightningPrefab, position, Quaternion.identity
        );

        Lightning2D script = lightning.GetComponent<Lightning2D>();

        if (script != null)
        {
            script.damage = lightningDamage;
            script.SetTarget(player);
        }
    }

    // ---------- Проверки состояния игрока ----------

    private bool IsPlayerGrounded()
    {
        if (player == null)
            return false;

        if (playerController == null)
            CachePlayer();

        if (playerController == null || playerController.groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            playerController.groundCheck.position,
            playerController.groundCheckRadius,
            playerController.groundLayer
        );
    }

    private bool IsPlayerAboveBoss()
    {
        return player != null &&
               player.position.y > transform.position.y + 1f;
    }

    // ---------- Предупреждение ----------

    private IEnumerator WarnRoutine(float duration)
    {
        SetWarning(true);
        yield return new WaitForSeconds(duration);
        SetWarning(false);
    }

    private void SetWarning(bool active)
    {
        if (warningGlow != null)
            warningGlow.color = active ? warningColor : normalColor;
    }

    // ---------- Урон и смерть ----------

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth > 0)
            return;

        isDead = true;
        isAttacking = false;
        isCasting = false;

        StopAllCoroutines();
        SetWarning(false);

        if (skillIndicator != null)
            skillIndicator.color = idleColor;

        onDeath?.Invoke();
        Destroy(gameObject);
    }

    // ---------- Gizmos ----------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, farDistance);

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

        // Точка индикатора скилов.
        Gizmos.color = Color.magenta;
        Vector3 indicatorPos = indicatorPoint != null
            ? indicatorPoint.position
            : transform.position + fallbackOffset;
        Gizmos.DrawWireCube(indicatorPos, Vector3.one * indicatorSize);
    }
}