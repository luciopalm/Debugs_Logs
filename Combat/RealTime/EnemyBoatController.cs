using UnityEngine;
using System.Collections;

public class EnemyBoatController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;                    // NOVO: Vida do barco inimigo
    private int currentHealth;
    public bool isDestroyed = false;
    public float invincibilityTime = 0.3f;
    private bool isInvincible = false;
    public GameObject destructionEffect;         // NOVO: Prefab de efeito de destruição

    [Header("Detection Settings")]
    public float detectionRange = 50f;           // Detecta de longe
    public float attackRange = 16f;
    public float perfectRange = 14f;            // Range ideal (como na foto)
    public float perfectRangeTolerance = 3f;    // Margem aceitável ±3
    public float minDistance = 8f;              // Mínimo permitido

    [Header("Movement Settings")]
    public float baseMoveSpeed = 2.8f;          // Velocidade base
    public float maxMoveSpeed = 3.8f;           // Máximo quando perseguindo (36% mais)
    public float accelerationTime = 2.5f;       // Tempo para acelerar totalmente
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayers;

    [Header("Combat Settings")]
    public float attackCooldown = 1.5f;
    public float aimTime = 0.15f;
    public int projectileDamage = 1;

    [Header("Smart Shooting System")]
    public float minShootForce = 28f;
    public float maxShootForce = 65f;
    public float distanceMultiplier = 2.2f;
    public float overshootOffset = 7f;
    public float predictionMultiplier = 1.3f;

    [Header("Projectile Settings")]
    public GameObject navalProjectilePrefab;
    public float projectileLifetime = 4.5f;

    [Header("Projectile Rotation")]
    public float projectileRotationOffset = 0f;

    [Header("Layer Settings")]
    public LayerMask waterLayer;
    public LayerMask navalEnemiesLayer;
    public LayerMask boatLayer;

    // Components
    private Transform playerBoat;
    private Rigidbody2D rb;
    private Animator animator;
    private Camera mainCamera;
    private BoatController playerBoatController;
    private SpriteRenderer spriteRenderer;       // NOVO: Para efeitos visuais

    // State
    private bool canAttack = true;
    private float lastAttackTime = 0f;
    private Vector2 lastMoveDirection = Vector2.right;
    private bool isAiming = false;
    private float sideSwitchTimer = 0f;
    private bool moveRight = true;
    private Vector2 lastPlayerPosition;
    private Vector2 lastPlayerVelocity;
    private float screenWidth;
    private float screenHeight;

    // Sistema de perseguição
    private enum PursuitState { Perfect, Adjusting, Chasing, Disengaging }
    private PursuitState currentState = PursuitState.Chasing;
    private float currentMoveSpeed;
    private float accelerationTimer = 0f;
    private float timeOutsidePerfectRange = 0f;
    private float pursuitEngagementTime = 0f;
    private bool isPlayerFaster = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.layer = LayerMask.NameToLayer("NavalEnemies");

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0.25f;           // Muito responsivo
        }

        if (mainCamera != null)
        {
            screenHeight = 2f * mainCamera.orthographicSize;
            screenWidth = screenHeight * mainCamera.aspect;
        }

        // NOVO: Inicializa sistema de vida
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;

        Debug.Log($"🚢 EnemyBoat - Vida: {currentHealth}/{maxHealth} | Perseguição INSISTENTE ativada");
        FindPlayerBoat();

        moveRight = Random.value > 0.5f;
        sideSwitchTimer = Random.Range(6f, 12f); // Troca de lado bem espaçada
    }

    void Update()
    {
        UpdateAnimations();
        TrackPlayerMovement();

        if (playerBoat == null && Time.frameCount % 45 == 0)
        {
            FindPlayerBoat();
        }
    }

    void FixedUpdate()
    {
        if (isDestroyed)
        {
            SmoothStop();
            return;
        }

        if (playerBoat == null || !playerBoat.gameObject.activeSelf)
        {
            if (Time.frameCount % 30 == 0) FindPlayerBoat();
            SmoothStop();
            return;
        }

        Vector2 toPlayer = (Vector2)playerBoat.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        // Atualiza estado de perseguição
        UpdatePursuitState(distance);

        // Verifica se player é mais rápido
        CheckIfPlayerIsFaster();

        // DEBUG periódico
        if (Time.frameCount % 150 == 0 && distance <= detectionRange && !isDestroyed)
        {
            Debug.Log($"[EnemyBoat] Vida: {currentHealth}/{maxHealth} | Estado: {currentState} | Dist: {distance:F1}u");
        }

        // SISTEMA DE ATAQUE (só ataca se no range, não destruído e não desistindo)
        if (distance <= attackRange && canAttack && !isAiming && currentState != PursuitState.Disengaging && !isDestroyed)
        {
            StartCoroutine(ShootWhileMoving(toPlayer.normalized, distance));
        }

        // SISTEMA DE MOVIMENTO baseado no estado (só se não destruído)
        if (!isDestroyed)
        {
            HandleStateBasedMovement(toPlayer, distance);
        }
    }

    // NOVO: Sistema de vida e dano
    public void TakeDamage(int damage)
    {
        if (isDestroyed || isInvincible)
        {
            Debug.Log("⏭️ Inimigo invencível ou já destruído");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"💥 Barco INIMIGO levou {damage} dano! Vida: {currentHealth}/{maxHealth}");

        // Efeito visual de dano
        StartCoroutine(DamageEffect());

        if (currentHealth <= 0)
        {
            DestroyBoat();
        }
        else
        {
            StartCoroutine(InvincibilityFrame());
        }
    }

    private IEnumerator DamageEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        for (int i = 0; i < 4; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void DestroyBoat()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log("💀 BARCO INIMIGO DESTRUÍDO!");

        if (GameDataManager.Instance != null)
        {
            string enemyID = gameObject.name;
            string enemyType = "naval_enemy";

            GameDataManager.Instance.RecordEnemyDefeat(enemyID, enemyType, transform.position);

            // Adiciona experiência (mais XP por barco inimigo)
            GameDataManager.Instance.AddExperience(50);

            // Chance de dropar recursos
            if (Random.value > 0.7f)
            {
                GameDataManager.Instance.GetInventoryData().wood += Random.Range(3, 8);
                GameDataManager.Instance.GetInventoryData().iron += Random.Range(1, 4);
            }
        }

        // Para todos os movimentos e ataques
        rb.linearVelocity = Vector2.zero;
        canAttack = false;
        isAiming = false;

        // Efeito de destruição
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Desativa colisores
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // Animação de afundamento
        StartCoroutine(SinkAnimation());
    }

    private IEnumerator SinkAnimation()
    {
        // Animação simples de afundamento
        float sinkTime = 1.5f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < sinkTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / sinkTime);
            spriteRenderer.color = new Color(1, 1, 1, 1 - (elapsed / sinkTime));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroi o objeto
        Destroy(gameObject);
    }

    void UpdatePursuitState(float distance)
    {
        if (isDestroyed) return;

        // Calcula quão longe está do perfect range
        float rangeDiff = Mathf.Abs(distance - perfectRange);

        // ESTADO PERFEITO: Dentro da tolerância
        if (rangeDiff <= perfectRangeTolerance)
        {
            currentState = PursuitState.Perfect;
            timeOutsidePerfectRange = 0f;
            pursuitEngagementTime = 0f;
        }
        // ESTADO DE AJUSTE: Fora mas ainda perto
        else if (rangeDiff <= perfectRangeTolerance * 2f)
        {
            currentState = PursuitState.Adjusting;
            timeOutsidePerfectRange += Time.fixedDeltaTime;
            pursuitEngagementTime = 0f;
        }
        // ESTADO DE PERSEGUIÇÃO: Muito fora do range
        else
        {
            currentState = PursuitState.Chasing;
            timeOutsidePerfectRange += Time.fixedDeltaTime;
            pursuitEngagementTime += Time.fixedDeltaTime;

            // Se player é mais rápido e perseguição muito longa, considera desistir
            if (isPlayerFaster && pursuitEngagementTime > 10f)
            {
                currentState = PursuitState.Disengaging;
            }
        }
    }

    void CheckIfPlayerIsFaster()
    {
        if (playerBoatController == null && playerBoat != null)
        {
            playerBoatController = playerBoat.GetComponent<BoatController>();
        }

        if (playerBoatController != null)
        {
            float playerSpeed = playerBoatController.baseMoveSpeed;
            // Player é considerado "mais rápido" se tiver pelo menos 90% da velocidade máxima do inimigo
            isPlayerFaster = playerSpeed > (maxMoveSpeed * 0.9f);
        }
        else
        {
            isPlayerFaster = false;
        }
    }

    void HandleStateBasedMovement(Vector2 toPlayer, float distance)
    {
        if (isDestroyed) return;

        Vector2 desiredDirection;
        float targetSpeed = currentMoveSpeed;

        switch (currentState)
        {
            case PursuitState.Perfect:
                // NO RANGE PERFEITO: Movimento paralelo puro
                desiredDirection = GetPureParallelDirection(toPlayer);
                targetSpeed = baseMoveSpeed * 0.9f; // Levemente mais devagar
                accelerationTimer = Mathf.Max(0, accelerationTimer - Time.fixedDeltaTime * 2f);
                break;

            case PursuitState.Adjusting:
                // AJUSTANDO: Combina paralelo com aproximação
                Vector2 parallelDir = GetPureParallelDirection(toPlayer);
                Vector2 approachDir = toPlayer.normalized * (distance > perfectRange ? 1f : -1f);
                desiredDirection = (parallelDir + approachDir * 0.3f).normalized;
                targetSpeed = baseMoveSpeed;
                accelerationTimer = 0f;
                break;

            case PursuitState.Chasing:
                // PERSEGUINDO: Mais direto + paralelo
                Vector2 chaseParallel = GetPureParallelDirection(toPlayer);
                Vector2 chaseDirect = toPlayer.normalized;
                desiredDirection = (chaseParallel + chaseDirect * 0.5f).normalized;

                // Acelera GRADUALMENTE (leva accelerationTime segundos para chegar no máximo)
                accelerationTimer = Mathf.Min(accelerationTime, accelerationTimer + Time.fixedDeltaTime);
                float speedProgress = accelerationTimer / accelerationTime;
                targetSpeed = Mathf.Lerp(baseMoveSpeed, maxMoveSpeed, speedProgress);
                break;

            case PursuitState.Disengaging:
                // DESISTINDO: Volta ao patrulhamento básico
                desiredDirection = GetPureParallelDirection(toPlayer);
                targetSpeed = baseMoveSpeed * 0.7f;
                accelerationTimer = Mathf.Max(0, accelerationTimer - Time.fixedDeltaTime * 3f);
                break;

            default:
                desiredDirection = GetPureParallelDirection(toPlayer);
                targetSpeed = baseMoveSpeed;
                break;
        }

        // Atualiza velocidade atual suavemente
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, Time.fixedDeltaTime * 2f);

        // Aplica movimento
        if (CanMoveInDirection(desiredDirection))
        {
            Vector2 targetVelocity = desiredDirection * currentMoveSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 5f);
            lastMoveDirection = desiredDirection;
        }
        else
        {
            TryAlternativeDirection(desiredDirection);
        }
    }

    Vector2 GetPureParallelDirection(Vector2 toPlayer)
    {
        // Troca de lado periódica (mais lenta quando no perfect range)
        sideSwitchTimer -= Time.fixedDeltaTime;
        if (sideSwitchTimer <= 0f)
        {
            moveRight = !moveRight;
            float switchInterval = currentState == PursuitState.Perfect ?
                Random.Range(8f, 15f) : Random.Range(5f, 10f);
            sideSwitchTimer = switchInterval;
        }

        Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x).normalized;
        return moveRight ? perpendicular : -perpendicular;
    }

    void TryAlternativeDirection(Vector2 blockedDirection)
    {
        Vector2[] alternativeDirs = {
            -blockedDirection,
            new Vector2(blockedDirection.y, -blockedDirection.x).normalized,
            new Vector2(-blockedDirection.y, blockedDirection.x).normalized,
            blockedDirection * 0.5f // Tenta mesmo que seja mais devagar
        };

        foreach (Vector2 testDir in alternativeDirs)
        {
            if (CanMoveInDirection(testDir))
            {
                rb.linearVelocity = testDir * (currentMoveSpeed * 0.8f);
                lastMoveDirection = testDir;
                break;
            }
        }
    }

    void SmoothStop()
    {
        // Para suavemente quando não há player ou destruído
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 3f);
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.fixedDeltaTime * 2f);
    }

    IEnumerator ShootWhileMoving(Vector2 aimDirection, float distance)
    {
        if (isAiming || !canAttack || isDestroyed) yield break;

        isAiming = true;
        canAttack = false;

        // Reduz velocidade APENAS durante a mira
        Vector2 originalVelocity = rb.linearVelocity;
        rb.linearVelocity = originalVelocity * 0.7f;

        yield return new WaitForSeconds(aimTime);

        if (playerBoat != null && distance <= attackRange * 1.3f && playerBoat.gameObject.activeSelf && !isDestroyed)
        {
            FireGuaranteedProjectiles(aimDirection, distance);
        }

        // Recupera velocidade RAPIDAMENTE
        float restoreTime = 0.1f;
        float timer = 0f;
        Vector2 slowedVelocity = rb.linearVelocity;

        while (timer < restoreTime)
        {
            rb.linearVelocity = Vector2.Lerp(slowedVelocity, originalVelocity, timer / restoreTime);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = originalVelocity;

        yield return new WaitForSeconds(attackCooldown);

        isAiming = false;
        canAttack = true;
    }

    float CalculateGuaranteedForce(float distance)
    {
        float baseForce;

        if (mainCamera != null)
        {
            float maxScreenDistance = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) * 0.8f;
            float normalizedDistance = Mathf.Clamp01(distance / maxScreenDistance);
            baseForce = Mathf.Lerp(minShootForce, maxShootForce, normalizedDistance);
        }
        else
        {
            baseForce = distance * distanceMultiplier;
        }

        baseForce += overshootOffset;
        baseForce = Mathf.Clamp(baseForce, minShootForce, maxShootForce);

        return baseForce;
    }

    Vector2 PredictPlayerMovement(Vector2 currentDirection, float distance, float force)
    {
        if (playerBoat == null || lastPlayerVelocity.magnitude < 1.5f)
            return currentDirection;

        float flightTime = distance / Mathf.Max(force, 1f);
        Vector2 predictedPosition = (Vector2)playerBoat.position + lastPlayerVelocity * flightTime * 0.5f;
        Vector2 predictedDirection = (predictedPosition - (Vector2)transform.position).normalized;

        return Vector2.Lerp(currentDirection, predictedDirection, 0.6f).normalized;
    }

    void FireGuaranteedProjectiles(Vector2 direction, float distanceToPlayer)
    {
        if (navalProjectilePrefab == null || isDestroyed) return;

        float guaranteedForce = CalculateGuaranteedForce(distanceToPlayer);
        Vector2 predictedDirection = PredictPlayerMovement(direction, distanceToPlayer, guaranteedForce);

        Debug.Log($"🎯 Inimigo atirando | Força: {guaranteedForce:F1} | Dist: {distanceToPlayer:F1}u");

        FireParallelProjectiles(predictedDirection, guaranteedForce);
    }

    void FireParallelProjectiles(Vector2 shootDirection, float force)
    {
        if (isDestroyed) return;

        Vector2 spawnBase = (Vector2)transform.position + shootDirection * 1.8f;
        Vector2 perpendicular = new Vector2(-shootDirection.y, shootDirection.x);
        float[] offsets = { -0.9f, 0f, 0.9f };

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = spawnBase + perpendicular * offsets[i];
            GameObject proj = Instantiate(navalProjectilePrefab, spawnPos, Quaternion.identity);
            NavalProjectile np = proj.GetComponent<NavalProjectile>();

            if (np != null)
            {
                np.damageType = NavalProjectile.DamageType.Boat;
                np.damage = projectileDamage;
                np.lifetime = projectileLifetime;

                Vector2 launchVelocity = shootDirection.normalized * force;
                np.Launch(launchVelocity, force, minShootForce, maxShootForce);

                if (i == 1) // Log só para flecha do meio
                {
                    Debug.Log($"🚀 Inimigo disparou | Tipo: {np.damageType}");
                }
            }
        }

        lastAttackTime = Time.time;
    }

    bool CanMoveInDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f || isDestroyed) return true;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            obstacleCheckDistance,
            obstacleLayers
        );

        return hit.collider == null;
    }

    void FindPlayerBoat()
    {
        BoatController[] boats = FindObjectsByType<BoatController>(FindObjectsSortMode.None);
        if (boats.Length == 0) return;

        foreach (BoatController boat in boats)
        {
            if (boat.isPlayerInside && !boat.isBoatDestroyed)
            {
                playerBoat = boat.transform;
                playerBoatController = boat;
                lastPlayerPosition = playerBoat.position;
                return;
            }
        }
    }

    void TrackPlayerMovement()
    {
        if (playerBoat == null) return;

        Vector2 currentPos = playerBoat.position;
        Vector2 currentVelocity = (currentPos - lastPlayerPosition) / Time.deltaTime;
        lastPlayerVelocity = Vector2.Lerp(lastPlayerVelocity, currentVelocity, Time.deltaTime * 3f);
        lastPlayerPosition = currentPos;
    }

    void UpdateAnimations()
    {
        if (animator == null || isDestroyed) return;

        // CORREÇÃO DA ANIMAÇÃO: Experimente inverter se necessário
        Vector2 animDirection = lastMoveDirection;

        // Se a animação aparece invertida, tente esta linha:
        // animDirection = -lastMoveDirection; // Descomente se animação estiver 180° errada

        animator.SetFloat("VelocityX", animDirection.x);
        animator.SetFloat("VelocityY", animDirection.y);

        bool isMoving = rb.linearVelocity.magnitude > 0.3f && !isDestroyed;
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("Speed", Mathf.Clamp01(rb.linearVelocity.magnitude / baseMoveSpeed));
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || playerBoat == null) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, playerBoat.position);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, lastMoveDirection * 3f);

        // Perfect range (verde)
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, perfectRange);

        // Perfect range ± tolerância (amarelo)
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, perfectRange - perfectRangeTolerance);
        Gizmos.DrawWireSphere(transform.position, perfectRange + perfectRangeTolerance);

        // Attack range (vermelho claro)
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
