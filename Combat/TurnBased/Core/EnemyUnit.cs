// EnemyUnit.cs - VERSÃO CORRIGIDA
using UnityEngine;

namespace Combat.TurnBased
{
    public class EnemyUnit : BattleUnit
    {
        [Header("Enemy Data")]
        public EnemyData enemyData;
        
        public override void Initialize()
        {
            if (enemyData != null)
            {
                unitName = enemyData.enemyName;
                maxHP = enemyData.maxHP;
                maxMP = enemyData.maxMP;
                attack = enemyData.attack;
                defense = enemyData.defense;
                speed = enemyData.speed;
                battleSprite = enemyData.battleSprite;
                
                if (enemyData.battleAnimator != null && animator != null)
                    animator.runtimeAnimatorController = enemyData.battleAnimator;
                    
                skills = enemyData.skills;
            }
            
            base.Initialize();
        }
        
        public override BattleAction SelectAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // ⭐⭐ CORREÇÃO: aliados = outros inimigos, inimigos = jogadores
            if (enemyData == null || enemies.Length == 0)
                return CreateDefaultAction(enemies); // ⬅️ AGORA PASSA OS INIMIGOS (jogadores)
            
            switch (enemyData.aiType)
            {
                case EnemyData.AIType.Aggressive:
                    return SelectAggressiveAction(allies, enemies);
                    
                case EnemyData.AIType.Defensive:
                    return SelectDefensiveAction(allies, enemies);
                    
                case EnemyData.AIType.Strategic:
                    return SelectStrategicAction(allies, enemies);
                    
                case EnemyData.AIType.Support:
                    return SelectSupportAction(allies, enemies);
                    
                case EnemyData.AIType.Random:
                default:
                    return SelectRandomAction(allies, enemies);
            }
        }
        
        private BattleAction CreateDefaultAction(BattleUnit[] targets) // ⬅️ PARÂMETRO RENOMEADO
        {
            if (skills.Length > 0 && currentMP >= skills[0].mpCost && Random.value > 0.7f)
            {
                return new BattleAction
                {
                    user = this,
                    target = targets[Random.Range(0, targets.Length)], // ⬅️ AGORA ATACA JOGADORES
                    skill = skills[0],
                    isAttack = false
                };
            }
            
            return new BattleAction
            {
                user = this,
                target = targets[Random.Range(0, targets.Length)], // ⬅️ AGORA ATACA JOGADORES
                isAttack = true
            };
        }
        
        private BattleAction SelectAggressiveAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // Sempre ataca o jogador com menos HP
            BattleUnit weakest = enemies[0]; // ⬅️ AGORA USA ENEMIES (jogadores)
            foreach (var enemy in enemies)
            {
                if (enemy.GetHPPercentage() < weakest.GetHPPercentage())
                    weakest = enemy;
            }
            
            return new BattleAction
            {
                user = this,
                target = weakest,
                isAttack = true
            };
        }
        
        private BattleAction SelectDefensiveAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // 50% chance de defender se estiver com menos de 50% HP
            if (GetHPPercentage() < 0.5f && Random.value > 0.5f)
            {
                return new BattleAction
                {
                    user = this,
                    isDefend = true
                };
            }
            
            return CreateDefaultAction(enemies); // ⬅️ CORREÇÃO
        }
        
        private BattleAction SelectStrategicAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // Usa habilidades quando vantajoso
            foreach (var skill in skills)
            {
                if (currentMP >= skill.mpCost)
                {
                    if (skill.CanTargetEnemies())
                    {
                        return new BattleAction
                        {
                            user = this,
                            target = enemies[Random.Range(0, enemies.Length)], // ⬅️ CORREÇÃO
                            skill = skill,
                            isAttack = false
                        };
                    }
                }
            }
            
            return CreateDefaultAction(enemies); // ⬅️ CORREÇÃO
        }
        
        private BattleAction SelectSupportAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // Cura aliados feridos
            foreach (var ally in allies) // ⬅️ allies = outros inimigos
            {
                if (ally.GetHPPercentage() < 0.3f && ally != this)
                {
                    foreach (var skill in skills)
                    {
                        if (skill.skillType == SkillData.SkillType.Healing && 
                            skill.CanTargetAllies() && 
                            currentMP >= skill.mpCost)
                        {
                            return new BattleAction
                            {
                                user = this,
                                target = ally,
                                skill = skill,
                                isAttack = false
                            };
                        }
                    }
                }
            }
            
            return CreateDefaultAction(enemies); // ⬅️ CORREÇÃO
        }
        
        private BattleAction SelectRandomAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            return CreateDefaultAction(enemies); // ⬅️ CORREÇÃO
        }
    }
}