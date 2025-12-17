using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Battle System/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public Sprite battleSprite;
    public RuntimeAnimatorController battleAnimator;
    public EnemyType enemyType = EnemyType.Normal;
    
    public enum EnemyType { Normal, Elite, Boss, Miniboss }
    
    [Header("Base Stats")]
    public int maxHP = 50;
    public int maxMP = 20;
    public int attack = 8;
    public int defense = 4;
    public int magicAttack = 6;
    public int magicDefense = 3;
    public int speed = 8;
    public int experienceReward = 10;
    public int goldReward = 5;
    
    [Header("AI Behavior")]
    public AIType aiType = AIType.Aggressive;
    
    public enum AIType 
    { 
        Aggressive,     // Sempre ataca
        Defensive,      // Prioriza defesa/cura
        Strategic,      // Usa habilidades quando vantajoso
        Random,         // Ações aleatórias
        Support         // Cura/buff aliados
    }
    
    [Header("Resistances & Weaknesses")]
    public ElementType[] weaknesses;
    public ElementType[] resistances;
    public ElementType[] immunities;
    
    [Header("Skills & Drops")]
    public SkillData[] skills;
    public DropItem[] possibleDrops;
    
    [System.Serializable]
    public class DropItem
    {
        public ItemData item;
        [Range(0f, 1f)] public float dropRate = 0.1f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }
    
    [Header("Visuals")]
    public Color tintColor = Color.white;
    public Vector3 battleScale = Vector3.one;
    
    [Header("Battle Positioning")]
    public BattlePosition preferredPosition = BattlePosition.Front;
    
    public enum BattlePosition { Front, Mid, Back }
    
    [Header("Descriptions")]
    [TextArea(3, 5)] public string description;
    
    // Helper methods
    public bool IsWeakTo(ElementType element)
    {
        if (weaknesses == null) return false;
        foreach (var weak in weaknesses)
            if (weak == element) return true;
        return false;
    }
    
    public bool IsResistantTo(ElementType element)
    {
        if (resistances == null) return false;
        foreach (var res in resistances)
            if (res == element) return true;
        return false;
    }
    
    public bool IsImmuneTo(ElementType element)
    {
        if (immunities == null) return false;
        foreach (var immune in immunities)
            if (immune == element) return true;
        return false;
    }
}