using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;

public class GameDataManager : MonoBehaviour
{
    // ============================================
    // SINGLETON SIMPLIFICADO
    // ============================================
    private static GameDataManager _instance;
    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameDataManager>();
            }
            return _instance;
        }
    }
    
    // Dados atuais do jogo
    private GameData currentGameData;
    
    // Caminho para salvar os arquivos
    private string saveFolderPath;
    
    [Header("Configurações Gerais")]
    public string defaultPlayerName = "Player";
    public int defaultMaxHealth = 15;
    public int defaultMaxMana = 10;
    public int defaultMaxStamina = 20;
    public int startCurrency = 50;
    
    [Header("Auto Save Settings")]
    public bool enableAutoSaveOnEvents = true;
    public float autoSaveInterval = 300f; // 5 minutos
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showSaveLoadMessages = true;
    
    private float autoSaveTimer = 0f;

    
    
    void Awake()
    {
        // Bootstrap garante que só há uma instância
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (showDebugLogs) Debug.Log("[GDM] Initialized");
    }
    
    void Start()
    {
        InitializeSaveSystem();
    }
    
    void InitializeSaveSystem()
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, "saves");
        
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        
        ClearAutoSave();
        
        // Sempre inicializa currentGameData
        if (currentGameData == null)
        {
            currentGameData = new GameData();
            currentGameData.isNewGame = true;
            currentGameData.saveSlot = 1;
        }
        
        // Verifica slots manuais (1-5)
        bool hasSaveFiles = false;
        int lastValidSlot = 1;
        
        for (int i = 1; i <= 5; i++)
        {
            if (SaveFileExists(i))
            {
                hasSaveFiles = true;
                lastValidSlot = i;
            }
        }
        
        if (hasSaveFiles)
        {
            int lastSlot = PlayerPrefs.GetInt("LastSaveSlot", lastValidSlot);
            if (lastSlot == 0) lastSlot = lastValidSlot;
            
            LoadGame(lastSlot);
        }
        
        // Garantia final
        if (currentGameData == null)
        {
            currentGameData = new GameData();
            currentGameData.isNewGame = true;
            currentGameData.saveSlot = 1;
        }
    }
    
    public void CreateNewGame()
    {
        currentGameData = new GameData();
        
        PlayerData playerData = currentGameData.playerData;
        playerData.playerName = defaultPlayerName;
        playerData.maxHealth = defaultMaxHealth;
        playerData.currentHealth = defaultMaxHealth;
        playerData.lastPosition = new SerializableVector3(9999f, 9999f, 9999f);
        playerData.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        playerData.hasBoat = true;
        playerData.boatPosition = new SerializableVector3(9999f, 9999f, 9999f);
        playerData.boatHealth = 10;
        playerData.boatMaxHealth = 10;
        playerData.wasInsideBoat = false;
        
        currentGameData.inventoryData.currency = startCurrency;
        currentGameData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentGameData.isNewGame = true;
        currentGameData.version = Application.version;
        currentGameData.saveSlot = 1;
    }
    
    public void SaveGame(int slot = 1, bool isAutoSave = false)
    {   // ⭐⭐ DEBUG INICIAL
        Debug.Log($"╔══════════════════════════════════════╗");
        Debug.Log($"║ [GDM] SAVEGAME - DIAGNÓSTICO INICIAL");
        Debug.Log($"╠══════════════════════════════════════╣");
        Debug.Log($"║ Slot: {slot}, isAutoSave: {isAutoSave}");
        Debug.Log($"║ currentGameData.saveSlot ANTES: {currentGameData?.saveSlot}");
        Debug.Log($"║ currentGameData.currency ANTES: {currentGameData?.inventoryData?.currency}");
        Debug.Log($"╚══════════════════════════════════════╝");
        if (currentGameData == null) 
        {
            Debug.LogWarning("[GDM] Cannot save - currentGameData is null");
            return;
        }
        
        // ⭐⭐ NUNCA permite slot 0 para saves manuais
        if (!isAutoSave && slot == 0) 
        {
            Debug.LogError("[GDM] ❌❌❌ SLOT 0 NÃO PERMITIDO PARA SAVE MANUAL!");
            slot = 1;
        }
        
        Debug.Log($"[GDM] 💾 Preparando save para slot {slot} (isAutoSave: {isAutoSave})");
        
        // ⭐⭐ PASSO 1: ATUALIZAR dados de todos os sistemas ANTES do snapshot
        UpdateAllSystemsDataBeforeSave();
        
        // ⭐⭐ PASSO 2: Criar SNAPSHOT do estado atual (cópia via serialização)
        string originalJson = JsonUtility.ToJson(currentGameData);
        GameData snapshotData = JsonUtility.FromJson<GameData>(originalJson);
        
        //  ATUALIZAR O SNAPSHOT (não o currentGameData!)
        UpdateSnapshotWithCurrentSystemsData(snapshotData); 

        // ⭐⭐ PASSO 3: Configurar metadata do SNAPSHOT (não do original)
        snapshotData.saveSlot = slot;
        snapshotData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        snapshotData.isNewGame = false;
        snapshotData.version = Application.version;
        
        // ⭐⭐ PASSO 4: Garantir que listas estão inicializadas no snapshot
        EnsureDataStructuresInitialized(snapshotData);
        
        // ⭐⭐ PASSO 5: Salvar o SNAPSHOT (não o original)
        string jsonData = JsonUtility.ToJson(snapshotData, true);
        string filePath = GetSaveFilePath(slot);
        
        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"[GDM] ✅ Save criado no slot {slot}: {filePath}");

            // ⭐⭐ DEBUG FINAL 
            Debug.Log($"╔══════════════════════════════════════╗");
            Debug.Log($"║ [GDM] SAVEGAME - DIAGNÓSTICO FINAL");
            Debug.Log($"╠══════════════════════════════════════╣");
            Debug.Log($"║ currentGameData.saveSlot DEPOIS: {currentGameData?.saveSlot}");
            Debug.Log($"║ currentGameData.currency DEPOIS: {currentGameData?.inventoryData?.currency}");
            Debug.Log($"║ Arquivo salvo: {filePath}");
            Debug.Log($"║ Tamanho JSON: {jsonData.Length} chars");
            Debug.Log($"╚══════════════════════════════════════╝");
            
            // ⭐⭐ PASSO 6: Atualizar APENAS metadata no currentGameData
            if (!isAutoSave)
            {
                currentGameData.saveSlot = slot; // Slot ATUAL em memória
                currentGameData.saveDate = snapshotData.saveDate;
                PlayerPrefs.SetInt("LastSaveSlot", slot);
                PlayerPrefs.Save();
            }
            
            if (showSaveLoadMessages)
            {
                Debug.Log($"[GDM] {(isAutoSave ? "Auto-save" : "Game saved")} in slot {slot}");
            }
        }

        
        catch (Exception e)
        {
            Debug.LogError($"[GDM] ❌ Save error in slot {slot}: {e.Message}");
        }
    }

    /// <summary>
    /// ⭐ Atualiza dados de TODOS os sistemas antes de salvar
    /// </summary>
    private void UpdateAllSystemsDataBeforeSave()
    {
        // 1. Inventory System
        UpdateInventoryDataBeforeSave();
        
        // 2. Party System (FUTURO - já preparado!)
        // UpdatePartyDataBeforeSave();
        
        // 3. Player System (posição, saúde, etc.)
        UpdatePlayerDataBeforeSave();
        
        Debug.Log("[GDM] ⭐ Todos os sistemas atualizados para save");
    }

    /// <summary>
    /// ⭐ Garante que todas as estruturas de dados estão inicializadas
    /// </summary>
    private void EnsureDataStructuresInitialized(GameData data)
    {
        if (data == null) return;
        
        // WorldData
        if (data.worldData.defeatedEnemies == null)
            data.worldData.defeatedEnemies = new List<EnemyDefeatRecord>();
        if (data.worldData.collectedItems == null)
            data.worldData.collectedItems = new List<ItemCollectionRecord>();
        if (data.worldData.questProgress == null)
            data.worldData.questProgress = new List<QuestProgress>();
        
        // InventoryData  
        if (data.inventoryData.items == null)
            data.inventoryData.items = new List<InventoryItemData>();
        if (data.inventoryData.categoryStates == null)
            data.inventoryData.categoryStates = new SerializableDictionary<string, bool>();
        
        // PlayerData (Party - FUTURO)
        if (data.playerData.characterEquipment == null)
            data.playerData.characterEquipment = new CharacterEquipmentData();
        if (data.playerData.characterEquipment.partyMembers == null)
            data.playerData.characterEquipment.partyMembers = new List<PartyMemberData>();
    }

    /// <summary>
    /// ⭐ Atualiza dados do player (posição, saúde, etc.)
    /// </summary>
    private void UpdatePlayerDataBeforeSave()
    {
        // Implementação básica - será expandida
        // Por enquanto, apenas marca que não é novo jogo
        if (currentGameData != null)
        {
            currentGameData.isNewGame = false;
        }
    }
    
    public bool LoadGame(int slot = 1)
    {   Debug.Log($"╔══════════════════════════════════════╗");
        Debug.Log($"║ [GDM] LOADGAME - DIAGNÓSTICO");
        Debug.Log($"╠══════════════════════════════════════╣");
        Debug.Log($"║ Slot solicitado: {slot}");
        Debug.Log($"║ currentGameData.saveSlot ANTES: {currentGameData?.saveSlot}");
        Debug.Log($"║ currentGameData.currency ANTES: {currentGameData?.inventoryData?.currency}");
        string filePath = GetSaveFilePath(slot);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[GDM] ❌ Save file not found: {filePath}");
            return false;
        }
        
        try
        {
            string jsonData = File.ReadAllText(filePath);
            GameData loadedData = JsonUtility.FromJson<GameData>(jsonData);
            
            if (loadedData == null)
            {
                Debug.LogError("[GDM] ❌ Failed to deserialize save data");
                return false;
            }
            
            loadedData.saveSlot = slot;
            
            // ⭐⭐ SUBSTITUI COMPLETAMENTE o estado do jogo
            currentGameData = loadedData;
            
            if (slot != 0)
            {
                currentGameData.isNewGame = false;
                PlayerPrefs.SetInt("LastSaveSlot", slot);
                PlayerPrefs.Save();
            }
            
            // ⭐⭐ PASSO CRÍTICO: Notificar TODOS os sistemas sobre o novo estado
            NotifyAllSystemsAfterLoad();
            
            Debug.Log($"[GDM] ✅ Game loaded from slot {slot}");
            Debug.Log($"║ currentGameData.saveSlot DEPOIS: {currentGameData.saveSlot}");
            Debug.Log($"║ currentGameData.currency DEPOIS: {currentGameData.inventoryData.currency}");
            Debug.Log($"║ JSON carregado: {jsonData.Length} chars");
            Debug.Log($"╚══════════════════════════════════════╝");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GDM] ❌ Load error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// ⭐ Notifica todos os sistemas que os dados foram carregados
    /// </summary>
    private void NotifyAllSystemsAfterLoad()
    {
        // 1. Inventory System
        if (InventoryManager.Instance != null)
        {
            Debug.Log("[GDM] 🔄 Notifying InventoryManager...");
            InventoryManager.Instance.LoadInventoryFromGameData();
        }
        
        // 2. Party System (FUTURO)
        // if (PartyManager.Instance != null)
        // {
        //     Debug.Log("[GDM] 🔄 Notifying PartyManager...");
        //     PartyManager.Instance.LoadPartyFromGameData();
        // }
        
        // 3. Outros sistemas futuros...
        
        Debug.Log("[GDM] ✅ Todos os sistemas notificados sobre load");
    }
    
    // ============================================
    // MÉTODOS DE ATUALIZAÇÃO
    // ============================================
    
    public void UpdatePlayerPosition(Vector3 position)
    {
        if (currentGameData == null) CreateNewGame();
        currentGameData.playerData.lastPosition = position.ToSerializable();
    }
    
    public void UpdatePlayerHealth(int currentHealth, int maxHealth)
    {
        if (currentGameData == null) CreateNewGame();
        
        currentGameData.playerData.currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentGameData.playerData.maxHealth = maxHealth;
    }
    
    public void UpdatePlayerHealth(int currentHealth)
    {
        if (currentGameData == null || currentGameData.playerData == null) return;
        
        int newHealth = Mathf.Clamp(currentHealth, 0, currentGameData.playerData.maxHealth);
        currentGameData.playerData.currentHealth = newHealth;
    }
    
    public void HealPlayer(int amount)
    {
        if (currentGameData == null || currentGameData.playerData == null) return;
        
        int newHealth = Mathf.Clamp(
            currentGameData.playerData.currentHealth + amount, 
            0, 
            currentGameData.playerData.maxHealth
        );
        
        int healedAmount = newHealth - currentGameData.playerData.currentHealth;
        
        if (healedAmount > 0)
        {
            currentGameData.playerData.currentHealth = newHealth;
            
            if (enableAutoSaveOnEvents)
                SaveGame(currentGameData.saveSlot);
        }
    }
    
    public void DamagePlayer(int amount)
    {
        if (currentGameData == null || currentGameData.playerData == null) return;
        
        int newHealth = Mathf.Clamp(
            currentGameData.playerData.currentHealth - amount, 
            0, 
            currentGameData.playerData.maxHealth
        );
        
        int damageAmount = currentGameData.playerData.currentHealth - newHealth;
        
        if (damageAmount > 0)
        {
            currentGameData.playerData.currentHealth = newHealth;
            
            if (enableAutoSaveOnEvents)
                SaveGame(currentGameData.saveSlot);
        }
    }

// ============================================
    // MÉTODOS DE BARCO
    // ============================================
    
    public class BoatData
    {
        public int currentHealth;
        public int maxHealth;
        public SerializableVector3 position;
        public bool destroyed;
        public bool hasBoat;
        public int upgradeLevel;
        public float durability;
        
        public BoatData()
        {
            currentHealth = 10;
            maxHealth = 10;
            position = Vector3.zero.ToSerializable();
            destroyed = false;
            hasBoat = false;
            upgradeLevel = 0;
            durability = 100f;
        }
    }
    
    public BoatData GetBoatData()
    {
        if (currentGameData == null) return new BoatData();
        
        return new BoatData
        {
            currentHealth = currentGameData.playerData.boatHealth,
            maxHealth = currentGameData.playerData.boatMaxHealth,
            position = currentGameData.playerData.boatPosition,
            destroyed = currentGameData.playerData.isBoatDestroyed,
            hasBoat = currentGameData.playerData.hasBoat,
            upgradeLevel = currentGameData.playerData.boatUpgradeLevel,
            durability = currentGameData.playerData.boatDurability
        };
    }
    
    public void UpdateBoatData(int currentHealth, int maxHealth, Vector3 position, bool destroyed, bool hasBoat)
    {
        if (currentGameData == null) return;
        
        currentGameData.playerData.boatHealth = currentHealth;
        currentGameData.playerData.boatMaxHealth = maxHealth;
        currentGameData.playerData.boatPosition = position.ToSerializable();
        currentGameData.playerData.isBoatDestroyed = destroyed;
        currentGameData.playerData.hasBoat = hasBoat;
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    // ============================================
    // SISTEMA DE INIMIGOS E ITENS
    // ============================================
    
    public void RecordEnemyDefeat(string enemyID, string enemyType, Vector3 position, string dropItems = "")
    {
        if (currentGameData == null) return;
        
        SerializableVector3 serializablePos = position.ToSerializable();
        
        EnemyDefeatRecord existingRecord = currentGameData.worldData.defeatedEnemies
            .Find(record => record.enemyID == enemyID && record.position.Approximately(serializablePos));
        
        if (existingRecord != null)
        {
            existingRecord.timesDefeated++;
            existingRecord.defeatDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrEmpty(dropItems))
                existingRecord.dropItems = dropItems;
        }
        else
        {
            EnemyDefeatRecord newRecord = new EnemyDefeatRecord
            {
                enemyID = enemyID,
                enemyType = enemyType,
                position = serializablePos,
                dropItems = dropItems
            };
            
            currentGameData.worldData.defeatedEnemies.Add(newRecord);
        }
        
        currentGameData.playerData.enemiesDefeated++;
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    public void AddItemToInventory(string itemID, string itemName, string itemType, Vector3 collectionPoint)
    {
        if (currentGameData == null) return;
        
        ItemCollectionRecord existingItem = currentGameData.worldData.collectedItems
            .Find(item => item.itemID == itemID);
        
        if (existingItem != null)
        {
            existingItem.quantity++;
        }
        else
        {
            ItemCollectionRecord newItem = new ItemCollectionRecord
            {
                itemID = itemID,
                itemName = itemName,
                itemType = itemType,
                collectionPoint = collectionPoint.ToSerializable()
            };
            
            currentGameData.worldData.collectedItems.Add(newItem);
        }
        
        currentGameData.playerData.itemsCollected++;
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    public void AddCurrency(int amount)
    {
        if (currentGameData == null) return;
        
        currentGameData.inventoryData.currency += amount;
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    public bool SpendCurrency(int amount)
    {
        if (currentGameData == null || currentGameData.inventoryData.currency < amount)
            return false;
        
        currentGameData.inventoryData.currency -= amount;
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
        
        return true;
    }
    
    public void AddExperience(int amount)
    {
        if (currentGameData == null) return;
        
        currentGameData.playerData.experience += amount;
        
        while (currentGameData.playerData.experience >= currentGameData.playerData.experienceToNextLevel)
        {
            currentGameData.playerData.level++;
            currentGameData.playerData.experience -= currentGameData.playerData.experienceToNextLevel;
            currentGameData.playerData.experienceToNextLevel = Mathf.RoundToInt(currentGameData.playerData.experienceToNextLevel * 1.5f);
            currentGameData.playerData.skillPoints++;
            currentGameData.playerData.maxHealth += 5;
            currentGameData.playerData.currentHealth = currentGameData.playerData.maxHealth;
        }
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    // ============================================
    // RECURSOS DO BARCO
    // ============================================
    
    public bool UseBoatResource(string resourceType, int amount)
    {
        if (currentGameData == null) return false;
        
        bool success = false;
        
        switch (resourceType.ToLower())
        {
            case "repairkit":
            case "repair_kit":
                if (currentGameData.inventoryData.boatRepairKits >= amount)
                {
                    currentGameData.inventoryData.boatRepairKits -= amount;
                    success = true;
                }
                break;
                
            case "wood":
                if (currentGameData.inventoryData.wood >= amount)
                {
                    currentGameData.inventoryData.wood -= amount;
                    success = true;
                }
                break;
                
            case "iron":
                if (currentGameData.inventoryData.iron >= amount)
                {
                    currentGameData.inventoryData.iron -= amount;
                    success = true;
                }
                break;
                
            case "currency":
            case "coins":
            case "money":
                if (currentGameData.inventoryData.currency >= amount)
                {
                    currentGameData.inventoryData.currency -= amount;
                    success = true;
                }
                break;
        }
        
        if (success && enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
        
        return success;
    }
    
    public void AddBoatResource(string resourceType, int amount)
    {
        if (currentGameData == null) return;
        
        switch (resourceType.ToLower())
        {
            case "repairkit":
            case "repair_kit":
                currentGameData.inventoryData.boatRepairKits += amount;
                break;
                
            case "wood":
                currentGameData.inventoryData.wood += amount;
                break;
                
            case "iron":
                currentGameData.inventoryData.iron += amount;
                break;
                
            case "currency":
            case "coins":
            case "money":
                currentGameData.inventoryData.currency += amount;
                break;
        }
        
        if (enableAutoSaveOnEvents)
            SaveGame(currentGameData.saveSlot);
    }
    
    public bool HasBoatResources(string resourceType, int amount)
    {
        if (currentGameData == null) return false;
        
        switch (resourceType.ToLower())
        {
            case "repairkit":
            case "repair_kit":
                return currentGameData.inventoryData.boatRepairKits >= amount;
                
            case "wood":
                return currentGameData.inventoryData.wood >= amount;
                
            case "iron":
                return currentGameData.inventoryData.iron >= amount;
                
            case "currency":
            case "coins":
            case "money":
                return currentGameData.inventoryData.currency >= amount;
                
            default:
                return false;
        }
    }
    
    // ============================================
    // GETTERS E UTILIDADES
    // ============================================
    
    public GameData GetCurrentGameData() => currentGameData;
    public PlayerData GetPlayerData() => currentGameData?.playerData;
    public WorldData GetWorldData() => currentGameData?.worldData;
    public InventoryData GetInventoryData() => currentGameData?.inventoryData;
    
    public bool SaveFileExists(int slot = 1)
    {
        return File.Exists(GetSaveFilePath(slot));
    }
    
    public int GetLastManualSaveSlot()
    {
        int slot = PlayerPrefs.GetInt("LastSaveSlot", 1);
        return slot == 0 ? 1 : slot;
    }
    
    private string GetSaveFilePath(int slot)
    {
        if (string.IsNullOrEmpty(saveFolderPath))
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, "saves");
    }
    
    return Path.Combine(saveFolderPath, $"save_{slot}.json");
    }
    
    public void ClearAutoSave()
    {
        if (SaveFileExists(0))
        {
            DeleteSave(0);
        }
    }
    
    public bool DeleteSave(int slot = 1)
    {
        string filePath = GetSaveFilePath(slot);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        
        return false;
    }
    
    // ============================================
    // UPDATE
    // ============================================
    
    void Update()
    {
        if (currentGameData != null && !currentGameData.isNewGame)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveGame(currentGameData.saveSlot);
                autoSaveTimer = 0f;
            }
            
            currentGameData.playerData.playTime += Time.deltaTime;
        }
    }

    private void UpdateInventoryDataBeforeSave()
    {
        if (currentGameData == null || currentGameData.inventoryData == null)
        {
            Debug.LogWarning("[GDM] Cannot update inventory data - GameData or InventoryData is null");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[GDM] InventoryManager.Instance is null - skipping inventory save");
            return;
        }
        
        try
        {
            Debug.Log("[GDM] ⭐ Verificando estado do inventário (NÃO MODIFICA currentGameData!)");
            
            var invManager = InventoryManager.Instance;
            
            // ⭐⭐ APENAS LOG - NÃO MODIFICA!
            Debug.Log($"[GDM]   • Currency: {invManager.Currency}");
            Debug.Log($"[GDM]   • Slots usados: {invManager.GetUsedSlotCount()}/{invManager.InventorySize}");
            Debug.Log($"[GDM]   • Peso: {invManager.CurrentWeight:F1}/{invManager.MaxWeight:F1} kg");
            
            // ⭐⭐ NÃO FAÇA NADA MAIS! O SaveGame() cuida do snapshot!
            // NÃO modifique inventoryData.currency
            // NÃO modifique inventoryData.items
            // NÃO chame SaveSharedEquipmentDirectly()
            
            Debug.Log("[GDM] ✅ Verificação concluída (dados NÃO modificados)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GDM] ❌ Erro verificando inventário: {e.Message}");
        }
    }

    /// <summary>
    /// ⭐ Salva equipamentos compartilhados diretamente (sem loop)
    /// </summary>
    private void SaveSharedEquipmentDirectly(InventoryData inventoryData)
    {
        if (inventoryData == null || InventoryManager.Instance == null) return;
        
        // Limpa equipamentos antigos
        inventoryData.sharedEquipmentLoadout = new EquipmentLoadoutData();
        
        var equipment = InventoryManager.Instance.Equipment;
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var equippedItem = equipment.GetItemInSlot(slot);
            if (equippedItem != null)
            {
                inventoryData.sharedEquipmentLoadout.SetItemIDForSlot(slot, equippedItem.itemID);
            }
        }
        
        Debug.Log("[GDM] ✅ Equipamentos compartilhados sincronizados");
    }

    /// <summary>
    /// ⭐⭐ ATUALIZA SNAPSHOT com dados ATUAIS dos sistemas
    /// </summary>
    private void UpdateSnapshotWithCurrentSystemsData(GameData snapshot)
    {
        if (snapshot == null) return;
        
        Debug.Log("[GDM] 🔄 Atualizando snapshot com dados atuais dos sistemas...");
        
        // 1. INVENTORY SYSTEM
        if (InventoryManager.Instance != null && snapshot.inventoryData != null)
        {
            var invManager = InventoryManager.Instance;
            var invSnapshot = snapshot.inventoryData;
            
            // Atualiza APENAS o snapshot
            invSnapshot.currency = invManager.Currency;
            invSnapshot.currentWeight = invManager.CurrentWeight;
            invSnapshot.maxWeight = invManager.MaxWeight;
            invSnapshot.inventorySize = invManager.InventorySize;
            
            // Limpa itens antigos do snapshot
            invSnapshot.items.Clear();
            
            // Adiciona itens ATUAIS ao snapshot
            var allSlots = invManager.GetAllSlots();
            foreach (var slot in allSlots)
            {
                if (!slot.IsEmpty && slot.item != null)
                {
                    var itemData = new InventoryItemData(slot.item, slot.quantity)
                    {
                        slotIndex = slot.slotIndex,
                        isEquipped = slot.isEquipped
                    };
                    invSnapshot.items.Add(itemData);
                }
            }
            
            // Equipamentos compartilhados no snapshot
            SaveSharedEquipmentToSnapshot(invSnapshot);
            
            Debug.Log($"[GDM]   • Snapshot inventory: {invSnapshot.currency} moedas, {invSnapshot.items.Count} itens");
        }
        
        // 2. PARTY SYSTEM (FUTURO) - placeholder
        // UpdatePartyDataToSnapshot(snapshot);
        
        Debug.Log("[GDM] ✅ Snapshot atualizado com dados atuais");
    }

    /// <summary>
    /// Salva equipamentos compartilhados NO SNAPSHOT (não no currentGameData)
    /// </summary>
    private void SaveSharedEquipmentToSnapshot(InventoryData inventorySnapshot)
    {
        if (inventorySnapshot == null || InventoryManager.Instance == null) return;
        
        // Limpa equipamentos antigos do snapshot
        inventorySnapshot.sharedEquipmentLoadout = new EquipmentLoadoutData();
        
        var equipment = InventoryManager.Instance.Equipment;
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var equippedItem = equipment.GetItemInSlot(slot);
            if (equippedItem != null)
            {
                inventorySnapshot.sharedEquipmentLoadout.SetItemIDForSlot(slot, equippedItem.itemID);
            }
        }
        
        Debug.Log("[GDM]   • Equipamentos salvos no snapshot");
    }

    // ============================================
    // VERIFICAÇÃO DE INIMIGOS DERROTADOS
    // ============================================
    
    public bool WasEnemyDefeatedAtPosition(string enemyID, Vector3 position)
    {
        if (currentGameData == null) return false;
        
        SerializableVector3 serializablePos = position.ToSerializable();
        return currentGameData.worldData.defeatedEnemies
            .Exists(record => record.enemyID == enemyID && 
                             record.position.Approximately(serializablePos));
    }

    public int GetEnemyDefeatCount(string enemyID)
    {
        if (currentGameData == null) return 0;
        
        int count = 0;
        foreach (var record in currentGameData.worldData.defeatedEnemies)
        {
            if (record.enemyID == enemyID)
                count += record.timesDefeated;
        }
        return count;
    }

    public bool HasItem(string itemID)
    {
        if (currentGameData == null) return false;
        
        return currentGameData.worldData.collectedItems
            .Exists(item => item.itemID == itemID && item.quantity > 0);
    }

    public int GetItemQuantity(string itemID)
    {
        if (currentGameData == null) return 0;
        
        ItemCollectionRecord item = currentGameData.worldData.collectedItems
            .Find(i => i.itemID == itemID);
        
        return item?.quantity ?? 0;
    }
    
    // ============================================
    // FORCE REFRESH (Simplificado)
    // ============================================
    
    public void ForceRefreshFromFile(int slot = 1)
    {
        string filePath = GetSaveFilePath(slot);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[GDM] File does not exist: {filePath}");
            return;
        }
        
        try
        {
            string jsonData = File.ReadAllText(filePath);
            GameData freshData = JsonUtility.FromJson<GameData>(jsonData);
            
            if (freshData == null)
            {
                Debug.LogError("[GDM] Failed to deserialize");
                return;
            }
            
            freshData.saveSlot = slot;
            currentGameData = freshData;
            currentGameData.isNewGame = false;
            
            if (showDebugLogs)
                Debug.Log($"[GDM] Force refreshed from slot {slot}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GDM] Force refresh error: {e.Message}");
        }
    }
    
    // ============================================
    // DEBUG METHODS
    // ============================================
    
    [ContextMenu("Debug: Print Player Data")]
    public void DebugCheckPlayerData()
    {
        if (currentGameData == null)
        {
            Debug.Log("[GDM] currentGameData is NULL");
            return;
        }
        
        if (currentGameData.playerData == null)
        {
            Debug.Log("[GDM] playerData is NULL");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== PLAYER DATA DEBUG ===");
        sb.AppendLine($"Name: {currentGameData.playerData.playerName}");
        sb.AppendLine($"Health: {currentGameData.playerData.currentHealth}/{currentGameData.playerData.maxHealth}");
        sb.AppendLine($"Position: {currentGameData.playerData.lastPosition}");
        sb.AppendLine($"isNewGame: {currentGameData.isNewGame}");
        sb.AppendLine($"Level: {currentGameData.playerData.level}");
        sb.AppendLine($"XP: {currentGameData.playerData.experience}/{currentGameData.playerData.experienceToNextLevel}");
        sb.AppendLine($"Enemies Defeated: {currentGameData.playerData.enemiesDefeated}");
        sb.AppendLine($"Items Collected: {currentGameData.playerData.itemsCollected}");
        sb.AppendLine("=========================");
        
        Debug.Log(sb.ToString());
    }
    
    [ContextMenu("Debug: Print Save Summary")]
    public void PrintSaveSummary()
    {
        if (currentGameData == null)
        {
            Debug.Log("[GDM] No game data loaded");
            return;
        }
        
        StringBuilder summary = new StringBuilder();
        summary.AppendLine("=== SAVE SUMMARY ===");
        summary.AppendLine($"Player: {currentGameData.playerData.playerName}");
        summary.AppendLine($"Level: {currentGameData.playerData.level}");
        summary.AppendLine($"Health: {currentGameData.playerData.currentHealth}/{currentGameData.playerData.maxHealth}");
        summary.AppendLine($"Position: {currentGameData.playerData.lastPosition}");
        summary.AppendLine($"isNewGame: {currentGameData.isNewGame}");
        summary.AppendLine($"saveSlot: {currentGameData.saveSlot}");
        summary.AppendLine($"Instance: {(Instance != null ? "SET ✅" : "NULL ❌")}");
        summary.AppendLine("=====================");
        
        Debug.Log(summary.ToString());
    }
    
    [ContextMenu("Debug: Check All Slots")]
    public void DebugCheckAllSlots()
    {
        Debug.Log("=== SLOT VERIFICATION ===");
        
        for (int i = 0; i <= 5; i++)
        {
            string filePath = GetSaveFilePath(i);
            bool exists = File.Exists(filePath);
            string type = i == 0 ? "AUTO" : "MANUAL";
            Debug.Log($"Slot {i} ({type}): {(exists ? "✅" : "❌")} - {filePath}");
        }
        
        Debug.Log($"Last manual slot: {GetLastManualSaveSlot()}");
        Debug.Log("============================");
    }
    
    [ContextMenu("Debug: Check Data Corruption")]
    public void DebugCheckDataCorruption()
    {
        if (currentGameData == null)
        {
            Debug.Log("[GDM] currentGameData is NULL");
            return;
        }
        
        Debug.Log("=== DATA CORRUPTION CHECK ===");
        Debug.Log($"currentGameData reference: {currentGameData.GetHashCode()}");
        Debug.Log($"Slot in memory: {currentGameData.saveSlot}");
        Debug.Log($"Position in memory: {currentGameData.playerData.lastPosition}");
        Debug.Log($"isNewGame: {currentGameData.isNewGame}");
        
        // Check all slots on disk
        for (int i = 1; i <= 5; i++)
        {
            string filePath = GetSaveFilePath(i);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    GameData fileData = JsonUtility.FromJson<GameData>(json);
                    Debug.Log($"--- Slot {i} on DISK ---");
                    Debug.Log($"  saveSlot: {fileData.saveSlot}");
                    Debug.Log($"  position: {fileData.playerData.lastPosition}");
                    Debug.Log($"  isNewGame: {fileData.isNewGame}");
                }
                catch { }
            }
        }
        Debug.Log("========================================");
    }
    
    [ContextMenu("Clean Corrupted Slots")]
    public void CleanCorruptedSlots()
    {
        Debug.Log("[GDM] Starting cleanup of corrupted slots...");
        
        int cleanedCount = 0;
        
        for (int slot = 1; slot <= 5; slot++)
        {
            string filePath = GetSaveFilePath(slot);
            
            if (!File.Exists(filePath)) continue;
            
            try
            {
                string json = File.ReadAllText(filePath);
                GameData data = JsonUtility.FromJson<GameData>(json);
                
                if (data == null)
                {
                    Debug.LogWarning($"Slot {slot}: NULL - DELETING");
                    File.Delete(filePath);
                    cleanedCount++;
                    continue;
                }
                
                // Check if player was inside boat
                if (data.playerData.wasInsideBoat)
                {
                    Vector3 playerPos = data.playerData.lastPosition.ToVector3();
                    Vector3 boatPos = data.playerData.boatPosition.ToVector3();
                    float distance = Vector3.Distance(playerPos, boatPos);
                    
                    if (distance > 0.1f)
                    {
                        Debug.LogError($"Slot {slot}: CORRUPTED (dist: {distance:F2}u) - DELETING");
                        File.Delete(filePath);
                        cleanedCount++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Slot {slot}: ERROR - DELETING ({e.Message})");
                File.Delete(filePath);
                cleanedCount++;
            }
        }
        
        Debug.Log($"[GDM] Cleanup complete: {cleanedCount} slot(s) deleted");
        
        if (cleanedCount > 0)
        {
            Debug.Log("✅ Run the game again to create clean saves");
        }
    }

    [ContextMenu("🔍 Debug: Test Inventory Save Integration")]
    public void DebugTestInventorySave()
    {
        Debug.Log("=== 🔍 TESTE INTEGRAÇÃO INVENTÁRIO ===");
        
        if (currentGameData == null)
        {
            Debug.LogError("currentGameData is null!");
            return;
        }
        
        var inventoryData = currentGameData.inventoryData;
        Debug.Log($"Items in GameData: {inventoryData.items.Count}");
        Debug.Log($"Currency in GameData: {inventoryData.currency}");
        
        // Testar atualização
        UpdateInventoryDataBeforeSave();
        
        Debug.Log($"After update - Items: {inventoryData.items.Count}");
        Debug.Log($"After update - Currency: {inventoryData.currency}");
        
        Debug.Log("=== FIM TESTE ===");
    }

    [ContextMenu("🔍 Debug: Safe Check")]
    public void DebugSafeCheck()
    {
        Debug.Log("=== 🔍 VERIFICAÇÃO SEGURA ===");
        Debug.Log("GameDataManager está vivo!");
        Debug.Log("=== FIM ===");
    }

    [ContextMenu("🆕 Debug: Force New Game")]
    public void DebugForceNewGame()
    {
        Debug.Log("=== 🆕 FORÇANDO NOVO JOGO ===");
        
        // 1. Deleta todos os saves
        for (int i = 0; i <= 5; i++)
        {
            DeleteSave(i); // Usa o método já existente
        }
        
        Debug.Log("✅ Todos os saves deletados do disco");
        
        // 2. Limpa PlayerPrefs
        PlayerPrefs.DeleteKey("LastSaveSlot");
        PlayerPrefs.Save();
        Debug.Log("✅ PlayerPrefs cleared");
        
        // 3. Cria novo GameData em MEMÓRIA apenas
        currentGameData = new GameData();
        currentGameData.isNewGame = true;
        currentGameData.saveSlot = 1;
        
        // 4. NÃO salva no disco ainda - será feito quando iniciar
        Debug.Log("✅ New GameData created in memory (NOT saved to disk)");
        Debug.Log("⚠️ IMPORTANT: Click Play to start fresh new game");
        Debug.Log("=== FIM ===");
    }

    [ContextMenu("🔍 Debug: Compare All Save Files")]
    public void DebugCompareAllSaveFiles()
    {
        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║ 🔍 COMPARAÇÃO DE TODOS OS SAVE FILES");
        Debug.Log("╠══════════════════════════════════════════════════════╣");
        
        for (int slot = 1; slot <= 5; slot++)
        {
            string filePath = GetSaveFilePath(slot);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    GameData data = JsonUtility.FromJson<GameData>(json);
                    
                    Debug.Log($"║ SLOT {slot}:");
                    Debug.Log($"║   • Currency: {data.inventoryData.currency}");
                    Debug.Log($"║   • Items: {data.inventoryData.items.Count}");
                    Debug.Log($"║   • SaveSlot in file: {data.saveSlot}");
                    Debug.Log($"║   • isNewGame: {data.isNewGame}");
                    Debug.Log($"║   ─────────────────────────");
                }
                catch (Exception e)
                {
                    Debug.Log($"║ SLOT {slot}: ❌ ERROR - {e.Message}");
                }
            }
            else
            {
                Debug.Log($"║ SLOT {slot}: ❌ FILE NOT EXISTS");
            }
        }
        
        Debug.Log("║ currentGameData in memory:");
        Debug.Log($"║   • Currency: {currentGameData?.inventoryData?.currency}");
        Debug.Log($"║   • SaveSlot: {currentGameData?.saveSlot}");
        Debug.Log($"╚══════════════════════════════════════════════════════╝");
    }
    
    
}