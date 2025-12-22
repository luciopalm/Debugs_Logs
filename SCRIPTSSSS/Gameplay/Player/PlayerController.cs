using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text;
using Combat.TurnBased;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    
    [Header("Character System Integration")] // ⭐ NOVO
    [SerializeField] private bool useCharacterSystem = true;
    [SerializeField] private CharacterStatsCache statsCache;
    [SerializeField] private bool useCharacterStatsForMovement = true;
    private float baseMoveSpeed = 4f; // ⭐ NOVO: Para calcular bônus de character
    private float effectiveMoveSpeed = 4f; // ⭐ NOVO: Velocidade final com bônus
    private PartyManager partyManager; // ⭐ NOVO
    private InventoryManager inventoryManager; // ⭐ NOVO
    private CharacterData currentCharacterData; // ⭐ NOVO: Cache do character atual

    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth { get; private set; }
    public bool isInvincible = false;
    public float invincibleTime = 0.5f;

    [Header("Attack Settings")]
    public float attackCooldown = 0.25f;
    public bool isAttacking = false;
    public GameObject hitboxUp;
    public GameObject hitboxDown;
    public GameObject hitboxLeft;
    public GameObject hitboxRight;
    public LayerMask battleLayer;

    [Header("Knockback Settings")]
    public float knockbackDuration = 0.3f;
    private bool isKnocked = false;

    // Componentes
    private Rigidbody2D rb;
    private Animator animator;

    // Input e Movimento
    private Vector2 moveInput;
    private Vector2 physicsInput;
    private Vector2 lastMoveDir = Vector2.down;
    private float attackTimer = 0;

    // Sistema de Água
    private Tilemap waterTilemap;
    public bool isTouchingWater { get; private set; } = false;
    private Vector3 lastTilePos;

    // Interação
    public bool canInteract = true;

    // Debug
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Inicializa saúde
        currentHealth = maxHealth;
        lastTilePos = Vector3Int.RoundToInt(transform.position);
        
        // ⭐ CHARACTER SYSTEM: Inicializar sistema de character
        baseMoveSpeed = moveSpeed;
        effectiveMoveSpeed = moveSpeed;
        
        if (useCharacterSystem)
        {
            // Inicializar stats cache se necessário
            statsCache = GetComponent<CharacterStatsCache>();
            if (statsCache == null)
            {
                statsCache = gameObject.AddComponent<CharacterStatsCache>();
            }
            
            // Buscar managers
            partyManager = PartyManager.Instance;
            inventoryManager = InventoryManager.Instance;
            
            if (showDebugLogs) 
                Debug.Log("[PlayerController] Character system integration initialized");
        }

        if (showDebugLogs) Debug.Log($"[PlayerController] Awake - Vida: {currentHealth}/{maxHealth}");
    }

    private void Start()
    {
        // ⭐ CHARACTER SYSTEM: Conectar ao sistema de character se habilitado
        if (useCharacterSystem)
        {
            ConnectToCharacterSystem();
            LoadActiveCharacter();
        }
        
        if (showDebugLogs) 
        {
            Debug.Log($"[PlayerController] Start - Posição: {transform.position}");
            Debug.Log($"[PlayerController] Aguardando save/load externo");
        }
    }

    private void Update()
    {
        HandleUpdate();
        HandleDebugInputs();
    }
    
private void HandleUpdate()
{
    // ⭐⭐ VERIFICAR SE O JOGO ESTÁ PAUSADO (inventário aberto)
    if (Time.timeScale == 0f)
    {
        // Jogo pausado - apenas animações básicas
        UpdateAnimations();
        return;
    }
    
    // Jogo normal - processar inputs
    moveInput.x = Input.GetAxisRaw("Horizontal");
    moveInput.y = Input.GetAxisRaw("Vertical");
    physicsInput = moveInput;

    CheckForWater();

    if (Input.GetKeyDown(KeyCode.Z) && !isAttacking && canInteract && !isTouchingWater)
    {
        TryInteract();
    }

    if (Input.GetKeyDown(KeyCode.X) && !isAttacking && !isKnocked && !isTouchingWater)
    {
        StartAttack();
    }
    
    UpdateAnimations();

    if (isAttacking)
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0) FinishAttack();
    }
}

    private void HandleDebugInputs()
    {
        // ⭐⭐ NÃO processar debug inputs se jogo pausado
        if (Time.timeScale == 0f) return;
        
        // Alternar character com Tab (DEBUG)
        if (Input.GetKeyDown(KeyCode.Tab) && useCharacterSystem && !isTouchingWater)
        {
            DebugSwitchCharacter();
        }
          
        // ⭐ NOVO: Debug - Dano (F1)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (useCharacterSystem && statsCache != null)
            {
                statsCache.TakeDamage(10);
                SyncHealthFromCharacterData();
                Debug.Log($"[DEBUG] {statsCache.CurrentCharacter?.characterName} took 10 damage");
            }
        }
        
        // ⭐ NOVO: Debug - Cura (F2)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (useCharacterSystem && statsCache != null)
            {
                statsCache.Heal(20);
                SyncHealthFromCharacterData();
                Debug.Log($"[DEBUG] {statsCache.CurrentCharacter?.characterName} healed 20 HP");
            }
        }
        
        // ⭐ NOVO: Debug - Print Stats (F3)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugShowCompleteInfo();
        }
    }

    private void DebugSwitchCharacter()
    {
        if (partyManager == null) 
        {
            Debug.LogWarning("[PlayerController] PartyManager not found!");
            return;
        }
        
        int memberCount = partyManager.GetMemberCount();
        
        if (memberCount <= 1)
        {
            Debug.Log("[PlayerController] Only one party member, cannot switch");
            return;
        }
        
        // Salva o character atual antes de trocar
        CharacterData previousChar = partyManager.GetActiveMember();
        string previousName = previousChar?.characterName ?? "Unknown";
        
        // Alterna para próximo character
        partyManager.NextMember();
        
        // Obtém o novo character ativo
        CharacterData newActive = partyManager.GetActiveMember();
        string newName = newActive?.characterName ?? "Unknown";
        
        Debug.Log($"🔀 SWITCHED CHARACTER:");
        Debug.Log($"   From: {previousName}");
        Debug.Log($"   To: {newName}");
        Debug.Log($"   Party Index: {partyManager.GetActiveIndex() + 1}/{memberCount}");
        
        // ⭐ IMPORTANTE: O LoadActiveCharacter() será chamado AUTOMATICAMENTE
        // pelo evento OnActiveMemberChanged que já está conectado no Start()
    }
    private void FixedUpdate()
    {
        HandleMovement();
    }

    // ⭐ CHARACTER SYSTEM: NOVOS MÉTODOS PARA INTEGRAÇÃO
    /// <summary>
    /// Conecta ao sistema de party e inventário
    /// </summary>
    private void ConnectToCharacterSystem()
    {
        if (partyManager != null)
        {
            partyManager.OnActiveMemberChanged += OnActiveMemberChanged;
            partyManager.OnPartyChanged += OnPartyChanged;
            
            if (showDebugLogs) 
                Debug.Log("[PlayerController] Connected to PartyManager");
        }
        else
        {
            Debug.LogWarning("[PlayerController] PartyManager not found - character system disabled");
            useCharacterSystem = false;
        }
        
        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged += OnEquipmentChanged;
            
            if (showDebugLogs) 
                Debug.Log("[PlayerController] Connected to InventoryManager");
        }
    }
    
    /// <summary>
    /// Carrega o character ativo do PartyManager
    /// </summary>
    private void LoadActiveCharacter()
    {
        if (!useCharacterSystem || partyManager == null) return;
        
        CharacterData activeCharacter = partyManager.GetActiveMember();
        
        if (activeCharacter == null)
        {
            Debug.LogWarning("[PlayerController] No active character found");
            return;
        }
        
        currentCharacterData = activeCharacter;
        statsCache.UpdateFromCharacterData(activeCharacter);
        
        // Atualizar vida do character system para o sistema local (para compatibilidade)
        SyncHealthFromCharacterData();
        
        // Atualizar velocidade de movimento baseada nos stats
        if (useCharacterStatsForMovement)
        {
            // Preserva o moveSpeed original como base, adiciona bônus do character
            effectiveMoveSpeed = baseMoveSpeed;
            float characterSpeedBonus = statsCache.Speed * 0.05f; // Cada ponto de speed = +5% velocidade
            effectiveMoveSpeed *= (1 + characterSpeedBonus);
            
            if (showDebugLogs)
                Debug.Log($"Character speed bonus: {characterSpeedBonus:P0}, Effective speed: {effectiveMoveSpeed:F2}");
        }
        
        // Atualizar visual se necessário
        UpdateCharacterVisual();
        
        Debug.Log($"🎮 PlayerController now controlling: {activeCharacter.characterName}");
    }
    
    /// <summary>
    /// Atualiza aparência visual baseada no CharacterData
    /// </summary>
    private void UpdateCharacterVisual()
    {
        if (currentCharacterData == null) return;
        
        // Atualizar sprite se disponível
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && currentCharacterData.overworldSprite != null)
        {
            spriteRenderer.sprite = currentCharacterData.overworldSprite;
        }
        
        // Atualizar animator se disponível
        if (animator != null && currentCharacterData.battleAnimator != null)
        {
            animator.runtimeAnimatorController = currentCharacterData.battleAnimator;
        }
    }
    
    /// <summary>
    /// Sincroniza a vida do CharacterData com o sistema local
    /// </summary>
    private void SyncHealthFromCharacterData()
    {
        if (currentCharacterData == null || statsCache == null) return;
        
        // Atualiza maxHealth e currentHealth do PlayerController baseado no CharacterData
        maxHealth = statsCache.MaxHP;
        currentHealth = Mathf.Clamp(statsCache.CurrentHP, 0, maxHealth);
        
        if (showDebugLogs)
            Debug.Log($"Health synced from CharacterData: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Sincroniza a vida do sistema local para o CharacterData
    /// </summary>
    private void SyncHealthToCharacterData()
    {
        if (currentCharacterData == null || statsCache == null) return;
        
        // Atualiza o CharacterData com a vida atual do PlayerController
        currentCharacterData.currentHP = currentHealth;
        statsCache.UpdateFromCharacterData(currentCharacterData);
    }
    
    // ⭐ CHARACTER SYSTEM: EVENT HANDLERS
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        if (!useCharacterSystem) return;
        
        Debug.Log($"🔄 PlayerController: Active character changed to {newActiveMember?.characterName}");
        
        if (newActiveMember != null)
        {
            currentCharacterData = newActiveMember;
            statsCache.UpdateFromCharacterData(newActiveMember);
            SyncHealthFromCharacterData();
            UpdateCharacterVisual();
        }
    }
    
    private void OnPartyChanged()
    {
        if (!useCharacterSystem) return;
        
        if (showDebugLogs) 
            Debug.Log("[PlayerController] Party changed - reloading character");
        
        LoadActiveCharacter();
    }
    
    private void OnEquipmentChanged()
    {
        if (!useCharacterSystem) return;
        
        if (showDebugLogs) 
            Debug.Log("[PlayerController] Equipment changed - updating stats");
        
        // Recarregar stats do character atual
        if (currentCharacterData != null)
        {
            statsCache.UpdateFromCharacterData(currentCharacterData);
            SyncHealthFromCharacterData();
            
            // Recalcular velocidade se usando stats do character
            if (useCharacterStatsForMovement)
            {
                effectiveMoveSpeed = baseMoveSpeed * (1 + (statsCache.Speed * 0.05f));
            }
        }
    }


    /// <summary>
/// Cria um BattleUnitData baseado no CharacterData atual
/// Para uso em batalhas por turnos
/// </summary>
    public BattleUnitData CreateBattleUnitData()
    {
        if (!useCharacterSystem || currentCharacterData == null)
        {
            Debug.LogWarning("[PlayerController] Character system not enabled, using default stats");
            return new BattleUnitData(); // Retorna dados padrão
        }
        
        // ⭐ Cria BattleUnitData com stats ATUAIS (incluindo equipamentos)
        BattleUnitData battleData = new BattleUnitData(currentCharacterData, currentCharacterData.currentLevel);
        
        // ⭐ ATUALIZA HP/MP para valores atuais (não máximos)
        battleData.currentHP = Mathf.Clamp(currentCharacterData.currentHP, 1, battleData.maxHP);
        battleData.currentMP = Mathf.Clamp(currentCharacterData.currentMP, 0, battleData.maxMP);
        
        Debug.Log($"[PlayerController] BattleUnitData criado para {currentCharacterData.characterName}:");
        Debug.Log($"   HP: {battleData.currentHP}/{battleData.maxHP}");
        Debug.Log($"   ATK: {battleData.attack} | DEF: {battleData.defense}");
        
        return battleData;
    }
    /// <summary>
    /// Atualiza o CharacterData após a batalha (para salvar HP/MP)
    /// </summary>
    public void UpdateFromBattleUnitData(BattleUnitData battleData)
    {
        if (!useCharacterSystem || currentCharacterData == null || battleData == null)
            return;
        
        // Atualiza HP/MP do CharacterData
        currentCharacterData.currentHP = Mathf.Clamp(battleData.currentHP, 0, currentCharacterData.GetCurrentMaxHP());
        currentCharacterData.currentMP = Mathf.Clamp(battleData.currentMP, 0, currentCharacterData.GetCurrentMaxMP());
        
        // Atualiza o cache
        if (statsCache != null)
        {
            statsCache.UpdateFromCharacterData(currentCharacterData);
        }
        
        Debug.Log($"[PlayerController] CharacterData atualizado após batalha:");
        Debug.Log($"   HP: {currentCharacterData.currentHP}/{currentCharacterData.GetCurrentMaxHP()}");
    }
    
    // ⭐ CHARACTER SYSTEM: NOVOS MÉTODOS PÚBLICOS
    public CharacterData GetCurrentCharacterData()
    {
        return currentCharacterData;
    }
    
    public CharacterStatsCache GetStatsCache()
    {
        return statsCache;
    }
    
    public bool IsUsingCharacterSystem()
    {
        return useCharacterSystem;
    }
    
    public void EnableCharacterSystem(bool enable)
    {
        useCharacterSystem = enable;
        if (enable && partyManager != null)
        {
            LoadActiveCharacter();
        }
    }

    // FIM DAS ADIÇÕES DO CHARACTER SYSTEM

    public void SetWaterTilemap(Tilemap tilemap)
    {
        waterTilemap = tilemap;
    }

      

    private void HandleMovement()
    {
        if (isKnocked) return;
        if (isTouchingWater)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!canInteract && !isKnocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (physicsInput != Vector2.zero)
        {
            physicsInput.Normalize();
            lastMoveDir = physicsInput;
            
            // ⭐ MODIFICADO: Usar effectiveMoveSpeed se character system habilitado
            float speedToUse = useCharacterSystem && useCharacterStatsForMovement ? effectiveMoveSpeed : moveSpeed;
            rb.linearVelocity = physicsInput * speedToUse;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool shouldMove = moveInput != Vector2.zero && !isTouchingWater && !isAttacking && !isKnocked;

        animator.SetFloat("moveX", moveInput.x);
        animator.SetFloat("moveY", moveInput.y);
        animator.SetBool("isMoving", shouldMove);
        animator.SetFloat("lastMoveX", lastMoveDir.x);
        animator.SetFloat("lastMoveY", lastMoveDir.y);
        animator.SetBool("isAttacking", isAttacking);
    }

    private void CheckForWater()
    {
        if (waterTilemap == null)
        {
            isTouchingWater = false;
            return;
        }

        Vector3Int cellPosition = waterTilemap.WorldToCell(transform.position);
        UnityEngine.Tilemaps.TileBase tile = waterTilemap.GetTile(cellPosition);

        isTouchingWater = (tile != null);

        Vector3Int currentTile = Vector3Int.RoundToInt(transform.position);
        if (currentTile != lastTilePos && !isTouchingWater)
        {
            lastTilePos = currentTile;
            CheckForEncounters();
        }
    }

    private void TryInteract()
    {
        Vector2 dir = lastMoveDir;
        LayerMask interactableLayers = LayerMask.GetMask("Interactable", "Boat");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1f, interactableLayers);

        if (hit.collider != null)
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        int attackDir;
        if (Mathf.Abs(lastMoveDir.x) > Mathf.Abs(lastMoveDir.y))
            attackDir = lastMoveDir.x > 0 ? 3 : 2;
        else
            attackDir = lastMoveDir.y > 0 ? 0 : 1;

        animator.SetInteger("attackDirection", attackDir);

        DisableAllHitboxes();

        switch (attackDir)
        {
            case 0: if (hitboxUp != null) hitboxUp.SetActive(true); break;
            case 1: if (hitboxDown != null) hitboxDown.SetActive(true); break;
            case 2: if (hitboxLeft != null) hitboxLeft.SetActive(true); break;
            case 3: if (hitboxRight != null) hitboxRight.SetActive(true); break;
        }
    }

    public void FinishAttack()
    {
        isAttacking = false;
        DisableAllHitboxes();
    }

    private void DisableAllHitboxes()
    {
        if (hitboxUp != null) hitboxUp.SetActive(false);
        if (hitboxDown != null) hitboxDown.SetActive(false);
        if (hitboxLeft != null) hitboxLeft.SetActive(false);
        if (hitboxRight != null) hitboxRight.SetActive(false);
    }

    private void CheckForEncounters()
    {
        Vector2 checkPoint = new Vector2(transform.position.x, transform.position.y - 0.1f);
        if (Physics2D.OverlapCircle(checkPoint, 0.4f, battleLayer))
        {
            if (Random.Range(0, 100) < 25)
            {
                Debug.Log("Batalha iniciada!");
            }
        }
    }

    public void TakeDamage(int amount, Vector2 knockbackForce)
    {
        if (!gameObject.activeSelf || isKnocked || isInvincible) return;

        currentHealth -= amount;
        
        // ⭐ CHARACTER SYSTEM: Sincronizar dano com CharacterData
        if (useCharacterSystem && statsCache != null)
        {
            statsCache.TakeDamage(amount);
            SyncHealthFromCharacterData(); // Atualiza currentHealth do PlayerController
        }

        if (showDebugLogs) 
            Debug.Log($"[PlayerController] Levou {amount} de dano. Vida: {currentHealth}/{maxHealth}");

        // ⭐ MODIFICADO: Agora usa SaveLoadManager para salvar (opcional)
        // Pode manter ou remover - se quiser salvar ao levar dano
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveCurrentState();
        }

        if (gameObject.activeSelf)
        {
            StartCoroutine(KnockbackRoutine(knockbackForce));

            if (currentHealth > 0)
            {
                StartCoroutine(InvincibilityFrames());
            }
        }

        if (currentHealth <= 0)
        {
            Debug.Log("[PlayerController] Player morreu!");
        }
    }

    public void Heal(int amount)
    {
        int newHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        int healedAmount = newHealth - currentHealth;

        if (healedAmount > 0)
        {
            currentHealth = newHealth;
            
            // ⭐ CHARACTER SYSTEM: Sincronizar cura com CharacterData
            if (useCharacterSystem && statsCache != null)
            {
                statsCache.Heal(amount);
                SyncHealthFromCharacterData(); // Atualiza currentHealth do PlayerController
            }
            
            if (showDebugLogs) 
                Debug.Log($"[PlayerController] Curado em {healedAmount}. Vida: {currentHealth}/{maxHealth}");
        }
    }

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        
        // ⭐ CHARACTER SYSTEM: Sincronizar com CharacterData
        if (useCharacterSystem && statsCache != null && currentCharacterData != null)
        {
            currentCharacterData.currentHP = currentHealth;
            statsCache.UpdateFromCharacterData(currentCharacterData);
        }
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        // ⭐ CHARACTER SYSTEM: Sincronizar com CharacterData
        if (useCharacterSystem && statsCache != null && currentCharacterData != null)
        {
            // Nota: MaxHP do CharacterData é calculado, não settable diretamente
            // Apenas atualizamos o currentHP
            SyncHealthToCharacterData();
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackForce)
    {
        isKnocked = true;

        rb.WakeUp();
        rb.linearVelocity = knockbackForce;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            if (elapsed < knockbackDuration * 0.3f)
            {
                rb.linearVelocity = knockbackForce;
            }
            else
            {
                float t = (elapsed - knockbackDuration * 0.3f) / (knockbackDuration * 0.7f);
                rb.linearVelocity = Vector2.Lerp(knockbackForce, Vector2.zero, t);
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        isKnocked = false;
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            float blinkDuration = invincibleTime;
            float blinkInterval = 0.1f;
            float elapsed = 0f;

            while (elapsed < blinkDuration)
            {
                sprite.enabled = !sprite.enabled;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            sprite.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(invincibleTime);
        }

        isInvincible = false;
    }

    /// <summary>
/// Obtém o valor de ataque atual (incluindo equipamentos)
/// </summary>
public int GetPlayerAttackStat()
{
    if (useCharacterSystem && statsCache != null)
    {
        return statsCache.Attack;
    }
    return 10; // Fallback
}

/// <summary>
/// Obtém o valor de defesa atual (incluindo equipamentos)
/// </summary>
public int GetPlayerDefenseStat()
{
    if (useCharacterSystem && statsCache != null)
    {
        return statsCache.Defense;
    }
    return 5; // Fallback
}

/// <summary>
/// Obtém o valor de magia atual (incluindo equipamentos)
/// </summary>
public int GetPlayerMagicAttackStat()
{
    if (useCharacterSystem && statsCache != null)
    {
        return statsCache.MagicAttack;
    }
    return 8; // Fallback
}

/// <summary>
/// Obtém o valor de defesa mágica atual (incluindo equipamentos)
/// </summary>
public int GetPlayerMagicDefenseStat()
{
    if (useCharacterSystem && statsCache != null)
    {
        return statsCache.MagicDefense;
    }
    return 4; // Fallback
}



    public bool CanEnterBoat()
    {
        if (waterTilemap == null) return false;
        Vector3Int cellPosition = waterTilemap.WorldToCell(transform.position);
        UnityEngine.Tilemaps.TileBase tile = waterTilemap.GetTile(cellPosition);
        return (tile != null);
    }

    public void OnExitBoat(Vector3 exitPosition)
    {
        transform.position = exitPosition;
        gameObject.SetActive(true);
        canInteract = true;
        isAttacking = false;
        isKnocked = false;
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        physicsInput = Vector2.zero;
    }

    public void OnBoatEnterExit(bool enteringBoat)
    {
        if (SaveLoadManager.Instance != null)
        {
            if (enteringBoat)
            {
                if (showDebugLogs)
                    Debug.Log($"[PlayerController] Entrando no barco");

                // ⭐ APENAS notifica o SaveLoadManager
                SaveLoadManager.Instance.OnPlayerEnterBoat();
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"[PlayerController] Saindo do barco");

                // ⭐⭐ CRÍTICO: NÃO SALVA POSIÇÃO AQUI! ⭐⭐
                // A posição será salva quando o usuário clicar em "Save"
                // Esta linha estava causando o bug:
                // GameDataManager.Instance.UpdatePlayerPosition(currentPlayerPos); ❌ REMOVIDO

                // ⭐ APENAS notifica o SaveLoadManager
                SaveLoadManager.Instance.OnPlayerExitBoat();
            }

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerController] Transição {(enteringBoat ? "entrar" : "sair")} notificada");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerController] SaveLoadManager não encontrado!");
        }
    }

    public void SavePlayerData()
    {
        if (GameDataManager.Instance == null)
        {
            if (showDebugLogs) 
                Debug.LogWarning("[PlayerController] GameDataManager não encontrado.");
            return;
        }

        if (isTouchingWater)
        {
            if (showDebugLogs)
                Debug.Log("[PlayerController] ⚠️ Não salvando: Player está na água");
            return;
        }

        if (showDebugLogs) 
        {
            Debug.Log($"[PlayerController] Salvando dados:");
            Debug.Log($"  - Posição: {transform.position}");
            Debug.Log($"  - Vida: {currentHealth}/{maxHealth}");
        }

        GameDataManager.Instance.UpdatePlayerPosition(transform.position);
        GameDataManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
    }

    public void ApplyLoadedData(Vector3 position, int health, int maxHealthValue)
    {
        if (position.magnitude > 0.5f)
        {
            transform.position = position;
            if (rb != null) rb.position = position;
        }

        maxHealth = maxHealthValue;
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        
        // ⭐ CHARACTER SYSTEM: Sincronizar dados carregados com CharacterData
        if (useCharacterSystem && statsCache != null && currentCharacterData != null)
        {
            currentCharacterData.currentHP = currentHealth;
            statsCache.UpdateFromCharacterData(currentCharacterData);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerController] Dados carregados:");
            Debug.Log($"  - Posição: {transform.position}");
            Debug.Log($"  - Vida: {currentHealth}/{maxHealth}");
        }
    }

    public void ForceActivate()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (showDebugLogs) Debug.Log("[PlayerController] 🔓 Ativado forçadamente");
        }
    }
    
    // ⭐ CHARACTER SYSTEM: NOVOS MÉTODOS DE DEBUG
    [ContextMenu("[Player] Mostrar Info Completa")]
    public void DebugShowCompleteInfo()
    {
        Debug.Log("=== PLAYER CONTROLLER INFO ===");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"Health: {currentHealth}/{maxHealth}");
        Debug.Log($"Is Touching Water: {isTouchingWater}");
        Debug.Log($"Can Interact: {canInteract}");
        Debug.Log($"Move Speed: {moveSpeed} (Effective: {effectiveMoveSpeed})");
        
        if (useCharacterSystem)
        {
            Debug.Log("--- CHARACTER SYSTEM ---");
            Debug.Log($"Current Character: {currentCharacterData?.characterName ?? "None"}");
            if (statsCache != null)
            {
                Debug.Log($"Stats Cache: {statsCache.GetStatsSummary()}");
            }
        }
    }
    
    [ContextMenu("[Player] Take 10 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(10, Vector2.right * 3f);
    }
    
    [ContextMenu("[Player] Heal 15 HP")]
    public void DebugHeal()
    {
        Heal(15);
    }
    
    [ContextMenu("[Player] Switch Character System")]
    public void DebugToggleCharacterSystem()
    {
        EnableCharacterSystem(!useCharacterSystem);
        Debug.Log($"Character System: {(useCharacterSystem ? "ENABLED" : "DISABLED")}");
    }
    
    [ContextMenu("[Player] Reload Active Character")]
    public void DebugReloadCharacter()
    {
        LoadActiveCharacter();
    }

    // Clean up event subscriptions
    private void OnDestroy()
    {
        if (useCharacterSystem && partyManager != null)
        {
            partyManager.OnActiveMemberChanged -= OnActiveMemberChanged;
            partyManager.OnPartyChanged -= OnPartyChanged;
        }
        
        if (useCharacterSystem && inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    [ContextMenu("[TEST] Check Battle Stats")]
    public void DebugCheckBattleStats()
    {
        Debug.Log("=== BATTLE STATS CHECK ===");
        
        if (!useCharacterSystem)
        {
            Debug.Log("Character system: DISABLED");
            Debug.Log("Attack: 10 (default)");
            Debug.Log("Defense: 5 (default)");
            return;
        }
        
        Debug.Log($"Character: {currentCharacterData?.characterName ?? "None"}");
        Debug.Log($"Using Character System: {useCharacterSystem}");
        
        if (statsCache != null)
        {
            Debug.Log($"Cache Attack: {statsCache.Attack}");
            Debug.Log($"Cache Defense: {statsCache.Defense}");
            Debug.Log($"Cache Speed: {statsCache.Speed}");
        }
        
        if (currentCharacterData != null)
        {
            Debug.Log($"GetCurrentAttack(): {currentCharacterData.GetCurrentAttack()}");
            Debug.Log($"GetCurrentDefense(): {currentCharacterData.GetCurrentDefense()}");
            
            if (currentCharacterData.currentEquipment != null)
            {
                Debug.Log($"Equipment ATK bonus: {currentCharacterData.currentEquipment.GetTotalStatBonus(ItemData.StatType.Attack)}");
                Debug.Log($"Equipment DEF bonus: {currentCharacterData.currentEquipment.GetTotalStatBonus(ItemData.StatType.Defense)}");
            }
        }
        
        // Teste BattleUnitData
        try
        {
            BattleUnitData testData = CreateBattleUnitData();
            Debug.Log($"BattleUnitData ATK: {testData.attack}");
            Debug.Log($"BattleUnitData DEF: {testData.defense}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao criar BattleUnitData: {e.Message}");
        }
    }
}