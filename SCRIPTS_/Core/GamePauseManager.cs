using UnityEngine;

public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager Instance { get; private set; }
    
    public bool IsGamePaused => Time.timeScale == 0f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void PauseGame(string reason = "")
    {
        if (!IsGamePaused)
        {
            Time.timeScale = 0f;
            Debug.Log($"⏸️ Jogo pausado: {reason}");
            
            // Evento para outros scripts
            // OnGamePaused?.Invoke();
        }
    }
    
    public void ResumeGame()
    {
        if (IsGamePaused)
        {
            Time.timeScale = 1f;
            Debug.Log("▶️ Jogo despausado");
            
            // Evento para outros scripts
            // OnGameResumed?.Invoke();
        }
    }
    
    public void TogglePause()
    {
        if (IsGamePaused)
            ResumeGame();
        else
            PauseGame("Toggle via tecla");
    }
}
