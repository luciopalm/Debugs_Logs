using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Battle System/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    public ItemType itemType = ItemType.Consumable;
    [Header("Identification")]
    public string itemID; 
    
    public enum ItemType 
    { 
        Consumable, 
        Equipment, 
        KeyItem, 
        Material,
        QuestItem
    }
    
    [Header("Rarity System")]
    public ItemRarity rarity = ItemRarity.Common;
    
    public enum ItemRarity 
    { 
        Common,     // White - No bonus
        Uncommon,   // Green - +10% stats
        Rare,       // Blue - +25% stats + special effect
        Epic,       // Purple - +50% stats + 2 effects
        Legendary   // Orange - +100% stats + unique effect
    }
    
    [Header("Equipment Properties")]
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;
    
    // NO ItemData.cs, MODIFICAR a enum EquipmentSlot (aprox linha 45):

public enum EquipmentSlot 
{ 
    None,
    Weapon,
    Armor,
    Body,
    Helmet,
    Gloves,
    Boots,
    Accessory,
    Ring,
    Amulet,
    
    // ⭐ NOVOS: Slots para seu paper doll (10 slots totais)
    OffHand,      // Escudo ou arma secundária
    LongRange,    // Arcos/balestras
    MainHand      // Arma principal (diferente de Weapon genérico)
}
    
    [Header("Weapon Properties")]
    public WeaponType weaponType = WeaponType.None;
    
    public enum WeaponType 
    { 
        None,
        Sword,
        Axe,
        Mace,
        Staff,
        Wand,
        Bow,
        Dagger,
        Spear,
        Shield
    }
    
    // ⭐ NOVO: Sistema de Peso/Carga
    [Header("Weight System")]
    [Range(0.1f, 100f)] public float weight = 0.5f;
    
    [Header("Consumable Effects")]
    public int hpRestore = 0;
    public int mpRestore = 0;
    public bool revive = false;
    public bool cureAllStatus = false;
    
    [Header("Equipment Stat Bonuses")]
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int magicAttackBonus = 0;
    public int magicDefenseBonus = 0;
    public int speedBonus = 0;
    public int criticalRateBonus = 0;
    public int evasionBonus = 0;
    
    [Header("Special Effects")]
    public ElementType element = ElementType.None;
    public StatusType statusImmunity = StatusType.None;
    public SkillData grantedSkill = null;
    
    [Header("Usage Properties")]
    public bool usableInBattle = true;
    public bool usableInField = true;
    public TargetType usableTarget = TargetType.SingleAlly;
    
    public enum TargetType
    {
        SingleAlly,
        AllAllies,
        Self,
        AnySingle,
        SingleEnemy,
        AllEnemies
    }

    public enum StatType 
    { 
        Attack, 
        Defense, 
        MagicAttack, 
        MagicDefense, 
        Speed,
        Accuracy,
        Evasion,
        CriticalRate,
        MaxHP,
        MaxMP
    }

    
    [Header("Inventory Properties")]
    [Range(1, 99)] public int stackLimit = 1;
    [Range(1, 99)] public int requiredLevel = 1;
    public bool isSellable = true;
    public bool isDroppable = true;
    public bool isTradeable = true;
    
    [Header("Durability System")]
    public bool hasDurability = false;
    [Range(1, 1000)] public int maxDurability = 100;
    public int currentDurability = 100;
    
    [Header("Shop & Economy")]
    public int buyPrice = 10;
    public int sellPrice = 5;
    
    [Header("Visuals & Sounds")]
    public GameObject useEffect;
    public AudioClip useSound;
    public Color rarityColor = Color.white;
    
    [Header("Descriptions")]
    [TextArea(3, 5)] public string description;
    [TextArea(2, 3)] public string flavorText;
    
    // Helper methods
    public bool CanUseOnTarget(bool isAlly, bool isSelf)
    {
        switch (usableTarget)
        {
            case TargetType.Self:
                return isSelf;
                
            case TargetType.SingleAlly:
            case TargetType.AllAllies:
                return isAlly && !isSelf;
                
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
                return !isAlly;
                
            case TargetType.AnySingle:
                return true;
                
            default:
                return false;
        }
    }
    
    public bool IsEquipment()
    {
        return itemType == ItemType.Equipment;
    }
    
    public bool IsConsumable()
    {
        return itemType == ItemType.Consumable;
    }
    
    public bool IsWeapon()
    {
        return equipmentSlot == EquipmentSlot.Weapon;
    }
    
    public bool IsArmor()
    {
        return equipmentSlot == EquipmentSlot.Armor || 
               equipmentSlot == EquipmentSlot.Helmet ||
               equipmentSlot == EquipmentSlot.Gloves ||
               equipmentSlot == EquipmentSlot.Boots;
    }
    
    public bool IsAccessory()
    {
        return equipmentSlot == EquipmentSlot.Accessory ||
               equipmentSlot == EquipmentSlot.Ring ||
               equipmentSlot == EquipmentSlot.Amulet;
    }
    
    // ⭐ NOVO: Helper para categoria da tabela
    public string GetCategoryName()
    {
        if (IsWeapon()) return "Weapons";
        if (IsArmor()) return "Armor";
        if (IsAccessory()) return "Accessories";
        if (IsConsumable()) return "Consumables";
        if (itemType == ItemType.Material) return "Materials";
        if (itemType == ItemType.KeyItem || itemType == ItemType.QuestItem) return "Key Items";
        return "Miscellaneous";
    }
    
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:      return Color.white;
            case ItemRarity.Uncommon:    return Color.green;
            case ItemRarity.Rare:        return new Color(0.2f, 0.4f, 1f); // Blue
            case ItemRarity.Epic:        return new Color(0.8f, 0.2f, 1f); // Purple
            case ItemRarity.Legendary:   return new Color(1f, 0.5f, 0f);   // Orange
            default:                     return Color.white;
        }
    }
    
    public float GetRarityMultiplier()
    {
        switch (rarity)
        {
            case ItemRarity.Common:      return 1.0f;
            case ItemRarity.Uncommon:    return 1.1f;
            case ItemRarity.Rare:        return 1.25f;
            case ItemRarity.Epic:        return 1.5f;
            case ItemRarity.Legendary:   return 2.0f;
            default:                     return 1.0f;
        }
    }
    
    public int GetCalculatedBuyPrice()
    {
        return Mathf.RoundToInt(buyPrice * GetRarityMultiplier());
    }
    
    public int GetCalculatedSellPrice()
    {
        return Mathf.RoundToInt(sellPrice * GetRarityMultiplier());
    }
    
    public int GetTotalStatBonus()
    {
        return attackBonus + defenseBonus + magicAttackBonus + 
               magicDefenseBonus + speedBonus + criticalRateBonus + evasionBonus;
    }
    
    public string GetTooltipText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Name with rarity color tag
        sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{itemName}</color>");
        
        // Type and slot
        sb.AppendLine($"Type: {itemType}");
        if (IsEquipment())
        {
            sb.AppendLine($"Slot: {equipmentSlot}");
            if (weaponType != WeaponType.None)
                sb.AppendLine($"Weapon: {weaponType}");
        }
        
        // Stats
        if (attackBonus != 0) sb.AppendLine($"Attack: +{attackBonus}");
        if (defenseBonus != 0) sb.AppendLine($"Defense: +{defenseBonus}");
        if (magicAttackBonus != 0) sb.AppendLine($"Magic Attack: +{magicAttackBonus}");
        if (magicDefenseBonus != 0) sb.AppendLine($"Magic Defense: +{magicDefenseBonus}");
        if (speedBonus != 0) sb.AppendLine($"Speed: +{speedBonus}");
        
        // ⭐ NOVO: Peso
        sb.AppendLine($"Weight: {weight:F1} kg");
        
        // Consumable effects
        if (hpRestore != 0) sb.AppendLine($"Restores {hpRestore} HP");
        if (mpRestore != 0) sb.AppendLine($"Restores {mpRestore} MP");
        if (revive) sb.AppendLine($"Revives fallen ally");
        if (cureAllStatus) sb.AppendLine($"Cures all status effects");
        
        // Requirements
        if (requiredLevel > 1) sb.AppendLine($"Required Level: {requiredLevel}");
        
        // Stack info
        if (stackLimit > 1) sb.AppendLine($"Stack: Up to {stackLimit}");
        
        // Price
        sb.AppendLine($"Buy: {GetCalculatedBuyPrice()} gold");
        
        // Description
        if (!string.IsNullOrEmpty(description))
            sb.AppendLine($"\n{description}");
            
        if (!string.IsNullOrEmpty(flavorText))
            sb.AppendLine($"<i>{flavorText}</i>");
        
        return sb.ToString();
    }
    
    // For debugging
    public override string ToString()
    {
        return $"{itemName} ({rarity} {itemType}) - Stats: ATK+{attackBonus} DEF+{defenseBonus}";
    }
}