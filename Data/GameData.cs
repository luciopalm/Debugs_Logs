using System;
using UnityEngine;
using System.Collections.Generic;

// ============================================
// ARQUIVO PRINCIPAL DE DADOS DO JOGO
// ============================================

// Classe principal que contém TODOS os dados do jogo
[System.Serializable]
public class GameData
{
    // Dados do jogador
    public PlayerData playerData = new PlayerData();
    
    // Dados do mundo
    public WorldData worldData = new WorldData();
    
    // Dados do inventário
    public InventoryData inventoryData = new InventoryData();
    
    // Metadata do save
    public string saveDate;
    public int saveSlot = 1;
    public string version = "1.0";
    public bool isNewGame = true;
}

// ============================================
// DADOS DO JOGADOR
// ============================================

[System.Serializable]
public class PlayerData
{
    // Identificação
    public string playerName = "Player";
    
    // Progresso de nível
    public int level = 1;
    public int experience = 0;
    public int experienceToNextLevel = 100;
    public int skillPoints = 0;
    
    // Sistema de vida
    public int maxHealth = 15;
    public int currentHealth = 15;
    
    // Sistema de recursos
    public int maxMana = 10;
    public int currentMana = 10;
    public int maxStamina = 20;
    public int currentStamina = 20;
    
    // Posição e progresso no mundo
    public SerializableVector3 lastPosition = Vector3.zero.ToSerializable();
    public string currentScene = "MainScene";
    
    // ===== SISTEMA DE BARCO =====
    public bool hasBoat = false;
    public int boatHealth = 0;
    public int boatMaxHealth = 10;
    public int boatUpgradeLevel = 0;
    public SerializableVector3 boatPosition = Vector3.zero.ToSerializable();
    public bool isBoatDestroyed = false;
    public float boatDurability = 100f;
    public bool wasInsideBoat = false;
    // =============================
    
    // Estatísticas básicas
    public float playTime = 0f;
    public int deaths = 0;
    public int enemiesDefeated = 0;
    public int itemsCollected = 0;
    public int bossesDefeated = 0;
    public int navalVictories = 0;
    
    // Habilidades desbloqueadas
    public bool hasSwordAttack = true;
    public bool hasNavalAttack = false;
    public bool hasSpecialAttack1 = false;
    public bool hasSpecialAttack2 = false;
    
    // Configurações do jogador
    public float gameVolume = 0.8f;
    public float musicVolume = 0.6f;
    public float sfxVolume = 0.7f;
}

// ============================================
// DADOS DO MUNDO
// ============================================

[System.Serializable]
public class WorldData
{
    // Progresso básico da história
    public bool tutorialCompleted = false;
    public bool firstBoatObtained = false;
    public bool firstNavalCombatCompleted = false;
    public bool firstBossDefeated = false;
    public bool mainStoryCompleted = false;
    
    // Listas expandidas
    public List<EnemyDefeatRecord> defeatedEnemies = new List<EnemyDefeatRecord>();
    public List<ItemCollectionRecord> collectedItems = new List<ItemCollectionRecord>();
    public List<QuestProgress> questProgress = new List<QuestProgress>();
    public List<AreaUnlock> areaUnlocks = new List<AreaUnlock>();
    
    // Pontos de interesse descobertos
    public List<string> discoveredLandingZones = new List<string>();
    public List<string> discoveredIslands = new List<string>();
    public List<string> discoveredSecrets = new List<string>();
    
    // Sistema de spawn
    public List<Vector3> clearedEnemyAreas = new List<Vector3>();
    
    // NPCs encontrados
    public List<string> npcsMet = new List<string>();
    
    // Diálogos já vistos
    public List<string> seenDialogues = new List<string>();
    
    // Eventos especiais ativados
    public List<string> activatedEvents = new List<string>();
}

// ============================================
// DADOS DE INVENTÁRIO
// ============================================

[System.Serializable]
public class InventoryData
{
    // Sistema simples por enquanto
    public int currency = 0;
    public int healthPotions = 0;
    public int manaPotions = 0;
    public int arrows = 0;
    public int cannonballs = 0;
    public int wood = 0;
    public int iron = 0;
    public int gold = 0;
    public int gems = 0;
    
    // Itens especiais
    public bool hasCompass = false;
    public bool hasTreasureMap = false;
    public bool hasShipKey = false;
    public bool hasAncientArtifact = false;
    
    // Equipamentos atuais
    public string currentWeapon = "wooden_sword";
    public string currentArmor = "leather_armor";
    public string currentShield = "none";
    public string currentAccessory = "none";
    
    // ===== RECURSOS PARA BARCO =====
    public int boatRepairKits = 0;
    public int sailCloth = 0;
    public int navalCannons = 0;
    public int anchorCount = 0;
    public int hullPlanks = 0;
    public int ropeLength = 0;
    // ================================
}

// ============================================
// REGISTROS ESPECÍFICOS
// ============================================

[System.Serializable]
public class EnemyDefeatRecord
{
    public string enemyID;
    public string enemyType;
    public SerializableVector3 position;
    public int timesDefeated = 1;
    public string defeatDate;
    public string dropItems;
    
    public EnemyDefeatRecord()
    {
        defeatDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class ItemCollectionRecord
{
    public string itemID;
    public string itemName;
    public string itemType;
    public int quantity = 1;
    public SerializableVector3 collectionPoint;
    public bool isEquipped = false;
    public string collectionDate;
    
    public ItemCollectionRecord()
    {
        collectionDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class QuestProgress
{
    public string questID;
    public string questName;
    public string questDescription;
    public bool isActive = false;
    public bool isCompleted = false;
    public int currentStep = 0;
    public int totalSteps = 1;
    public List<string> completedObjectives = new List<string>();
    public string startDate;
    public string completionDate;
    
    public QuestProgress()
    {
        startDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class AreaUnlock
{
    public string areaID;
    public string areaName;
    public string areaDescription;
    public bool isUnlocked = false;
    public bool isDiscovered = false;
    public string discoveryDate;
    public List<string> secretsFound = new List<string>();
    
    public AreaUnlock()
    {
        discoveryDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// ============================================
// HELPER CLASSES
// ============================================

[System.Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    
    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    
    public override string ToString()
    {
        return $"({x:F1}, {y:F1}, {z:F1})";
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
    
    public bool Approximately(SerializableVector3 other, float tolerance = 0.1f)
    {
        return Mathf.Abs(x - other.x) < tolerance &&
               Mathf.Abs(y - other.y) < tolerance &&
               Mathf.Abs(z - other.z) < tolerance;
    }
}

public static class Vector3Extensions
{
    public static SerializableVector3 ToSerializable(this Vector3 vector)
    {
        return new SerializableVector3(vector.x, vector.y, vector.z);
    }
    
    public static Vector3 ToVector3(this SerializableVector3 sVector)
    {
        return new Vector3(sVector.x, sVector.y, sVector.z);
    }
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> list = new List<T>();
    
    public void Add(T item)
    {
        list.Add(item);
    }
    
    public bool Contains(T item)
    {
        return list.Contains(item);
    }
    
    public int Count
    {
        get { return list.Count; }
    }
    
    public T this[int index]
    {
        get { return list[index]; }
        set { list[index] = value; }
    }
}

// ============================================
// CLASSES PARA EVENTOS E CONQUISTAS
// ============================================

[System.Serializable]
public class GameEvent
{
    public string eventID;
    public string eventName;
    public bool isTriggered = false;
    public string triggerDate;
    public int triggerCount = 0;
    
    public GameEvent()
    {
        triggerDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class Achievement
{
    public string achievementID;
    public string achievementName;
    public string description;
    public bool isUnlocked = false;
    public string unlockDate;
    public int progress = 0;
    public int requirement = 1;
    
    public Achievement()
    {
        unlockDate = "";
    }
}

// ============================================
// CLASSE PARA ESTATÍSTICAS DETALHADAS
// ============================================

[System.Serializable]
public class GameStatistics
{
    // Combate
    public int totalDamageDealt = 0;
    public int totalDamageTaken = 0;
    public int totalHealingReceived = 0;
    public int criticalHits = 0;
    public int perfectDodges = 0;
    
    // Navegação
    public float distanceTraveled = 0f;
    public float distanceSailed = 0f;
    public int islandsVisited = 0;
    public int portsVisited = 0;
    
    // Coleta
    public int resourcesGathered = 0;
    public int treasuresFound = 0;
    public int secretsDiscovered = 0;
    
    // Tempo
    public float fastestBossKill = 0f;
    public float longestSurvival = 0f;
    public float totalGameTime = 0f;
    
    // Misc
    public int npcsTalkedTo = 0;
    public int questsCompleted = 0;
    public int shipsDestroyed = 0;
}