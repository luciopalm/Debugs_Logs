// CharacterStatsCache.cs
using UnityEngine;

[System.Serializable]
public class CharacterStatsCache : MonoBehaviour
{
    [Header("Current Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;
    [SerializeField] private int attack = 10;
    [SerializeField] private int defense = 5;
    [SerializeField] private int magicAttack = 8;
    [SerializeField] private int magicDefense = 4;
    [SerializeField] private int speed = 10;
    
    [Header("References")]
    [SerializeField] private CharacterData currentCharacter;
    
    [Header("Configuration")]
    [SerializeField] private bool useEquipmentBonuses = true;
    [SerializeField] private float speedToMoveSpeedRatio = 0.05f; // Cada ponto de speed = +5% moveSpeed
    
    // Eventos
    public System.Action OnStatsUpdated;
    public System.Action OnDamageTaken;
    public System.Action OnHealed;
    public System.Action OnCharacterChanged;
    
    #region Properties
    public float MoveSpeed => moveSpeed;
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public int Defense => defense;
    public int MagicAttack => magicAttack;
    public int MagicDefense => magicDefense;
    public int Speed => speed;
    public bool IsAlive => currentHP > 0;
    public CharacterData CurrentCharacter => currentCharacter;
    #endregion
    
    private void Awake()
    {
        // Garantir que temos um CharacterData se não tiver sido atribuído
        if (currentCharacter == null)
        {
            Debug.LogWarning("[CharacterStatsCache] No CharacterData assigned, trying to find from PartyManager");
            
            var partyManager = PartyManager.Instance;
            if (partyManager != null)
            {
                var activeChar = partyManager.GetActiveMember();
                if (activeChar != null)
                {
                    UpdateFromCharacterData(activeChar);
                }
            }
        }
    }
    
    /// <summary>
    /// Atualiza todos os stats baseados no CharacterData
    /// </summary>
    public void UpdateFromCharacterData(CharacterData character)
    {
        if (character == null)
        {
            Debug.LogError("[CharacterStatsCache] Cannot update from null CharacterData!");
            return;
        }
        
        currentCharacter = character;
        
        // Base stats do character
        maxHP = character.GetCurrentMaxHP();
        currentHP = Mathf.Clamp(character.currentHP, 1, maxHP); // Garantir pelo menos 1 HP
        attack = character.GetCurrentAttack();
        defense = character.GetCurrentDefense();
        magicAttack = character.GetCurrentMagicAttack();
        magicDefense = character.GetCurrentMagicDefense();
        speed = character.GetCurrentSpeed();
        
        // Calcular moveSpeed baseado nos stats do character
        // Fórmula: moveSpeed base (5) + (speed * ratio)
        float baseMoveSpeed = 5f;
        moveSpeed = baseMoveSpeed + (speed * speedToMoveSpeedRatio);
        
        // Aplicar bônus de equipamentos se habilitado
        if (useEquipmentBonuses && character.currentEquipment != null)
        {
            ApplyEquipmentBonuses(character);
        }
        
        Debug.Log($"[CharacterStatsCache] Stats updated from {character.characterName}:");
        Debug.Log($"   HP: {currentHP}/{maxHP}");
        Debug.Log($"   ATK: {attack} | DEF: {defense}");
        Debug.Log($"   M.ATK: {magicAttack} | M.DEF: {magicDefense}");
        Debug.Log($"   SPD: {speed} | Move Speed: {moveSpeed:F2}");
        
        OnStatsUpdated?.Invoke();
        OnCharacterChanged?.Invoke();
    }
    
    /// <summary>
    /// Aplica bônus adicionais de equipamentos
    /// </summary>
    private void ApplyEquipmentBonuses(CharacterData character)
    {
        // Movimento adicional de equipamentos leves/ágil
        int agilityBonus = 0;
        var equipment = character.currentEquipment;
        
        // Verificar equipamentos para bônus de velocidade
        // (Você pode adicionar lógica específica aqui baseada nos itens equipados)
        
        // Aplicar bônus de velocidade se houver
        if (agilityBonus > 0)
        {
            moveSpeed += agilityBonus * 0.1f;
            Debug.Log($"   +{agilityBonus * 0.1f:F2} move speed from equipment agility");
        }
    }
    
    /// <summary>
    /// Recebe dano, considerando defesa
    /// </summary>
    public void TakeDamage(int rawDamage, bool isMagical = false)
    {
        if (currentCharacter == null || !IsAlive) return;
        
        int defenseValue = isMagical ? magicDefense : defense;
        int actualDamage = Mathf.Max(1, rawDamage - (defenseValue / 2));
        
        currentHP = Mathf.Max(0, currentHP - actualDamage);
        currentCharacter.currentHP = currentHP;
        
        Debug.Log($"[CharacterStatsCache] {currentCharacter.characterName} took {actualDamage} damage! HP: {currentHP}/{maxHP}");
        
        OnDamageTaken?.Invoke();
        
        if (currentHP <= 0)
        {
            Debug.Log($"[CharacterStatsCache] {currentCharacter.characterName} has been defeated!");
            // TODO: Disparar evento de morte
        }
    }
    
    /// <summary>
    /// Recupera vida
    /// </summary>
    public void Heal(int amount)
    {
        if (currentCharacter == null || !IsAlive) return;
        
        int oldHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        currentCharacter.currentHP = currentHP;
        
        int healedAmount = currentHP - oldHP;
        
        if (healedAmount > 0)
        {
            Debug.Log($"[CharacterStatsCache] {currentCharacter.characterName} healed {healedAmount} HP! HP: {currentHP}/{maxHP}");
            OnHealed?.Invoke();
        }
    }
    
    /// <summary>
    /// Restaura toda a vida
    /// </summary>
    public void RestoreFullHealth()
    {
        if (currentCharacter == null) return;
        
        currentHP = maxHP;
        currentCharacter.currentHP = currentHP;
        
        Debug.Log($"[CharacterStatsCache] {currentCharacter.characterName} fully restored! HP: {currentHP}/{maxHP}");
        OnHealed?.Invoke();
    }
    
    /// <summary>
    /// Usa uma porcentagem da vida atual (para custos de habilidades)
    /// </summary>
    public bool UsePercentageOfHealth(float percentage)
    {
        if (currentCharacter == null || !IsAlive) return false;
        
        int cost = Mathf.RoundToInt(currentHP * percentage);
        
        if (cost >= currentHP)
        {
            Debug.LogWarning($"[CharacterStatsCache] Cannot use {percentage:P0} of health - would cause death");
            return false;
        }
        
        currentHP -= cost;
        currentCharacter.currentHP = currentHP;
        
        Debug.Log($"[CharacterStatsCache] {currentCharacter.characterName} used {cost} HP ({percentage:P0}) for ability");
        return true;
    }
    
    /// <summary>
    /// Atualiza o cache quando equipamentos mudam
    /// </summary>
    public void RefreshFromCurrentCharacter()
    {
        if (currentCharacter != null)
        {
            UpdateFromCharacterData(currentCharacter);
        }
    }
    
    /// <summary>
    /// Retorna uma string com resumo dos stats
    /// </summary>
    public string GetStatsSummary()
    {
        if (currentCharacter == null) return "No character data";
        
        return $"{currentCharacter.characterName}\n" +
               $"HP: {currentHP}/{maxHP}\n" +
               $"ATK: {attack} | DEF: {defense}\n" +
               $"M.ATK: {magicAttack} | M.DEF: {magicDefense}\n" +
               $"SPD: {speed} | Move: {moveSpeed:F2}";
    }
    
    #region Debug Methods
    [ContextMenu("Debug: Print Current Stats")]
    public void DebugPrintCurrentStats()
    {
        Debug.Log("=== CHARACTER STATS CACHE ===");
        Debug.Log(GetStatsSummary());
    }
    
    [ContextMenu("Debug: Take 15 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(15);
    }
    
    [ContextMenu("Debug: Heal 25 HP")]
    public void DebugHeal()
    {
        Heal(25);
    }
    
    [ContextMenu("Debug: Restore Full Health")]
    public void DebugRestoreFullHealth()
    {
        RestoreFullHealth();
    }
    #endregion
}