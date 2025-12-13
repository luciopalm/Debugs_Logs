// BattleAction.cs - VERSÃO CORRIGIDA
using UnityEngine;

namespace Combat.TurnBased
{
    [System.Serializable]
    public struct BattleAction
    {
        public BattleUnit user;
        public BattleUnit target;
        public SkillData skill;
        public bool isAttack;
        public bool isDefend;
        public bool isItem;
        public bool isRun;
        
        // Helper method para validar ação
        public bool IsValid()
        {
            if (isRun) return true;
            if (isAttack && target != null && user != null) return true;
            if (skill != null && target != null && user != null) return true;
            if (isDefend && user != null) return true;
            if (isItem && user != null) return true;
            return false;
        }
        
        public void Execute()
        {
            if (isRun) return;
            
            if (isDefend)
            {
                // TODO: Implementar defesa (reduz dano no próximo turno)
                Debug.Log($"{user.unitName} defende!");
                return;
            }
            
            if (isAttack && target != null)
            {
                int damage = CalculatePhysicalDamage();
                target.TakeDamage(damage);
            }
            else if (skill != null && target != null)
            {
                ExecuteSkill();
            }
        }
        
        private int CalculatePhysicalDamage()
        {
            int baseDamage = user.attack;
            int targetDefense = target.defense;
            
            int damage = Mathf.Max(1, baseDamage - targetDefense);
            
            // Adicionar variação aleatória
            damage = Mathf.RoundToInt(damage * Random.Range(0.9f, 1.1f));
            
            Debug.Log($"{user.unitName} ataca {target.unitName} causando {damage} de dano!");
            return damage;
        }
        
        private void ExecuteSkill()
        {
            Debug.Log($"{user.unitName} usa {skill.skillName} em {target.unitName}!");
            
            // Verificar MP
            if (user.currentMP < skill.mpCost)
            {
                Debug.Log($"{user.unitName} não tem MP suficiente!");
                return;
            }
            
            // Consumir MP
            user.UseMP(skill.mpCost);
            
            // Verificar acerto
            if (Random.value > skill.hitRate)
            {
                Debug.Log($"{skill.skillName} errou!");
                return;
            }
            
            // Calcular dano/efeito baseado no tipo de habilidade
            switch (skill.skillType)
            {
                case SkillData.SkillType.Physical:
                    int physicalDamage = CalculateSkillDamage(user.attack, target.defense);
                    target.TakeDamage(physicalDamage);
                    break;
                    
                case SkillData.SkillType.Magical:
                    // TODO: Usar MagicAttack/MagicDefense quando implementado
                    int magicalDamage = CalculateSkillDamage(user.attack, target.defense);
                    target.TakeDamage(magicalDamage);
                    break;
                    
                case SkillData.SkillType.Healing:
                    int healAmount = skill.basePower;
                    target.Heal(healAmount);
                    Debug.Log($"{target.unitName} recupera {healAmount} HP!");
                    break;
                    
                case SkillData.SkillType.Support:
                    // TODO: Implementar buffs/debuffs
                    Debug.Log($"{skill.skillName} aplicado!");
                    break;
                    
                case SkillData.SkillType.Special:
                    // TODO: Implementar efeitos especiais
                    Debug.Log("Efeito especial!");
                    break;
            }
            
            // Aplicar efeitos adicionais (veneno, buffs, etc.)
            ApplyAdditionalEffects();
        }
        
        private int CalculateSkillDamage(int userStat, int targetStat)
        {
            float multiplier = 1f;
            
            // Verificar fraquezas/resistências elementais
            if (target is EnemyUnit enemyUnit && enemyUnit.enemyData != null)
            {
                if (enemyUnit.enemyData.IsWeakTo(skill.element))
                    multiplier *= 1.5f;
                else if (enemyUnit.enemyData.IsResistantTo(skill.element))
                    multiplier *= 0.5f;
                else if (enemyUnit.enemyData.IsImmuneTo(skill.element))
                    multiplier = 0f;
            }
            
            int damage = Mathf.RoundToInt(skill.basePower * multiplier - targetStat * 0.5f);
            damage = Mathf.Max(1, damage);
            damage = Mathf.RoundToInt(damage * Random.Range(0.85f, 1.15f));
            
            return damage;
        }
        
        private void ApplyAdditionalEffects()
        {
            if (skill.inflictsStatus != null && skill.inflictsStatus.Length > 0)
            {
                foreach (var status in skill.inflictsStatus)
                {
                    if (Random.value <= status.chance)
                    {
                        // TODO: Implementar sistema de status
                        Debug.Log($"{target.unitName} foi afetado por {status.status}!");
                    }
                }
            }
        }
    }
}