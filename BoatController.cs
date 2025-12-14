using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class BoatController : MonoBehaviour, Interactable 
{
    [Header("Boat Health System")]
    public int maxBoatHealth = 10;
    public int currentBoatHealth;
    public bool isBoatDestroyed = false;
    public float invincibilityTime = 0.5f;
    private bool isInvincible = false;
    
    public bool isPlayerInside = false;
    private Rigidbody2D rb;
    private GameObject player;
    
    [SerializeField] private CameraManager cameraManager;
    public float baseMoveSpeed = 3f;

    private Vector3 lastSafePlayerPosition;
    private Animator animator;
    private Vector2 lastMoveDirection = Vector2.down;
    
    // Sistema de Landing Zones
    private List<LandingZone> landingZones = new List<LandingZone>();
    private LandingZone currentLandingZone;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableAdvancedDebug = false;

    [Header("Naval Combat Settings")]
    public float attackAngle = 120f;
    public LayerMask enemyLayerMask;

    [Header("Projectile Settings")]
    public GameObject navalProjectilePrefab;
    public float minProjectileForce = 15f;
    public float maxProjectileForce = 30f;
    public float chargeTime = 1.5f;

    private Vector2 aimDirection = Vector2.right;
    private bool canAim = false;
    private Camera mainCamera;

    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private float currentChargeForce = 0f;

    [Header("Attack Cooldown")]
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    private bool canAttack = true;

    [Header("Force Indicator Settings")]
    public GameObject forceIndicator;
    public SpriteRenderer forceArrowFill;
    public SpriteRenderer forceArrowOutline;
    public float maxArrowHeight = 2f;
    
    public Color yellowColor = Color.yellow;
    public Color greenColor = Color.green;
    public Color redColor = Color.red;

    // ⭐ NOVO: Flag para evitar reset no Start
    private bool hasBeenInitialized = false;

    
   // ============================================
// 🔧 CORREÇÃO: Awake() - Vida do Barco
// ============================================
// Substitua o método Awake() COMPLETO no BoatController.cs

private void Awake()
{
    if (enableAdvancedDebug) Debug.Log("🚤 BoatController.Awake() iniciando...");
    
    // ✅ SEMPRE inicializa componentes essenciais PRIMEIRO
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    mainCamera = Camera.main;
    
    // ✅ VALIDAÇÃO CRÍTICA: Verifica se Rigidbody2D existe
    if (rb == null)
    {
        Debug.LogError("❌ CRÍTICO: BoatController não tem Rigidbody2D!");
        Debug.LogError($"   GameObject: {gameObject.name}");
        Debug.LogError($"   Adicione um Rigidbody2D ao GameObject do barco!");
        
        // Tenta adicionar automaticamente (medida de emergência)
        rb = gameObject.AddComponent<Rigidbody2D>();
        Debug.LogWarning("⚠️ Rigidbody2D adicionado automaticamente");
    }
    
    // Configura Rigidbody2D
    if (rb != null)
    {
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        if (enableAdvancedDebug)
        {
            Debug.Log("🚤 Rigidbody2D configurado:");
            Debug.Log($"   • Gravity: {rb.gravityScale}");
            Debug.Log($"   • Interpolation: {rb.interpolation}");
            Debug.Log($"   • Collision: {rb.collisionDetectionMode}");
        }
    }
    
    // Busca player
    player = GameObject.FindWithTag("Player");
    if (player == null && enableAdvancedDebug)
    {
        Debug.LogWarning("⚠️ Player não encontrado no Awake (pode estar inativo)");
    }
    
    // ✅ Inicializa flags essenciais
    isPlayerInside = false;
    
    // ✅✅ CORREÇÃO CRÍTICA: Inicializa vida SEMPRE (se estiver zerada)
    if (currentBoatHealth <= 0)
    {
        currentBoatHealth = maxBoatHealth;
        if (enableAdvancedDebug) 
            Debug.Log($"🚤 Barco: Vida inicializada no Awake: {currentBoatHealth}/{maxBoatHealth}");
    }
    else if (enableAdvancedDebug)
    {
        Debug.Log($"🚤 Barco: Vida já existe: {currentBoatHealth}/{maxBoatHealth}");
    }
    
    hasBeenInitialized = true;
    
    if (enableAdvancedDebug)
    {
        Debug.Log("🚤 BoatController.Awake() completo:");
        Debug.Log($"   • Rigidbody2D: {(rb != null ? "✅" : "❌")}");
        Debug.Log($"   • Animator: {(animator != null ? "✅" : "❌")}");
        Debug.Log($"   • Camera: {(mainCamera != null ? "✅" : "❌")}");
        Debug.Log($"   • Player: {(player != null ? "✅" : "❌")}");
        Debug.Log($"   • Vida: {currentBoatHealth}/{maxBoatHealth}");
        Debug.Log($"   • hasBeenInitialized: {hasBeenInitialized}");
    }
}

private void Start()
{
    if (enableAdvancedDebug) Debug.Log("🚤 BoatController.Start()");
    
    // ✅ VALIDAÇÃO ADICIONAL: Verifica novamente componentes críticos
    if (rb == null)
    {
        Debug.LogError("❌ CRÍTICO: rb ainda é NULL no Start()!");
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("❌ FATAL: Não foi possível obter Rigidbody2D!");
            return;
        }
    }
    
    // Busca Landing Zones
    FindAllLandingZones();
    
    // Inicializa sistemas de ataque
    InitializeAttackSystem();
    InitializeForceIndicator();
    
    // Garante que o barco está ativo
    if (!gameObject.activeSelf)
    {
        gameObject.SetActive(true);
    }
    
    // ✅ REMOVIDO: NÃO reseta mais a vida aqui!
    // currentBoatHealth = maxBoatHealth; ← ISTO CAUSAVA O BUG
    
    if (enableAdvancedDebug)
    {
        Debug.Log($"🚤 Barco Start() concluído:");
        Debug.Log($"   Posição: {transform.position}");
        Debug.Log($"   Vida: {currentBoatHealth}/{maxBoatHealth}");
        Debug.Log($"   Player dentro: {isPlayerInside}");
        Debug.Log($"   Rigidbody2D válido: {rb != null}");
    }
}

    private void Update()
{
    // ⭐⭐ VERIFICAÇÃO DE SEGURANÇA CRÍTICA ⭐⭐
    // Evita qualquer processamento durante batalha por turnos
    if (GameController.Instance != null && GameController.Instance.IsInTurnBasedBattle())
    {
        // Debug.Log("⏸️ BoatController pausado durante batalha por turnos");
        return;
    }
    
    // Verificar se GameController existe e está no estado correto
    if (GameController.Instance != null && GameController.Instance.GetCurrentState() != GameState.FreeRoam)
    {
        // Não processar se não estiver em FreeRoam (ex: Dialog, etc)
        return;
    }
    
    if (!isPlayerInside || isBoatDestroyed) return;
    
    HandleInput();
    CheckLandingZones();
    
    if (canAim)
    {
        UpdateAimDirection();
        CheckAttackInput();
    }
    
    if (isCharging)
    {
        UpdateForceIndicator();
    }
}

    private void FixedUpdate()
    {
        if (!isPlayerInside || isBoatDestroyed) return;
        HandleMovement();
    }

    // ============================================
    // SISTEMA DE VIDA DO BARCO
    // ============================================

    public void TakeBoatDamage(int damage)
    {
        if (isBoatDestroyed || isInvincible || !isPlayerInside) return;
        
        currentBoatHealth -= damage;
        if (enableAdvancedDebug) Debug.Log($"💥 BARCO levou {damage} dano! Vida: {currentBoatHealth}/{maxBoatHealth}");
        
        StartCoroutine(BoatDamageEffect());
        
        if (currentBoatHealth <= 0)
        {
            DestroyBoat();
        }
        else
        {
            StartCoroutine(InvincibilityFrame());
        }
    }

    public void RepairBoat(int amount, bool useRepairKit = false)
    {
        if (isBoatDestroyed) 
        {
            Debug.Log("⚠️ Não é possível reparar: barco destruído.");
            return;
        }
        
        if (useRepairKit && GameDataManager.Instance != null)
        {
            if (!GameDataManager.Instance.UseBoatResource("repairkit", 1))
            {
                Debug.Log("⚠️ Nenhum kit de reparo disponível!");
                return;
            }
        }
        
        int oldHealth = currentBoatHealth;
        currentBoatHealth = Mathf.Min(currentBoatHealth + amount, maxBoatHealth);
        int actualRepair = currentBoatHealth - oldHealth;
        
        if (enableAdvancedDebug) Debug.Log($"🔧 Barco reparado: +{actualRepair} HP (Total: {currentBoatHealth}/{maxBoatHealth})");
        
        StartCoroutine(RepairEffect());
        
        if (isBoatDestroyed && currentBoatHealth > 0)
        {
            isBoatDestroyed = false;
            gameObject.SetActive(true);
            if (enableAdvancedDebug) Debug.Log("🚤 Barco reativado após reparo!");
        }
    }

    private IEnumerator RepairEffect()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) yield break;

        Color originalColor = sprite.color;
        for (int i = 0; i < 3; i++)
        {
            sprite.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            sprite.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator BoatDamageEffect()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) yield break;

        Color originalColor = sprite.color;
        for (int i = 0; i < 3; i++)
        {
            sprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sprite.color = originalColor;
            yield return new WaitForSeconds(0.1f);
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
        if (isBoatDestroyed) return;
        
        isBoatDestroyed = true;
        Debug.Log("💀 BARCO DESTRUÍDO!");
        
        rb.linearVelocity = Vector2.zero;
        canAim = false;
        canAttack = false;
        isCharging = false;
        
        if (isPlayerInside && player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                Debug.Log("☠️ PLAYER MORREU com o barco!");
            }
        }
        
        Invoke("DeactivateBoat", 3f);
    }

    private void DeactivateBoat()
    {
        gameObject.SetActive(false);
    }

    public int GetCurrentBoatHealth() => currentBoatHealth;
    public int GetMaxBoatHealth() => maxBoatHealth;
    public bool CanBeRepaired() => !isBoatDestroyed && currentBoatHealth < maxBoatHealth;
    public bool IsFullyRepaired() => currentBoatHealth >= maxBoatHealth;

    // ============================================
    // SISTEMA DE COMBATE NAVAL (mantido igual)
    // ============================================

    private void InitializeForceIndicator()
    {
        if (forceIndicator != null) forceIndicator.SetActive(false);
        
        if (forceArrowFill != null)
        {
            forceArrowFill.color = new Color(1f, 1f, 1f, 0f);
            forceArrowFill.transform.localScale = new Vector3(1f, 0.1f, 1f);
        }
    }

    private void UpdateForceIndicator()
    {
        if (forceIndicator == null || forceArrowFill == null) return;

        float chargeProgress = (Time.time - chargeStartTime) / chargeTime;
        chargeProgress = Mathf.Clamp01(chargeProgress);
        
        Vector2 indicatorOffset = aimDirection * 0.8f;
        Vector3 targetPosition = transform.position + (Vector3)indicatorOffset;
        forceIndicator.transform.position = targetPosition;
        
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        float adjustedAngle = angle - 90f;
        forceIndicator.transform.rotation = Quaternion.AngleAxis(adjustedAngle, Vector3.forward);
        
        float currentHeight = chargeProgress * maxArrowHeight;
        forceArrowFill.transform.localScale = new Vector3(1f, currentHeight, 1f);
        
        UpdateArrowColor(chargeProgress);
        
        Color fillColor = forceArrowFill.color;
        fillColor.a = chargeProgress;
        forceArrowFill.color = fillColor;
    }
    
    private void UpdateArrowColor(float progress)
    {
        if (forceArrowFill == null) return;
        
        Color targetColor;
        if (progress <= 0.3f) targetColor = yellowColor;
        else if (progress <= 0.7f) targetColor = greenColor;
        else targetColor = redColor;
        
        targetColor.a = forceArrowFill.color.a;
        forceArrowFill.color = targetColor;
    }

    private void ShowForceIndicator()
    {
        if (forceIndicator != null) forceIndicator.SetActive(true);
        
        if (forceArrowFill != null)
        {
            Color fillColor = yellowColor;
            fillColor.a = 0.3f;
            forceArrowFill.color = fillColor;
            forceArrowFill.transform.localScale = new Vector3(1f, 0.1f, 1f);
        }
    }

    private void HideForceIndicator()
    {
        if (forceIndicator != null) forceIndicator.SetActive(false);
        
        if (forceArrowFill != null)
        {
            Color fillColor = yellowColor;
            fillColor.a = 0.3f;
            forceArrowFill.color = fillColor;
            forceArrowFill.transform.localScale = new Vector3(1f, 0.1f, 1f);
        }
    }

    private void InitializeAttackSystem()
    {
        canAim = true;
        canAttack = true;
        if (enableAdvancedDebug) Debug.Log("🎯 Sistema de mira naval inicializado");
    }

    private void UpdateAimDirection()
    {
        if (mainCamera == null) return;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        aimDirection = (mouseWorldPos - transform.position).normalized;
    }

    private void CheckAttackInput()
    {
        // ⭐⭐ NOVA VERIFICAÇÃO: Ignora clique se estiver sobre UI ⭐⭐
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Clique está sobre UI (menu), ignora ataque
            return;
        }
        
        if (Input.GetMouseButtonDown(0) && IsAimValid() && canAttack && !isCharging)
        {
            StartCharging();
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ReleaseCharge();
        }

        if (isCharging)
        {
            UpdateCharge();
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeStartTime = Time.time;
        currentChargeForce = minProjectileForce;
        ShowForceIndicator();
        if (enableAdvancedDebug) Debug.Log("⚡ Iniciando carga do disparo...");
    }

    private void UpdateCharge()
    {
        float chargeProgress = (Time.time - chargeStartTime) / chargeTime;
        chargeProgress = Mathf.Clamp01(chargeProgress);
        currentChargeForce = Mathf.Lerp(minProjectileForce, maxProjectileForce, chargeProgress);
    }

    private void ReleaseCharge()
    {
        if (!isCharging) return;
        if (enableAdvancedDebug) Debug.Log($"💥 DISPARANDO! Força: {currentChargeForce:F1}");
        StartNavalAttack(currentChargeForce);
        HideForceIndicator();
        isCharging = false;
        currentChargeForce = 0f;
    }

    private void StartNavalAttack(float force)
    {
        if (!canAttack || navalProjectilePrefab == null) return;
        
        canAttack = false;
        lastAttackTime = Time.time;
        
        Vector2 sideDirection = GetBoatSideDirection();
        Vector3[] spawnPositions = GetDynamicSpawnPositions(sideDirection);
        
        for (int i = 0; i < 3; i++)
        {
            SpawnProjectile(spawnPositions[i], force, i);
        }
        
        Invoke(nameof(ResetAttackCooldown), attackCooldown);
    }

    private Vector3[] GetDynamicSpawnPositions(Vector2 sideDirection)
    {
        Vector3[] spawnPositions = new Vector3[3];
        Vector2 perpendicular = new Vector2(-sideDirection.y, sideDirection.x);
        float baseDistance = 0.8f;
        float[] verticalOffsets = { -1f, 0f, 1f };
        
        for (int i = 0; i < 3; i++)
        {
            spawnPositions[i] = transform.position + 
                               (Vector3)sideDirection * baseDistance + 
                               (Vector3)perpendicular * verticalOffsets[i];
        }
        return spawnPositions;
    }

    private void SpawnProjectile(Vector3 spawnPosition, float force, int projectileIndex)
    {
        GameObject projectileObj = Instantiate(navalProjectilePrefab, spawnPosition, Quaternion.identity);
        NavalProjectile projectile = projectileObj.GetComponent<NavalProjectile>();
        
        if (projectile != null)
        {
            Vector2 launchVelocity = aimDirection.normalized * force;
            projectile.Launch(launchVelocity, force, minProjectileForce, maxProjectileForce);
        }
    }
    
    private void ResetAttackCooldown()
    {
        canAttack = true;
    }

    public bool IsAimValid()
    {
        float angle = Vector2.Angle(GetBoatSideDirection(), aimDirection);
        return angle <= (attackAngle / 2f);
    }

    private Vector2 GetBoatSideDirection()
    {
        if (lastMoveDirection == Vector2.zero) return Vector2.right;
        
        Vector2 forward = lastMoveDirection.normalized;
        Vector2 right = new Vector2(forward.y, -forward.x);
        Vector2 left = new Vector2(-forward.y, forward.x);
        
        float rightDot = Vector2.Dot(right, aimDirection);
        float leftDot = Vector2.Dot(left, aimDirection);
        
        if (Mathf.Abs(rightDot) > Mathf.Abs(leftDot))
            return rightDot > 0 ? right : -right;
        else
            return leftDot > 0 ? left : -left;
    }

    // ============================================
    // SISTEMA DE LANDING ZONES
    // ============================================

    private void FindAllLandingZones()
    {
        LandingZone[] zones = FindObjectsByType<LandingZone>(FindObjectsSortMode.None);
        landingZones = new List<LandingZone>(zones);
        if (enableAdvancedDebug) Debug.Log($"🏝️ Encontradas {landingZones.Count} Landing Zones");
    }
    
    private void CheckLandingZones()
    {
        if (landingZones.Count == 0) return;

        LandingZone nearestZone = null;
        float nearestDistance = float.MaxValue;
        
        foreach (LandingZone zone in landingZones)
        {
            if (zone == null) continue;
            
            float distance = Vector3.Distance(transform.position, zone.transform.position);
            if (zone.IsBoatInRange(transform.position) && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZone = zone;
            }
        }
        
        if (nearestZone != currentLandingZone)
        {
            currentLandingZone = nearestZone;
        }
    }
    
    public bool CanExitBoat()
    {
        return currentLandingZone != null && !isBoatDestroyed;
    }
    
    private Vector3 GetExitPosition()
    {
        if (currentLandingZone != null)
        {
            return currentLandingZone.GetRandomExitPosition();
        }
        return transform.position + Vector3.up * 2f;
    }

    // ============================================
    // ENTRAR/SAIR DO BARCO
    // ============================================

    public void Interact()
    {
        EnterBoat();
    }

    public void EnterBoat()
    {
        if (isPlayerInside || isBoatDestroyed) return;
        
        if (enableAdvancedDebug) Debug.Log("[BARCO] Entrando no barco");
        isPlayerInside = true;
        
        if (player != null)
        {
            lastSafePlayerPosition = player.transform.position;
            
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.OnBoatEnterExit(true);
            }
        }
        
        if (cameraManager != null) cameraManager.SwitchToBoat();
        if (player != null) player.SetActive(false);
        
        UpdateAnimations(Vector2.zero);
    }
    
    public void ExitBoat()
    {
        if (!isPlayerInside || isBoatDestroyed) return;
        
        if (!CanExitBoat())
        {
            Debug.LogWarning("⚠️ Não pode desembarcar aqui! Aproxime-se de um porto.");
            return;
        }
        
        if (enableAdvancedDebug) Debug.Log("[BARCO] Saindo do barco");
        isPlayerInside = false;
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            Vector3 safeExitPos = GetExitPosition();
            player.transform.position = safeExitPos;
            player.SetActive(true);
            
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.OnExitBoat(safeExitPos);
            }
        }

        if (cameraManager != null) cameraManager.SwitchToPlayer();
        UpdateAnimations(Vector2.zero);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.X)) ExitBoat();
    }

    // ============================================
    // MOVIMENTO
    // ============================================

    private void HandleMovement()
    {
        if (!isPlayerInside || isBoatDestroyed) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 moveInput = new Vector2(h, v).normalized;

        if (moveInput.magnitude > 0.1f)
        {
            if (CanMoveInDirection(moveInput))
            {
                rb.linearVelocity = moveInput * baseMoveSpeed;
                lastMoveDirection = moveInput;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        UpdateAnimations(moveInput);
    }

    private void UpdateAnimations(Vector2 moveInput)
    {
        if (animator != null)
        {
            animator.SetFloat("VelocityX", moveInput.x);
            animator.SetFloat("VelocityY", moveInput.y);
            
            bool isMoving = moveInput.magnitude > 0.1f && rb.linearVelocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
            
            if (isMoving)
            {
                lastMoveDirection = moveInput;
            }
            else
            {
                animator.SetFloat("VelocityX", lastMoveDirection.x);
                animator.SetFloat("VelocityY", lastMoveDirection.y);
            }
        }
    }

    private bool CanMoveInDirection(Vector2 direction)
    {
        if (direction.magnitude == 0) return false;
        
        float checkDistance = 0.5f;
        int hitCount = 0;
        
        Vector2[] rayOffsets = new Vector2[] {
            Vector2.zero,
            new Vector2(0.2f, 0), new Vector2(-0.2f, 0)
        };
        
        LayerMask obstacleMask = LayerMask.GetMask("Default", "Water", "Obstacles");
        
        foreach (Vector2 offset in rayOffsets)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                (Vector2)transform.position + offset,
                direction.normalized,
                checkDistance,
                obstacleMask
            );
            
            if (hit.collider != null && hit.collider.CompareTag("LandBound")) hitCount++;
        }
        
        return hitCount < 2;
    }

    // ============================================
    // ⭐ MÉTODO DE DEBUG ⭐
    // ============================================
    
    [ContextMenu("🚤 Mostrar Estado Atual")]
    public void DebugShowBoatState()
    {
        Debug.Log("=== BOAT STATE DEBUG ===");
        Debug.Log($"Posição: {transform.position}");
        Debug.Log($"Vida: {currentBoatHealth}/{maxBoatHealth}");
        Debug.Log($"Player dentro: {isPlayerInside}");
        Debug.Log($"Destruído: {isBoatDestroyed}");
        Debug.Log($"Inicializado: {hasBeenInitialized}");
        
        if (rb != null)
        {
            Debug.Log($"Rigidbody position: {rb.position}");
            Debug.Log($"Rigidbody velocity: {rb.linearVelocity}");
        }
    }
    
}