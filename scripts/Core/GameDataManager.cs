using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;


[System.Serializable]
public struct CharacterEquipmentMapping
{
    public string characterID;
    public EquipmentLoadoutData equipmentLoadout;
}

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

    [Header("Multi-Instance Support")]
    private int currentGameInstanceID = -1;
    private string currentGameInstancePath = "";
    private string currentGameInstanceName = "";
    
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
    {   StartCoroutine(EnsureGameInstanceSync());
        InitializeSaveSystem();
        
    }
    /// <summary>
    /// 🔥 Garante que o GameInstanceManager está sincronizado
    /// </summary>
   private System.Collections.IEnumerator EnsureGameInstanceSync()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║ [GDM] 🔄 SINCRONIZANDO COM GAMEINSTANCEMANAGER");
        Debug.Log("╠═══════════════════════════════════════════╣");
        if (PlayerPrefs.HasKey("PendingInstanceID"))
        {
            int pendingID = PlayerPrefs.GetInt("PendingInstanceID");
            string pendingPath = PlayerPrefs.GetString("PendingInstancePath", "");
            string pendingName = PlayerPrefs.GetString("PendingInstanceName", "");
            
            Debug.Log($"║  📋 Instância pendente detectada no PlayerPrefs:");
            Debug.Log($"║     ID: {pendingID}");
            Debug.Log($"║     Nome: {pendingName}");
            Debug.Log($"║     Path: {pendingPath}");
            
            if (!string.IsNullOrEmpty(pendingPath) && System.IO.Directory.Exists(pendingPath))
            {
                Debug.Log($"║  ✅ Restaurando instância do PlayerPrefs...");
                
                currentGameInstanceID = pendingID;
                currentGameInstancePath = pendingPath;
                currentGameInstanceName = pendingName;
                
                Debug.Log($"║  ✅ Instância restaurada: {pendingName} (ID={pendingID})");
                
                // 🔥 Limpa o PlayerPrefs após usar
                PlayerPrefs.DeleteKey("PendingInstanceID");
                PlayerPrefs.DeleteKey("PendingInstancePath");
                PlayerPrefs.DeleteKey("PendingInstanceName");
                PlayerPrefs.Save();
                
                Debug.Log($"║  🧹 PlayerPrefs limpos");
                Debug.Log("╚═══════════════════════════════════════════╝");
                
                // 🔥 Pula para InitializeSaveSystem
                InitializeSaveSystem();
                yield break;
            }
            else
            {
                Debug.LogWarning($"║  ⚠️ Path da instância pendente não existe!");
            }
        }

        

        // 🔥 PASSO 1: AGUARDA ATÉ 3 SEGUNDOS PELO GAMEINSTANCEMANAGER
        float timeout = 3f;
        float elapsed = 0f;
        
        while (GameInstanceManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("║  ❌ TIMEOUT: GameInstanceManager não encontrado após 3s!");
            Debug.Log("╚═══════════════════════════════════════════╝");
            
            // 🔥 FALLBACK: Sistema antigo
            InitializeSaveSystem();
            yield break;
        }
        
        Debug.Log("║  ✅ GameInstanceManager encontrado");
        
        // 🔥 PASSO 2: AGUARDA ATÉ 2 SEGUNDOS POR UMA INSTÂNCIA SELECIONADA
        timeout = 2f;
        elapsed = 0f;
        
        while (!GameInstanceManager.Instance.HasSelectedGameInstance() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            
            // A cada 0.5s, verifica novamente
            if (Mathf.FloorToInt(elapsed * 2) != Mathf.FloorToInt((elapsed - Time.deltaTime) * 2))
            {
                Debug.Log($"║  ⏳ Aguardando instância ser selecionada... ({elapsed:F1}s)");
            }
            
            yield return null;
        }
        
        // 🔥 PASSO 3: VERIFICA SE TEM INSTÂNCIA SELECIONADA
        if (GameInstanceManager.Instance.HasSelectedGameInstance())
        {
            int instanceID = GameInstanceManager.Instance.currentGameInstanceID;
            string instancePath = GameInstanceManager.Instance.currentGameInstancePath;
            string instanceName = GameInstanceManager.Instance.currentGameInstanceName;
            
            Debug.Log($"║  🎯 Instância ativa detectada:");
            Debug.Log($"║     ID: {instanceID}");
            Debug.Log($"║     Nome: {instanceName}");
            Debug.Log($"║     Path: {instancePath}");
            
            // 🔥🔥🔥 SINCRONIZA IMEDIATAMENTE
            currentGameInstanceID = instanceID;
            currentGameInstancePath = instancePath;
            currentGameInstanceName = instanceName;
            
            Debug.Log($"║  ✅ GameDataManager sincronizado com instância {instanceID}");
        }
        else
        {
            Debug.Log($"║  ℹ️ Nenhuma instância selecionada ainda");
            
            // 🔥 Verifica se estamos no MainMenu ou GameScene
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene.Contains("Game"))
            {
                Debug.LogWarning($"║  ⚠️ AVISO: Na GameScene sem instância!");
                Debug.LogWarning($"║  Isso NÃO deveria acontecer!");
                
                // 🔥 TENTA RECUPERAR A ÚLTIMA INSTÂNCIA
                if (GameInstanceManager.Instance.GetInstanceCount() > 0)
                {
                    var firstInstance = GameInstanceManager.Instance.gameInstances[0];
                    Debug.LogWarning($"║  🔧 RECUPERAÇÃO: Usando primeira instância disponível");
                    Debug.LogWarning($"║     ID: {firstInstance.instanceID}");
                    Debug.LogWarning($"║     Nome: {firstInstance.instanceName}");
                    
                    currentGameInstanceID = firstInstance.instanceID;
                    currentGameInstancePath = firstInstance.saveFolderPath;
                    currentGameInstanceName = firstInstance.instanceName;
                }
            }
        }
        
        Debug.Log("╚═══════════════════════════════════════════╝");
        
        // 🔥 AGORA inicializa o save system COM a instância sincronizada
        InitializeSaveSystem();
    }
    
    // Substituir o método InitializeSaveSystem() (linha ~66-80)
    void InitializeSaveSystem()
    {
        // 🔥 CORREÇÃO: NÃO usar mais "saves", usar estrutura multi-instância
        
        // Se temos instância ativa, usa caminho da instância
        if (currentGameInstanceID != -1 && !string.IsNullOrEmpty(currentGameInstancePath))
        {
            saveFolderPath = Path.Combine(currentGameInstancePath, "SaveSlots");
            Debug.Log($"[GDM] ✅ Usando caminho multi-instância: {saveFolderPath}");
        }
        else
        {
            // Fallback temporário - mas NÃO criar pasta "saves"
            saveFolderPath = Path.Combine(Application.persistentDataPath, "GameInstances", "Temporary");
            Debug.LogWarning($"[GDM] ⚠️ Sem instância - usando fallback: {saveFolderPath}");
        }
        
        // 🔥 NÃO CRIAR DIRETÓRIO AQUI! Só criar quando for salvar
        // O GetSaveFilePath() já cria o diretório quando necessário
        
        ClearAutoSave();
        
        // Sempre inicializa currentGameData
        if (currentGameData == null)
        {
            currentGameData = new GameData();
            currentGameData.isNewGame = true;
            currentGameData.saveSlot = 1;
        }
        
        // 🔥🔥🔥 CORREÇÃO CRÍTICA: Se é um NOVO JOGO, NÃO tenta carregar saves!
        if (currentGameData != null && currentGameData.isNewGame)
        {
            Debug.Log($"[GDM] ⭐ É UM NOVO JOGO - NÃO carregar saves antigos");
            Debug.Log($"[GDM] ⭐ Mantendo estado novo: saveSlot={currentGameData.saveSlot}");
            
            // ⭐ INICIALIZA ITENS APENAS NA MEMÓRIA (se necessário)
            if (InventoryManager.Instance != null)
            {
                // Garante inventário limpo
                InventoryManager.Instance.ClearInventory();
            }
            
            return; // ⭐ PARA AQUI - NÃO carrega saves!
        }
        
        // 🔥 SÓ executa abaixo se NÃO for novo jogo (é um LOAD)
        if (currentGameInstanceID != -1)
        {
            // ESTRATÉGIA 1: PlayerPrefs (MAIS CONFIÁVEL - atualizado imediatamente no save)
            int playerPrefsSlot = PlayerPrefs.GetInt($"LastSaveSlot_Instance_{currentGameInstanceID}", -1);
            
            if (playerPrefsSlot > 0 && SaveFileExists(playerPrefsSlot))
            {
                Debug.Log($"[GDM] 📂 [ESTRATÉGIA 1] Carregando do PlayerPrefs: slot {playerPrefsSlot}");
                LoadGame(playerPrefsSlot);
            }
            else
            {
                // ESTRATÉGIA 2: Instance Info no GameInstanceManager
                if (GameInstanceManager.Instance != null)
                {
                    var instanceInfo = GameInstanceManager.Instance.GetInstanceInfo(currentGameInstanceID);
                    if (instanceInfo != null && instanceInfo.lastSaveSlot > 0 && SaveFileExists(instanceInfo.lastSaveSlot))
                    {
                        Debug.Log($"[GDM] 📂 [ESTRATÉGIA 2] Carregando do InstanceInfo: slot {instanceInfo.lastSaveSlot}");
                        LoadGame(instanceInfo.lastSaveSlot);
                    }
                    else
                    {
                        // ESTRATÉGIA 3: Primeiro save disponível na instância
                        LoadFirstAvailableSaveInInstance();
                    }
                }
                else
                {
                    // ESTRATÉGIA 3 (fallback)
                    LoadFirstAvailableSaveInInstance();
                }
            }
        }
        else
        {
            // Sistema antigo (compatibilidade) - MAS NÃO CRIAR "saves"
            Debug.LogWarning("[GDM] ⚠️ Inicializando sem instância - sistema limitado");
            // Não tenta carregar nada, fica como novo jogo
        }
        
        // Garantia final
        if (currentGameData == null)
        {
            currentGameData = new GameData();
            currentGameData.isNewGame = true;
            currentGameData.saveSlot = 1;
        }
    }

    /// <summary>
    /// 🔥 NOVO: Carrega primeiro save disponível na instância atual
    /// </summary>
    private void LoadFirstAvailableSaveInInstance()
    {
        if (currentGameInstanceID == -1 || string.IsNullOrEmpty(currentGameInstancePath))
            return;
        
        // Verifica slots 1-5 na pasta da instância
        for (int slot = 1; slot <= 5; slot++)
        {
            if (SaveFileExistsInInstance(slot))
            {
                Debug.Log($"[GDM] 📂 Encontrou save no slot {slot} - carregando");
                LoadGame(slot);
                return;
            }
        }
        
        Debug.Log($"[GDM] ℹ️ Nenhum save encontrado na instância {currentGameInstanceID}");
        // Mantém currentGameData como novo jogo
    }
    private int FindMostRecentSaveSlot()
    {
        DateTime mostRecentDate = DateTime.MinValue;
        int mostRecentSlot = -1;
        
        // Verifica slots 1-6
        for (int i = 1; i <= 6; i++)
        {
            string filePath = GetSaveFilePath(i);
            
            if (!File.Exists(filePath)) continue;
            
            try
            {
                // Opção 1: Usar data do arquivo
                DateTime fileDate = File.GetLastWriteTime(filePath);
                
                if (fileDate > mostRecentDate)
                {
                    mostRecentDate = fileDate;
                    mostRecentSlot = i;
                }
                
                // Opção 2 (mais precisa): Ler saveDate do JSON
                string json = File.ReadAllText(filePath);
                GameData data = JsonUtility.FromJson<GameData>(json);
                
                if (data != null && !string.IsNullOrEmpty(data.saveDate))
                {
                    if (DateTime.TryParse(data.saveDate, out DateTime saveDate))
                    {
                        if (saveDate > mostRecentDate)
                        {
                            mostRecentDate = saveDate;
                            mostRecentSlot = i;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GDM] Erro ao verificar slot {i}: {e.Message}");
            }
        }
        
        if (mostRecentSlot > 0)
        {
            Debug.Log($"[GDM] ✅ Slot mais recente encontrado: {mostRecentSlot} ({mostRecentDate:yyyy-MM-dd HH:mm:ss})");
        }
        
        return mostRecentSlot;
    }
    /// <summary>
    /// 🎮 Chamado pelo GameInstanceManager quando uma instância é selecionada
    /// </summary>
    public void OnGameInstanceChanged(int gameInstanceID, string gameInstancePath)
    {
        Debug.Log($"[GDM] 🔄 Mudando para instância: ID={gameInstanceID}");
        
        // 🔥🔥🔥 NOVO: RESET COMPLETO para garantir novo jogo
        // 1. Reseta instância
        currentGameInstanceID = gameInstanceID;
        currentGameInstancePath = gameInstancePath;
        
        // 2. 🔥 CRÍTICO: Força estado de NOVO JOGO
        currentGameData = new GameData();
        currentGameData.isNewGame = true; // ⭐⭐ ESTA É A CHAVE!
        currentGameData.saveSlot = 1;
        currentGameData.playerData.playerName = defaultPlayerName;
        
        // 3. Configura caminho
        saveFolderPath = Path.Combine(gameInstancePath, "SaveSlots");
        
        // 🔥 GARANTE que a pasta SaveSlots existe
        if (!System.IO.Directory.Exists(saveFolderPath))
        {
            System.IO.Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"[GDM] 📁 Criada pasta SaveSlots: {saveFolderPath}");
        }
        
        Debug.Log($"[GDM] ✅ Instância configurada para NOVO JOGO: {gameInstanceID}");
        
        // ⚠️ NÃO chama InitializeSaveSystem() aqui!
        // O SaveLoadManager cuidará de criar o novo jogo
    }

    /// <summary>
    /// Obtém o ID da instância atual (para outros scripts verificarem)
    /// </summary>
    public int GetCurrentGameInstanceID()
    {
        return currentGameInstanceID;
    }

    /// <summary>
    /// Verifica se há uma instância ativa
    /// </summary>
    public bool HasActiveGameInstance()
    {
        return currentGameInstanceID != -1 && !string.IsNullOrEmpty(currentGameInstancePath);
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

     /// <summary>
    /// 🎁 Adiciona os itens iniciais configurados no InventoryManager
    /// </summary>
    public void AddStartingItemsToNewGame()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[GDM] InventoryManager not found - cannot add starting items");
            return;
        }
        
        Debug.Log("[GDM] 🎁 Adding starting items to new game...");
        
        // Pega os itens iniciais do InventoryManager
        var inventoryManager = InventoryManager.Instance;
        
        // Verifica se tem startingItems configurados
        var startingItemsField = typeof(InventoryManager).GetField("startingItems", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (startingItemsField == null)
        {
            Debug.LogError("[GDM] Cannot find startingItems field in InventoryManager!");
            return;
        }
        
        ItemData[] startingItems = (ItemData[])startingItemsField.GetValue(inventoryManager);
        
        if (startingItems == null || startingItems.Length == 0)
        {
            Debug.Log("[GDM] No starting items configured in InventoryManager");
            return;
        }
        
        // Adiciona cada item ao inventário
        foreach (var item in startingItems)
        {
            if (item != null)
            {
                // Quantidade baseada no tipo de item
                int quantity = 1;
                if (item.itemType == ItemData.ItemType.Material)
                    quantity = 5;
                else if (item.stackLimit > 1)
                    quantity = 3;
                
                Debug.Log($"  🎁 Adding {quantity}x {item.itemName}");
                inventoryManager.AddItem(item, quantity);
            }
        }
        
        // Adiciona moeda inicial
        var startingCurrencyField = typeof(InventoryManager).GetField("startingCurrency",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (startingCurrencyField != null)
        {
            int startingCurrency = (int)startingCurrencyField.GetValue(inventoryManager);
            if (startingCurrency > 0)
            {
                inventoryManager.AddCurrency(startingCurrency);
                Debug.Log($"  💰 Adding {startingCurrency} starting currency");
            }
        }
    }

    /// <summary>
    /// 🆕 Cria um novo jogo dentro da instância atual
    /// </summary>
    public void CreateNewGameInCurrentInstance()
    {
        if (currentGameInstanceID == -1)
        {
            Debug.LogError("[GDM] Cannot create new game - no game instance selected!");
            return;
        }
        
        CreateNewGame();

        AddStartingItemsToNewGame();

        
        Debug.Log($"[GDM] ✅ New game created in instance {currentGameInstanceID}");
    }

   
        
    public void SaveGame(int slot = 1, bool isAutoSave = false)
    {   
        // ⭐⭐ DEBUG INICIAL
        Debug.Log($"╔══════════════════════════════════════════════════════╗");
        Debug.Log($"║ [GDM] SAVEGAME - DIAGNÓSTICO INICIAL");
        Debug.Log($"╠══════════════════════════════════════════════════════╣");
        Debug.Log($"║ Slot: {slot}, isAutoSave: {isAutoSave}");
        Debug.Log($"║ currentGameData.saveSlot ANTES: {currentGameData?.saveSlot}");
        Debug.Log($"║ currentGameData.currency ANTES: {currentGameData?.inventoryData?.currency}");
        
        // 🔥🔥🔥 VALIDAÇÃO CRÍTICA: Verificar instância ativa PRIMEIRO
        if (currentGameInstanceID == -1 || string.IsNullOrEmpty(currentGameInstancePath))
        {
            Debug.LogError("║ ❌ SAVE BLOQUEADO: Nenhuma instância de jogo ativa!");
            Debug.LogError($"║    currentGameInstanceID: {currentGameInstanceID}");
            Debug.LogError($"║    currentGameInstancePath: '{currentGameInstancePath}'");
            Debug.LogError("║    Use GameInstanceManager.CreateNewGameInstance() primeiro");
            
            // 🔥 SE FOR AUTO-SAVE, apenas cancela silenciosamente
            if (isAutoSave)
            {
                Debug.LogWarning("║ ⚠️ Auto-save cancelado (sem instância)");
                Debug.Log($"╚══════════════════════════════════════════════════════╝");
                return;
            }
            
            // 🔥 SE FOR SAVE MANUAL, tenta corrigir
            if (GameInstanceManager.Instance != null && GameInstanceManager.Instance.HasSelectedGameInstance())
            {
                Debug.LogWarning("║ 🔧 Tentando sincronizar instância do GameInstanceManager...");
                
                currentGameInstanceID = GameInstanceManager.Instance.currentGameInstanceID;
                currentGameInstancePath = GameInstanceManager.Instance.currentGameInstancePath;
                currentGameInstanceName = GameInstanceManager.Instance.currentGameInstanceName;
                
                Debug.Log($"║ ✅ Sincronizado: ID={currentGameInstanceID}, Path={currentGameInstancePath}");
            }
            else
            {
                Debug.LogError("║ ❌ CRÍTICO: Não foi possível corrigir - ABORTANDO SAVE!");
                Debug.Log($"╚══════════════════════════════════════════════════════╝");
                return;
            }
        }
        
        Debug.Log($"║ ✅ Instância ativa: {currentGameInstanceID} ('{currentGameInstanceName}')");
        Debug.Log($"╚══════════════════════════════════════════════════════╝");
        
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
        
        // ⭐ ATUALIZAR O SNAPSHOT (não o currentGameData!)
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
        
        // 🔥 VERIFICAR SE GetSaveFilePath retornou NULL (sem instância)
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("[GDM] ❌ GetSaveFilePath retornou NULL - ABORTANDO SAVE!");
            return;
        }
        
        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"[GDM] ✅ Save criado no slot {slot}: {filePath}");

            // ⭐⭐ DEBUG FINAL 
            Debug.Log($"╔══════════════════════════════════════════════════════╗");
            Debug.Log($"║ [GDM] SAVEGAME - DIAGNÓSTICO FINAL");
            Debug.Log($"╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║ currentGameData.saveSlot DEPOIS: {currentGameData?.saveSlot}");
            Debug.Log($"║ currentGameData.currency DEPOIS: {currentGameData?.inventoryData?.currency}");
            Debug.Log($"║ Arquivo salvo: {filePath}");
            Debug.Log($"║ Tamanho JSON: {jsonData.Length} chars");
            Debug.Log($"╚══════════════════════════════════════════════════════╝");
            
            // ⭐⭐ PASSO 6: Atualizar APENAS metadata no currentGameData
            if (!isAutoSave)
            {
                // ⭐ Atualizar TODOS os dados, não apenas metadata!
                // 1. Primeiro, força a atualização do saveSlot
                currentGameData.saveSlot = slot; // Slot ATUAL em memória
                
                // 2. Atualiza a data
                currentGameData.saveDate = snapshotData.saveDate;
                
                // 3. 🔥 IMPORTANTE: Sincroniza os DADOS REAIS do snapshot para currentGameData!
                SyncCurrentGameDataFromSnapshot(snapshotData);
                
                // 4. Salva último slot PARA ESTA INSTÂNCIA
                if (currentGameInstanceID != -1)
                {
                    PlayerPrefs.SetInt($"LastSaveSlot_Instance_{currentGameInstanceID}", slot);
                    PlayerPrefs.Save();
                    
                    // Atualiza no GameInstanceManager também
                    if (GameInstanceManager.Instance != null)
                    {
                        Debug.Log($"[GDM] 🔥 CHAMANDO UpdateLastSaveSlot: Instância {currentGameInstanceID} → Slot {slot}");
                        
                        // VERIFICAÇÃO ANTES
                        var instanceBefore = GameInstanceManager.Instance.GetInstanceInfo(currentGameInstanceID);
                        if (instanceBefore != null)
                        {
                            Debug.Log($"[GDM]   ANTES: lastSaveSlot = {instanceBefore.lastSaveSlot}");
                        }
                        
                        // CHAMADA
                        GameInstanceManager.Instance.UpdateLastSaveSlot(currentGameInstanceID, slot);
                        
                        // VERIFICAÇÃO DEPOIS
                        var instanceAfter = GameInstanceManager.Instance.GetInstanceInfo(currentGameInstanceID);
                        if (instanceAfter != null)
                        {
                            Debug.Log($"[GDM]   DEPOIS: lastSaveSlot = {instanceAfter.lastSaveSlot}");
                            
                            if (instanceAfter.lastSaveSlot != slot)
                            {
                                Debug.LogError($"[GDM] ❌ FALHA: UpdateLastSaveSlot não funcionou! Esperado: {slot}, Atual: {instanceAfter.lastSaveSlot}");
                            }
                        }
                    }
                }
                else
                {
                    // Fallback: sistema antigo
                    PlayerPrefs.SetInt("LastSaveSlot", slot);
                    PlayerPrefs.Save();
                }
                
                PlayerPrefs.Save();
                
                Debug.Log($"✅ currentGameData.saveSlot atualizado para: {currentGameData.saveSlot}");
                Debug.Log($"✅ currentGameData.currency atualizado para: {currentGameData.inventoryData.currency}");
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
    /// 🔥 SINCRONIZA currentGameData com os dados CORRETOS do snapshot
    /// Corrige o bug onde currentGameData fica com dados antigos após save
    /// </summary>
    private void SyncCurrentGameDataFromSnapshot(GameData snapshot)
    {
        if (currentGameData == null || snapshot == null) return;
        
        Debug.Log("[GDM] 🔄 Sincronizando currentGameData com snapshot...");
        
        // 1. Moedas (o bug mais crítico!)
        currentGameData.inventoryData.currency = snapshot.inventoryData.currency;
        
        // 2. Itens do inventário
        currentGameData.inventoryData.items.Clear();
        foreach (var item in snapshot.inventoryData.items)
        {
            currentGameData.inventoryData.items.Add(item);
        }
        
        // 3. Dados do player
        currentGameData.playerData.currentHealth = snapshot.playerData.currentHealth;
        currentGameData.playerData.maxHealth = snapshot.playerData.maxHealth;
        currentGameData.playerData.level = snapshot.playerData.level;
        currentGameData.playerData.experience = snapshot.playerData.experience;
        
        // 4. Party System (se aplicável)
        if (snapshot.playerData.characterEquipment != null)
        {
            currentGameData.playerData.characterEquipment.partyMembers.Clear();
            foreach (var member in snapshot.playerData.characterEquipment.partyMembers)
            {
                currentGameData.playerData.characterEquipment.partyMembers.Add(member);
            }
            currentGameData.playerData.characterEquipment.activeCharacterIndex = 
                snapshot.playerData.characterEquipment.activeCharacterIndex;
        }
        
        Debug.Log($"[GDM] ✅ Sincronização completa. Currency: {currentGameData.inventoryData.currency}");
    }

    /// <summary>
    /// ⭐ Atualiza dados de TODOS os sistemas antes de salvar
    /// </summary>
    private void UpdateAllSystemsDataBeforeSave()
    {
        // 1. Inventory System
        UpdateInventoryDataBeforeSave();
        
        // 2. Party System 
        UpdatePartyDataBeforeSave();
        
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
    {      
        Debug.Log($"╔═══════════════════════════════════════════════╗");
        Debug.Log($"║ [GDM] LOADGAME - DIAGNÓSTICO            ║");
        Debug.Log($"╠═══════════════════════════════════════════════╣");
        Debug.Log($"║ Slot solicitado: {slot}");
        Debug.Log($"║ Instância atual: {currentGameInstanceID}");
        Debug.Log($"║ Path atual: {currentGameInstancePath}");
        
        // 🔥 VALIDAÇÃO CRÍTICA
        if (currentGameInstanceID == -1 || string.IsNullOrEmpty(currentGameInstancePath))
        {
            Debug.LogError($"║ ❌ LOAD BLOQUEADO: Nenhuma instância ativa!");
            Debug.Log($"╚═══════════════════════════════════════════════╝");
            return false;
        }
        
        string filePath = GetSaveFilePath(slot);
        Debug.Log($"║ Arquivo: {filePath}");
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"║ ❌ Arquivo não encontrado!");
            Debug.Log($"╚═══════════════════════════════════════════════╝");
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
            
            // 🔥🔥🔥 CORREÇÃO CRÍTICA: SEMPRE atualizar saveSlot ANTES de atribuir
            loadedData.saveSlot = slot;
            loadedData.isNewGame = false;
            
            // ⭐ SUBSTITUI COMPLETAMENTE o estado do jogo
            currentGameData = loadedData;
            
            // 🔥🔥🔥 GARANTIR que o saveSlot foi atualizado corretamente
            if (currentGameData.saveSlot != slot)
            {
                Debug.LogError($"║ ⚠️ CORREÇÃO: saveSlot era {currentGameData.saveSlot}, forçando para {slot}");
                currentGameData.saveSlot = slot;
            }
            
            // 🔥 ATUALIZAR PlayerPrefs e GameInstanceManager
            if (slot != 0)
            {
                // PlayerPrefs para esta instância
                PlayerPrefs.SetInt($"LastSaveSlot_Instance_{currentGameInstanceID}", slot);
                PlayerPrefs.Save();
                
                // GameInstanceManager
                if (GameInstanceManager.Instance != null)
                {
                    Debug.Log($"║ 📝 Atualizando lastSaveSlot no GameInstanceManager: {slot}");
                    GameInstanceManager.Instance.UpdateLastSaveSlot(currentGameInstanceID, slot);
                    
                    // 🔥 VERIFICAÇÃO IMEDIATA
                    var instance = GameInstanceManager.Instance.GetInstanceInfo(currentGameInstanceID);
                    if (instance != null)
                    {
                        Debug.Log($"║    Verificação: instance.lastSaveSlot = {instance.lastSaveSlot}");
                        
                        if (instance.lastSaveSlot != slot)
                        {
                            Debug.LogError($"║    ❌ FALHA: lastSaveSlot não foi atualizado!");
                        }
                    }
                }
            }
            
            // ⭐ Notificar TODOS os sistemas
            NotifyAllSystemsAfterLoad();
            
            Debug.Log($"[GDM] ✅ Game loaded from slot {slot}");
            Debug.Log($"║ currentGameData.saveSlot DEPOIS: {currentGameData.saveSlot}");
            Debug.Log($"║ currentGameData.currency DEPOIS: {currentGameData.inventoryData.currency}");
            Debug.Log($"║ JSON carregado: {jsonData.Length} chars");
            Debug.Log($"╚═══════════════════════════════════════════════╝");

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
        if (PartyManager.Instance != null)
        {
            Debug.Log("[GDM] 🔄 Notifying PartyManager...");
            LoadPartyFromGameData();
        }
        Debug.Log("[GDM] ✅ Todos os sistemas notificados sobre load");
    }
    /// <summary>
    ///  Carrega dados do Party System do save
    /// CORRIGIDO para trabalhar com CharacterData ScriptableObject
    /// </summary>
    private void LoadPartyFromGameData()
    {
        if (PartyManager.Instance == null) return;
        
        var gameData = GetCurrentGameData();
        if (gameData?.playerData?.characterEquipment?.partyMembers == null) return;
        
        Debug.Log("[GDM] 🔄 Carregando Party System do save...");
        
        var partyManager = PartyManager.Instance;
        var allMembers = partyManager.GetAllMembers();
        
        // 🔥 1. Restaura personagem ativo
        int savedActiveIndex = gameData.playerData.characterEquipment.activeCharacterIndex;
        if (savedActiveIndex >= 0 && savedActiveIndex < allMembers.Count)
        {
            partyManager.SetActiveMember(savedActiveIndex);
            Debug.Log($"   ✅ Personagem ativo restaurado: índice {savedActiveIndex}");
        }
        
        // 🔥 2. Para cada membro salvo, atualiza o correspondente
        foreach (var savedMember in gameData.playerData.characterEquipment.partyMembers)
        {
            // 🔥 ENCONTRA O CHARACTERDATA CORRETO:
            // Tenta por name do ScriptableObject primeiro, depois por characterName
            CharacterData existingMember = null;
            
            foreach (var member in allMembers)
            {
                if (member == null) continue;
                
                // 1. Tenta match por name do ScriptableObject (mais confiável)
                if (!string.IsNullOrEmpty(savedMember.characterID) && 
                    member.name == savedMember.characterID)
                {
                    existingMember = member;
                    break;
                }
                
                // 2. Tenta por characterName (fallback)
                if (member.characterName == savedMember.characterName)
                {
                    existingMember = member;
                    break;
                }
            }
            
            if (existingMember != null)
            {
                // 🔥 3. Atualiza stats básicos (APENAS runtime, não SO)
                existingMember.currentLevel = savedMember.level;
                existingMember.currentHP = savedMember.currentHP;      // CORREÇÃO
                existingMember.currentMP = savedMember.currentMP;      // CORREÇÃO
                
                // 🔥 4. STATS BASE (não modifica o ScriptableObject, apenas valores runtime)
                // CharacterData já tem esses valores no SO, não precisamos modificar
                
                // 🔥🔥🔥 5. CARREGA EQUIPAMENTOS DESTE PERSONAGEM (CORREÇÃO DO PAPER DOLL)
                LoadCharacterEquipmentFromData(existingMember, savedMember.equipmentLoadout);
                
                Debug.Log($"   ✅ {existingMember.characterName} carregado com {CountEquipmentSlots(savedMember.equipmentLoadout)} equipamentos");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Membro não encontrado: {savedMember.characterName} (ID: {savedMember.characterID})");
                Debug.Log($"      Membros disponíveis: {string.Join(", ", allMembers.ConvertAll(m => m?.characterName ?? "NULL"))}");
            }
        }
        
        // 🔥 6. SINCRONIZA com InventoryManager (equipamentos compartilhados)
        SyncCharacterEquipmentWithInventory();
        
        Debug.Log("[GDM] ✅ Party System carregado do save");
    }


    /// 🔥 Carrega equipamentos de dados serializados para um CharacterData
    /// CORRIGIDO para ScriptableObject
    /// </summary>
    private void LoadCharacterEquipmentFromData(CharacterData character, EquipmentLoadoutData savedLoadout)
    {
        if (character == null || savedLoadout == null) return;
        
        // 🔥 Garante que o personagem tem um EquipmentLoadout
        if (character.currentEquipment == null)
        {
            character.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        // 🔥 Limpa equipamentos atuais
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            character.currentEquipment.UnequipItem(slot);
        }
        
        // 🔥 Carrega cada slot salvo
        int loadedCount = 0;
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            string itemID = savedLoadout.GetItemIDForSlot(slot);
            if (!string.IsNullOrEmpty(itemID))
            {
                ItemData item = ItemRegistry.GetItem(itemID);
                if (item != null)
                {
                    // 🔥 VERIFICA SE O PERSONAGEM PODE EQUIPAR
                    if (character.CanEquipItem(item))
                    {
                        character.currentEquipment.EquipItem(item);
                        loadedCount++;
                        Debug.Log($"      🔧 {character.characterName}: {item.itemName} equipado em {slot}");
                    }
                    else
                    {
                        Debug.LogWarning($"      ⚠️ {character.characterName} não pode equipar {item.itemName}!");
                    }
                }
                else
                {
                    Debug.LogWarning($"      ⚠️ Item não encontrado: {itemID} para slot {slot}");
                }
            }
        }
        
        Debug.Log($"   🔧 {character.characterName}: {loadedCount} equipamentos carregados");
    }

    /// <summary>
    /// 🔥 Conta quantos slots de equipamento estão preenchidos
    /// </summary>
    private int CountEquipmentSlots(EquipmentLoadoutData loadout)
    {
        if (loadout == null) return 0;
        
        int count = 0;
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            if (!string.IsNullOrEmpty(loadout.GetItemIDForSlot(slot)))
            {
                count++;
            }
        }
        
        return count;
    }

   /// <summary>
    /// 🔥 SINCRONIZA equipamentos dos personagens com InventoryManager
    /// CORRIGIDO para CharacterData
    /// </summary>
    private void SyncCharacterEquipmentWithInventory()
    {
        if (PartyManager.Instance == null || InventoryManager.Instance == null) return;
        
        Debug.Log("[GDM] 🔄 Sincronizando equipamentos com InventoryManager...");
        
        var partyManager = PartyManager.Instance;
        var inventoryManager = InventoryManager.Instance;
        
        // 🔥 1. Obtém personagem ativo
        var activeMember = partyManager.GetActiveMember();
        if (activeMember == null || activeMember.currentEquipment == null)
        {
            Debug.LogWarning("[GDM] ⚠️ Nenhum personagem ativo com equipamentos para sincronizar");
            return;
        }
        
        // 🔥 2. Limpa equipamentos compartilhados no InventoryManager
        var sharedEquipment = inventoryManager.Equipment;
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            sharedEquipment.UnequipItem(slot);
        }
        
        // 🔥 3. Copia equipamentos do personagem ativo para o inventário compartilhado
        int syncCount = 0;
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var charItem = activeMember.currentEquipment.GetItemInSlot(slot);
            if (charItem != null)
            {
                // 🔥 Verifica se o item existe no inventário compartilhado
                if (inventoryManager.HasItem(charItem, 1))
                {
                    sharedEquipment.EquipItem(charItem);
                    syncCount++;
                    Debug.Log($"   🔗 {charItem.itemName} sincronizado do {activeMember.characterName}");
                }
                else
                {
                    Debug.LogWarning($"   ⚠️ {charItem.itemName} não está no inventário compartilhado");
                    
                    // 🔥 TENTA ADICIONAR AO INVENTÁRIO (para consistência)
                    if (inventoryManager.AddItem(charItem, 1))
                    {
                        sharedEquipment.EquipItem(charItem);
                        syncCount++;
                        Debug.Log($"   🔧 {charItem.itemName} adicionado ao inventário e sincronizado");
                    }
                }
            }
        }
        
        // 🔥 4. Notifica a UI
        if (syncCount > 0)
        {
            inventoryManager.OnEquipmentChanged?.Invoke();
            Debug.Log($"[GDM] ✅ {syncCount} equipamentos sincronizados com InventoryManager");
        }
        else
        {
            Debug.Log("[GDM] ⚠️ Nenhum equipamento para sincronizar");
        }
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

    public void UpdatePlayerName(string playerName)
    {
        if (currentGameData == null) CreateNewGame();
        
        // 1. Atualiza no GameData (isso é específico por save)
        currentGameData.playerData.playerName = playerName;
        
        // 2. 🔥🔥🔥 NÃO modificar o CharacterData ScriptableObject!
        // Em vez disso, salvar em PlayerPrefs com chave da instância
        
        // Chave ÚNICA por instância de jogo
        string instanceKey = $"Instance_{currentGameInstanceID}_PlayerName";
        PlayerPrefs.SetString(instanceKey, playerName);
        PlayerPrefs.Save();
        
        Debug.Log($"[GDM] ✅ Nome do jogador salvo: '{playerName}' (Instância: {currentGameInstanceID})");
        
        // 3. 🔥 NOTIFICAR a UI para atualizar (mas não modificar asset)
        // Isso será feito via GameData ou PlayerPrefs
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
        // Se temos uma instância ativa, usa a estrutura multi-instância
        if (currentGameInstanceID != -1 && !string.IsNullOrEmpty(currentGameInstancePath))
        {
            string instanceSavePath = Path.Combine(currentGameInstancePath, "SaveSlots");
            
            // 🔥 CORREÇÃO: Só criar se for o caminho da instância
            if (!Directory.Exists(instanceSavePath))
            {
                Directory.CreateDirectory(instanceSavePath);
            }
            
            return Path.Combine(instanceSavePath, $"slot_{slot}.json");
        }
        else
        {
            // 🔥🔥🔥 NUNCA MAIS CRIAR INSTÂNCIA DE EMERGÊNCIA!
            Debug.LogError($"[GDM] ❌❌❌ BLOQUEIO DE SAVE: Nenhuma instância ativa!");
            Debug.LogError($"   currentGameInstanceID: {currentGameInstanceID}");
            Debug.LogError($"   currentGameInstancePath: {currentGameInstancePath}");
            Debug.LogError($"   TRACE: {System.Environment.StackTrace}");
            
            // 🔥 RETORNA NULL para forçar erro e impedir save
            return null;
        }
    }

    private bool SaveFileExistsInInstance(int slot)
    {
        if (currentGameInstanceID == -1 || string.IsNullOrEmpty(currentGameInstancePath))
            return false;
            
        string filePath = Path.Combine(currentGameInstancePath, "SaveSlots", $"slot_{slot}.json");
        return File.Exists(filePath);
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

    /// 🔥 NOVO: Atualiza dados do Party System antes de salvar
    /// </summary>
    private void UpdatePartyDataBeforeSave()
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[GDM] PartyManager.Instance is null - skipping party save");
            return;
        }
        
        try
        {
            Debug.Log("[GDM] ⭐ Verificando estado do Party System (NÃO MODIFICA currentGameData!)");
            
            var partyManager = PartyManager.Instance;
            var activeMember = partyManager.GetActiveMember();
            
            // ⭐⭐ APENAS LOG - NÃO MODIFICA!
            Debug.Log($"[GDM]   • Party members: {partyManager.GetAllMembers().Count}");
            Debug.Log($"[GDM]   • Active member: {activeMember?.characterName ?? "NULL"}");
            Debug.Log($"[GDM]   • Active index: {partyManager.GetActiveIndex()}");
            
            // ⭐⭐ NÃO FAÇA NADA MAIS! O SaveGame() cuida do snapshot!
            // NÃO modifique characterEquipment.partyMembers
            // NÃO modifique activeCharacterLoadout
            // NÃO modifique activeCharacterIndex
            
            Debug.Log("[GDM] ✅ Verificação concluída (dados NÃO modificados)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GDM] ❌ Erro verificando Party System: {e.Message}");
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
        
        // 2. PARTY SYSTEM - placeholder
        if (PartyManager.Instance != null && snapshot.playerData != null)
        {
            UpdatePartyDataToSnapshot(snapshot);
        }
        // 🔥🔥🔥 FIM DO BLOCO
        
        Debug.Log("[GDM] ✅ Snapshot atualizado com dados atuais");
    }

    /// <summary>
    /// 🔥🔥🔥 CRÍTICO: Atualiza snapshot com dados ATUAIS do Party System
    /// CORRIGIDO para trabalhar com CharacterData ScriptableObject
    /// </summary>
    private void UpdatePartyDataToSnapshot(GameData snapshot)
    {
        if (PartyManager.Instance == null || snapshot?.playerData?.characterEquipment == null) 
            return;
        
        Debug.Log("[GDM] 🔄 Atualizando snapshot com Party System...");
        
        var partyManager = PartyManager.Instance;
        var partySnapshot = snapshot.playerData.characterEquipment;
        
        // 🔥 1. Salva o personagem ativo
        partySnapshot.activeCharacterIndex = partyManager.GetActiveIndex();
        
        // 🔥 2. Limpa membros antigos do snapshot
        partySnapshot.partyMembers.Clear();
        
        // 🔥 3. Salva TODOS os membros da party NO SNAPSHOT
        var allMembers = partyManager.GetAllMembers();
        
        foreach (var member in allMembers)
        {
            if (member == null) continue;
            
            var partyMemberData = new PartyMemberData
            {
                // 🔥 IDENTIFICAÇÃO: Use o nome do ScriptableObject como ID
                characterID = member.name, // ScriptableObject.name funciona como ID única
                characterName = member.characterName,
                level = member.currentLevel,
                
                // 🔥 CORREÇÃO: CharacterData usa currentHP/currentMP, não currentHealth/currentMana
                currentHP = member.currentHP,
                currentMP = member.currentMP,
                maxHP = member.GetCurrentMaxHP(),
                maxMP = member.GetCurrentMaxMP(),
                
                // 🔥 STATS BASE do CharacterData
                baseAttack = member.baseAttack,
                baseDefense = member.baseDefense,
                baseMagicAttack = member.baseMagicAttack,
                baseMagicDefense = member.baseMagicDefense,
                baseSpeed = member.baseSpeed,
                baseCrit = 5, // Valor default do CharacterData
                baseEvasion = 5, // Valor default do CharacterData
                
                // 🔥 EXPERIENCE (não tem no CharacterData, usar defaults)
                experience = 0,
                experienceToNextLevel = 100,
                
                // 🔥 SKILLS (não tem lista no CharacterData, usar array startingSkills)
                unlockedSkillIDs = GetSkillIDsFromCharacterData(member)
            };
            
            // 🔥🔥🔥 4. SALVA EQUIPAMENTOS DESTE PERSONAGEM (CORREÇÃO DO PAPER DOLL)
            if (member.currentEquipment != null)
            {
                partyMemberData.equipmentLoadout = SaveCharacterEquipmentToData(member.currentEquipment);
            }
            
            partySnapshot.partyMembers.Add(partyMemberData);
        }
        
        // 🔥 5. Atualiza stats do jogador com o personagem ativo
        var activeMember = partyManager.GetActiveMember();
        if (activeMember != null)
        {
            snapshot.playerData.characterCurrentHP = activeMember.currentHP;  // CORREÇÃO
            snapshot.playerData.characterCurrentMP = activeMember.currentMP;  // CORREÇÃO
            snapshot.playerData.characterMaxHP = activeMember.GetCurrentMaxHP();
            snapshot.playerData.characterMaxMP = activeMember.GetCurrentMaxMP();
        }
        
        Debug.Log($"[GDM]   • Snapshot party: {allMembers.Count} members, active: {activeMember?.characterName}");
    }

    /// <summary>
    /// 🔥 Helper: Extrai IDs das skills do CharacterData
    /// </summary>
    private List<string> GetSkillIDsFromCharacterData(CharacterData character)
    {
        var skillIDs = new List<string>();
        
        if (character.startingSkills != null)
        {
            foreach (var skill in character.startingSkills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillName))
                {
                    skillIDs.Add(skill.skillName);
                }
            }
        }
        
        return skillIDs;
    }

    /// <summary>
    /// 🔥 Converte EquipmentLoadout para dados serializáveis
    /// </summary>
    private EquipmentLoadoutData SaveCharacterEquipmentToData(InventoryManager.EquipmentLoadout equipment)
    {
        var loadoutData = new EquipmentLoadoutData();
        
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var equippedItem = equipment.GetItemInSlot(slot);
            if (equippedItem != null)
            {
                loadoutData.SetItemIDForSlot(slot, equippedItem.itemID);
            }
        }
        
        return loadoutData;
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

        // ⭐⭐ DEBUG TEMPORÁRIO: Forçar sincronização
        if (GameInstanceManager.Instance != null && currentGameInstanceID == -1)
        {
            var instanceManager = GameInstanceManager.Instance;
            if (instanceManager.HasSelectedGameInstance())
            {
                currentGameInstanceID = instanceManager.currentGameInstanceID;
                currentGameInstancePath = instanceManager.currentGameInstancePath;
                currentGameInstanceName = instanceManager.currentGameInstanceName;
                Debug.Log($"║ 🔄 SINCRONIZADO: Instância {currentGameInstanceID} carregada");
            }
        }
        
        if (currentGameInstanceID != -1 && !string.IsNullOrEmpty(currentGameInstancePath))
        {
            Debug.Log($"║ INSTÂNCIA ATUAL: {currentGameInstanceID} ('{currentGameInstanceName}')");
            Debug.Log($"║ CAMINHO: {currentGameInstancePath}");
            Debug.Log($"║ ───────────────────────────────────────────────────");
            
            // Verifica slots 1-3 da instância atual
            for (int slot = 1; slot <= 3; slot++)
            {
                string filePath = Path.Combine(currentGameInstancePath, "SaveSlots", $"slot_{slot}.json");
                bool exists = File.Exists(filePath);
                
                if (exists)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        GameData data = JsonUtility.FromJson<GameData>(json);
                        Debug.Log($"║ SLOT {slot}: ✅ EXISTE");
                        Debug.Log($"║   • Player: {data.playerData.playerName}");
                        Debug.Log($"║   • Level: {data.playerData.level}");
                        Debug.Log($"║   • Currency: {data.inventoryData.currency}");
                        Debug.Log($"║   • SaveSlot in file: {data.saveSlot}");
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"║ SLOT {slot}: ⚠️ CORRUPTED - {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"║ SLOT {slot}: ❌ NÃO EXISTE");
                }
            }
        }
        else
        {
            Debug.Log($"║ NENHUMA INSTÂNCIA SELECIONADA");
            Debug.Log($"║ Usando sistema antigo...");
            
            // Fallback para sistema antigo
            for (int i = 1; i <= 5; i++)
            {
                string filePath = GetSaveFilePath(i);
                if (File.Exists(filePath))
                {
                    Debug.Log($"║ SLOT {i}: ✅ EXISTE (sistema antigo)");
                }
                else
                {
                    Debug.Log($"║ SLOT {i}: ❌ NÃO EXISTE");
                }
            }
        }
        
        Debug.Log($"║");
        Debug.Log($"║ currentGameData in memory:");
        Debug.Log($"║   • Currency: {currentGameData?.inventoryData?.currency}");
        Debug.Log($"║   • SaveSlot: {currentGameData?.saveSlot}");
        Debug.Log($"╚══════════════════════════════════════════════════════╝");
    }

    [ContextMenu("🔍 Debug: Verify Save Slot Corruption")]
    public void DebugVerifySaveSlotCorruption()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO DE CORRUPÇÃO DE SAVE SLOT               ║");
        Debug.Log("╠══════════════════════════════════════════════════════════╣");
        
        // 1. Estado atual na memória
        Debug.Log($"║  📊 MEMÓRIA (currentGameData):");
        Debug.Log($"║     • saveSlot: {currentGameData?.saveSlot ?? -999}");
        Debug.Log($"║     • isNewGame: {currentGameData?.isNewGame ?? false}");
        Debug.Log($"║     • currency: {currentGameData?.inventoryData?.currency ?? -1}");
        Debug.Log($"║     • HashCode: {currentGameData?.GetHashCode() ?? 0}");
        
        // 2. Verificar slots 1-3 no DISCO
        Debug.Log($"║");
        Debug.Log($"║  💾 DISCO (arquivos salvos):");
        for (int slot = 1; slot <= 3; slot++)
        {
            string filePath = GetSaveFilePath(slot);
            bool exists = File.Exists(filePath);
            
            if (exists)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    GameData diskData = JsonUtility.FromJson<GameData>(json);
                    
                    Debug.Log($"║     Slot {slot}: ✅ EXISTE");
                    Debug.Log($"║        • saveSlot no arquivo: {diskData.saveSlot}");
                    Debug.Log($"║        • isNewGame: {diskData.isNewGame}");
                    Debug.Log($"║        • currency: {diskData.inventoryData.currency}");
                    Debug.Log($"║        • Player: {diskData.playerData.playerName}");
                    
                    // Comparar com memória
                    if (diskData.saveSlot != currentGameData?.saveSlot)
                    {
                        Debug.Log($"║        ⚠️  INCONSISTÊNCIA: Disco={diskData.saveSlot}, Memória={currentGameData?.saveSlot}");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"║     Slot {slot}: ❌ CORROMPIDO - {e.Message}");
                }
            }
            else
            {
                Debug.Log($"║     Slot {slot}: ❌ NÃO EXISTE");
            }
        }
        
        // 3. Quem está chamando SaveGame?
        Debug.Log($"║");
        Debug.Log($"║  🔗 CHAMADORES:");
        Debug.Log($"║     • SaveLoadManager: {(SaveLoadManager.Instance != null ? "✅" : "❌")}");
        Debug.Log($"║     • InventoryManager: {(InventoryManager.Instance != null ? "✅" : "❌")}");
        Debug.Log($"║     • PartyManager: {(PartyManager.Instance != null ? "✅" : "❌")}");
        
        Debug.Log("╚══════════════════════════════════════════════════════════╝");
    }

    [ContextMenu("🔧 Fix: Force Save Slot to 1")]
    public void DebugForceFixSaveSlot()
    {
        if (currentGameData == null)
        {
            Debug.LogError("❌ currentGameData é NULL!");
            return;
        }
        
        int oldSlot = currentGameData.saveSlot;
        currentGameData.saveSlot = 1;
        
        Debug.Log($"✅ SaveSlot corrigido: {oldSlot} → {currentGameData.saveSlot}");
        
        // Salva imediatamente para testar
        SaveGame(1);
    }

    [ContextMenu("🔍 Debug: Deep Check Save Slot 1")]
    public void DebugDeepCheckSlot1()
    {
        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO PROFUNDA DO SLOT 1                  ║");
        Debug.Log("╠══════════════════════════════════════════════════════╣");
        
        // 1. Caminho do arquivo no disco
        string filePath = GetSaveFilePath(1);
        Debug.Log($"║  📁 Caminho do Arquivo: {filePath}");
        Debug.Log($"║  📂 Existe no disco? {(File.Exists(filePath) ? "✅ SIM" : "❌ NÃO")}");
        
        // 2. Conteúdo do arquivo no disco (Slot 1)
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                GameData fileData = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"║  💾 Dados no DISCO (Slot 1):");
                Debug.Log($"║     • saveSlot: {fileData.saveSlot}");
                Debug.Log($"║     • playerName: {fileData.playerData.playerName}");
                Debug.Log($"║     • currency: {fileData.inventoryData.currency}");
                Debug.Log($"║     • Data: {fileData.saveDate}");
            }
            catch (Exception e)
            {
                Debug.LogError($"║  ❌ Erro ao ler arquivo: {e.Message}");
            }
        }
        
        // 3. Estado na MEMÓRIA (currentGameData)
        Debug.Log($"║");
        Debug.Log($"║  🧠 Dados na MEMÓRIA (currentGameData):");
        Debug.Log($"║     • saveSlot: {currentGameData?.saveSlot ?? -1}");
        Debug.Log($"║     • playerName: {currentGameData?.playerData?.playerName ?? "NULL"}");
        Debug.Log($"║     • currency: {currentGameData?.inventoryData?.currency ?? -1}");
        
        // 4. Comparação
        Debug.Log($"║");
        Debug.Log($"║  🔄 COMPARAÇÃO:");
        if (currentGameData != null && File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            GameData fileData = JsonUtility.FromJson<GameData>(json);
            
            bool slotsMatch = currentGameData.saveSlot == fileData.saveSlot;
            bool namesMatch = currentGameData.playerData.playerName == fileData.playerData.playerName;
            
            Debug.Log($"║     • Slots iguais? {(slotsMatch ? "✅" : "❌")} (Mem={currentGameData.saveSlot}, Disco={fileData.saveSlot})");
            Debug.Log($"║     • Nomes iguais? {(namesMatch ? "✅" : "❌")}");
            
            if (!slotsMatch)
            {
                Debug.LogError($"║  ⚠️ INCONSISTÊNCIA CRÍTICA: O slot em memória não bate com o arquivo!");
            }
        }
        
        Debug.Log("╚══════════════════════════════════════════════════════╝");
    }
    [ContextMenu("🔍 Debug: Verify Save Isolation")]
    public void DebugVerifySaveIsolation()
    {
        Debug.Log("╔══════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO DE ISOLAMENTO DE SAVES          ║");
        Debug.Log("╠══════════════════════════════════════════════════╣");
        
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("║  ❌ GameInstanceManager não encontrado!");
            Debug.Log("╚══════════════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📊 Total de instâncias: {GameInstanceManager.Instance.GetInstanceCount()}");
        Debug.Log($"║");
        
        // Para cada instância, verificar saves
        foreach (var instance in GameInstanceManager.Instance.gameInstances)
        {
            Debug.Log($"║  🎮 INSTÂNCIA {instance.instanceID}: {instance.instanceName}");
            Debug.Log($"║     Path: {instance.saveFolderPath}");
            
            // Verificar slots 1-3
            for (int slot = 1; slot <= 3; slot++)
            {
                string savePath = Path.Combine(instance.saveFolderPath, "SaveSlots", $"slot_{slot}.json");
                bool exists = File.Exists(savePath);
                
                if (exists)
                {
                    try
                    {
                        string json = File.ReadAllText(savePath);
                        GameData data = JsonUtility.FromJson<GameData>(json);
                        
                        Debug.Log($"║       [Slot {slot}]: ✅ {data.playerData.playerName} (Lv {data.playerData.level})");
                        Debug.Log($"║                   💰 {data.inventoryData.currency} moedas");
                    }
                    catch
                    {
                        Debug.LogError($"║       [Slot {slot}]: ❌ CORROMPIDO");
                    }
                }
                else
                {
                    Debug.Log($"║       [Slot {slot}]: ⬜ VAZIO");
                }
            }
            
            Debug.Log($"║");
        }
        
        Debug.Log($"║  🎯 INSTÂNCIA ATUAL:");
        Debug.Log($"║     ID: {currentGameInstanceID}");
        Debug.Log($"║     Nome: {currentGameInstanceName}");
        Debug.Log($"║     Path: {currentGameInstancePath}");
        
        Debug.Log("╚══════════════════════════════════════════════════╝");
    }

    [ContextMenu("🧹 Migrar Sistema Antigo para Multi-Instância")]
    public void MigrateOldSavesToMultiInstance()
    {
        Debug.Log("🔄 INICIANDO MIGRAÇÃO DE SAVES ANTIGOS...");
        
        string oldSavesPath = Path.Combine(Application.persistentDataPath, "saves");
        
        if (!Directory.Exists(oldSavesPath))
        {
            Debug.Log("✅ Nenhum save antigo encontrado para migrar");
            return;
        }
        
        // Verifica se temos instância ativa
        if (currentGameInstanceID == -1)
        {
            Debug.LogError("❌ Nenhuma instância selecionada! Crie uma instância primeiro.");
            return;
        }
        
        // 🔥 CORREÇÃO: Lista específica de arquivos para migrar
        string[] savePatterns = new string[] { "save_*.json", "slot_*.json" };
        List<string> filesToMigrate = new List<string>();
        
        foreach (string pattern in savePatterns)
        {
            filesToMigrate.AddRange(Directory.GetFiles(oldSavesPath, pattern));
        }
        
        // 🔥 FILTRAR: Remover slot_0.json (auto-save não deve ser migrado)
        filesToMigrate.RemoveAll(f => f.Contains("slot_0") || f.Contains("save_0"));
        
        Debug.Log($"📁 Arquivos para migrar: {filesToMigrate.Count}");
        
        int migratedCount = 0;
        int skippedCount = 0;
        
        foreach (string oldFile in filesToMigrate)
        {
            try
            {
                string fileName = Path.GetFileName(oldFile);
                
                // 🔥 CORREÇÃO: Extrair slot corretamente
                int slotNumber = -1;
                
                if (fileName.StartsWith("save_") && fileName.EndsWith(".json"))
                {
                    string slotStr = fileName.Replace("save_", "").Replace(".json", "");
                    if (int.TryParse(slotStr, out slotNumber))
                    {
                        // Mantém mesmo número
                    }
                }
                else if (fileName.StartsWith("slot_") && fileName.EndsWith(".json"))
                {
                    string slotStr = fileName.Replace("slot_", "").Replace(".json", "");
                    if (int.TryParse(slotStr, out slotNumber))
                    {
                        // Já está no formato novo
                    }
                }
                
                // 🔥 PULAR slot 0 (auto-save) e slots inválidos
                if (slotNumber <= 0 || slotNumber > 10) // Ajuste 10 para seu máximo
                {
                    Debug.LogWarning($"   ⚠️ Pulando {fileName} (slot inválido: {slotNumber})");
                    skippedCount++;
                    continue;
                }
                
                // Novo caminho
                string newPath = GetSaveFilePath(slotNumber);
                
                // 🔥 VERIFICAR se já existe (não sobrescrever)
                if (File.Exists(newPath))
                {
                    Debug.LogWarning($"   ⚠️ {fileName} já existe no destino - pulando");
                    skippedCount++;
                    continue;
                }
                
                // Copia arquivo
                File.Copy(oldFile, newPath, false); // false = não sobrescrever
                migratedCount++;
                
                Debug.Log($"   ✅ {fileName} → {Path.GetFileName(newPath)} (slot {slotNumber})");
            }
            catch (Exception e)
            {
                Debug.LogError($"   ❌ Erro ao migrar {Path.GetFileName(oldFile)}: {e.Message}");
            }
        }
        
        Debug.Log($"📊 RESUMO: {migratedCount} migrados, {skippedCount} pulados, {filesToMigrate.Count} arquivos");
        
        // 🔥 CORREÇÃO: Limpar pasta ANTES de deletar
        if (migratedCount > 0)
        {
            Debug.Log("🔄 Limpando arquivos migrados da pasta antiga...");
            
            // Deletar APENAS os arquivos que foram migrados
            foreach (string oldFile in filesToMigrate)
            {
                try
                {
                    // Verificar se foi migrado com sucesso
                    string fileName = Path.GetFileName(oldFile);
                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                        Debug.Log($"   🗑️ Removido: {fileName}");
                    }
                }
                catch { }
            }
            
            // 🔥 TENTAR deletar pasta se estiver vazia
            try
            {
                if (Directory.GetFiles(oldSavesPath).Length == 0 && 
                    Directory.GetDirectories(oldSavesPath).Length == 0)
                {
                    Directory.Delete(oldSavesPath, false);
                    Debug.Log("✅ Pasta antiga 'saves' deletada (estava vazia)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Pasta 'saves' não está vazia - mantendo");
                    Debug.Log($"   Arquivos restantes: {string.Join(", ", Directory.GetFiles(oldSavesPath))}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Não foi possível deletar pasta: {e.Message}");
            }
        }
        
        Debug.Log("✅ Migração finalizada!");
    }

    [ContextMenu("🔧 Forçar Sistema Multi-Instância")]
    public void ForceMultiInstanceSystem()
    {
        Debug.Log("🔧 FORÇANDO SISTEMA MULTI-INSTÂNCIA...");
        
        // 1. Verificar se está usando caminho antigo
        if (saveFolderPath.Contains("saves") && !saveFolderPath.Contains("GameInstances"))
        {
            Debug.LogWarning("⚠️ Usando caminho antigo - corrigindo...");
            
            if (currentGameInstanceID != -1 && GameInstanceManager.Instance != null)
            {
                var instance = GameInstanceManager.Instance.GetInstanceInfo(currentGameInstanceID);
                if (instance != null)
                {
                    saveFolderPath = Path.Combine(instance.saveFolderPath, "SaveSlots");
                    Debug.Log($"✅ Caminho atualizado: {saveFolderPath}");
                }
            }
        }
        
        // 2. Deletar slot_0.json se existir (auto-save antigo)
        string slot0Path = GetSaveFilePath(0);
        if (File.Exists(slot0Path))
        {
            Debug.LogWarning($"⚠️ Deletando slot_0.json (auto-save antigo)...");
            File.Delete(slot0Path);
        }
        
        // 3. Atualizar PlayerPrefs para usar novo sistema
        if (currentGameInstanceID != -1)
        {
            PlayerPrefs.SetInt("UsingMultiInstanceSystem", 1);
            PlayerPrefs.Save();
            Debug.Log("✅ PlayerPrefs atualizado para multi-instância");
        }
        
        Debug.Log("✅ Sistema multi-instância forçado!");
    }

    [ContextMenu("🔍 Debug: Verify Current Instance Path")]
    public void DebugVerifyCurrentInstancePath()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║  🔍 CURRENT INSTANCE PATH VERIFICATION   ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║  Current Instance ID: {currentGameInstanceID}");
        Debug.Log($"║  Current Instance Name: {currentGameInstanceName}");
        Debug.Log($"║  Current Instance Path: {currentGameInstancePath}");
        Debug.Log($"║  Save Folder Path: {saveFolderPath}");
        Debug.Log($"║");
        
        if (GameInstanceManager.Instance != null)
        {
            Debug.Log($"║  GameInstanceManager Active ID: {GameInstanceManager.Instance.currentGameInstanceID}");
            Debug.Log($"║  GameInstanceManager Active Path: {GameInstanceManager.Instance.currentGameInstancePath}");
            
            bool pathsMatch = currentGameInstancePath == GameInstanceManager.Instance.currentGameInstancePath;
            Debug.Log($"║  Paths Match: {(pathsMatch ? "✅ YES" : "❌ NO")}");
        }
        
        Debug.Log("╚═══════════════════════════════════════════╝");
    }

    [ContextMenu("🔍 Debug: Who is Calling SaveGame?")]
    public void DebugWhoIsCallingSaveGame()
    {
        Debug.Log("🔍 STACK TRACE do último SaveGame:");
        Debug.Log(System.Environment.StackTrace);
    }

    [ContextMenu("🔍 Debug: Verificar Nome do Jogador")]
    public void DebugCheckPlayerName()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  🎮 GAMEDATAMANAGER - NOME DO JOGADOR               ║");
        Debug.Log("╠═══════════════════════════════════════════════════════╣");
        
        if (currentGameData == null)
        {
            Debug.LogError("║  ❌ currentGameData é NULL!");
        }
        else
        {
            Debug.Log($"║  currentGameData.playerData.playerName: '{currentGameData.playerData?.playerName ?? "NULL"}'");
            Debug.Log($"║  currentGameData.saveSlot: {currentGameData.saveSlot}");
            Debug.Log($"║  currentGameData.isNewGame: {currentGameData.isNewGame}");
        }
        
        Debug.Log($"║");
        Debug.Log($"║  Instance ID: {currentGameInstanceID}");
        Debug.Log($"║  Instance Name: '{currentGameInstanceName}'");
        Debug.Log($"║  Instance Path: '{currentGameInstancePath}'");
        
        // Verificar PlayerPrefs
        if (currentGameInstanceID != -1)
        {
            string instanceKey = $"Instance_{currentGameInstanceID}_PlayerName";
            string savedName = PlayerPrefs.GetString(instanceKey, "NOT FOUND");
            Debug.Log($"║");
            Debug.Log($"║  PlayerPrefs key: '{instanceKey}'");
            Debug.Log($"║  PlayerPrefs value: '{savedName}'");
        }
        else
        {
            string genericKey = "CurrentPlayerName";
            string savedName = PlayerPrefs.GetString(genericKey, "NOT FOUND");
            Debug.Log($"║");
            Debug.Log($"║  PlayerPrefs (generic): '{genericKey}'");
            Debug.Log($"║  Value: '{savedName}'");
        }
        
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
    }
    
}