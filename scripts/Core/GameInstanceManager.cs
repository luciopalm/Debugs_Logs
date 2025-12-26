using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class GameInstanceManager : MonoBehaviour
{
    public static GameInstanceManager Instance { get; private set; }
    
    [Header("Current Game Instance")]
    public int currentGameInstanceID = -1;
    public string currentGameInstanceName = "";
    public string currentGameInstancePath = "";
    
    [Header("Available Game Instances")]
    public List<GameInstanceInfo> gameInstances = new List<GameInstanceInfo>();
    
    [Header("Settings")]
    public int maxGameInstances = 10;
    public string defaultGameName = "My Adventure";
    
    [Serializable]
    public class GameInstanceInfo
    {
        public int instanceID;
        public string instanceName;
        public string difficulty; // Easy, Normal, Hard
        public string creationDateString; // Serializado como string
        public string lastPlayedDateString; // Serializado como string
        public float playTimeHours;
        public string saveFolderPath;
        public int lastSaveSlot = 1; // Último slot salvo (1-3)
        
        // Propriedades para facilitar uso
        public DateTime CreationDate
        {
            get 
            { 
                if (string.IsNullOrEmpty(creationDateString))
                    return DateTime.Now;
                return DateTime.Parse(creationDateString); 
            }
            set { creationDateString = value.ToString("o"); }
        }
        
        public DateTime LastPlayedDate
        {
            get 
            { 
                if (string.IsNullOrEmpty(lastPlayedDateString))
                    return DateTime.Now;
                return DateTime.Parse(lastPlayedDateString); 
            }
            set { lastPlayedDateString = value.ToString("o"); }
        }
        
        // Para display na UI
        public string GetDisplayName()
        {
            return $"{instanceName} ({difficulty})";
        }
        
        public string GetPlayTimeFormatted()
        {
            int hours = (int)playTimeHours;
            int minutes = (int)((playTimeHours - hours) * 60);
            return $"{hours}h {minutes}m";
        }
        
        public string GetLastPlayedFormatted()
        {
            DateTime lastPlayed = LastPlayedDate;
            TimeSpan timeSince = DateTime.Now - lastPlayed;
            
            if (timeSince.TotalDays < 1)
                return "Today";
            else if (timeSince.TotalDays < 2)
                return "Yesterday";
            else if (timeSince.TotalDays < 7)
                return $"{(int)timeSince.TotalDays} days ago";
            else
                return lastPlayed.ToString("yyyy-MM-dd");
        }
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GIM] ⚠️ Duplicata detectada! Destruindo...");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[GIM] ✅ GameInstanceManager inicializado");
        
        // Carrega lista de instâncias salvas
        LoadGameInstancesList();
        
        // 🔥🔥🔥 ADICIONE ISTO: Limpa seleção ao iniciar
        currentGameInstanceID = -1;
        currentGameInstanceName = "";
        currentGameInstancePath = "";
        
        Debug.Log($"[GIM] Estado inicial: ID={currentGameInstanceID} (esperado: -1)");
    }
    
    private void Start()
    {
        // Se tem instâncias mas nenhuma selecionada, seleciona a primeira
        if (gameInstances.Count > 0 && currentGameInstanceID == -1)
        {
            SelectGameInstance(gameInstances[0].instanceID);
        }
    }
    
    // ============================================
    // PUBLIC METHODS - Criação e Gerenciamento
    // ============================================
    
    /// <summary>
    /// Cria uma nova instância de jogo
    /// </summary>
    public int CreateNewGameInstance(string gameName, string difficulty = "Normal")
    {
        //Se gameName for vazio, pega do SaveLoadManager
        if (string.IsNullOrEmpty(gameName))
        {
            var details = SaveLoadManager.GetNewGameDetails();
            gameName = details.gameName;
            
            Debug.Log($"[GIM] Usando nome do SaveLoadManager: '{gameName}'");
        }
        
        if (gameInstances.Count >= maxGameInstances)
        {
            Debug.LogError($"[GameInstanceManager] Cannot create new game - maximum of {maxGameInstances} instances reached!");
            return -1;
        }
        
        // Gera novo ID
        int newID = GetNextInstanceID();
        
        // Cria nome da pasta (sem caracteres especiais)
        string sanitizedName = SanitizeFileName(gameName);
        string folderName = $"Game_{newID:000}_{sanitizedName}";
        string fullPath = Path.Combine(Application.persistentDataPath, "GameInstances", folderName);
        
        // Cria estrutura de pastas
        try
        {
            // Pasta principal
            Directory.CreateDirectory(fullPath);
            
            // Subpastas
            Directory.CreateDirectory(Path.Combine(fullPath, "SaveSlots"));
            Directory.CreateDirectory(Path.Combine(fullPath, "Backups"));
            
            // Cria arquivo de configuração da instância
            CreateInstanceConfigFile(fullPath, gameName, difficulty);
            
            Debug.Log($"[GameInstanceManager] Created folder structure at: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameInstanceManager] Failed to create game instance folder: {e.Message}");
            return -1;
        }
        
        // Cria info da instância
        GameInstanceInfo newInstance = new GameInstanceInfo
        {
            instanceID = newID,
            instanceName = gameName,
            difficulty = difficulty,
            creationDateString = DateTime.Now.ToString("o"),
            lastPlayedDateString = DateTime.Now.ToString("o"),
            playTimeHours = 0f,
            saveFolderPath = fullPath,
            lastSaveSlot = 1
        };
        
        // Adiciona à lista e salva
        gameInstances.Add(newInstance);
        SaveGameInstancesList();
        
        // Seleciona automaticamente
        SelectGameInstance(newID);
        
        Debug.Log($"[GameInstanceManager] ✅ New game instance created: ID={newID}, Name='{gameName}', Difficulty={difficulty}");

        var playerDetails = SaveLoadManager.GetNewGameDetails();
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.UpdatePlayerName(playerDetails.playerName);
        }
        
        return newID;
    }
    
    /// <summary>
    /// Seleciona uma instância para jogar
    /// </summary>
    public bool SelectGameInstance(int instanceID)
    {
        Debug.Log($"[GIM] 🔄 SelectGameInstance chamado: ID={instanceID}");
        
        GameInstanceInfo instance = gameInstances.Find(i => i.instanceID == instanceID);
        
        if (instance == null)
        {
            Debug.LogError($"[GIM] ❌ Instância não encontrada: ID={instanceID}");
            return false;
        }
        
        // 🔥🔥🔥 NOVO: LIMPA INVENTÁRIO ANTES DE MUDAR DE INSTÂNCIA
        if (InventoryManager.Instance != null)
        {
            Debug.Log($"[GIM] 🧹 Limpando inventário da instância anterior...");
            InventoryManager.Instance.ClearInventory();
        }
        else
        {
            Debug.LogWarning($"[GIM] ⚠️ InventoryManager não encontrado para limpar inventário");
        }
        
        // Resto do método continua igual...
        currentGameInstanceID = instanceID;
        currentGameInstanceName = instance.instanceName;
        currentGameInstancePath = instance.saveFolderPath;
        
        // Atualiza last played
        instance.LastPlayedDate = DateTime.Now;
        SaveGameInstancesList();
        
        if (currentGameInstanceID != instanceID)
        {
            Debug.LogError($"[GIM] ❌ CRÍTICO: Falha ao atualizar currentGameInstanceID!");
            Debug.LogError($"   Esperado: {instanceID}, Atual: {currentGameInstanceID}");
            return false;
        }
        
        // Notifica GameDataManager sobre a mudança
        if (GameDataManager.Instance != null)
        {
            Debug.Log($"[GIM] 🔔 Notificando GameDataManager...");
            GameDataManager.Instance.OnGameInstanceChanged(instanceID, currentGameInstancePath);
        }
        else
        {
            Debug.LogWarning($"[GIM] ⚠️ GameDataManager não encontrado para notificar");
        }
        
        Debug.Log($"[GIM] ✅ Instância selecionada: '{instance.instanceName}' (ID: {instanceID})");
        return true;
    }
    
    /// <summary>
    /// Exclui uma instância de jogo
    /// </summary>
    public bool DeleteGameInstance(int instanceID)
    {
        GameInstanceInfo instance = gameInstances.Find(i => i.instanceID == instanceID);
        
        if (instance == null)
        {
            Debug.LogWarning($"[GameInstanceManager] Cannot delete - instance not found: ID={instanceID}");
            return false;
        }
        
        // Se está deletando a instância atual, limpa seleção
        if (currentGameInstanceID == instanceID)
        {
            currentGameInstanceID = -1;
            currentGameInstanceName = "";
            currentGameInstancePath = "";
        }
        
        try
        {
            // Exclui a pasta inteira
            if (Directory.Exists(instance.saveFolderPath))
            {
                Directory.Delete(instance.saveFolderPath, true);
                Debug.Log($"[GameInstanceManager] Deleted game folder: {instance.saveFolderPath}");
            }
            
            // Remove da lista
            gameInstances.Remove(instance);
            SaveGameInstancesList();
            
            Debug.Log($"[GameInstanceManager] ✅ Game instance deleted: '{instance.instanceName}' (ID: {instanceID})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameInstanceManager] Failed to delete game instance: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Atualiza tempo de jogo de uma instância
    /// </summary>
    public void UpdatePlayTime(int instanceID, float additionalHours)
    {
        GameInstanceInfo instance = gameInstances.Find(i => i.instanceID == instanceID);
        
        if (instance != null)
        {
            instance.playTimeHours += additionalHours;
            instance.LastPlayedDate = DateTime.Now;
            SaveGameInstancesList();
        }
    }
    
    /// <summary>
    /// Atualiza último slot salvo
    /// </summary>
    public void UpdateLastSaveSlot(int instanceID, int slotNumber)
    {
        GameInstanceInfo instance = gameInstances.Find(i => i.instanceID == instanceID);
        
        if (instance != null && slotNumber >= 1 && slotNumber <= 6)
        {
            int oldSlot = instance.lastSaveSlot;
            instance.lastSaveSlot = slotNumber;
            
            // 🔥 ADICIONE ESTE LOG!
            Debug.Log($"[GIM] 🔄 UpdateLastSaveSlot: Instância {instanceID} ({instance.instanceName})");
            Debug.Log($"   Slot antigo: {oldSlot} → Novo slot: {slotNumber}");
            
            SaveGameInstancesList(); // 🔥 GARANTA QUE ESTÁ SENDO CHAMADO!
        }
        else
        {
            Debug.LogError($"[GIM] ❌ Não foi possível atualizar lastSaveSlot: InstanceID={instanceID}, Slot={slotNumber}");
        }
    }
    
    // ============================================
    // HELPER METHODS
    // ============================================
    
    /// <summary>
    /// Gera próximo ID disponível
    /// </summary>
    private int GetNextInstanceID()
    {
        if (gameInstances.Count == 0)
            return 1;
        
        int maxID = 0;
        foreach (var instance in gameInstances)
        {
            if (instance.instanceID > maxID)
                maxID = instance.instanceID;
        }
        
        return maxID + 1;
    }
    
    /// <summary>
    /// Remove caracteres inválidos para nome de arquivo
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName.Trim();
    }
    
    /// <summary>
    /// Cria arquivo de configuração da instância
    /// </summary>
    private void CreateInstanceConfigFile(string folderPath, string gameName, string difficulty)
    {
        string configPath = Path.Combine(folderPath, "instance_config.json");
        
        InstanceConfig config = new InstanceConfig
        {
            gameName = gameName,
            difficulty = difficulty,
            creationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            unityVersion = Application.unityVersion,
            gameVersion = Application.version
        };
        
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(configPath, json);
    }
    
    [Serializable]
    private class InstanceConfig
    {
        public string gameName;
        public string difficulty;
        public string creationDate;
        public string unityVersion;
        public string gameVersion;
    }
    
    // ============================================
    // SAVE/LOAD DA LISTA DE INSTÂNCIAS
    // ============================================
    
    /// <summary>
    /// Carrega a lista de instâncias salvas
    /// </summary>
    private void LoadGameInstancesList()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "GameInstances", "instances_list.json");
        
        if (!File.Exists(savePath))
        {
            Debug.Log("[GameInstanceManager] No saved instances list found - starting fresh");
            gameInstances = new List<GameInstanceInfo>();
            return;
        }
        
        try
        {
            string json = File.ReadAllText(savePath);
            InstanceListWrapper wrapper = JsonUtility.FromJson<InstanceListWrapper>(json);
            
            if (wrapper != null && wrapper.instances != null)
            {
                gameInstances = wrapper.instances;
                Debug.Log($"[GameInstanceManager] Loaded {gameInstances.Count} game instances");
                
                // Verifica se pastas ainda existem
                List<GameInstanceInfo> validInstances = new List<GameInstanceInfo>();
                
                foreach (var instance in gameInstances)
                {
                    if (Directory.Exists(instance.saveFolderPath))
                    {
                        validInstances.Add(instance);
                    }
                    else
                    {
                        Debug.LogWarning($"[GameInstanceManager] Instance folder missing: {instance.instanceName} - removing from list");
                    }
                }
                
                gameInstances = validInstances;
                SaveGameInstancesList(); // Salva lista corrigida
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameInstanceManager] Failed to load instances list: {e.Message}");
            gameInstances = new List<GameInstanceInfo>();
        }
    }
    
    /// <summary>
    /// Salva a lista de instâncias
    /// </summary>
    private void SaveGameInstancesList()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "GameInstances");
        Directory.CreateDirectory(folderPath);
        
        string savePath = Path.Combine(folderPath, "instances_list.json");
        
        try
        {
            InstanceListWrapper wrapper = new InstanceListWrapper { instances = gameInstances };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(savePath, json);
            
            // Debug.Log($"[GameInstanceManager] Saved instances list: {gameInstances.Count} instances");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameInstanceManager] Failed to save instances list: {e.Message}");
        }
    }
    
    [Serializable]
    private class InstanceListWrapper
    {
        public List<GameInstanceInfo> instances;
    }
    
    // ============================================
    // PUBLIC GETTERS
    // ============================================
    
    public GameInstanceInfo GetCurrentInstanceInfo()
    {
        return gameInstances.Find(i => i.instanceID == currentGameInstanceID);
    }
    
    public GameInstanceInfo GetInstanceInfo(int instanceID)
    {
        return gameInstances.Find(i => i.instanceID == instanceID);
    }
    
    public bool HasSelectedGameInstance()
    {
        return currentGameInstanceID != -1 && !string.IsNullOrEmpty(currentGameInstancePath);
    }
    
    public int GetInstanceCount()
    {
        return gameInstances.Count;
    }
    
    public bool CanCreateNewInstance()
    {
        return gameInstances.Count < maxGameInstances;
    }
    
    // ============================================
    // DEBUG METHODS
    // ============================================
    
    [ContextMenu("🔍 Debug: Print All Instances")]
    public void DebugPrintAllInstances()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════╗");
        Debug.Log("║  🎮 GAME INSTANCES LIST                                ║");
        Debug.Log("╠══════════════════════════════════════════════════════════╣");
        
        if (gameInstances.Count == 0)
        {
            Debug.Log("║  No game instances created yet.");
        }
        else
        {
            foreach (var instance in gameInstances)
            {
                string currentMark = (instance.instanceID == currentGameInstanceID) ? " [CURRENT]" : "";
                Debug.Log($"║  ID {instance.instanceID}: {instance.instanceName} ({instance.difficulty}){currentMark}");
                Debug.Log($"║     Created: {instance.CreationDate:yyyy-MM-dd}");
                Debug.Log($"║     Play Time: {instance.GetPlayTimeFormatted()}");
                Debug.Log($"║     Last Played: {instance.GetLastPlayedFormatted()}");
                Debug.Log($"║     Path: {instance.saveFolderPath}");
                Debug.Log($"║     ───────────────────────────────");
            }
        }
        
        Debug.Log($"║  Total: {gameInstances.Count}/{maxGameInstances} instances");
        Debug.Log($"║  Current: {currentGameInstanceID} ('{currentGameInstanceName}')");
        Debug.Log("╚══════════════════════════════════════════════════════════╝");
    }
    
    [ContextMenu("🧪 Debug: Create Test Instance")]
    public void DebugCreateTestInstance()
    {
        int id = CreateNewGameInstance($"Test Game {UnityEngine.Random.Range(1, 100)}", "Normal");
        if (id != -1)
        {
            DebugPrintAllInstances();
        }
    }
    
    [ContextMenu("🧪 Complete Test: Create+Save")]
    public void DebugCompleteTest()
    {
        // 1. Cria instância
        int instanceID = CreateNewGameInstance($"Test_{UnityEngine.Random.Range(100, 999)}", "Normal");
        if (instanceID == -1) return;
        
        Debug.Log($"✅ Instance {instanceID} created and selected");
        
        // 2. Tenta salvar via GameDataManager
        if (GameDataManager.Instance != null)
        {
            // Aguarda um frame para garantir sincronização
            StartCoroutine(DelayedSaveTest(instanceID));
        }
    }
    
    private System.Collections.IEnumerator DelayedSaveTest(int instanceID)
    {
        yield return null; // Aguarda 1 frame
        
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame(1);
            Debug.Log($"💾 Save attempted for instance {instanceID}");
            
            yield return new WaitForSeconds(0.1f);
            
            // Verifica
            GameDataManager.Instance.DebugCompareAllSaveFiles();
        }
    }
    
    [ContextMenu("🧹 Debug: Clear All Instances")]
    public void DebugClearAllInstances()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "GameInstances");
        
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
            Debug.Log($"[GameInstanceManager] Deleted entire GameInstances folder");
        }
        
        gameInstances.Clear();
        currentGameInstanceID = -1;
        currentGameInstanceName = "";
        currentGameInstancePath = "";
        
        Debug.Log("[GameInstanceManager] All instances cleared");
    }
    
    [ContextMenu("🎮 Select First Instance")]
    public void DebugSelectFirstInstance()
    {
        if (gameInstances.Count == 0)
        {
            Debug.LogError("❌ No instances available!");
            return;
        }
        
        SelectGameInstance(gameInstances[0].instanceID);
        Debug.Log($"✅ Selected instance: {gameInstances[0].instanceName}");
    }
    
    [ContextMenu("🧪 Force Save in Current Instance")]
    public void DebugForceSaveInCurrentInstance()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager not found!");
            return;
        }
        
        if (!HasSelectedGameInstance())
        {
            Debug.LogError("❌ No game instance selected! Use [🎮 Select First Instance] first.");
            return;
        }
        
        Debug.Log($"💾 Forcing save in instance {currentGameInstanceID}...");
        GameDataManager.Instance.SaveGame(1);
        Debug.Log("✅ Save forced. Check folder or run DebugCompareAllSaveFiles.");
    }

    [ContextMenu("🔍 Debug: Check Last Save Slot for Instance")]
    public void DebugCheckLastSaveSlotForCurrentInstance()
    {
        if (currentGameInstanceID == -1)
        {
            Debug.Log("❌ Nenhuma instância selecionada!");
            return;
        }
        
        // 1. Ver PlayerPrefs
        int playerPrefsSlot = PlayerPrefs.GetInt($"LastSaveSlot_Instance_{currentGameInstanceID}", -1);
        
        // 2. Ver no GameInstanceInfo
        var instance = GetInstanceInfo(currentGameInstanceID);
        int instanceLastSlot = instance != null ? instance.lastSaveSlot : -1;
        
        Debug.Log($"╔════════════════════════════════════════╗");
        Debug.Log($"║ 🔍 VERIFICAÇÃO LAST SAVE SLOT         ║");
        Debug.Log($"╠════════════════════════════════════════╣");
        Debug.Log($"║ Instância ID: {currentGameInstanceID}");
        Debug.Log($"║ PlayerPrefs Slot: {playerPrefsSlot}");
        Debug.Log($"║ Instance Info Slot: {instanceLastSlot}");
        
        if (playerPrefsSlot != instanceLastSlot)
        {
            Debug.LogError($"║ ⚠️ INCONSISTÊNCIA! PlayerPrefs={playerPrefsSlot}, InstanceInfo={instanceLastSlot}");
        }
        else
        {
            Debug.Log($"║ ✅ Slots consistentes");
        }
        
        Debug.Log($"╚════════════════════════════════════════╝");
    }

    [ContextMenu("🧪 Testar Contaminação de Dados")]
    public void DebugTestDataContamination()
    {
        Debug.Log("🧪 TESTE DE CONTAMINAÇÃO DE DADOS");
        
        if (gameInstances == null || gameInstances.Count < 2)
        {
            Debug.LogError("❌ Precisa de pelo menos 2 instâncias!");
            return;
        }
        
        try
        {
            // Usa as duas primeiras instâncias
            var instance1 = gameInstances[0];
            var instance2 = gameInstances[1];
            
            Debug.Log($"Instância 1: {instance1.instanceName} (ID: {instance1.instanceID})");
            Debug.Log($"Instância 2: {instance2.instanceName} (ID: {instance2.instanceID})");
            
            // Teste 1: Verifica se paths são diferentes
            bool differentPaths = !instance1.saveFolderPath.Equals(instance2.saveFolderPath, StringComparison.OrdinalIgnoreCase);
            Debug.Log($"📁 Pastas diferentes? {(differentPaths ? "✅ SIM" : "❌ NÃO")}");
            
            if (!differentPaths)
            {
                Debug.LogError("❌❌❌ MESMA PASTA! CONTAMINAÇÃO GARANTIDA!");
                return;
            }
            
            // Teste 2: Verifica arquivos de save
            string path1_slot1 = System.IO.Path.Combine(instance1.saveFolderPath, "SaveSlots", "slot_1.json");
            string path2_slot1 = System.IO.Path.Combine(instance2.saveFolderPath, "SaveSlots", "slot_1.json");
            
            bool file1Exists = System.IO.File.Exists(path1_slot1);
            bool file2Exists = System.IO.File.Exists(path2_slot1);
            
            Debug.Log($"📄 Instância 1 tem slot_1.json? {(file1Exists ? "✅ SIM" : "❌ NÃO")}");
            Debug.Log($"📄 Instância 2 tem slot_1.json? {(file2Exists ? "✅ SIM" : "❌ NÃO")}");
            
            if (file1Exists && file2Exists)
            {
                string json1 = System.IO.File.ReadAllText(path1_slot1);
                string json2 = System.IO.File.ReadAllText(path2_slot1);
                
                // Comparação simples
                bool sameContent = json1 == json2;
                
                Debug.Log($"📝 Conteúdo IDÊNTICO? {(sameContent ? "❌ SIM" : "✅ NÃO")}");
                
                if (sameContent)
                {
                    Debug.LogError("❌❌❌ CONTAMINAÇÃO DETECTADA: Mesmo conteúdo nos dois saves!");
                    Debug.Log($"   Ambos têm {json1.Length} caracteres idênticos");
                }
                else
                {
                    Debug.Log("✅✅✅ ISOLAMENTO CONFIRMADO: Conteúdos diferentes!");
                    
                    // Mostra diferenças básicas
                    try
                    {
                        // Tenta desserializar para ver detalhes
                        var type = Type.GetType("GameData, Assembly-CSharp");
                        if (type != null)
                        {
                            var data1 = JsonUtility.FromJson(json1, type);
                            var data2 = JsonUtility.FromJson(json2, type);
                            
                            if (data1 != null && data2 != null)
                            {
                                // Usa reflexão para comparar campos simples
                                Debug.Log("🔍 Diferenças detectadas nos arquivos");
                            }
                        }
                    }
                    catch { }
                }
            }
            else if (!file1Exists && !file2Exists)
            {
                Debug.Log("ℹ️ Nenhuma das instâncias tem save no slot 1");
            }
            else
            {
                Debug.LogWarning("⚠️ Apenas uma instância tem save - teste limitado");
            }
            
            // Teste 3: Verifica estrutura de pastas
            Debug.Log("📂 ESTRUTURA DE PASTAS:");
            
            string[] instanceFolders = System.IO.Directory.GetDirectories(
                System.IO.Path.Combine(Application.persistentDataPath, "GameInstances")
            );
            
            foreach (string folder in instanceFolders)
            {
                string folderName = System.IO.Path.GetFileName(folder);
                string saveSlotsPath = System.IO.Path.Combine(folder, "SaveSlots");
                bool hasSaveSlots = System.IO.Directory.Exists(saveSlotsPath);
                
                Debug.Log($"  {folderName}: {(hasSaveSlots ? "✅ SaveSlots" : "❌ Sem SaveSlots")}");
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro no teste: {e.Message}");
            Debug.LogError($"Stack: {e.StackTrace}");
        }
    }

    [ContextMenu("🔍 Comparar FUNGO vs GUNFO")]
    public void DebugCompareFungoVsGunfo()
    {
        Debug.Log("🔍 COMPARAÇÃO: FUNGO's Adventure vs GUNFO's Adventure");
        
        // Encontra instâncias específicas
        GameInstanceInfo fungoInstance = null;
        GameInstanceInfo gunfoInstance = null;
        
        foreach (var instance in gameInstances)
        {
            if (instance.instanceName.Contains("FUNGO") || instance.instanceID == 2)
            {
                fungoInstance = instance;
                Debug.Log($"✅ Encontrou FUNGO: ID={instance.instanceID}, Nome='{instance.instanceName}'");
            }
            
            if (instance.instanceName.Contains("GUNFO") || instance.instanceID == 4)
            {
                gunfoInstance = instance;
                Debug.Log($"✅ Encontrou GUNFO: ID={instance.instanceID}, Nome='{instance.instanceName}'");
            }
        }
        
        if (fungoInstance == null || gunfoInstance == null)
        {
            Debug.LogError("❌ Não encontrou ambas as instâncias!");
            Debug.Log($"   FUNGO encontrado? {fungoInstance != null}");
            Debug.Log($"   GUNFO encontrado? {gunfoInstance != null}");
            return;
        }
        
        // Compara SLOT 1
        CompareTwoSaves(fungoInstance, gunfoInstance, 1, "SLOT 1");
        
        // Compara SLOT 2  
        CompareTwoSaves(fungoInstance, gunfoInstance, 2, "SLOT 2");
    }

    private void CompareTwoSaves(GameInstanceInfo inst1, GameInstanceInfo inst2, int slot, string label)
    {
        Debug.Log($"\n🎯 COMPARANDO {label}:");
        
        string path1 = System.IO.Path.Combine(inst1.saveFolderPath, "SaveSlots", $"slot_{slot}.json");
        string path2 = System.IO.Path.Combine(inst2.saveFolderPath, "SaveSlots", $"slot_{slot}.json");
        
        bool exists1 = System.IO.File.Exists(path1);
        bool exists2 = System.IO.File.Exists(path2);
        
        Debug.Log($"   FUNGO slot_{slot}.json existe? {(exists1 ? "✅ SIM" : "❌ NÃO")}");
        Debug.Log($"   GUNFO slot_{slot}.json existe? {(exists2 ? "✅ SIM" : "❌ NÃO")}");
        
        if (exists1 && exists2)
        {
            try
            {
                string json1 = System.IO.File.ReadAllText(path1);
                string json2 = System.IO.File.ReadAllText(path2);
                
                // Comparação 1: Tamanho
                long size1 = json1.Length;
                long size2 = json2.Length;
                bool sameSize = size1 == size2;
                
                Debug.Log($"   📏 Tamanhos: FUNGO={size1}, GUNFO={size2}, Iguais? {sameSize}");
                
                // Comparação 2: Conteúdo exato
                bool exactMatch = json1 == json2;
                Debug.Log($"   📝 Conteúdo IDÊNTICO? {(exactMatch ? "❌ SIM" : "✅ NÃO")}");
                
                if (exactMatch)
                {
                    Debug.LogError($"   ❌❌❌ CONTAMINAÇÃO NO {label}: Arquivos são IDÊNTICOS!");
                }
                else
                {
                    Debug.Log($"   ✅ {label} isolado - arquivos diferentes");
                    
                    // Tentar extrair posições para confirmar
                    ExtractAndComparePositions(json1, json2, label);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"   Erro: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"   ⚠️ {label}: Uma das instâncias não tem save");
        }
    }

    private void ExtractAndComparePositions(string json1, string json2, string label)
    {
        try
        {
            // Extrai posições manualmente (simples)
            int pos1Start = json1.IndexOf("\"lastPosition\":");
            int pos2Start = json2.IndexOf("\"lastPosition\":");
            
            if (pos1Start > 0 && pos2Start > 0)
            {
                // Pega próximo 50 caracteres após a posição
                string pos1 = json1.Substring(pos1Start, Mathf.Min(100, json1.Length - pos1Start));
                string pos2 = json2.Substring(pos2Start, Mathf.Min(100, json2.Length - pos2Start));
                
                Debug.Log($"   📍 Posição FUNGO: {GetFirstNumbers(pos1)}");
                Debug.Log($"   📍 Posição GUNFO: {GetFirstNumbers(pos2)}");
                
                bool samePosition = pos1 == pos2;
                Debug.Log($"   🎯 Mesma posição? {(samePosition ? "❌ SIM" : "✅ NÃO")}");
            }
        }
        catch { }
    }

    private string GetFirstNumbers(string text)
    {
        // Extrai números do texto
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(text, @"[-+]?\d*\.?\d+");
        if (match.Success)
        {
            return match.Value + "...";
        }
        return "não encontrada";
    }
    [ContextMenu("🧪 Teste Completo Fluxo Save/Load")]
    public void DebugCompleteSaveLoadFlow()
    {
        StartCoroutine(CompleteSaveLoadFlowTest());
    }

    private System.Collections.IEnumerator CompleteSaveLoadFlowTest()
    {
        Debug.Log("🧪 INICIANDO TESTE COMPLETO DE FLUXO");
        
        if (gameInstances.Count < 2)
        {
            Debug.LogError("❌ Precisa de 2 instâncias!");
            yield break;
        }
        
        // 1. Encontra instâncias
        var fungo = gameInstances.Find(i => i.instanceName.Contains("FUNGO"));
        var gunfo = gameInstances.Find(i => i.instanceName.Contains("GUNFO"));
        
        if (fungo == null || gunfo == null)
        {
            Debug.LogError("❌ Não encontrou instâncias!");
            yield break;
        }
        
        Debug.Log($"🎮 Instância A: {fungo.instanceName} (ID: {fungo.instanceID})");
        Debug.Log($"🎮 Instância B: {gunfo.instanceName} (ID: {gunfo.instanceID})");
        
        // 2. TESTE 1: Salva em FUNGO
        Debug.Log("\n📝 TESTE 1: Salvando em FUNGO com dados ÚNICOS");
        SelectGameInstance(fungo.instanceID);
        yield return new WaitForSeconds(0.1f);
        
        if (GameDataManager.Instance != null)
        {
            // Dados ÚNICOS para FUNGO
            GameDataManager.Instance.UpdatePlayerName("JOGADOR_FUNGO");
            GameDataManager.Instance.AddCurrency(500);
            GameDataManager.Instance.SaveGame(3); // Slot 3 para teste
            Debug.Log("   💾 FUNGO salvo: Nome=JOGADOR_FUNGO, Moedas=500, Slot=3");
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 3. TESTE 2: Salva em GUNFO
        Debug.Log("\n📝 TESTE 2: Salvando em GUNFO com dados DIFERENTES");
        SelectGameInstance(gunfo.instanceID);
        yield return new WaitForSeconds(0.1f);
        
        if (GameDataManager.Instance != null)
        {
            // Dados DIFERENTES para GUNFO
            GameDataManager.Instance.UpdatePlayerName("JOGADOR_GUNFO");
            GameDataManager.Instance.AddCurrency(1000);
            GameDataManager.Instance.SaveGame(3); // Mesmo slot 3!
            Debug.Log("   💾 GUNFO salvo: Nome=JOGADOR_GUNFO, Moedas=1000, Slot=3");
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 4. TESTE 3: Verifica se mantém isolamento
        Debug.Log("\n🔍 TESTE 3: Verificando isolamento pós-save");
        
        // Volta para FUNGO e carrega
        SelectGameInstance(fungo.instanceID);
        yield return new WaitForSeconds(0.1f);
        
        if (GameDataManager.Instance != null)
        {
            bool loaded = GameDataManager.Instance.LoadGame(3);
            if (loaded)
            {
                var data = GameDataManager.Instance.GetCurrentGameData();
                if (data != null)
                {
                    Debug.Log($"   📂 FUNGO após load:");
                    Debug.Log($"      Nome: {data.playerData?.playerName ?? "NULL"}");
                    Debug.Log($"      Moedas: {data.inventoryData?.currency ?? -1}");
                    
                    bool correctData = data.playerData?.playerName == "JOGADOR_FUNGO" && 
                                    data.inventoryData?.currency == 500;
                    
                    Debug.Log($"      Dados corretos? {(correctData ? "✅ SIM" : "❌ NÃO")}");
                    
                    if (!correctData)
                    {
                        Debug.LogError("   ❌❌❌ CONTAMINAÇÃO: FUNGO carregou dados do GUNFO!");
                    }
                }
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 5. TESTE 4: Verifica GUNFO
        Debug.Log("\n🔍 TESTE 4: Verificando GUNFO");
        SelectGameInstance(gunfo.instanceID);
        yield return new WaitForSeconds(0.1f);
        
        if (GameDataManager.Instance != null)
        {
            bool loaded = GameDataManager.Instance.LoadGame(3);
            if (loaded)
            {
                var data = GameDataManager.Instance.GetCurrentGameData();
                if (data != null)
                {
                    Debug.Log($"   📂 GUNFO após load:");
                    Debug.Log($"      Nome: {data.playerData?.playerName ?? "NULL"}");
                    Debug.Log($"      Moedas: {data.inventoryData?.currency ?? -1}");
                    
                    bool correctData = data.playerData?.playerName == "JOGADOR_GUNFO" && 
                                    data.inventoryData?.currency == 1000;
                    
                    Debug.Log($"      Dados corretos? {(correctData ? "✅ SIM" : "❌ NÃO")}");
                }
            }
        }
        
        // 6. Verificação final no disco
        Debug.Log("\n📂 VERIFICAÇÃO FINAL NO DISCO:");
        
        string fungoPath = System.IO.Path.Combine(fungo.saveFolderPath, "SaveSlots", "slot_3.json");
        string gunfoPath = System.IO.Path.Combine(gunfo.saveFolderPath, "SaveSlots", "slot_3.json");
        
        if (System.IO.File.Exists(fungoPath) && System.IO.File.Exists(gunfoPath))
        {
            string fungoJson = System.IO.File.ReadAllText(fungoPath);
            string gunfoJson = System.IO.File.ReadAllText(gunfoPath);
            
            bool sameContent = fungoJson == gunfoJson;
            Debug.Log($"   Arquivos idênticos? {(sameContent ? "❌ SIM" : "✅ NÃO")}");
            
            if (!sameContent)
            {
                Debug.Log("🎉 ISOLAMENTO 100% CONFIRMADO!");
                Debug.Log("   Cada instância mantém seus próprios dados no disco e na memória!");
            }
            else
            {
                Debug.LogError("❌❌❌ FALHA CRÍTICA: Arquivos idênticos!");
            }
        }
        
        Debug.Log("🧪 TESTE COMPLETO!");
    }

    [ContextMenu("🔍 Debug: Verify Instance State")]
    public void DebugVerifyInstanceState()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║  🔍 GAMEINSTANCEMANAGER STATE            ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║  Current Instance ID: {currentGameInstanceID}");
        Debug.Log($"║  Current Instance Name: {currentGameInstanceName}");
        Debug.Log($"║  Current Instance Path: {currentGameInstancePath}");
        Debug.Log($"║");
        Debug.Log($"║  Total Instances: {gameInstances.Count}");
        
        foreach (var instance in gameInstances)
        {
            string marker = (instance.instanceID == currentGameInstanceID) ? " [ACTIVE]" : "";
            Debug.Log($"║    - ID={instance.instanceID}: {instance.instanceName}{marker}");
        }
        
        Debug.Log("╚═══════════════════════════════════════════╝");
    }
}