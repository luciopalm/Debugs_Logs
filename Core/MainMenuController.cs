using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis de UI")]
    public GameObject mainMenuPanel;
    public GameObject newGamePanel;
    
    [Header("Botões Menu Principal")]
    public Button startNewGameButton;
    public Button continueButton;
    public Button quitButton;
    
    [Header("Botões Painel Novo Jogo")]
    public Button startGameButton;
    public Button backButton;
    public TMP_InputField playerNameInput;
    
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
        
        Debug.Log("[MainMenu] Initialized");
    }
    
    void SetupButtons()
    {
        // START NEW GAME
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
        
        // CONTINUE
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => {
                SaveLoadManager.RequestContinue();
                LoadGameScene();
            });
        }
        
        // QUIT
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
        
        // START GAME (dentro do painel novo jogo)
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() => {
                string playerName = "Player";
                if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
                    playerName = playerNameInput.text.Trim();
                
                SaveLoadManager.RequestNewGame();
                LoadGameScene();
            });
        }
        
        // BACK
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                if (newGamePanel != null) newGamePanel.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            });
        }
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