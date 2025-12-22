using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    [Header("Basic Settings")]
    public float speed = 2f;
    public int maxHealth = 3;
    public float knockbackForce = 7.5f;
    public float knockbackDuration = 0.4f;
    
    [Header("Combat Settings")]
    public float attackRange = 1.5f;
    public float stoppingDistance = 1.8f;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.1f;
    public int attackDamage = 1;
    
    [Header("Sistema de Investida")]
    public float chargeDistance = 3f;
    public float chargeSpeed = 4f;
    public float chargeDuration = 0.5f;
    public float chargeCooldown = 2f;
    
    [Header("Health Bar Settings")]
    public GameObject healthBarCanvas;
    public UnityEngine.UI.Slider healthBarSlider;
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Obstacle Detection")]
    public float obstacleCheckDistance = 1f;
    public LayerMask obstacleLayers;
    
    [Header("Unstuck System - VERSÃO ORIGINAL MELHORADA")]
    public float stuckThreshold = 0.6f;
    public float avoidDuration = 1.0f;  
    public float avoidForce = 1.8f;
    
    [Header("Enemy Area Settings")]
    public string enemyAreaTag = "EnemyArea";
    public bool showAreaDebug = false;
    
    [Header("Enemy Identification")]
    public string enemyID = "skeleton_warrior"; // ID único para save system
    public string enemyType = "skeleton"; // Tipo para categorização
    public bool isUniqueEnemy = false; // Se for único (não respawna)
    public bool respawnOnLoad = true; // Se respawna quando o jogo carrega
    
    [Header("Loot Settings")]
    public string[] dropItems; // Itens que pode dropar
    public int dropCurrency = 0; // Moedas que pode dropar
    public float dropChance = 0.3f; // Chance de dropar algo
    
    [Header("Debug")]
    public bool showDebug = false;
    
    // Components
    private Transform player;
    private Rigidbody2D rb;
    private Tilemap enemyAreaTilemap;
    
    // State
    private int currentHealth;
    private bool isDead = false;
    private bool isKnockedBack = false;
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    
    // Sistema de Investida
    private bool isCharging = false;
    private float lastChargeTime = -999f;
    private Vector2 chargeDirection = Vector2.zero;
    
    // Movement
    private Vector2 currentMoveDir;
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    
    // Sistema de desvio
    private bool isAvoiding = false;
    private float avoidTimer = 0f;
    private Vector2 avoidDirection = Vector2.zero;
    private int lastAvoidSide = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Verifica se este inimigo já foi derrotado (para inimigos únicos)
        if (isUniqueEnemy && !respawnOnLoad)
        {
            CheckIfAlreadyDefeated();
        }
        
        if (isDead) return; // Se já estiver morto, não inicializa
        
        // BUSCA PLAYER MAS VERIFICA SE ESTÁ ATIVO
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.activeSelf)
        {
            player = playerObj.transform;
            if (showDebug) Debug.Log($"✅ Player encontrado e ativo: {playerObj.name}");
        }
        else
        {
            if (showDebug) Debug.Log("ℹ️ Player não encontrado ou inativo (no barco)");
            player = null;
        }
        
        currentHealth = maxHealth;
        lastPosition = transform.position;

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Inicializa a barra de vida
        InitializeHealthBar();

        // Busca a EnemyArea (Tilemap)
        InitializeEnemyArea();

        if (showDebug) Debug.Log($"✅ Enemy spawned - Health: {currentHealth}/{maxHealth}");
    }
    
    // NOVO: Verifica se inimigo único já foi derrotado
    void CheckIfAlreadyDefeated()
    {
        if (GameDataManager.Instance == null) return;
        
        // Usa a posição atual para verificar se este inimigo específico já foi derrotado
        if (GameDataManager.Instance.WasEnemyDefeatedAtPosition(enemyID, transform.position))
        {
            if (showDebug) Debug.Log($"⚠️ Inimigo único {enemyID} já foi derrotado. Desativando...");
            isDead = true;
            
            // Desativa componentes visuais mas mantém o GameObject (para referência)
            if (healthBarCanvas != null)
                healthBarCanvas.SetActive(false);
            
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.enabled = false;
            
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
            
            // Desativa este script
            enabled = false;
        }
    }

    // NOVO: Gera string de drops para registro
    string GenerateDropString()
    {
        if (dropItems == null || dropItems.Length == 0)
            return dropCurrency > 0 ? $"{dropCurrency}_coins" : "";
        
        List<string> drops = new List<string>();
        
        // Adiciona moedas se houver
        if (dropCurrency > 0)
            drops.Add($"{dropCurrency}_coins");
        
        // Adiciona itens com base na chance
        foreach (string item in dropItems)
        {
            if (Random.value <= dropChance)
                drops.Add(item);
        }
        
        return string.Join(",", drops.ToArray());
    }

    void InitializeEnemyArea()
    {
        if (string.IsNullOrEmpty(enemyAreaTag))
        {
            enemyAreaTag = "EnemyArea";
        }
        
        GameObject areaObj = GameObject.FindGameObjectWithTag(enemyAreaTag);
        if (areaObj != null)
        {
            enemyAreaTilemap = areaObj.GetComponent<Tilemap>();
        }
    }

    void InitializeHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true);
        }
        
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(true);
        }
    }

    void Update()
    {
        if (isDead) return;
        
        // Atualiza a posição da barra de vida para seguir o inimigo
        UpdateHealthBarPosition();
    }

    void UpdateHealthBarPosition()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.position = transform.position + healthBarOffset;
            if (Camera.main != null)
                healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // VERIFICA SE PLAYER EXISTE E ESTÁ ATIVO
        if (player == null)
        {
            // Tenta encontrar player ativo
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && playerObj.activeSelf)
            {
                player = playerObj.transform;
                if (showDebug) Debug.Log("✅ Player reencontrado!");
            }
            else
            {
                // Player não existe ou está no barco, inimigo fica parado
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }
        
        // VERIFICA SE PLAYER AINDA ESTÁ ATIVO (não no barco)
        if (player.gameObject.activeSelf == false)
        {
            // Player entrou no barco, inimigo para tudo
            rb.linearVelocity = Vector2.zero;
            isAttacking = false;
            isCharging = false;
            
            if (showDebug && Time.frameCount % 180 == 0)
            {
                Debug.Log("ℹ️ Player está no barco, inimigo parado");
            }
            return;
        }
        
        if (isKnockedBack) return;
        
        if (isAttacking || isCharging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        ChasePlayer();
    }

    void ChasePlayer()
    {
        if (player == null || !player.gameObject.activeSelf) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // VERIFICA SE PLAYER ESTÁ DENTRO DE RANGE RAZOÁVEL (20 unidades)
        if (distanceToPlayer > 20f)
        {
            // Player muito longe, para de perseguir
            rb.linearVelocity = Vector2.zero;
            
            if (showDebug && Time.frameCount % 240 == 0)
            {
                Debug.Log($"ℹ️ Player muito longe: {distanceToPlayer:F1}u, parando perseguição");
            }
            return;
        }
        
        Vector2 desiredDir = directionToPlayer.normalized;

        // NOVO: Verificar se pode se mover na direção do player (dentro da EnemyArea)
        if (!CanMoveInDirection(desiredDir))
        {
            // Está na borda da área, para de perseguir
            if (showDebug && Time.frameCount % 60 == 0)
            {
                Debug.Log("🚧 Inimigo na borda da EnemyArea - Parando perseguição");
            }
            
            rb.linearVelocity = Vector2.zero;
            
            // Tenta se afastar da borda
            if (!isAvoiding && !isCharging)
            {
                StartAvoidance(-desiredDir); // Tenta se mover para longe da borda
            }
            return;
        }

        // Pode fazer investida?
        if (CanCharge(distanceToPlayer, desiredDir))
        {
            StartCoroutine(ChargeAttack(desiredDir));
            return;
        }

        // Pode atacar normal?
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            if (showDebug) Debug.Log($"💥 Atacando! Dist: {distanceToPlayer:F2}");
            StartCoroutine(AttackPlayer());
            return;
        }

        // Cooldown do ataque? Mantém distância
        if (distanceToPlayer <= stoppingDistance && Time.time < lastAttackTime + attackCooldown)
        {
            if (distanceToPlayer > attackRange * 1.1f)
            {
                rb.linearVelocity = desiredDir * (speed * 0.5f);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        // VERIFICA OBSTÁCULOS À FRENTE
        bool hasObstacleAhead = CheckObstacleAhead(desiredDir);
        bool isStuck = CheckIfStuck();
        
        // SÓ DESVIA SE HOUVER OBSTÁCULO À FRENTE OU ESTIVER TRAVADO
        if ((hasObstacleAhead || isStuck) && !isAvoiding && !isCharging)
        {
            StartAvoidance(desiredDir);
        }

        // SE ESTÁ DESVIANDO, mantém a direção de desvio
        if (isAvoiding && !isCharging)
        {
            avoidTimer -= Time.fixedDeltaTime;
            
            if (avoidTimer <= 0)
            {
                isAvoiding = false;
                //if (showDebug) Debug.Log("🔄 Fim do desvio, voltando ao player");
            }
            else
            {
                desiredDir = avoidDirection;
            }
        }

        // MOVIMENTO FINAL - SEMPRE TENTA IR DIRETO AO PLAYER (se não estiver carregando)
        if (!isCharging)
        {
            // NOVO: Verifica novamente se pode mover nesta direção
            if (CanMoveInDirection(desiredDir))
            {
                currentMoveDir = desiredDir;
                rb.linearVelocity = currentMoveDir * speed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        
        lastPosition = transform.position;
    }

    bool IsPositionInsideEnemyArea(Vector2 position)
    {
        if (enemyAreaTilemap == null) return true; // Se não há área, permite movimento
        
        // Converte posição mundial para coordenada do tilemap
        Vector3Int cellPosition = enemyAreaTilemap.WorldToCell(position);
        
        // Verifica se há um tile nesta posição do tilemap
        return enemyAreaTilemap.HasTile(cellPosition);
    }

    bool CanMoveInDirection(Vector2 direction)
    {
        if (enemyAreaTilemap == null) return true;
        if (direction.magnitude < 0.01f) return true;
        
        Vector2 currentPos = transform.position;
        float checkDistance = 0.5f; // Distância fixa para verificação
        Vector2 targetPos = currentPos + direction.normalized * checkDistance;
        
        return IsPositionInsideEnemyArea(targetPos);
    }

    bool CanCharge(float distanceToPlayer, Vector2 directionToPlayer)
    {
        if (player == null || !player.gameObject.activeSelf) return false;
        
        if (distanceToPlayer <= chargeDistance && 
            distanceToPlayer > attackRange && 
            Time.time >= lastChargeTime + chargeCooldown &&
            !isCharging && 
            !isAttacking && 
            !isAvoiding &&
            !CheckObstacleAhead(directionToPlayer) &&
            player.gameObject.activeSelf)
        {
            return true;
        }
        return false;
    }

    IEnumerator ChargeAttack(Vector2 direction)
    {
        isCharging = true;
        lastChargeTime = Time.time;
        
        if (showDebug) Debug.Log($"⚡ INICIANDO INVESTIDA! Dist: {Vector2.Distance(transform.position, player.position):F1}");
        
        float chargeTimer = 0f;
        chargeDirection = direction;
        
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.1f);
        
        while (chargeTimer < chargeDuration && isCharging && player != null && player.gameObject.activeSelf)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            chargeTimer += Time.fixedDeltaTime;
            
            if (player != null && player.gameObject.activeSelf)
            {
                float currentDistance = Vector2.Distance(transform.position, player.position);
                if (currentDistance <= attackRange)
                {
                    PlayerController pc = player.GetComponent<PlayerController>();
                    if (pc != null && pc.gameObject.activeSelf)
                    {
                        Vector2 knockDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                        Vector2 knockForce = knockDir * (knockbackForce * 1.5f);
                        pc.TakeDamage(1, knockForce);
                        
                        if (showDebug) Debug.Log("✅ INVESTIDA ACERTOU!");
                    }
                    break;
                }
            }
            else
            {
                // Player entrou no barco durante a investida
                break;
            }
            
            yield return new WaitForFixedUpdate();
        }
        
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        
        if (showDebug) Debug.Log("💨 Investida terminada");
        
        yield return new WaitForSeconds(0.3f);
    }

    bool CheckObstacleAhead(Vector2 direction)
    {
        if (direction.magnitude < 0.01f) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            direction, 
            obstacleCheckDistance, 
            obstacleLayers
        );

        return hit.collider != null;
    }

    bool CheckIfStuck()
    {
        float distanceMoved = Vector2.Distance(transform.position, lastPosition);
        bool isMoving = distanceMoved > 0.05f;

        if (!isMoving)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckThreshold)
            {
               // if (showDebug) Debug.Log($"🚧 INIMIGO TRAVADO! Timer: {stuckTimer:F1}s");
                return true;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        return false;
    }

    void StartAvoidance(Vector2 originalDirection)
    {
        isAvoiding = true;
        avoidTimer = avoidDuration;
        
        lastAvoidSide *= -1;
        
        float avoidAngle = 40f * lastAvoidSide;
        avoidDirection = RotateVector(originalDirection, avoidAngle) * avoidForce;
        
        stuckTimer = 0f;
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

   IEnumerator AttackPlayer()
    {
        if (player == null || !player.gameObject.activeSelf)
        {
            isAttacking = false;
            yield break;
        }
        
        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;

        if (showDebug) Debug.Log("💥 Atacando player!");

        yield return new WaitForSeconds(attackWindup);

        if (player == null || !player.gameObject.activeSelf)
        {
            isAttacking = false;
            yield break;
        }
        
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange * 1.2f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.gameObject.activeSelf)
            {
                //Dano base que será reduzido pela defesa
                int baseDamage = 1; // Seu dano atual é 1
                
                // ⭐⭐ NOVO: Calcular dano considerando defesa do player
                int finalDamage = attackDamage;
                
                if (pc.IsUsingCharacterSystem())
                {
                    int playerDefense = pc.GetPlayerDefenseStat();
                    // Fórmula: dano - (defesa/3), mínimo 1
                    finalDamage = Mathf.Max(1, baseDamage - (playerDefense / 3));
                    
                    if (showDebug) 
                    {
                        Debug.Log($"🎯 Dano calculado:");
                        Debug.Log($"   Base: {baseDamage}");
                        Debug.Log($"   Defesa Player: {playerDefense}");
                        Debug.Log($"   Redução: {playerDefense / 3}");
                        Debug.Log($"   Dano final: {finalDamage}");
                    }
                }
                
                Vector2 knockDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                Vector2 knockForce = knockDir * knockbackForce;
                pc.TakeDamage(finalDamage, knockForce);
                
                if (showDebug) Debug.Log("✅ Acertou!");
            }
        }

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isKnockedBack) return;

        currentHealth -= damage;
        
        // ATUALIZA A BARRA DE VIDA
        UpdateHealthBar();
        
        if (showDebug) Debug.Log($"💔 Inimigo levou {damage} dano. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (isCharging)
            {
                isCharging = false;
                rb.linearVelocity = Vector2.zero;
            }
            
            StartCoroutine(KnockbackRoutine());
            StartCoroutine(DamageBlinkEffect());
        }
    }

    IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true;
        
        // Cancela qualquer movimento ativo
        if (isCharging) isCharging = false;
        if (isAttacking) isAttacking = false;
        
        Vector2 dir = ((Vector2)transform.position - (Vector2)player.position).normalized;
        Vector2 vel = dir * knockbackForce;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            rb.linearVelocity = Vector2.Lerp(vel, Vector2.zero, elapsed / knockbackDuration);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    IEnumerator DamageBlinkEffect()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) yield break;

        Material mat = sprite.material;

        if (mat.HasProperty("_FlashAmount"))
        {
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                mat.SetFloat("_FlashAmount", Mathf.Lerp(1f, 0f, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            mat.SetFloat("_FlashAmount", 0f);
        }
        else
        {
            Color original = sprite.color;
            for (int i = 0; i < 5; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.07f);
                sprite.color = original;
                yield return new WaitForSeconds(0.07f);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        
        // Esconde a barra de vida ao morrer
        if (healthBarCanvas != null)
            healthBarCanvas.SetActive(false);
        
        if (showDebug) Debug.Log("💀 Enemy died!");
        
        // NOVO: Registrar inimigo derrotado no sistema de save
        if (GameDataManager.Instance != null)
        {
            string drops = GenerateDropString();
            
            GameDataManager.Instance.RecordEnemyDefeat(
                enemyID, 
                enemyType, 
                transform.position,
                drops
            );
            
            // Adiciona moedas ao jogador
            if (dropCurrency > 0)
            {
                GameDataManager.Instance.AddCurrency(dropCurrency);
            }
            
            // Adiciona experiência ao jogador
            GameDataManager.Instance.AddExperience(10); // 10 XP por inimigo básico
        }
        
        // Efeito visual antes de destruir
        StartCoroutine(DeathAnimation());
    }
    
    // NOVO: Animação de morte antes de destruir
    IEnumerator DeathAnimation()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            // Pisca algumas vezes
            for (int i = 0; i < 3; i++)
            {
                sprite.color = new Color(1, 1, 1, 0.5f);
                yield return new WaitForSeconds(0.1f);
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Desaparece gradualmente
            float fadeTime = 0.5f;
            float elapsed = 0f;
            Color originalColor = sprite.color;
            
            while (elapsed < fadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                sprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // Destroi o objeto
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebug || !Application.isPlaying) return;

        if (isCharging)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, chargeDirection * 2f);
        }
        else
        {
            Gizmos.color = isAvoiding ? Color.yellow : Color.green;
            Gizmos.DrawRay(transform.position, currentMoveDir * 1.5f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chargeDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        // Range máximo de perseguição (20 unidades)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, 20f);
    }
}