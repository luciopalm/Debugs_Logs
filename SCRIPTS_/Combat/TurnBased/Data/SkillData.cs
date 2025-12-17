using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle System/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public SkillType skillType = SkillType.Physical;
    
    public enum SkillType 
    { 
        Physical,       // Uses Attack stat
        Magical,        // Uses MagicAttack stat
        Healing,        // Heals allies
        Support,        // Buffs/Debuffs
        Special         // Unique effects
    }
    
    [Header("Targeting")]
    public TargetType targetType = TargetType.SingleEnemy;
    
    public enum TargetType
    {
        SingleEnemy,    // One enemy
        AllEnemies,     // All enemies
        SingleAlly,     // One ally
        AllAllies,      // All allies
        Self,           // Only user
        AnySingle,      // Any single target
        AnyAll          // Everyone on field
    }
    
    [Header("Effects")]
    public ElementType element = ElementType.None;
    public int basePower = 10;
    public int mpCost = 5;
    public float hitRate = 0.95f; // 95% hit chance
    
    [Header("ATB System")]
    public float actionTime = 2.0f; // Time to execute (seconds)
    public float recoveryTime = 1.5f; // Recovery time after use
    
    [Header("Additional Effects")]
    public StatusEffect[] inflictsStatus;
    public StatModifier[] appliesModifiers;
    
    [System.Serializable]
    public class StatusEffect
    {
        public StatusType status;
        [Range(0f, 1f)] public float chance = 0.3f;
        public int durationTurns = 3;
    }
    
    [System.Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float multiplier = 1.2f; // 20% increase
        public int durationTurns = 3;
    }
    
    // ⭐⭐ MOVIDO PARA GLOBAL (no final do arquivo)
    // public enum StatusType 
    // { 
    //     Poison, Burn, Freeze, Paralyze, Sleep, 
    //     Confuse, Silence, Blind, Haste, Slow, 
    //     Protect, Shell, Regen, Berserk 
    // }
    
    public enum StatType 
    { 
        Attack, Defense, MagicAttack, MagicDefense, Speed, 
        Accuracy, Evasion, CriticalRate 
    }
    
    [Header("Visuals & SFX")]
    public AnimationClip battleAnimation;
    public GameObject vfxPrefab;
    public AudioClip castSound;
    public AudioClip hitSound;
    
    [Header("Requirements")]
    public int requiredLevel = 1;
    public WeaponType requiredWeapon = WeaponType.Any;
    
    public enum WeaponType 
    { 
        Any, Sword, Axe, Bow, Staff, 
        Dagger, Spear, Hammer, Gun 
    }
    
    [Header("Descriptions")]
    [TextArea(3, 5)] public string description;
    [TextArea(2, 3)] public string effectDescription;
    
    // Helper methods
    public bool CanTargetEnemies()
    {
        return targetType == TargetType.SingleEnemy || 
               targetType == TargetType.AllEnemies ||
               targetType == TargetType.AnySingle ||
               targetType == TargetType.AnyAll;
    }
    
    public bool CanTargetAllies()
    {
        return targetType == TargetType.SingleAlly || 
               targetType == TargetType.AllAllies ||
               targetType == TargetType.Self ||
               targetType == TargetType.AnySingle ||
               targetType == TargetType.AnyAll;
    }
    
    public bool IsAoE()
    {
        return targetType == TargetType.AllEnemies || 
               targetType == TargetType.AllAllies ||
               targetType == TargetType.AnyAll;
    }
}

// ENUMS GLOBAIS (for everyone to use)
public enum ElementType 
{ 
    None, Fire, Water, Earth, Wind, 
    Lightning, Ice, Holy, Dark, Physical 
}

// ⭐⭐ STATUS TYPE GLOBAL ⭐⭐
public enum StatusType 
{ 
    None,
    Poison, 
    Burn, 
    Freeze, 
    Paralyze, 
    Sleep, 
    Confuse, 
    Silence, 
    Blind, 
    Haste, 
    Slow, 
    Protect, 
    Shell, 
    Regen, 
    Berserk 
}