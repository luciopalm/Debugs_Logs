using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    
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
    
    // ⭐ REMOVIDO: Sistema de auto-save movido para SaveLoadManager
    // [Header("Save System")]
    // public bool enableAutoSave = true;
    // public float autoSaveInterval = 60f;
    // private float autoSaveTimer = 0f;
    
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
        
        if (showDebugLogs) Debug.Log($"[PlayerController] Awake - Vida: {currentHealth}/{maxHealth}");
    }

    private void Start()
    {
        if (showDebugLogs) 
        {
            Debug.Log($"[PlayerController] Start - Posição: {transform.position}");
            Debug.Log($"[PlayerController] Aguardando save/load externo");
        }
    }

    private void Update()
    {
        HandleUpdate();
        
        // ⭐ REMOVIDO: Auto-save movido para SaveLoadManager
        // Sistema de auto-save
        // if (enableAutoSave && GameDataManager.Instance != null && !isTouchingWater)
        // {
        //     autoSaveTimer += Time.deltaTime;
        //     if (autoSaveTimer >= autoSaveInterval)
        //     {
        //         SavePlayerData();
        //         autoSaveTimer = 0f;
        //     }
        // }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    public void SetWaterTilemap(Tilemap tilemap)
    {
        waterTilemap = tilemap;
    }

    private void HandleUpdate()
    {
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
            rb.linearVelocity = physicsInput * moveSpeed;
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
            if (showDebugLogs) 
                Debug.Log($"[PlayerController] Curado em {healedAmount}. Vida: {currentHealth}/{maxHealth}");
        }
    }
    
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
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

    [ContextMenu("[Player] Mostrar Info Completa")]
    public void DebugShowCompleteState()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[PlayerController] ====== DEBUG ======");
        sb.AppendLine($"Posição: {transform.position}");
        sb.AppendLine($"Vida: {currentHealth}/{maxHealth}");
        sb.AppendLine($"Tocando água: {isTouchingWater}");
        sb.AppendLine($"Atacando: {isAttacking}");
        sb.AppendLine($"Invencível: {isInvincible}");
        
        if (GameDataManager.Instance != null)
        {
            var data = GameDataManager.Instance.GetCurrentGameData();
            if (data != null)
            {
                sb.AppendLine($"Dados no Save:");
                sb.AppendLine($"  - Posição salva: {data.playerData.lastPosition}");
                sb.AppendLine($"  - Vida salva: {data.playerData.currentHealth}/{data.playerData.maxHealth}");
                sb.AppendLine($"  - isNewGame: {data.isNewGame}");
                sb.AppendLine($"  - wasInsideBoat: {data.playerData.wasInsideBoat}");
            }
        }
        sb.AppendLine("=========================");
        Debug.Log(sb.ToString());
    }
}