using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Battle System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public Sprite overworldSprite;
    public Sprite battleSprite;
    public RuntimeAnimatorController battleAnimator;
    
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseMagicAttack = 8;
    public int baseMagicDefense = 4;
    public int baseSpeed = 10;
    
    [Header("Growth Rates")]
    [Range(1.0f, 2.0f)] public float hpGrowth = 1.1f;
    [Range(1.0f, 2.0f)] public float mpGrowth = 1.05f;
    [Range(1.0f, 2.0f)] public float attackGrowth = 1.07f;
    [Range(1.0f, 2.0f)] public float defenseGrowth = 1.06f;
    [Range(1.0f, 2.0f)] public float magicAttackGrowth = 1.07f;
    [Range(1.0f, 2.0f)] public float speedGrowth = 1.04f;
    
    [Header("Starting Skills")]
    public SkillData[] startingSkills;
    
    [Header("Equipment Slots")]
    public bool canUseSwords = true;
    public bool canUseAxes = false;
    public bool canUseBows = false;
    public bool canUseStaffs = false;
    public bool canUseShields = true;
    
    [Header("Elemental Affinities")]
    public ElementType[] elementalStrengths;
    public ElementType[] elementalWeaknesses;
    
    [Header("Inventory System")]
    [Range(10f, 200f)] public float weightCapacity = 50f;
    public List<ItemData.EquipmentSlot> allowedEquipmentSlots = new List<ItemData.EquipmentSlot>();
    
    // Para paper doll
    public Sprite portrait;
    public Color themeColor = Color.white;
    
    [Header("Current State (Runtime)")]
    [HideInInspector] public int currentLevel = 1;
    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMP;
    [HideInInspector] public InventoryManager.EquipmentLoadout currentEquipment = new InventoryManager.EquipmentLoadout();
    
    [Header("Descriptions")]
    [TextArea(3, 5)] public string biography;
    [TextArea(2, 3)] public string battleQuote;
    
    // ===== ORIGINAL METHODS =====
    
    // Helper method to calculate stat at specific level
    public int GetMaxHPAtLevel(int level)
    {
        return Mathf.RoundToInt(baseMaxHP * Mathf.Pow(hpGrowth, level - 1));
    }
    
    public int GetAttackAtLevel(int level)
    {
        return Mathf.RoundToInt(baseAttack * Mathf.Pow(attackGrowth, level - 1));
    }
    
    public int GetMagicAttackAtLevel(int level)
    {
        return Mathf.RoundToInt(baseMagicAttack * Mathf.Pow(magicAttackGrowth, level - 1));
    }
    
    public int GetSpeedAtLevel(int level)
    {
        return Mathf.RoundToInt(baseSpeed * Mathf.Pow(speedGrowth, level - 1));
    }
    
    public bool IsWeakTo(ElementType element)
    {
        if (elementalWeaknesses == null) return false;
        foreach (var weak in elementalWeaknesses)
            if (weak == element) return true;
        return false;
    }
    
    public bool IsStrongAgainst(ElementType element)
    {
        if (elementalStrengths == null) return false;
        foreach (var strong in elementalStrengths)
            if (strong == element) return true;
        return false;
    }
    
    // ===== INVENTORY HELPER METHODS =====
    
    public bool CanEquipItem(ItemData item)
    {
        if (item == null || !item.IsEquipment()) return false;
        
        // Check level requirement
        if (currentLevel < item.requiredLevel) return false;
        
        // Check if slot is allowed
        if (allowedEquipmentSlots.Count > 0 && !allowedEquipmentSlots.Contains(item.equipmentSlot))
            return false;
        
        // Weapon type restrictions
        if (item.weaponType != ItemData.WeaponType.None)
        {
            switch (item.weaponType)
            {
                case ItemData.WeaponType.Sword: return canUseSwords;
                case ItemData.WeaponType.Axe: return canUseAxes;
                case ItemData.WeaponType.Bow: return canUseBows;
                case ItemData.WeaponType.Staff: return canUseStaffs;
                case ItemData.WeaponType.Shield: return canUseShields;
            }
        }
        
        return true;
    }
    
    public int GetCurrentMaxHP()
    {
        return GetMaxHPAtLevel(currentLevel);
    }
    
    public int GetCurrentMaxMP()
    {
        return Mathf.RoundToInt(baseMaxMP * Mathf.Pow(mpGrowth, currentLevel - 1));
    }
    
    public int GetCurrentAttack()
    {
        int baseStat = GetAttackAtLevel(currentLevel);
        int equipmentBonus = currentEquipment != null ? currentEquipment.GetTotalStatBonus(ItemData.StatType.Attack) : 0;
        return baseStat + equipmentBonus;
    }
    
    public int GetCurrentDefense()
    {
        int baseStat = Mathf.RoundToInt(baseDefense * Mathf.Pow(defenseGrowth, currentLevel - 1));
        int equipmentBonus = currentEquipment != null ? currentEquipment.GetTotalStatBonus(ItemData.StatType.Defense) : 0;
        return baseStat + equipmentBonus;
    }
    
    public int GetCurrentMagicAttack()
    {
        int baseStat = GetMagicAttackAtLevel(currentLevel);
        int equipmentBonus = currentEquipment != null ? currentEquipment.GetTotalStatBonus(ItemData.StatType.MagicAttack) : 0;
        return baseStat + equipmentBonus;
    }
    
    public int GetCurrentMagicDefense()
    {
        int baseStat = Mathf.RoundToInt(baseMagicDefense * Mathf.Pow(defenseGrowth, currentLevel - 1));
        int equipmentBonus = currentEquipment != null ? currentEquipment.GetTotalStatBonus(ItemData.StatType.MagicDefense) : 0;
        return baseStat + equipmentBonus;
    }
    
    public int GetCurrentSpeed()
    {
        int baseStat = GetSpeedAtLevel(currentLevel);
        int equipmentBonus = currentEquipment != null ? currentEquipment.GetTotalStatBonus(ItemData.StatType.Speed) : 0;
        return baseStat + equipmentBonus;
    }
    
    public string GetStatSummary()
    {
        return $"{characterName} (Lv {currentLevel})\n" +
               $"HP: {currentHP}/{GetCurrentMaxHP()} | MP: {currentMP}/{GetCurrentMaxMP()}\n" +
               $"ATK: {GetCurrentAttack()} | DEF: {GetCurrentDefense()}\n" +
               $"M.ATK: {GetCurrentMagicAttack()} | M.DEF: {GetCurrentMagicDefense()}\n" +
               $"SPD: {GetCurrentSpeed()} | Capacity: {weightCapacity}kg";
    }
    
    public void InitializeForBattle()
    {
        currentHP = GetCurrentMaxHP();
        currentMP = GetCurrentMaxMP();
    }

    [ContextMenu("🔍 Debug: Print Character Equipment")]
    public void DebugPrintCharacterEquipment()
    {
        Debug.Log("╔════════════════════════════════════════════╗");
        Debug.Log($"║  🎯 CHARACTER: {characterName}");
        Debug.Log("╠════════════════════════════════════════════╣");
        
        if (currentEquipment == null)
        {
            Debug.LogError("❌ currentEquipment is NULL!");
            Debug.Log("╚════════════════════════════════════════════╝");
            return;
        }
        
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var item = currentEquipment.GetItemInSlot(slot);
            
            if (item != null)
            {
                Debug.Log($"║  ✅ [{slot}]: {item.itemName}");
            }
            else
            {
                Debug.Log($"║  ⬜ [{slot}]: Empty");
            }
        }
        
        Debug.Log("╚════════════════════════════════════════════╝");
    }

}