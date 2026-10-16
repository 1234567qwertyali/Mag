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
    Lightning,

    // Новые навыки
    FireballFan,     // веер огненных шаров
    MeteorRain,      // шары падают сверху
    FireCircleTrio,  // три огненных круга рядом
    LightningStorm,  // серия молний подряд
    DragonHeadPair,  // две головы дракона
    SpikeWaveBoth,   // шипы в обе стороны от босса
    SpikeChase       // шипы под игроком, один за другим
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

    [Header("TELEPORT (по урону)")]
    public Transform[] teleportPoints;
    public GameObject teleportEffect;
    public float teleportEffectLifetime = 2f;
    [Tooltip("Каждые N полученного урона босс телепортируется и сразу атакует. 0 = выключено.")]
    [Min(0)] public int damageToTeleport = 200;

    [Header("FIREBALL MACHINE GUN")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public int fireballDamage = 20;
    public int burstFireballCount = 7;
    public float burstFireballDelay = 0.2f;
    [Tooltip("Включи, если нос спрайта шара смотрит ВПРАВО (хвост слева). Выключи, если нос смотрит ВЛЕВО (хвост справа).")]
    public bool fireballSpriteFacesRight = false;

    [Header("FIREBALL FAN (веер)")]
    [Min(2)] public int fanFireballCount = 7;
    [Tooltip("Общий угол веера в градусах")]
    public float fanAngle = 80f;

    [Header("METEOR RAIN (шары сверху)")]
    [Min(1)] public int meteorCount = 8;
    public float meteorDelay = 0.2f;
    [Tooltip("Разброс по X вокруг игрока")]
    public float meteorSpreadX = 5f;
    [Tooltip("Высота над игроком, откуда падают шары")]
    public float meteorHeight = 8f;

    [Header("FIRE DRAGON HEAD")]
    public GameObject dragonHeadPrefab;
    public Transform dragonHeadSpawnPoint;
    public int dragonHeadDamage = 60;
    public float dragonHeadSpeed = 5f;
    public float dragonHeadLifetime = 6f;
    [Tooltip("Пауза между двумя головами в навыке DragonHeadPair")]
    public float dragonPairDelay = 0.8f;

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

    [Header("LIGHTNING STORM (серия молний)")]
    [Min(1)] public int stormStrikes = 4;
    public float stormStrikeDelay = 0.4f;

    [Header("EARTH SPIKES")]
    public GameObject earthSpikePrefab;
    public int earthSpikeDamage = 40;
    public int totalSpikes = 20;
    public float spikeDistance = 1.5f;
    public float spikeDelay = 0.15f;
    public float spikeLifetime = 2f;
    public float spikeHeight = 0f;

    [Header("SPIKE CHASE (шипы под игроком)")]
    [Min(1)] public int chaseSpikeCount = 5;
    [Tooltip("Сколько секунд игрок может убежать, прежде чем шип появится в запомненной точке")]
    public float chaseSpikeTelegraph = 0.5f;
    public float chaseSpikeGap = 0.15f;

    [Header("BIG SPIKE TRAP (до 10 шипов и больше)")]
    [Tooltip("Точки, где появятся шипы. Создай пустые объекты на арене и перетащи сюда (например, 10 штук).")]
    public Transform[] bigSpikePoints;
    public float bigSpikeLifetime = 4f;
    [Tooltip("0 = все шипы сразу. Больше 0 = шипы появляются по очереди с этой паузой.")]
    [Min(0f)] public float bigSpikeDelay = 0f;

    [Header("FIRE CIRCLE")]
    public GameObject fireCirclePrefab;
    public int fireCircleDamage = 40;
    public float fireCircleWarningTime = 1f;
    [Tooltip("Сколько кругов в навыке FireCircleTrio")]
    [Min(2)] public int fireCircleTrioCount = 3;
    [Tooltip("Расстояние между кругами")]
    public float fireCircleSpacing = 3f;

    [Header("ATTACK SETTINGS")]
    [Tooltip("Сколько навыков подряд (минимум)")]
    [Min(1)] public int minSkillsInCombo = 3;
    [Tooltip("Сколько навыков подряд (максимум)")]
    [Min(1)] public int maxSkillsInCombo = 4;
    [Tooltip("Сколько секунд босс ничего не делает после серии навыков")]
    [Min(0f)] public float restTime = 7f;
    public float warningTime = 0.5f;
    [Tooltip("Пауза между навыками в серии")]
    public float comboDelay = 0.5f;

    [Header("ATTACK WEIGHTS: ВБЛИЗИ (вес = относительный шанс)")]
    [Tooltip("Игрок на земле")]
    [Min(0f)] public float bigSpikeTrapChance = 0.2f;
    [Tooltip("Игрок на земле (комбо шипы + шары + молния)")]
    [Min(0f)] public float spikeChance = 0.3f;
    [Tooltip("Игрок на земле")]
    [Min(0f)] public float spikeWaveBothChance = 0.3f;
    [Tooltip("Игрок на земле")]
    [Min(0f)] public float spikeChaseChance = 0.3f;
    [Min(0f)] public float fireCircleChance = 0.2f;
    [Min(0f)] public float fireCircleTrioChance = 0.2f;
    [Tooltip("Игрок выше босса")]
    [Min(0f)] public float lightningChance = 0.4f;
    [Min(0f)] public float lightningStormChance = 0.3f;
    [Min(0f)] public float fireballChance = 0.3f;
    [Min(0f)] public float fireballFanChance = 0.3f;
    [Min(0f)] public float meteorRainChance = 0.3f;

    [Header("ATTACK WEIGHTS: ИЗДАЛЕКА")]
    [Min(0f)] public float farFireWallChance = 0.35f;
    [Min(0f)] public float farDragonHeadChance = 0.3f;
    [Min(0f)] public float farDragonPairChance = 0.2f;
    [Min(0f)] public float farMeteorRainChance = 0.3f;
    [Min(0f)] public float farFireballFanChance = 0.25f;
    [Min(0f)] public float farLightningStormChance = 0.2f;

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

    private bool isAttacking;   // идёт вся серия атак, включая отдых
    private bool isCasting;     // идёт сам скил, без отдыха
    private bool isDead;
    private int lastTeleportIndex = -1;
    private int damageAccumulated; // накопленный урон для телепорта

    private BossSkill lastSkill;
    private bool hasLastSkill;

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
        damageToTeleport = Mathf.Max(0, damageToTeleport);
        minSkillsInCombo = Mathf.Max(1, minSkillsInCombo);
        maxSkillsInCombo = Mathf.Max(minSkillsInCombo, maxSkillsInCombo);
    }

    private void Awake()
    {
        BuildColorLookup();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        damageAccumulated = 0;
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
            new BossSkillColor(BossSkill.FireballBurst,   new Color(1f, 0.5f, 0f)),       // оранжевый
            new BossSkillColor(BossSkill.DragonHead,      new Color(0.8f, 0f, 0f)),       // тёмно-красный
            new BossSkillColor(BossSkill.FireWall,        new Color(1f, 0.9f, 0f)),       // жёлтый
            new BossSkillColor(BossSkill.FireCircle,      new Color(1f, 0f, 0.5f)),       // розовый
            new BossSkillColor(BossSkill.EarthSpikes,     new Color(0.55f, 0.3f, 0.1f)),  // коричневый
            new BossSkillColor(BossSkill.SpikeCombo,      new Color(0.55f, 0.3f, 0.1f)),
            new BossSkillColor(BossSkill.BigSpikeTrap,    new Color(0.6f, 0f, 1f)),       // фиолетовый
            new BossSkillColor(BossSkill.Lightning,       new Color(0.3f, 0.8f, 1f)),     // голубой

            new BossSkillColor(BossSkill.FireballFan,     new Color(1f, 0.7f, 0.3f)),     // светло-оранжевый
            new BossSkillColor(BossSkill.MeteorRain,      new Color(0.6f, 0.1f, 0.1f)),   // бордовый
            new BossSkillColor(BossSkill.FireCircleTrio,  new Color(1f, 0.4f, 0.7f)),     // светло-розовый
            new BossSkillColor(BossSkill.LightningStorm,  new Color(0.1f, 0.3f, 1f)),     // синий
            new BossSkillColor(BossSkill.DragonHeadPair,  new Color(0.5f, 0f, 0f)),       // очень тёмно-красный
            new BossSkillColor(BossSkill.SpikeWaveBoth,   new Color(0.35f, 0.2f, 0.05f)), // тёмно-коричневый
            new BossSkillColor(BossSkill.SpikeChase,      new Color(0.4f, 1f, 0.3f))      // зелёный
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

    // ---------- Серия атак ----------

    private bool CanStartAttack()
    {
        if (IsPlayerGrounded())
            return true;

        return (transform.position - player.position).sqrMagnitude
               <= attackRange * attackRange;
    }

    // Серия из нескольких навыков, потом отдых.
    private IEnumerator StartAttack()
    {
        isAttacking = true;

        int skillsInCombo = Random.Range(
            minSkillsInCombo,
            Mathf.Max(minSkillsInCombo, maxSkillsInCombo) + 1
        );

        for (int i = 0; i < skillsInCombo; i++)
        {
            if (isDead || player == null)
                break;

            // Дальность определяем заново перед каждым навыком.
            bool playerIsFar = (transform.position - player.position).sqrMagnitude
                               >= farDistance * farDistance;

            BossSkill chosen;
            if (!TryChooseSkill(playerIsFar, out chosen))
                break;

            lastSkill = chosen;
            hasLastSkill = true;

            isCasting = true;
            yield return StartCoroutine(RunSkill(chosen));
            isCasting = false;

            if (isDead)
                break;

            if (i < skillsInCombo - 1)
                yield return new WaitForSeconds(comboDelay);
        }

        isCasting = false;
        SetWarning(false);

        // После серии босс ничего не делает.
        if (!isDead)
            yield return new WaitForSeconds(restTime);

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
            case BossSkill.FireballFan: return FireballFanAttack();
            case BossSkill.MeteorRain: return MeteorRainAttack();
            case BossSkill.FireCircleTrio: return FireCircleTrioAttack();
            case BossSkill.LightningStorm: return LightningStormAttack();
            case BossSkill.DragonHeadPair: return DragonHeadPairAttack();
            case BossSkill.SpikeWaveBoth: return SpikeWaveBothAttack();
            case BossSkill.SpikeChase: return SpikeChaseAttack();
            default: return EmptyRoutine();
        }
    }

    private static IEnumerator EmptyRoutine()
    {
        yield break;
    }

    // ---------- Выбор навыка ----------

    // Взвешенный выбор среди доступных навыков. Прошлый навык не повторяется,
    // если есть из чего выбирать.
    private bool TryChooseSkill(bool far, out BossSkill skill)
    {
        candidates.Clear();

        if (far)
            BuildFarCandidates();
        else
            BuildNearCandidates();

        if (candidates.Count > 1 && hasLastSkill)
            candidates.RemoveAll(c => c.Key == lastSkill);

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

    private void BuildNearCandidates()
    {
        if (IsPlayerGrounded())
        {
            AddCandidate(BossSkill.BigSpikeTrap, bigSpikeTrapChance);
            AddCandidate(BossSkill.SpikeCombo, spikeChance);
            AddCandidate(BossSkill.SpikeWaveBoth, spikeWaveBothChance);
            AddCandidate(BossSkill.SpikeChase, spikeChaseChance);
        }

        AddCandidate(BossSkill.FireCircle, fireCircleChance);
        AddCandidate(BossSkill.FireCircleTrio, fireCircleTrioChance);

        if (IsPlayerAboveBoss())
            AddCandidate(BossSkill.Lightning, lightningChance);

        AddCandidate(BossSkill.LightningStorm, lightningStormChance);
        AddCandidate(BossSkill.FireballBurst, fireballChance);
        AddCandidate(BossSkill.FireballFan, fireballFanChance);
        AddCandidate(BossSkill.MeteorRain, meteorRainChance);
    }

    private void BuildFarCandidates()
    {
        AddCandidate(BossSkill.FireWall, farFireWallChance);
        AddCandidate(BossSkill.DragonHead, farDragonHeadChance);
        AddCandidate(BossSkill.DragonHeadPair, farDragonPairChance);
        AddCandidate(BossSkill.MeteorRain, farMeteorRainChance);
        AddCandidate(BossSkill.FireballFan, farFireballFanChance);
        AddCandidate(BossSkill.LightningStorm, farLightningStormChance);
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
            case BossSkill.FireballFan:
                return fireballPrefab != null && firePoint != null;
            case BossSkill.MeteorRain:
                return fireballPrefab != null;
            case BossSkill.DragonHead:
            case BossSkill.DragonHeadPair:
                return dragonHeadPrefab != null;
            case BossSkill.FireWall:
                return fireWallPrefab != null;
            case BossSkill.FireCircle:
            case BossSkill.FireCircleTrio:
                return fireCirclePrefab != null;
            case BossSkill.EarthSpikes:
            case BossSkill.SpikeCombo:
            case BossSkill.SpikeWaveBoth:
            case BossSkill.SpikeChase:
                return earthSpikePrefab != null;
            case BossSkill.BigSpikeTrap:
                return earthSpikePrefab != null && HasBigSpikePoints();
            case BossSkill.Lightning:
            case BossSkill.LightningStorm:
                return lightningPrefab != null;
            default:
                return true;
        }
    }

    private bool HasBigSpikePoints()
    {
        if (bigSpikePoints == null)
            return false;

        for (int i = 0; i < bigSpikePoints.Length; i++)
        {
            if (bigSpikePoints[i] != null)
                return true;
        }

        return false;
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

    // Копим урон. Набралось damageToTeleport: телепорт и сразу новая серия атак.
    private void CheckDamageTeleport(int damage)
    {
        if (damageToTeleport <= 0)
            return;

        damageAccumulated += damage;

        if (damageAccumulated < damageToTeleport)
            return;

        // Остаток переносим дальше, чтобы лишний урон не терялся.
        damageAccumulated %= damageToTeleport;
        TeleportAndAttack();
    }

    // Прерывает всё текущее (скил или отдых), телепортируется и сразу атакует.
    private void TeleportAndAttack()
    {
        StopAllCoroutines();

        indicatorRoutine = null;
        isCasting = false;
        isAttacking = false;

        SetWarning(false);

        if (skillIndicator != null)
            skillIndicator.color = idleColor;

        TeleportToPoint();

        if (player != null)
            StartCoroutine(StartAttack());
    }

    // ---------- Общие спавнеры ----------

    private void SpawnFireball(Vector3 position, Vector2 direction)
    {
        GameObject fb = Instantiate(fireballPrefab, position, Quaternion.identity);

        AimFireball(fb, direction);

        Fireball2D fs = fb.GetComponent<Fireball2D>();

        if (fs != null)
        {
            fs.damage = fireballDamage;
            fs.SetDirection(direction);
        }
    }

    // Поворачивает шар носом по направлению полёта: влево, вправо, вверх, вниз, по диагонали.
    private void AimFireball(GameObject fb, Vector2 direction)
    {
        if (fb == null || direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool flyingLeft = direction.x < 0f;

        float rotation;
        bool flipY;

        if (fireballSpriteFacesRight)
        {
            // Нос спрайта смотрит вправо.
            rotation = angle;
            flipY = flyingLeft;     // при полёте влево шар не должен быть "вверх ногами"
        }
        else
        {
            // Нос спрайта смотрит влево.
            rotation = angle + 180f;
            flipY = !flyingLeft;
        }

        fb.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        if (flipY)
        {
            Vector3 s = fb.transform.localScale;
            s.y = -s.y;
            fb.transform.localScale = s;
        }
    }

    private void SpawnDragonHead()
    {
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

    private void SpawnFireCircle(Vector3 position)
    {
        GameObject circle = Instantiate(
            fireCirclePrefab, position, Quaternion.identity
        );

        FireCircle script = circle.GetComponent<FireCircle>();

        if (script != null)
        {
            script.damage = fireCircleDamage;
            script.warningTime = fireCircleWarningTime;
        }
    }

    private void SpawnLightning()
    {
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

    // ---------- Скилы ----------

    // Серия огненных шаров. Каждый шар летит носом на игрока.
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

            Vector2 direction =
                ((Vector2)player.position - (Vector2)firePoint.position).normalized;

            SpawnFireball(firePoint.position, direction);

            if (i < burstFireballCount - 1)
                yield return new WaitForSeconds(burstFireballDelay);
        }
    }

    // Веер шаров: все сразу, в сторону игрока, с разбросом по углу.
    private IEnumerator FireballFanAttack()
    {
        if (player == null || !HasAssets(BossSkill.FireballFan))
            yield break;

        ShowSkillColor(BossSkill.FireballFan);
        yield return WarnRoutine(warningTime);

        if (player == null || isDead)
            yield break;

        Vector2 toPlayer =
            ((Vector2)player.position - (Vector2)firePoint.position).normalized;

        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        int count = Mathf.Max(2, fanFireballCount);
        float step = fanAngle / (count - 1);
        float startAngle = baseAngle - fanAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float rad = (startAngle + step * i) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            SpawnFireball(firePoint.position, direction);
        }
    }

    // Шары падают сверху вокруг игрока.
    private IEnumerator MeteorRainAttack()
    {
        if (player == null || !HasAssets(BossSkill.MeteorRain))
            yield break;

        ShowSkillColor(BossSkill.MeteorRain);
        yield return WarnRoutine(warningTime);

        for (int i = 0; i < meteorCount; i++)
        {
            if (player == null || isDead)
                yield break;

            Vector3 position = new Vector3(
                player.position.x + Random.Range(-meteorSpreadX, meteorSpreadX),
                player.position.y + meteorHeight,
                player.position.z
            );

            // Падают вниз с лёгким наклоном.
            Vector2 direction =
                new Vector2(Random.Range(-0.15f, 0.15f), -1f).normalized;

            SpawnFireball(position, direction);

            if (i < meteorCount - 1)
                yield return new WaitForSeconds(meteorDelay);
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

        SpawnDragonHead();
    }

    // Две головы дракона с паузой.
    private IEnumerator DragonHeadPairAttack()
    {
        if (player == null || !HasAssets(BossSkill.DragonHeadPair))
            yield break;

        ShowSkillColor(BossSkill.DragonHeadPair);
        yield return WarnRoutine(warningTime);

        if (player == null || isDead)
            yield break;

        SpawnDragonHead();

        yield return new WaitForSeconds(dragonPairDelay);

        if (player == null || isDead)
            yield break;

        SpawnDragonHead();
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

        SpawnFireCircle(player.position);
    }

    // Несколько огненных кругов в ряд: один под игроком, остальные по бокам.
    private IEnumerator FireCircleTrioAttack()
    {
        if (player == null || !HasAssets(BossSkill.FireCircleTrio))
            yield break;

        ShowSkillColor(BossSkill.FireCircleTrio);
        yield return WarnRoutine(fireCircleWarningTime);

        if (player == null || isDead)
            yield break;

        int count = Mathf.Max(2, fireCircleTrioCount);
        float center = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 position = player.position +
                new Vector3((i - center) * fireCircleSpacing, 0f, 0f);

            SpawnFireCircle(position);
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

    // Волна шипов сразу в обе стороны от босса.
    private IEnumerator SpikeWaveBothAttack()
    {
        if (!HasAssets(BossSkill.SpikeWaveBoth))
            yield break;

        if (!IsPlayerGrounded())
            yield break;

        ShowSkillColor(BossSkill.SpikeWaveBoth);
        yield return WarnRoutine(warningTime);

        if (isDead)
            yield break;

        Vector3 origin = transform.position;
        int perSide = Mathf.Max(1, totalSpikes / 2);

        for (int i = 0; i < perSide; i++)
        {
            if (isDead)
                yield break;

            float offset = i * spikeDistance;

            SpawnSpike(
                new Vector3(origin.x + offset, origin.y + spikeHeight, origin.z),
                spikeLifetime
            );

            // Нулевой шип в центре не дублируем.
            if (i > 0)
            {
                SpawnSpike(
                    new Vector3(origin.x - offset, origin.y + spikeHeight, origin.z),
                    spikeLifetime
                );
            }

            if (i < perSide - 1)
                yield return new WaitForSeconds(spikeDelay);
        }
    }

    // Шипы под игроком: босс запоминает точку, через chaseSpikeTelegraph там вылезает шип.
    private IEnumerator SpikeChaseAttack()
    {
        if (player == null || !HasAssets(BossSkill.SpikeChase))
            yield break;

        if (!IsPlayerGrounded())
            yield break;

        ShowSkillColor(BossSkill.SpikeChase);
        yield return WarnRoutine(warningTime);

        for (int i = 0; i < chaseSpikeCount; i++)
        {
            if (player == null || isDead)
                yield break;

            Vector3 target = new Vector3(
                player.position.x,
                player.position.y + spikeHeight,
                player.position.z
            );

            SetWarning(true);
            yield return new WaitForSeconds(chaseSpikeTelegraph);
            SetWarning(false);

            if (isDead)
                yield break;

            SpawnSpike(target, spikeLifetime);

            if (i < chaseSpikeCount - 1)
                yield return new WaitForSeconds(chaseSpikeGap);
        }
    }

    // Шипы во всех заданных точках (можно 10 и больше).
    private IEnumerator BigSpikeTrapAttack()
    {
        if (!HasAssets(BossSkill.BigSpikeTrap))
            yield break;

        ShowSkillColor(BossSkill.BigSpikeTrap);
        yield return WarnRoutine(warningTime);

        if (isDead)
            yield break;

        for (int i = 0; i < bigSpikePoints.Length; i++)
        {
            if (isDead)
                yield break;

            Transform point = bigSpikePoints[i];

            if (point == null)
                continue;

            SpawnSpike(point.position, bigSpikeLifetime);

            if (bigSpikeDelay > 0f && i < bigSpikePoints.Length - 1)
                yield return new WaitForSeconds(bigSpikeDelay);
        }
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

        SpawnLightning();
    }

    // Серия молний подряд, каждая бьёт туда, где игрок сейчас.
    private IEnumerator LightningStormAttack()
    {
        if (player == null || !HasAssets(BossSkill.LightningStorm))
            yield break;

        ShowSkillColor(BossSkill.LightningStorm);
        yield return WarnRoutine(lightningWarningTime);

        for (int i = 0; i < stormStrikes; i++)
        {
            if (player == null || isDead)
                yield break;

            SpawnLightning();

            if (i < stormStrikes - 1)
                yield return new WaitForSeconds(stormStrikeDelay);
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
        {
            CheckDamageTeleport(damage);
            return;
        }

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

        Gizmos.color = new Color(0.6f, 0f, 1f);

        if (bigSpikePoints != null)
        {
            foreach (Transform point in bigSpikePoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.4f);
            }
        }

        // Точка индикатора скилов.
        Gizmos.color = Color.magenta;
        Vector3 indicatorPos = indicatorPoint != null
            ? indicatorPoint.position
            : transform.position + fallbackOffset;
        Gizmos.DrawWireCube(indicatorPos, Vector3.one * indicatorSize);
    }
}
