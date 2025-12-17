using UnityEngine;

namespace Combat.TurnBased
{
    [System.Serializable]
    public class BattleUnit : MonoBehaviour
    {
        [Header("Unit Stats")]
        public string unitName;
        public int level = 1;
        
        [Header("Health")]
        public int maxHP = 100;
        public int currentHP = 100;
        
        [Header("Mana/Stamina")]
        public int maxMP = 50;
        public int currentMP = 50;
        
        [Header("Attributes")]
        public int attack = 10;
        public int defense = 5;
        public int speed = 10;
        
        [Header("Visual")]
        public Sprite battleSprite;
        public Animator animator;
        
        [Header("Skills")]
        public SkillData[] skills;
        
        // Status
        private bool isDead = false;
        
        // Events
        public System.Action OnDamageTaken;
        public System.Action OnDeath;
        public System.Action OnHeal;
        
        public virtual void Initialize()
        {
            currentHP = maxHP;
            currentMP = maxMP;
            isDead = false;
        }
        
        public virtual bool TakeDamage(int damage)
        {
            if (isDead) return false;
            
            int actualDamage = Mathf.Max(1, damage - defense);
            currentHP -= actualDamage;
            
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
            
            OnDamageTaken?.Invoke();
            
            if (currentHP <= 0)
            {
                Die();
                return true;
            }
            
            return false;
        }
        
        public virtual void Heal(int amount)
        {
            if (isDead) return;
            
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
            OnHeal?.Invoke();
        }
        
        public virtual void RestoreMP(int amount)
        {
            currentMP = Mathf.Clamp(currentMP + amount, 0, maxMP);
        }
        
        public virtual void UseMP(int amount)
        {
            currentMP = Mathf.Clamp(currentMP - amount, 0, maxMP);
        }
        
        public virtual void Die()
        {
            isDead = true;
            OnDeath?.Invoke();
            
            if (animator != null)
                animator.SetTrigger("Die");
        }
        
        public virtual void Revive(int healAmount = 0)
        {
            isDead = false;
            Heal(healAmount > 0 ? healAmount : maxHP / 4);
            
            if (animator != null)
                animator.SetTrigger("Revive");
        }
        
        public bool IsDead() => isDead;
        public bool IsAlive() => !isDead && currentHP > 0;
        
        public float GetHPPercentage() => (float)currentHP / maxHP;
        public float GetMPPercentage() => (float)currentMP / maxMP;
        
        public virtual BattleAction SelectAction(BattleUnit[] allies, BattleUnit[] enemies)
        {
            // IA básica para inimigos
            // Sobrescrever nas classes filhas
            return new BattleAction
            {
                user = this,
                target = enemies[Random.Range(0, enemies.Length)],
                skill = skills.Length > 0 ? skills[0] : null,
                isAttack = true
            };
        }
    }
}