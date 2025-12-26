using UnityEngine;
using System.IO;      
using System;         
using System.Collections;

public enum GameStartMode
{
    NewGame,
    Continue,
    LoadSpecific
}

/// <summary>
/// SISTEMA CENTRALIZADO DE SAVE/LOAD - VERSÃO CORRIGIDA PARA 1 ÚNICO GAMEDATAMANAGER
/// ✅ Corrigido: Espera GameDataManager estar pronto antes de qualquer operação
/// ✅ Corrigido: Não tenta acessar Instance em Awake/Start
/// ✅ Corrigido: Sistema de inicialização assíncrona segura
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("Referências (Auto-detecta se vazio)")]
    public PlayerController player;
    public BoatController boat;
    public CameraManager cameraManager;
    
    [Header("Configurações de Auto-Save")]
    public bool enableAutoSave = true;
    public int autoSaveSlot = 0;
    public float autoSaveInterval = 60f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Multi-Instance Support")]
    private bool hasInitializedGameInstance = false;

    
    
    // Estado do sistema
    private bool isSystemReady = false;
    private bool hasPerformedInitialLoad = false;
    
    // Controle de salvamento
    private bool isSaving = false;
    private float autoSaveTimer = 0f;
    
    // Controle de estado do menu
    private bool isMenuOpen = false;
    
    // Variáveis estáticas para comunicação com o menu
    private static GameStartMode requestedStartMode = GameStartMode.Continue;
    private static int requestedLoadSlot = -1;

    // Variáveis estáticas para armazenar dados do novo jogo
    private static string newGamePlayerName = "Player";
    private static string newGameName = "My Adventure";

    //  Método para o menu passar informações
    public static void RequestNewGameWithDetails(string playerName, string gameName)
    {
        requestedStartMode = GameStartMode.NewGame;
        requestedLoadSlot = -1;
        newGamePlayerName = playerName;
        newGameName = gameName;
        
        Debug.Log($"[SLM] Novo jogo solicitado: '{gameName}' por '{playerName}'");
    }

    // ⭐⭐ NOVO: Getter para pegar os dados
    public static (string playerName, string gameName) GetNewGameDetails()
    {
        return (newGamePlayerName, newGameName);
    }
    
    // Singleton
    public static SaveLoadManager Instance { get; private set; }
    
    // ============================================
    // MÉTODOS PÚBLICOS PARA O MENU CHAMAR
    // ============================================
    
    public static void RequestNewGame()
    {
        requestedStartMode = GameStartMode.NewGame;
        requestedLoadSlot = -1;
        Debug.Log("🆕 NOVO JOGO solicitado pelo menu");
    }
    
    public static void RequestContinue()
    {
        requestedStartMode = GameStartMode.Continue;
        requestedLoadSlot = -1;
        Debug.Log("▶️ CONTINUE solicitado pelo menu");
    }
    
    public static void RequestLoadSlot(int slot)
    {
        requestedStartMode = GameStartMode.LoadSpecific;
        requestedLoadSlot = slot;
        Debug.Log($"📂 LOAD do slot {slot} solicitado pelo menu");
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        Debug.Log("=== SaveLoadManager Inicializado ===");
    }
    
    void Start()
    {
        // ✅ NÃO acessa GameDataManager aqui
        StartCoroutine(SafeInitialization());
    }
    
    // ============================================
    // 🔥 INICIALIZAÇÃO SEGURA E PROFISSIONAL
    // ============================================
    
    IEnumerator SafeInitialization()
    {
        Debug.Log("🔧 SaveLoadManager: Iniciando inicialização SEGURA...");
        
        // PASSO 1: Aguarda 1 frame
        yield return null;

        // PASSO 1.5: Verifica se temos GameInstanceManager
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("❌ GameInstanceManager não encontrado! O sistema multi-instância não funcionará.");
        }
        else
        {
            Debug.Log($"✅ GameInstanceManager encontrado: {GameInstanceManager.Instance.gameObject.name}");
        }
        
        // PASSO 2: Encontra componentes da cena
        FindComponents();
        
        // PASSO 3: ✅ AGUARDA GameDataManager estar disponível (ATÉ 5 SEGUNDOS)
        Debug.Log("⏳ Aguardando GameDataManager...");
        
        float timeout = 5f;
        float elapsed = 0f;
        
        while (GameDataManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            
            // Tenta encontrar manualmente a cada 0.2s
            if (Mathf.FloorToInt(elapsed * 5) != Mathf.FloorToInt((elapsed - Time.deltaTime) * 5))
            {
                GameDataManager found = FindFirstObjectByType<GameDataManager>();
                if (found != null)
                {
                    Debug.Log($"✅ GameDataManager encontrado manualmente: {found.gameObject.name}");
                    // O singleton será configurado automaticamente
                }
            }
            
            yield return null;
        }
        
        // PASSO 4: Verifica se conseguiu
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌❌❌ TIMEOUT: GameDataManager não disponível após 5 segundos!");
            Debug.LogError("🔥 SaveLoadManager NÃO SERÁ INICIALIZADO!");
            yield break;
        }
        
        Debug.Log($"✅ GameDataManager pronto: {GameDataManager.Instance.gameObject.name}");
        Debug.Log($"   Scene: {GameDataManager.Instance.gameObject.scene.name}");
        
        // PASSO 5: Aguarda mais 0.3s para garantir que GameDataManager.Start() rodou
        yield return new WaitForSeconds(0.3f);
        
        // PASSO 6: Sistema pronto
        isSystemReady = true;
        Debug.Log("✅✅✅ SaveLoadManager TOTALMENTE INICIALIZADO!");
        
        // PASSO 7: Executa auto-load se necessário
        yield return StartCoroutine(AutoLoadOnStart());
    }
    
    void FindComponents()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
        
        if (boat == null)
        {
            boat = FindFirstObjectByType<BoatController>();
        }
        
        if (cameraManager == null)
        {
            cameraManager = FindFirstObjectByType<CameraManager>();
        }
    }
    
    // ============================================
    // SISTEMA DE NOTIFICAÇÃO DE MENU
    // ============================================
    
    public void OnMenuStateChanged(bool menuOpen)
    {
        isMenuOpen = menuOpen;
    }
    
    // ============================================
    // AUTO-LOAD INTELIGENTE (PRESERVADO DO ORIGINAL)
    // ============================================
    
    IEnumerator AutoLoadOnStart()
    {
        if (hasPerformedInitialLoad)
        {
            yield break;
        }
        
        // ⭐⭐ NOVO FLUXO: Verifica se veio do menu ou se precisa criar instância
        Debug.Log("🔄 AutoLoadOnStart - Novo fluxo com instâncias");
        
        // Se não tem GameInstanceManager, usa sistema antigo (compatibilidade)
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogWarning("⚠️ Usando sistema antigo (sem GameInstanceManager)");
            yield return StartCoroutine(LegacyAutoLoad());
            hasPerformedInitialLoad = true;
            yield break;
        }
        
        // ⭐⭐ NOVO SISTEMA COM INSTÂNCIAS
        GameStartMode mode = requestedStartMode;
        
        switch (mode)
        {
            case GameStartMode.NewGame:
                yield return StartCoroutine(StartNewGameWithInstance());
                break;
                
            case GameStartMode.Continue:
                yield return StartCoroutine(ContinueLastGameInstance());
                break;
                
            case GameStartMode.LoadSpecific:
                Debug.LogWarning("⚠️ LoadSpecific não suportado em multi-instância - usando Continue");
                yield return StartCoroutine(ContinueLastGameInstance());
                break;
        }
        
        hasPerformedInitialLoad = true;
        
        // Reset para próxima vez
        requestedStartMode = GameStartMode.Continue;
        requestedLoadSlot = -1;
    }

    // ⭐⭐ NOVO: Sistema antigo (para compatibilidade)
    IEnumerator LegacyAutoLoad()
    {
        GameStartMode mode = requestedStartMode;
        
        switch (mode)
        {
            case GameStartMode.NewGame:
                yield return StartCoroutine(StartNewGame());
                break;
                
            case GameStartMode.Continue:
                yield return StartCoroutine(ContinueLastSave());
                break;
                
            case GameStartMode.LoadSpecific:
                yield return StartCoroutine(LoadSpecificSlot(requestedLoadSlot));
                break;
        }
    }
    
    // ============================================
    // 🆕 FLUXO: NOVO JOGO (PRESERVADO)
    // ============================================
    IEnumerator StartNewGame()
    {
        if (!EnsureSystemReady()) yield break;
        
        GameDataManager.Instance.CreateNewGame();
        yield return StartCoroutine(LoadNewGamePositions());
    }

    // Inicia novo jogo COM instância
   IEnumerator StartNewGameWithInstance()
    {
        if (!EnsureSystemReady()) yield break;
        
        if (GameInstanceManager.Instance != null)
        {
            if (!GameInstanceManager.Instance.CanCreateNewInstance())
            {
                Debug.LogError("❌ Não é possível criar novo jogo - limite de instâncias atingido!");
                yield break;
            }
            
            // ⭐⭐ USAR VARIÁVEL COM NOME DIFERENTE
            var newGameInfo = GetNewGameDetails();
            string gameName = newGameInfo.gameName;
            
            // Se gameName vazio, usa default
            if (string.IsNullOrEmpty(gameName))
                gameName = "My Adventure";
            
            Debug.Log($"✅ Criando nova instância: '{gameName}'");
            
            int newInstanceID = GameInstanceManager.Instance.CreateNewGameInstance(gameName, "Normal");
            
            if (newInstanceID == -1)
            {
                Debug.LogError("❌ Falha ao criar nova instância de jogo!");
                yield break;
            }
            
            Debug.Log($"✅ Nova instância criada: ID={newInstanceID}, Nome='{gameName}'");
            
            yield return new WaitForSeconds(0.1f);
            
            if (GameDataManager.Instance != null)
            {
                // ⭐⭐ ATUALIZA NOME DO JOGADOR (APENAS NO GAMEDATA E PLAYERPREFS)
                // NÃO modifica CharacterData ScriptableObject!
                GameDataManager.Instance.UpdatePlayerName(newGameInfo.playerName);
                
                // ⭐⭐ AGORA cria o novo jogo
                GameDataManager.Instance.CreateNewGameInCurrentInstance();
            }
            
            yield return StartCoroutine(LoadNewGamePositions());
        }
        else
        {
            Debug.LogWarning("⚠️ GameInstanceManager não encontrado - usando sistema antigo");
            yield return StartCoroutine(StartNewGame());
        }
    }

    //Continua último jogo (instância)
    //Continua último jogo (instância)
    IEnumerator ContinueLastGameInstance()
    {
        if (!EnsureSystemReady()) yield break;
        
        if (GameInstanceManager.Instance != null)
        {
            // Verifica se tem instâncias salvas
            if (GameInstanceManager.Instance.GetInstanceCount() == 0)
            {
                Debug.Log("ℹ️ Nenhuma instância salva encontrada - criando novo jogo");
                yield return StartCoroutine(StartNewGameWithInstance());
                yield break;
            }
            
            // 🔥 CORREÇÃO: USA A INSTÂNCIA JÁ SELECIONADA PELO MENU
            int selectedInstanceID = GameInstanceManager.Instance.currentGameInstanceID;
            
            // Se nenhuma instância foi selecionada, pega a primeira
            if (selectedInstanceID == -1)
            {
                Debug.Log("⚠️ Nenhuma instância selecionada - usando primeira disponível");
                var firstInstance = GameInstanceManager.Instance.GetInstanceInfo(1);
                if (firstInstance != null)
                {
                    selectedInstanceID = firstInstance.instanceID;
                    GameInstanceManager.Instance.SelectGameInstance(selectedInstanceID);
                }
            }
            
            Debug.Log($"🎯 ContinueLastGameInstance: Usando instância {selectedInstanceID}");
            
            // Pega informações da instância selecionada
            var selectedInstance = GameInstanceManager.Instance.GetInstanceInfo(selectedInstanceID);
            
            if (selectedInstance != null)
            {
                // Aguarda seleção
                yield return new WaitForSeconds(0.1f);
                
                // Carrega último slot salvo desta instância
                int lastSlot = selectedInstance.lastSaveSlot;
                Debug.Log($"📂 Carregando instância {selectedInstance.instanceID}, slot {lastSlot}");
                
                bool loaded = GameDataManager.Instance.LoadGame(lastSlot);
                
                if (!loaded)
                {
                    Debug.LogWarning($"⚠️ Não foi possível carregar slot {lastSlot} - criando novo");
                    GameDataManager.Instance.CreateNewGameInCurrentInstance();
                }
                
                yield return StartCoroutine(LoadSavedPositions());
            }
            else
            {
                Debug.LogError($"❌ Instância {selectedInstanceID} não encontrada!");
                yield return StartCoroutine(StartNewGameWithInstance());
            }
        }
        else
        {
            // Fallback: sistema antigo
            Debug.LogWarning("⚠️ GameInstanceManager não encontrado - usando sistema antigo");
            yield return StartCoroutine(ContinueLastSave());
        }
    }
    
    // ============================================
    // ▶️ FLUXO: CONTINUE (PRESERVADO)
    // ============================================
    IEnumerator ContinueLastSave()
    {
        if (!EnsureSystemReady()) yield break;
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        
        if (gameData == null || gameData.isNewGame)
        {
            yield return StartCoroutine(StartNewGame());
            yield break;
        }
        
        yield return StartCoroutine(LoadSavedPositions());
    }
    
    // ============================================
    // 📂 FLUXO: LOAD ESPECÍFICO (PRESERVADO)
    // ============================================
    IEnumerator LoadSpecificSlot(int slot)
    {
        if (!EnsureSystemReady()) yield break;
        
        bool loaded = GameDataManager.Instance.LoadGame(slot);
        
        if (!loaded)
        {
            Debug.LogError($"❌ Falha ao carregar slot {slot}");
            yield return StartCoroutine(StartNewGame());
            yield break;
        }
        
        yield return StartCoroutine(LoadSavedPositions());
    }
    
    // ============================================
    // 🛡️ MÉTODO DE SEGURANÇA
    // ============================================
    
    private bool EnsureSystemReady()
    {
        if (!isSystemReady)
        {
            Debug.LogError("❌ Sistema ainda não está pronto!");
            return false;
        }
        
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager não disponível!");
            return false;
        }
        
        return true;
    }
    
    // ============================================
    // LOAD DE POSIÇÕES (PRESERVADO INTEGRALMENTE)
    // ============================================
    
    private IEnumerator LoadNewGamePositions()
    {
        yield return null;
        
        if (player != null && player.gameObject.activeInHierarchy)
        {
            GameDataManager.Instance.UpdatePlayerPosition(player.transform.position);
            GameDataManager.Instance.UpdatePlayerHealth(player.currentHealth, player.maxHealth);
        }
        
        if (boat != null && boat.gameObject.activeInHierarchy)
        {
            // 🔥 CORREÇÃO: Temporariamente desabilita auto-save para novo jogo
            bool originalAutoSaveSetting = GameDataManager.Instance.enableAutoSaveOnEvents;
            GameDataManager.Instance.enableAutoSaveOnEvents = false;
            
            GameDataManager.Instance.UpdateBoatData(
                boat.GetCurrentBoatHealth(),
                boat.GetMaxBoatHealth(),
                boat.transform.position,
                boat.isBoatDestroyed,
                true
            );
            
            // 🔥 Restaura configuração original
            GameDataManager.Instance.enableAutoSaveOnEvents = originalAutoSaveSetting;
        }
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData != null)
        {
            gameData.isNewGame = false;
        }
    }
    
    IEnumerator LoadSavedPositions()
    {
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null) yield break;
        
        // Verifica marcadores de novo jogo
        bool isNewGameMarker = gameData.playerData.lastPosition.x > 9000f || 
                               gameData.playerData.boatPosition.x > 9000f;
        
        if (isNewGameMarker)
        {
            yield return StartCoroutine(LoadNewGamePositions());
            yield break;
        }
        
        // DESABILITA FÍSICA COMPLETAMENTE
        bool boatWasKinematic = false;
        bool playerWasKinematic = false;
        bool boatHadGravity = false;
        float boatOriginalGravity = 0f;
        
        if (boat != null)
        {
            Rigidbody2D boatRb = boat.GetComponent<Rigidbody2D>();
            if (boatRb != null)
            {
                boatWasKinematic = boatRb.isKinematic;
                boatHadGravity = boatRb.gravityScale > 0;
                boatOriginalGravity = boatRb.gravityScale;
                
                boatRb.isKinematic = true;
                boatRb.gravityScale = 0f;
                boatRb.linearVelocity = Vector2.zero;
                boatRb.angularVelocity = 0f;
                boatRb.sleepMode = RigidbodySleepMode2D.StartAsleep;
                boatRb.Sleep();
            }
        }
        
        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerWasKinematic = playerRb.isKinematic;
                playerRb.isKinematic = true;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.Sleep();
            }
        }
        
        // CARREGA BARCO
        if (boat != null && gameData.playerData.hasBoat)
        {
            Vector3 boatPos = gameData.playerData.boatPosition.ToVector3();
            boat.transform.position = boatPos;
            
            Rigidbody2D boatRb = boat.GetComponent<Rigidbody2D>();
            if (boatRb != null)
            {
                boatRb.position = boatPos;
            }
            
            boat.currentBoatHealth = gameData.playerData.boatHealth;
            boat.isBoatDestroyed = gameData.playerData.isBoatDestroyed;
        }
        
        // AGUARDA FÍSICA (10 frames)
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        yield return null;
        
        // CARREGA PLAYER
        if (player != null)
        {
            if (gameData.playerData.wasInsideBoat && boat != null && !boat.isBoatDestroyed)
            {
                Vector3 savedBoatPosition = gameData.playerData.boatPosition.ToVector3();
                
                boat.isPlayerInside = true;
                
                // Desabilita física do player temporariamente
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                bool playerWasKinematicTemp = false;
                if (playerRb != null)
                {
                    playerWasKinematicTemp = playerRb.isKinematic;
                    playerRb.isKinematic = true;
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                    playerRb.Sleep();
                }
                
                // Posiciona player na posição do barco
                player.transform.position = savedBoatPosition;
                
                if (playerRb != null)
                {
                    playerRb.position = savedBoatPosition;
                }
                
                // Desativa player (está dentro do barco)
                player.gameObject.SetActive(false);
                
                // Verificação crítica
                Vector3 finalPos = player.transform.position;
                float distance = Vector3.Distance(finalPos, savedBoatPosition);
                
                if (distance > 0.01f)
                {
                    Debug.LogError($"❌ FALHA NA CORREÇÃO! Distância: {distance:F3}u");
                    player.transform.position = savedBoatPosition;
                    if (playerRb != null) playerRb.position = savedBoatPosition;
                }
                
                // Configura câmera
                if (cameraManager != null)
                {
                    cameraManager.SwitchToBoat();
                    cameraManager.enabled = true;
                    cameraManager.TeleportToTarget();
                }
            }
            else
            {
                // Player FORA do barco
                Vector3 playerPos = gameData.playerData.lastPosition.ToVector3();
                player.transform.position = playerPos;
                
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.position = playerPos;
                }
                
                player.gameObject.SetActive(true);
                
                if (boat != null)
                {
                    boat.isPlayerInside = false;
                }
                
                if (cameraManager != null)
                {
                    cameraManager.SwitchToPlayer();
                    cameraManager.enabled = true;
                    cameraManager.TeleportToTarget();
                }
            }
            
            player.SetHealth(gameData.playerData.currentHealth);
            player.SetMaxHealth(gameData.playerData.maxHealth);
        }
        
        // REABILITA FÍSICA GRADUALMENTE
        if (boat != null)
        {
            Rigidbody2D boatRb = boat.GetComponent<Rigidbody2D>();
            if (boatRb != null)
            {
                if (boatHadGravity)
                {
                    boatRb.gravityScale = 0f;
                }
                
                boatRb.isKinematic = boatWasKinematic;
                boatRb.linearVelocity = Vector2.zero;
                boatRb.angularVelocity = 0f;
                boatRb.WakeUp();
                
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                
                if (boatHadGravity)
                {
                    boatRb.gravityScale = boatOriginalGravity;
                }
            }
        }
        
        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.isKinematic = playerWasKinematic;
                playerRb.linearVelocity = Vector2.zero;
            }
        }
        
        // VERIFICAÇÃO FINAL
        yield return new WaitForFixedUpdate();
        
        if (boat != null && gameData.playerData.wasInsideBoat)
        {
            Vector3 expectedPos = gameData.playerData.boatPosition.ToVector3();
            Vector3 actualPos = boat.transform.position;
            float distance = Vector3.Distance(expectedPos, actualPos);
            
            if (distance > 0.1f)
            {
                Debug.LogError($"❌ BARCO DESLOCADO {distance:F3}u - APLICANDO CORREÇÃO!");
                
                Rigidbody2D rb = boat.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.Sleep();
                }
                
                boat.transform.position = expectedPos;
                if (rb != null) rb.position = expectedPos;
                
                if (player != null && !player.gameObject.activeSelf)
                {
                    player.transform.position = expectedPos;
                }
                
                if (cameraManager != null)
                {
                    cameraManager.TeleportToTarget();
                }
                
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.WakeUp();
                }
            }
        }
    }
    
    // ============================================
    // SALVAR ESTADO ATUAL (PRESERVADO)
    // ============================================
    
    public void SaveCurrentState(int slot = -1, bool isAutoSave = false)
    {
        if (!EnsureSystemReady())
        {
            return;
        }
        
        if (isSaving)
        {
            return;
        }
        
        if (!isAutoSave && slot == 0)
        {
            Debug.LogError("❌❌❌ SLOT 0 NÃO PERMITIDO PARA SAVE MANUAL!");
            slot = 1;
        }
        
        StartCoroutine(SaveWithProperTiming(slot, isAutoSave));
    }
    
    IEnumerator SaveWithProperTiming(int slot, bool isAutoSave)
    {
        isSaving = true;
        
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        
        UpdateCurrentStateImmediate();
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        
        if (slot == -1)
        {
            if (isAutoSave)
            {
                slot = autoSaveSlot;
            }
            else
            {
                slot = gameData?.saveSlot ?? 1;
                
                if (slot == 0)
                {
                    Debug.LogError($"⚠️ gameData.saveSlot é 0! Corrigindo para 1");
                    slot = 1;
                    if (gameData != null) gameData.saveSlot = 1;
                }
            }
            
        }
        
        GameDataManager.Instance.SaveGame(slot, isAutoSave);
        
        if (gameData != null && !isAutoSave)
        {
            gameData.saveSlot = slot;
        }
        
        isSaving = false;
    }
    
    void UpdateCurrentStateImmediate()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager.Instance é NULL!");
            return;
        }
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null)
        {
            Debug.LogError("❌ currentGameData é NULL!");
            return;
        }
        
        bool isPlayerInsideBoat = false;
        Vector3 currentBoatPosition = Vector3.zero;
        Vector3 currentPlayerPosition = Vector3.zero;
        
        if (boat != null)
        {
            currentBoatPosition = boat.transform.position;
            isPlayerInsideBoat = boat.isPlayerInside;
            
            gameData.playerData.boatPosition = new SerializableVector3(
                currentBoatPosition.x,
                currentBoatPosition.y,
                0f
            );
            gameData.playerData.boatHealth = boat.GetCurrentBoatHealth();
            gameData.playerData.boatMaxHealth = boat.GetMaxBoatHealth();
            gameData.playerData.isBoatDestroyed = boat.isBoatDestroyed;
            gameData.playerData.hasBoat = true;
        }
        
        if (isPlayerInsideBoat)
        {
            if (player != null)
            {
                gameData.playerData.lastPosition = new SerializableVector3(
                    currentBoatPosition.x,
                    currentBoatPosition.y,
                    0f
                );
                gameData.playerData.currentHealth = player.currentHealth;
                gameData.playerData.maxHealth = player.maxHealth;
                gameData.playerData.wasInsideBoat = true;
            }
        }
        else if (player != null && player.gameObject.activeSelf)
        {
            currentPlayerPosition = player.transform.position;
            
            gameData.playerData.lastPosition = new SerializableVector3(
                currentPlayerPosition.x,
                currentPlayerPosition.y,
                0f
            );
            gameData.playerData.currentHealth = player.currentHealth;
            gameData.playerData.maxHealth = player.maxHealth;
            gameData.playerData.wasInsideBoat = false;
        }
        
        if (isPlayerInsideBoat)
        {
            float distance = Vector3.Distance(
                gameData.playerData.lastPosition.ToVector3(),
                gameData.playerData.boatPosition.ToVector3()
            );
            
            if (distance > 0.01f)
            {
                Debug.LogError($"⚠️⚠️⚠️ CORREÇÃO FALHOU! Distância: {distance:F3}u");
                gameData.playerData.lastPosition = gameData.playerData.boatPosition;
            }
        }
    }
    
    // ============================================
    // CARREGAR SAVE ESPECÍFICO 
    // ============================================
    
    public void LoadFromSlot(int slot)
    {
        if (!EnsureSystemReady())
        {
            return;
        }
        
        Debug.Log($"╔═══════════════════════════════════════════════════════╗");
        Debug.Log($"║ [SLM] LoadFromSlot({slot}) - DIAGNÓSTICO COMPLETO  ║");
        Debug.Log($"╠═══════════════════════════════════════════════════════╣");
        
        // 🔥 PASSO 1: Obter caminho via GameDataManager (método correto)
        string filePath = "";
        
        if (GameDataManager.Instance != null)
        {
            try
            {
                var method = typeof(GameDataManager).GetMethod("GetSaveFilePath", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    filePath = (string)method.Invoke(GameDataManager.Instance, new object[] { slot });
                    Debug.Log($"║ ✅ Caminho obtido via GameDataManager: {filePath}");
                }
                else
                {
                    Debug.LogError("║ ❌ Método GetSaveFilePath não encontrado!");
                    filePath = System.IO.Path.Combine(Application.persistentDataPath, "saves", $"save_{slot}.json");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"║ ❌ Erro ao obter caminho: {e.Message}");
                filePath = System.IO.Path.Combine(Application.persistentDataPath, "saves", $"save_{slot}.json");
            }
        }
        else
        {
            Debug.LogError("║ ❌ GameDataManager.Instance é NULL!");
            Debug.Log($"╚═══════════════════════════════════════════════════════╝");
            return;
        }
        
        // 🔥 PASSO 2: Verificar se arquivo existe
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"║ ❌ Arquivo do slot {slot} não existe: {filePath}");
            Debug.Log($"╚═══════════════════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║ ✅ Arquivo encontrado: {filePath}");
        Debug.Log($"║ 📦 Tamanho: {new System.IO.FileInfo(filePath).Length} bytes");
        
        // 🔥 PASSO 3: Carregar via GameDataManager (método padrão)
        Debug.Log($"║ 🔄 Chamando GameDataManager.LoadGame({slot})...");
        
        bool loaded = GameDataManager.Instance.LoadGame(slot);
        
        if (!loaded)
        {
            Debug.LogError($"║ ❌ Falha ao carregar slot {slot}");
            Debug.Log($"╚═══════════════════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║ ✅ LoadGame() retornou sucesso");
        
        // 🔥 PASSO 4: Verificar se dados foram carregados corretamente
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null)
        {
            Debug.LogError($"║ ❌ Dados carregados são NULL!");
            Debug.Log($"╚═══════════════════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║ 📊 Dados carregados:");
        Debug.Log($"║    • saveSlot no arquivo: {gameData.saveSlot}");
        Debug.Log($"║    • Slot solicitado: {slot}");
        Debug.Log($"║    • Player: {gameData.playerData?.playerName ?? "NULL"}");
        Debug.Log($"║    • Currency: {gameData.inventoryData?.currency ?? -1}");
        
        // 🔥🔥🔥 VERIFICAÇÃO CRÍTICA: saveSlot está correto?
        if (gameData.saveSlot != slot)
        {
            Debug.LogError($"║ ❌ BUG CRÍTICO: saveSlot INCORRETO!");
            Debug.LogError($"║    Esperado: {slot}");
            Debug.LogError($"║    Atual: {gameData.saveSlot}");
            
            // 🔥 FORÇA CORREÇÃO MANUAL
            Debug.Log($"║ 🔧 Forçando correção: gameData.saveSlot = {slot}");
            gameData.saveSlot = slot;
            
            // 🔥 SALVA IMEDIATAMENTE PARA PERSISTIR CORREÇÃO
            GameDataManager.Instance.SaveGame(slot);
            Debug.Log($"║ 💾 Correção salva no disco");
            
            // 🔥 RECARREGA PARA GARANTIR
            GameDataManager.Instance.LoadGame(slot);
            gameData = GameDataManager.Instance.GetCurrentGameData();
            
            Debug.Log($"║ 🔍 Após recarga: saveSlot = {gameData.saveSlot}");
            
            if (gameData.saveSlot != slot)
            {
                Debug.LogError($"║ ❌ AINDA INCORRETO! Abortando load.");
                Debug.Log($"╚═══════════════════════════════════════════════════════╝");
                return;
            }
        }
        else
        {
            Debug.Log($"║ ✅ saveSlot correto: {gameData.saveSlot}");
        }
        
        // 🔥 PASSO 5: Atualizar PlayerPrefs e GameInstanceManager (VIA GameDataManager)
        int instanceID = GameDataManager.Instance.GetCurrentGameInstanceID();
        
        if (instanceID != -1)
        {
            Debug.Log($"║ 📝 Atualizando referências do slot para instância {instanceID}...");
            
            PlayerPrefs.SetInt($"LastSaveSlot_Instance_{instanceID}", slot);
            PlayerPrefs.Save();
            Debug.Log($"║    • PlayerPrefs atualizado");
            
            if (GameInstanceManager.Instance != null)
            {
                GameInstanceManager.Instance.UpdateLastSaveSlot(instanceID, slot);
                Debug.Log($"║    • GameInstanceManager atualizado");
                
                // 🔥 VERIFICAÇÃO IMEDIATA
                var instance = GameInstanceManager.Instance.GetInstanceInfo(instanceID);
                if (instance != null)
                {
                    Debug.Log($"║    • Verificação: instance.lastSaveSlot = {instance.lastSaveSlot}");
                    
                    if (instance.lastSaveSlot != slot)
                    {
                        Debug.LogError($"║    ❌ FALHA: lastSaveSlot não foi atualizado!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"║ ⚠️ Sem instância ativa - usando sistema antigo");
            PlayerPrefs.SetInt("LastSaveSlot", slot);
            PlayerPrefs.Save();
        }
        
        // 🔥 PASSO 6: Carregar posições (corrotina)
        Debug.Log($"║ 🎯 Iniciando LoadSavedPositions()...");
        StartCoroutine(LoadSavedPositions());
        
        Debug.Log($"║ ✅ LoadFromSlot() COMPLETO!");
        Debug.Log($"╚═══════════════════════════════════════════════════════╝");
    }
    // ============================================
    // MÉTODOS SIMPLIFICADOS - ENTRAR/SAIR BARCO (PRESERVADOS)
    // ============================================
    
    public void OnPlayerEnterBoat()
    {
        if (GameDataManager.Instance != null && boat != null)
        {
            var gameData = GameDataManager.Instance.GetCurrentGameData();
            if (gameData != null)
            {
                gameData.playerData.wasInsideBoat = true;
                //SaveCurrentState();
            }
        }
    }
    
    public void OnPlayerExitBoat()
    {
        if (GameDataManager.Instance != null && boat != null)
        {
            var gameData = GameDataManager.Instance.GetCurrentGameData();
            if (gameData != null)
            {
                gameData.playerData.wasInsideBoat = false;
                //SaveCurrentState();
            }
        }
    }
    
    // ============================================
    // SISTEMA DE AUTO-SAVE CENTRALIZADO
    // ============================================
    
    void Update()
    {
        if (enableAutoSave && isSystemReady && !isSaving)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                TriggerAutoSave();
                autoSaveTimer = 0f;
            }
        }
    }
    
    void TriggerAutoSave()
    {
        if (!isSystemReady || isSaving) return;
        
        SaveCurrentState(autoSaveSlot, true);
    }
    
    // ============================================
    // DEBUG (PRESERVADOS)
    // ============================================
    
    [ContextMenu("[SLM] 🔍 Debug: Verificar Estado Atual")]
    public void DebugCheckCurrentState()
    {
        Debug.Log("🔍 SAVELOADMANAGER - ESTADO ATUAL:");
        Debug.Log($"   isSystemReady: {isSystemReady}");
        Debug.Log($"   isSaving: {isSaving}");
        Debug.Log($"   hasPerformedInitialLoad: {hasPerformedInitialLoad}");
        
        if (GameDataManager.Instance != null)
        {
            Debug.Log($"✅ GameDataManager:");
            Debug.Log($"   • Nome: {GameDataManager.Instance.gameObject.name}");
            Debug.Log($"   • Scene: {GameDataManager.Instance.gameObject.scene.name}");
            Debug.Log($"   • Ativo: {GameDataManager.Instance.gameObject.activeSelf}");
            
            var gameData = GameDataManager.Instance.GetCurrentGameData();
            if (gameData != null)
            {
                Debug.Log($"   • saveSlot: {gameData.saveSlot}");
                Debug.Log($"   • isNewGame: {gameData.isNewGame}");
            }
        }
        else
        {
            Debug.LogError("❌ GameDataManager.Instance é NULL!");
        }
        
        Debug.Log($"👤 Player: {(player != null ? "OK" : "NULL")}");
        Debug.Log($"🚤 Boat: {(boat != null ? "OK" : "NULL")}");
        Debug.Log($"📷 CameraManager: {(cameraManager != null ? "OK" : "NULL")}");
    }
    
    [ContextMenu("🔍 Mostrar Estado Atual")]
    public void ShowCurrentState()
    {
        Debug.Log("=== ESTADO ATUAL ===");
        Debug.Log($"Sistema pronto: {isSystemReady}");
        Debug.Log($"Salvando: {isSaving}");
        Debug.Log($"Load inicial feito: {hasPerformedInitialLoad}");
        Debug.Log($"Menu aberto: {isMenuOpen}");
        
        if (player != null)
        {
            Debug.Log($"👤 Player:");
            Debug.Log($"  - Ativo: {player.gameObject.activeSelf}");
            Debug.Log($"  - Posição REAL: {player.transform.position}");
            Debug.Log($"  - Vida: {player.currentHealth}/{player.maxHealth}");
        }
        
        if (boat != null)
        {
            Debug.Log($"🚤 Barco:");
            Debug.Log($"  - Ativo: {boat.gameObject.activeSelf}");
            Debug.Log($"  - Posição REAL: {boat.transform.position}");
            Debug.Log($"  - Player dentro: {boat.isPlayerInside}");
        }
        
        var gameData = GameDataManager.Instance?.GetCurrentGameData();
        if (gameData != null)
        {
            Debug.Log($"💾 Save Data:");
            Debug.Log($"  - isNewGame: {gameData.isNewGame}");
            Debug.Log($"  - wasInsideBoat: {gameData.playerData.wasInsideBoat}");
            Debug.Log($"  - Player pos SALVA: {gameData.playerData.lastPosition}");
            Debug.Log($"  - Boat pos SALVA: {gameData.playerData.boatPosition}");
        }
    }
    
    [ContextMenu("💾 Forçar Save Agora")]
    public void ForceSaveNow()
    {
        SaveCurrentState();
    }
    
    [ContextMenu("📂 Forçar Load Slot 1")]
    public void ForceLoadSlot1()
    {
        LoadFromSlot(1);
    }

    [ContextMenu("🛑 Disable Auto-Save (For Tests)")]
    public void DisableAutoSaveForTests()
    {
        enableAutoSave = false;
        autoSaveTimer = 0f;
        Debug.Log("🛑 Auto-save DESABILITADO para testes");
    }

    [ContextMenu("▶️ Enable Auto-Save")]
    public void EnableAutoSave()
    {
        enableAutoSave = true;
        autoSaveTimer = 0f;
        Debug.Log("▶️ Auto-save HABILITADO");
    }

    [ContextMenu("⏱️ Set Short Interval (30s - Test)")]
    public void SetShortTestInterval()
    {
        autoSaveInterval = 30f;
        Debug.Log("⏱️ Intervalo do auto-save: 30s (para testes)");
    }

    [ContextMenu("⏱️ Set Normal Interval (180s)")]
    public void SetNormalInterval()
    {
        autoSaveInterval = 180f;
        Debug.Log("⏱️ Intervalo do auto-save: 180s (normal)");
    }

    [ContextMenu("⏱️ Set Long Interval (300s)")]
    public void SetLongInterval()
    {
        autoSaveInterval = 300f;
        Debug.Log("⏱️ Intervalo do auto-save: 300s (longo)");
    }

    /// <summary>
    /// Método público para UI controlar auto-save
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        enableAutoSave = enabled;
        autoSaveTimer = 0f;
        Debug.Log($"[SLM] Auto-save {(enabled ? "HABILITADO" : "DESABILITADO")}");
    }

    /// <summary>
    /// Método público para UI controlar intervalo
    /// </summary>
    public void SetAutoSaveInterval(float seconds)
    {
        autoSaveInterval = Mathf.Clamp(seconds, 30f, 600f);
        Debug.Log($"[SLM] Intervalo do auto-save: {autoSaveInterval}s");
    }

    [ContextMenu("🔍 Debug: Check Instance Integration")]
    public void DebugCheckInstanceIntegration()
    {
        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 SAVELOAD MANAGER - INTEGRAÇÃO INSTÂNCIAS       ║");
        Debug.Log("╠══════════════════════════════════════════════════════╣");
        
        Debug.Log($"║  GameInstanceManager: {(GameInstanceManager.Instance != null ? "✅" : "❌")}");
        
        if (GameInstanceManager.Instance != null)
        {
            Debug.Log($"║  Instâncias disponíveis: {GameInstanceManager.Instance.GetInstanceCount()}");
            Debug.Log($"║  Instância atual: ID={GameInstanceManager.Instance.currentGameInstanceID}");
            Debug.Log($"║  Nome atual: {GameInstanceManager.Instance.currentGameInstanceName}");
            Debug.Log($"║  Tem seleção: {GameInstanceManager.Instance.HasSelectedGameInstance()}");
        }
        
        Debug.Log($"║  GameDataManager: {(GameDataManager.Instance != null ? "✅" : "❌")}");
        
        if (GameDataManager.Instance != null)
        {
            Debug.Log($"║  hasPerformedInitialLoad: {hasPerformedInitialLoad}");
            Debug.Log($"║  requestedStartMode: {requestedStartMode}");
        }
        
        Debug.Log("╚══════════════════════════════════════════════════════╝");
    }


}