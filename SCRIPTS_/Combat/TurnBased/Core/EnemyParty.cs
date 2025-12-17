using UnityEngine;
using System.Collections.Generic;

namespace Combat.TurnBased
{
    public class EnemyParty : MonoBehaviour
    {
        [Header("Enemies")]
        public List<BattleUnit> enemies = new List<BattleUnit>();
        
        [Header("Encounter Settings")]
        public bool isRandomEncounter = false;
        public int minEnemies = 1;
        public int maxEnemies = 3;
        
        public void InitializeParty()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    enemy.Initialize();
            }
        }
        
        public BattleUnit[] GetAliveUnits()
        {
            List<BattleUnit> alive = new List<BattleUnit>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive())
                    alive.Add(enemy);
            }
            return alive.ToArray();
        }
        
        public bool AreAllDead()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive())
                    return false;
            }
            return true;
        }
        
        public int GetAliveCount()
        {
            int count = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive())
                    count++;
            }
            return count;
        }
        
        public void AddEnemy(BattleUnit enemy)
        {
            if (!enemies.Contains(enemy))
                enemies.Add(enemy);
        }
        
        public void RemoveEnemy(BattleUnit enemy)
        {
            enemies.Remove(enemy);
        }
        
        public void GenerateRandomEncounter()
        {
            if (!isRandomEncounter) return;
            
            enemies.Clear();
            int enemyCount = Random.Range(minEnemies, maxEnemies + 1);
            
            // TODO: Instanciar inimigos baseados em EnemyData ScriptableObjects
            Debug.Log($"Gerando encontro com {enemyCount} inimigos");
        }
        
        public float GetTotalEXP()
        {
            float total = 0;
            foreach (var enemy in enemies)
            {
                // TODO: Adicionar XP baseado no nível do inimigo
                if (enemy != null)
                    total += enemy.level * 10;
            }
            return total;
        }
    }
}