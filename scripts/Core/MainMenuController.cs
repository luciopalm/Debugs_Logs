using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis de UI")]
    public GameObject mainMenuPanel;
    public GameObject newGamePanel;
    public GameObject loadGamePanel;
    
    [Header("Botões Menu Principal")]
    public Button startNewGameButton;
    public Button continueButton;
    public Button loadGameButton;
    public Button quitButton;
    
    [Header("Botões Painel Novo Jogo")]
    public Button startGameButton;
    public Button backButton;
    public TMP_InputField playerNameInput;

    [Header("Load Game Panel Elements")] // ⭐ NOVO
    public Transform instancesContainer;
    public GameObject instanceButtonPrefab;
    public Button loadBackButton;
    public TextMeshProUGUI loadPanelTitle;
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private List<GameInstanceButton> instanceButtons = new List<GameInstanceButton>();
    
    // ⭐ NOVA CLASSE para botões de instância
    [System.Serializable]
    public class GameInstanceButton
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI infoText;
        public int instanceID;
    }
    
    void Start()
    {
        // Bootstrap garante que Managers existe antes de MainMenu carregar
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[MainMenu] CRITICAL: GameDataManager not found! Check bootstrap order.");
            return;
        }
        
        InitializeMenu();
    }
    
    private void InitializeMenu()
    {
        SetupButtons();
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        
        Debug.Log("[MainMenu] Initialized with Multi-Instance support");
    }
    
    void SetupButtons()
    {
        // START NEW GAME (mantido igual)
        if (startNewGameButton != null)
        {
            startNewGameButton.onClick.RemoveAllListeners();
            startNewGameButton.onClick.AddListener(() => {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (newGamePanel != null) 
                {
                    newGamePanel.SetActive(true);
                    if (playerNameInput != null) playerNameInput.text = "";
                }
            });
        }
        
        // ⭐⭐ NOVO: LOAD GAME BUTTON
        if (loadGameButton != null)
        {
            loadGameButton.onClick.RemoveAllListeners();
            loadGameButton.onClick.AddListener(() => {
                ShowLoadGamePanel();
            });
        }
        
        // CONTINUE (modificado para usar sistema de instâncias)
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => {
                OnContinueClicked();
            });
        }
        
        // QUIT (mantido igual)
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
        }
        
        // START GAME (dentro do painel novo jogo) - MODIFICADO
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() => {
                OnStartNewGameClicked();
            });
        }
        
        // BACK (novo jogo)
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                if (newGamePanel != null) newGamePanel.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
        }
        
        // ⭐⭐ NOVO: LOAD BACK BUTTON
        if (loadBackButton != null)
        {
            loadBackButton.onClick.RemoveAllListeners();
            loadBackButton.onClick.AddListener(() => {
                if (loadGamePanel != null) loadGamePanel.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
        }
    }

    // ⭐⭐ NOVO: Mostra painel de carregar jogo
    private void ShowLoadGamePanel()
    {
        if (loadGamePanel == null)
        {
            Debug.LogError("[MainMenu] Load game panel not assigned!");
            return;
        }
        
        // Esconde outros painéis
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (newGamePanel != null) newGamePanel.SetActive(false);
        
        // Mostra painel de load
        loadGamePanel.SetActive(true);
        
        // Atualiza lista de instâncias
        RefreshInstancesList();
        
        Debug.Log("[MainMenu] Load game panel shown");
    }

    // ⭐⭐ NOVO: Atualiza lista de instâncias
    private void RefreshInstancesList()
    {
        if (instancesContainer == null || instanceButtonPrefab == null)
        {
            Debug.LogError("[MainMenu] Instances container or prefab not assigned!");
            return;
        }
        
        // Limpa botões antigos
        foreach (Transform child in instancesContainer)
        {
            Destroy(child.gameObject);
        }
        instanceButtons.Clear();
        
        // Verifica se tem GameInstanceManager
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("[MainMenu] GameInstanceManager not found!");
            return;
        }
        
        // Cria botões para cada instância
        foreach (var instance in GameInstanceManager.Instance.gameInstances)
        {
            GameObject buttonObj = Instantiate(instanceButtonPrefab, instancesContainer);
            
            // ⭐⭐ NOVO: Busca os componentes TextMeshPro nos filhos
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            
            if (texts.Length >= 2)
            {
                // Primeiro texto = Nome
                texts[0].text = instance.GetDisplayName(); // "Nome (Dificuldade)"
                
                // Segundo texto = Informações  
                texts[1].text = $"{instance.GetPlayTimeFormatted()} • {instance.GetLastPlayedFormatted()}";
                
                Debug.Log($"✅ Configurado botão: {instance.instanceName} - {texts[0].text}");
            }
            else
            {
                Debug.LogError($"❌ Botão não tem 2 TextMeshPro! Encontrados: {texts.Length}");
                
                // DEBUG: Lista todos os textos encontrados
                foreach (var text in texts)
                {
                    Debug.Log($"   Texto encontrado: {text.gameObject.name}");
                }
            }
            
            // Configura botão
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                int instanceID = instance.instanceID;
                button.onClick.AddListener(() => {
                    OnInstanceSelected(instanceID);
                });
            }
            
            // Posiciona manualmente (chama seu método)
            PositionInstanceButtons();
        }
        
        // Atualiza título
        if (loadPanelTitle != null)
        {
            int instanceCount = GameInstanceManager.Instance.GetInstanceCount();
            loadPanelTitle.text = $"Carregar Jogo ({instanceCount} salvo{(instanceCount != 1 ? "s" : "")})";
        }
    }

    /// <summary>
    /// Posiciona manualmente os botões na lista (sem Layout Group)
    /// </summary>
    private void PositionInstanceButtons()
    {
        if (instancesContainer == null) return;
        
        float buttonHeight = 80f;      // Altura do seu botão
        float spacing = 5f;           // Espaço entre botões
        float currentY = 0f;          // Posição Y atual
        
        // Para cada botão filho do container
        for (int i = 0; i < instancesContainer.childCount; i++)
        {
            Transform child = instancesContainer.GetChild(i);
            RectTransform rt = child.GetComponent<RectTransform>();
            
            if (rt == null) continue;
            
            // Configura Anchor para Top-Left
            rt.anchorMin = new Vector2(0, 1);     // Top-Left
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);         // Pivot no canto superior esquerdo
            
            // Posição: começa no topo e vai descendo
            rt.anchoredPosition = new Vector2(0, -currentY);
            rt.sizeDelta = new Vector2(720, buttonHeight); // Largura fixa, altura do botão
            
            // Atualiza posição Y para o próximo botão
            currentY += buttonHeight + spacing;
        }
        
        // ⭐⭐ IMPORTANTE: Ajusta a altura do Content para o ScrollView funcionar
        RectTransform contentRT = instancesContainer.GetComponent<RectTransform>();
        if (contentRT != null)
        {
            contentRT.sizeDelta = new Vector2(720, currentY);
            Debug.Log($"📏 Content height updated to: {currentY}px");
        }
    }

    // Quando uma instância é selecionada
    private void OnInstanceSelected(int instanceID)
    {
        Debug.Log($"╔═══════════════════════════════════════════╗");
        Debug.Log($"║  🎮 INSTANCE SELECTED: ID={instanceID}");
        Debug.Log($"╠═══════════════════════════════════════════╣");
        
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("║  ❌ GameInstanceManager não encontrado!");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            return;
        }
        
        // 🔥 1. SELECIONA A INSTÂNCIA NO GAMEINSTANCEMANAGER
        bool selected = GameInstanceManager.Instance.SelectGameInstance(instanceID);
        
        if (!selected)
        {
            Debug.LogError($"║  ❌ Falha ao selecionar instância {instanceID}!");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ Instância {instanceID} selecionada no GameInstanceManager");
        
        // 🔥🔥🔥 2. VERIFICA SE FOI SELECIONADA CORRETAMENTE
        int currentID = GameInstanceManager.Instance.currentGameInstanceID;
        string currentName = GameInstanceManager.Instance.currentGameInstanceName;
        string currentPath = GameInstanceManager.Instance.currentGameInstancePath;
        
        Debug.Log($"║  📋 Verificação:");
        Debug.Log($"║     ID: {currentID}");
        Debug.Log($"║     Nome: {currentName}");
        Debug.Log($"║     Path: {currentPath}");
        
        if (currentID != instanceID)
        {
            Debug.LogError($"║  ❌ CRÍTICO: ID não mudou! Esperado={instanceID}, Atual={currentID}");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            return;
        }
        
        // 🔥🔥🔥 3. FORÇA SINCRONIZAÇÃO COM GAMEDATAMANAGER
        if (GameDataManager.Instance != null)
        {
            Debug.Log($"║  🔄 Sincronizando GameDataManager...");
            GameDataManager.Instance.OnGameInstanceChanged(instanceID, currentPath);
            
            // 🔥 AGUARDA PARA GARANTIR SINCRONIZAÇÃO
            StartCoroutine(VerifyAndLoadScene(instanceID));
        }
        else
        {
            Debug.LogError("║  ❌ GameDataManager não encontrado!");
            Debug.Log($"╚═══════════════════════════════════════════╝");
        }
    }

    private System.Collections.IEnumerator VerifyAndLoadScene(int expectedInstanceID)
    {
        // Aguarda 3 frames para garantir sincronização TOTAL
        yield return null;
        yield return null;
        yield return null;
        
        Debug.Log($"║  🔍 Verificação final antes de carregar cena:");
        
        // 🔥 Verifica GameInstanceManager
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError($"║  ❌ GameInstanceManager PERDIDO!");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            yield break;
        }
        
        int gimID = GameInstanceManager.Instance.currentGameInstanceID;
        string gimName = GameInstanceManager.Instance.currentGameInstanceName;
        string gimPath = GameInstanceManager.Instance.currentGameInstancePath;
        
        Debug.Log($"║     GameInstanceManager:");
        Debug.Log($"║        ID: {gimID}");
        Debug.Log($"║        Nome: {gimName}");
        Debug.Log($"║        Path existe: {System.IO.Directory.Exists(gimPath)}");
        
        // 🔥 Verifica GameDataManager
        if (GameDataManager.Instance != null)
        {
            int gdmID = GameDataManager.Instance.GetCurrentGameInstanceID();
            Debug.Log($"║     GameDataManager ID: {gdmID}");
        }
        
        if (gimID == expectedInstanceID)
        {
            Debug.Log($"║  ✅ Sincronização confirmada!");
            Debug.Log($"║  🚀 Carregando cena...");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            
            // 🔥🔥🔥 IMPORTANTE: Salva o ID no PlayerPrefs ANTES de carregar cena
            PlayerPrefs.SetInt("PendingInstanceID", expectedInstanceID);
            PlayerPrefs.SetString("PendingInstancePath", gimPath);
            PlayerPrefs.SetString("PendingInstanceName", gimName);
            PlayerPrefs.Save();
            
            Debug.Log($"[MainMenu] 💾 Instância salva no PlayerPrefs para persistência");
            
            SaveLoadManager.RequestContinue();
            LoadGameScene();
        }
        else
        {
            Debug.LogError($"║  ❌ FALHA NA SINCRONIZAÇÃO!");
            Debug.LogError($"║     Esperado: {expectedInstanceID}");
            Debug.LogError($"║     Atual: {gimID}");
            Debug.Log($"╚═══════════════════════════════════════════╝");
        }
    }

    // 🔥🔥🔥 NOVO MÉTODO
    private System.Collections.IEnumerator LoadSceneAfterSync()
    {
    yield return null; // Aguarda sincronização
    
    SaveLoadManager.RequestContinue();
    LoadGameScene();
}

    // ⭐⭐ NOVO: Handler para botão Continuar
    private void OnContinueClicked()
    {
        Debug.Log("[MainMenu] Continue clicked");
        
        if (GameInstanceManager.Instance == null)
        {
            Debug.LogError("[MainMenu] GameInstanceManager not found - using legacy system");
            SaveLoadManager.RequestContinue();
            LoadGameScene();
            return;
        }
        
        // Verifica se tem instâncias
        if (GameInstanceManager.Instance.GetInstanceCount() == 0)
        {
            Debug.Log("[MainMenu] No saved games - showing new game panel");
            
            // Mostra painel de novo jogo
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (newGamePanel != null) 
            {
                newGamePanel.SetActive(true);
                if (playerNameInput != null) playerNameInput.text = "";
            }
            return;
        }
        
        // ⭐ Tenta usar a última instância jogada (ou a primeira)
        // Por enquanto, seleciona a primeira disponível
        var instances = GameInstanceManager.Instance.gameInstances;
        if (instances.Count > 0)
        {
            int instanceID = instances[0].instanceID;
            GameInstanceManager.Instance.SelectGameInstance(instanceID);
            
            SaveLoadManager.RequestContinue();
            LoadGameScene();
        }
    }

    // ⭐⭐ NOVO: Handler para botão Iniciar Novo Jogo
    private void OnStartNewGameClicked()
    {
        string playerName = "Player";
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            playerName = playerNameInput.text.Trim();
        
        string gameName = "My Adventure";
        gameName = $"{playerName}'s Adventure";

        Debug.Log($"[MainMenu] Starting new game: Player='{playerName}', Game='{gameName}'");
        
        // ⭐ Usa o novo sistema de instâncias
        SaveLoadManager.RequestNewGameWithDetails(playerName, gameName);
        LoadGameScene();
    }
    void LoadGameScene()
    {
        StartCoroutine(LoadGameSceneCoroutine());
    }
    
    private IEnumerator LoadGameSceneCoroutine()
    {
        Debug.Log("[MainMenu] Loading game scene...");
        
        // GameScene deve ser índice 2 no Build Settings (0=Managers, 1=MainMenu, 2=GameScene)
        int gameSceneIndex = 2;
        
        AsyncOperation loadGame = SceneManager.LoadSceneAsync(gameSceneIndex, LoadSceneMode.Single);
        loadGame.allowSceneActivation = true;
        
        while (!loadGame.isDone)
        {
            yield return null;
        }
        
        Debug.Log("[MainMenu] Game scene loaded!");
    }
}