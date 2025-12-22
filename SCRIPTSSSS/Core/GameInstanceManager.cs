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
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[GameInstanceManager] Initialized");
        
        // Carrega lista de instâncias salvas
        LoadGameInstancesList();
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
        GameInstanceInfo instance = gameInstances.Find(i => i.instanceID == instanceID);
        
        if (instance == null)
        {
            Debug.LogError($"[GameInstanceManager] Game instance not found: ID={instanceID}");
            return false;
        }
        
        // Atualiza dados
        currentGameInstanceID = instanceID;
        currentGameInstanceName = instance.instanceName;
        currentGameInstancePath = instance.saveFolderPath;
        
        // Atualiza last played
        instance.LastPlayedDate = DateTime.Now;
        SaveGameInstancesList();
        
        // Notifica GameDataManager sobre a mudança
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnGameInstanceChanged(instanceID, currentGameInstancePath);
        }
        
        Debug.Log($"[GameInstanceManager] ✅ Game instance selected: '{instance.instanceName}' (ID: {instanceID})");
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
        
        if (instance != null && slotNumber >= 1 && slotNumber <= 3)
        {
            instance.lastSaveSlot = slotNumber;
            SaveGameInstancesList();
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
}