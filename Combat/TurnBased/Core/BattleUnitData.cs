using UnityEngine;

namespace Combat.TurnBased
{
    [System.Serializable]
    public class BattleUnitData
    {
        public string unitName;
        public int level;
        public int maxHP;
        public int currentHP;
        public int maxMP;
        public int currentMP;
        public int attack;
        public int defense;
        public int speed;
        public SkillData[] skills;
        
        // Construtor vazio para serialização
        public BattleUnitData()
        {
            unitName = "New Unit";
            level = 1;
            maxHP = 100;
            currentHP = 100;
            maxMP = 50;
            currentMP = 50;
            attack = 10;
            defense = 5;
            speed = 10;
            skills = new SkillData[0];
        }
        
        // Construtor de BattleUnit real
        public BattleUnitData(BattleUnit unit)
        {
            if (unit != null)
            {
                unitName = unit.unitName;
                level = unit.level;
                maxHP = unit.maxHP;
                currentHP = unit.currentHP;
                maxMP = unit.maxMP;
                currentMP = unit.currentMP;
                attack = unit.attack;
                defense = unit.defense;
                speed = unit.speed;
                skills = unit.skills != null ? (SkillData[])unit.skills.Clone() : new SkillData[0];
            }
            else
            {
                // Valores padrão
                unitName = "Unknown";
                level = 1;
                maxHP = 100;
                currentHP = 100;
                maxMP = 50;
                currentMP = 50;
                attack = 10;
                defense = 5;
                speed = 10;
                skills = new SkillData[0];
            }
        }
        
        // Construtor de CharacterData (ScriptableObject)
    // Construtor de CharacterData (ScriptableObject) - VERSÃO ATUALIZADA
    public BattleUnitData(CharacterData characterData, int level = 1)
    {
        if (characterData != null)
        {
            unitName = characterData.characterName;
            this.level = level;
            
            // ⭐⭐ USAR MÉTODOS QUE CONSIDERAM EQUIPAMENTOS
            maxHP = characterData.GetCurrentMaxHP();
            currentHP = Mathf.Clamp(characterData.currentHP, 1, maxHP);
            maxMP = characterData.GetCurrentMaxMP();
            currentMP = Mathf.Clamp(characterData.currentMP, 0, maxMP);
            
            // ⭐⭐ CRÍTICO: Usar stats com bônus de equipamentos
            attack = characterData.GetCurrentAttack();
            defense = characterData.GetCurrentDefense();
            speed = characterData.GetCurrentSpeed();
            
            skills = characterData.startingSkills != null ? 
                (SkillData[])characterData.startingSkills.Clone() : new SkillData[0];
            
            // ⭐ DEBUG
            Debug.Log($"[BattleUnitData] Criado de {characterData.characterName}:");
            Debug.Log($"   ATK: {attack} (GetCurrentAttack)");
            Debug.Log($"   DEF: {defense} (GetCurrentDefense)");
            Debug.Log($"   Base ATK: {characterData.GetAttackAtLevel(level)}");
            Debug.Log($"   Equip ATK bonus: {characterData.currentEquipment?.GetTotalStatBonus(ItemData.StatType.Attack) ?? 0}");
        }
    }
        
        // Aplicar dados a um BattleUnit
        public void ApplyToUnit(BattleUnit unit)
        {
            if (unit != null)
            {
                unit.unitName = unitName;
                unit.level = level;
                unit.maxHP = maxHP;
                unit.currentHP = Mathf.Clamp(currentHP, 1, maxHP);
                unit.maxMP = maxMP;
                unit.currentMP = Mathf.Clamp(currentMP, 0, maxMP);
                unit.attack = attack;
                unit.defense = defense;
                unit.speed = speed;
                
                if (skills != null && skills.Length > 0)
                {
                    unit.skills = (SkillData[])skills.Clone();
                }
                else
                {
                    unit.skills = new SkillData[0];
                }
            }
        }
        
        // Salvar dados após batalha
        public void UpdateFromUnit(BattleUnit unit)
        {
            if (unit != null)
            {
                currentHP = Mathf.Clamp(unit.currentHP, 0, maxHP);
                currentMP = Mathf.Clamp(unit.currentMP, 0, maxMP);
                // Podemos salvar outros dados como level, stats, etc.
                level = unit.level;
                
                // Atualizar skills se necessário
                if (unit.skills != null && unit.skills.Length > 0)
                {
                    skills = (SkillData[])unit.skills.Clone();
                }
            }
        }
        
        // Métodos de cura/dano
        public void Heal(int amount)
        {
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        }
        
        public void TakeDamage(int amount)
        {
            currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        }
        
        public void RestoreMP(int amount)
        {
            currentMP = Mathf.Clamp(currentMP + amount, 0, maxMP);
        }
        
        public void UseMP(int amount)
        {
            currentMP = Mathf.Clamp(currentMP - amount, 0, maxMP);
        }
        
        // Helper properties
        public bool IsAlive => currentHP > 0;
        public float HPPercentage => maxHP > 0 ? (float)currentHP / maxHP : 0f;
        public float MPPercentage => maxMP > 0 ? (float)currentMP / maxMP : 0f;
        
        // Debug
        public override string ToString()
        {
            return $"{unitName} Lv{level}: HP {currentHP}/{maxHP}, MP {currentMP}/{maxMP}, ATK {attack}, DEF {defense}, SPD {speed}";
        }
    }
}