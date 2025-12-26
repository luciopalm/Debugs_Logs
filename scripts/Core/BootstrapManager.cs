using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 🚀 Sistema de Bootstrap - Inicializa managers e carrega cena inicial
/// DEVE estar na ManagerScene e ser a primeira cena no Build Settings
/// </summary>
public class BootstrapManager : MonoBehaviour
{
    [Header("🎮 Configuração Inicial")]
    [Tooltip("Cena que será carregada após inicializar os managers")]
    [SerializeField] private string initialSceneName = "MainMenu";
    
    [Tooltip("Se true, vai direto para GameScene (útil para testes)")]
    [SerializeField] private bool skipToGameScene = false;
    
    [Header("⏱️ Timings")]
    [SerializeField] private float delayBeforeLoadingScene = 0.5f;
    
    [Header("🔍 Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showDetailedLogs = false;
    
    // Singleton
    private static BootstrapManager _instance;
    public static BootstrapManager Instance => _instance;
    
    private bool isInitialized = false;
    
    // ============================================
    // INICIALIZAÇÃO
    // ============================================
    
    void Awake()
    {
        // Singleton check
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("⚠️ Duplicata de BootstrapManager detectada! Destruindo...");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Log("═══════════════════════════════════════════");
        Log("🚀 BOOTSTRAP MANAGER INICIANDO");
        Log("═══════════════════════════════════════════");
    }
    
    void Start()
    {
        StartCoroutine(InitializeGame());
    }
    
    // ============================================
    // SEQUÊNCIA DE INICIALIZAÇÃO
    // ============================================
    
    private IEnumerator InitializeGame()
    {
        Log("📋 Etapa 1/4: Validando managers críticos...");
        
        // Aguarda 1 frame para garantir que todos os Awake() rodaram
        yield return null;
        
        // Valida managers
        if (!ValidateManagers())
        {
            Debug.LogError("❌ FALHA CRÍTICA: Managers não encontrados!");
            Debug.LogError("   Certifique-se que os managers estão na ManagerScene:");
            Debug.LogError("   - GameDataManager");
            Debug.LogError("   - InventoryManager");
            Debug.LogError("   - PartyManager");
            yield break;
        }
        
        Log("✅ Etapa 1/4: Managers validados com sucesso!");
        
        // ============================================
        
        Log("📋 Etapa 2/4: Inicializando sistemas...");
        yield return InitializeSystems();
        Log("✅ Etapa 2/4: Sistemas inicializados!");
        
        // ============================================
        
        Log("📋 Etapa 3/4: Carregando dados salvos...");
        yield return LoadSavedData();
        Log("✅ Etapa 3/4: Dados carregados!");
        
        // ============================================
        
        Log("📋 Etapa 4/4: Preparando cena inicial...");
        yield return new WaitForSeconds(delayBeforeLoadingScene);
        
        string sceneToLoad = skipToGameScene ? "GameScene" : initialSceneName;
        Log($"🎬 Carregando cena: {sceneToLoad}");
        
        SceneManager.LoadScene(sceneToLoad);
        
        isInitialized = true;
        
        Log("═══════════════════════════════════════════");
        Log("✅ BOOTSTRAP COMPLETO!");
        Log("═══════════════════════════════════════════");
    }
    
    // ============================================
    // VALIDAÇÃO DE MANAGERS
    // ============================================
    
    private bool ValidateManagers()
    {
        bool allOk = true;
        int managersFound = 0;
        int managersExpected = 3;
        
        // GameDataManager
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager não encontrado!");
            LogDetailed("   Certifique-se que existe um GameObject com GameDataManager na ManagerScene");
            allOk = false;
        }
        else
        {
            Log("   ✅ GameDataManager OK");
            LogDetailed($"      GameObject: {GameDataManager.Instance.gameObject.name}");
            LogDetailed($"      Scene: {GameDataManager.Instance.gameObject.scene.name}");
            managersFound++;
        }
        
        // InventoryManager
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager não encontrado!");
            LogDetailed("   Certifique-se que existe um GameObject com InventoryManager na ManagerScene");
            allOk = false;
        }
        else
        {
            Log("   ✅ InventoryManager OK");
            LogDetailed($"      GameObject: {InventoryManager.Instance.gameObject.name}");
            LogDetailed($"      Scene: {InventoryManager.Instance.gameObject.scene.name}");
            managersFound++;
        }
        
        // PartyManager
        if (PartyManager.Instance == null)
        {
            Debug.LogError("❌ PartyManager não encontrado!");
            LogDetailed("   Certifique-se que existe um GameObject com PartyManager na ManagerScene");
            allOk = false;
        }
        else
        {
            Log("   ✅ PartyManager OK");
            LogDetailed($"      GameObject: {PartyManager.Instance.gameObject.name}");
            LogDetailed($"      Scene: {PartyManager.Instance.gameObject.scene.name}");
            managersFound++;
        }
        
        Log($"📊 Managers encontrados: {managersFound}/{managersExpected}");
        
        return allOk;
    }
    
    // ============================================
    // INICIALIZAÇÃO DE SISTEMAS
    // ============================================
    
    private IEnumerator InitializeSystems()
    {
        // GameDataManager já inicializa sozinho no Awake/Start
        
        // InventoryManager - forçar inicialização se necessário
        if (InventoryManager.Instance != null)
        {
            LogDetailed("   Inicializando InventoryManager...");
            // Se tiver método de inicialização pública, chame aqui
        }
        
        // PartyManager - inicializar party
        if (PartyManager.Instance != null)
        {
            LogDetailed("   Inicializando PartyManager...");
            // Se tiver método de inicialização pública, chame aqui
        }
        
        yield return null;
    }
    
    // ============================================
    // CARREGAMENTO DE DADOS
    // ============================================
    
    private IEnumerator LoadSavedData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ Não foi possível carregar dados - GameDataManager não encontrado");
            yield break;
        }
        
        // GameDataManager já carrega dados no Start()
        // Apenas aguardamos um frame para garantir que terminou
        yield return null;
        
        LogDetailed("   Dados do jogo carregados");
        
        // Verificar se há save
        if (GameDataManager.Instance.SaveFileExists(1))
        {
            LogDetailed("   ✅ Save file encontrado");
        }
        else
        {
            LogDetailed("   ℹ️ Nenhum save encontrado - novo jogo será criado quando necessário");
        }
    }
    
    // ============================================
    // MÉTODOS UTILITÁRIOS
    // ============================================
    
    public bool IsInitialized() => isInitialized;
    
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Bootstrap] {message}");
        }
    }
    
    private void LogDetailed(string message)
    {
        if (showDebugLogs && showDetailedLogs)
        {
            Debug.Log($"[Bootstrap] {message}");
        }
    }
    
    // ============================================
    // DEBUG METHODS
    // ============================================
    
    [ContextMenu("🔍 Validar Managers")]
    public void DebugValidateManagers()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("🔍 VALIDAÇÃO MANUAL DE MANAGERS");
        Debug.Log("═══════════════════════════════════════════");
        
        ValidateManagers();
        
        Debug.Log("═══════════════════════════════════════════");
    }
    
    [ContextMenu("📋 Listar Todas as Cenas do Build")]
    public void DebugListBuildScenes()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("📋 CENAS NO BUILD SETTINGS");
        Debug.Log("═══════════════════════════════════════════");
        
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"Total de cenas: {sceneCount}");
        Debug.Log("");
        
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            string marker = (i == 0) ? "← PRIMEIRA (MANAGER)" : "";
            Debug.Log($"   [{i}] {sceneName} {marker}");
        }
        
        Debug.Log("═══════════════════════════════════════════");
    }
    
    [ContextMenu("🎮 Simular Reload do Jogo")]
    public void DebugReloadGame()
    {
        Debug.Log("🔄 Recarregando ManagerScene...");
        SceneManager.LoadScene(0); // Recarrega a primeira cena (ManagerScene)
    }
}