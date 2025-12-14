using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class SaveLoadUI : MonoBehaviour
{   
    [Header("UI References")]
    public GameObject saveLoadPanel;
    public TextMeshProUGUI titleText;
    public Button saveButton;
    public Button loadButton;
    public Button deleteButton;
    public Button closeButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI infoText;
    
    [Header("Slots System")]
    public Transform slotsContainer;
    public GameObject saveSlotPrefab;
    public int totalSlots = 3; // ⭐ Apenas slots manuais 1-3
    
    [Header("Input Settings")]
    public KeyCode toggleMenuKey = KeyCode.F5;
    public bool pauseGameWhenOpen = true;
    
    private int currentSelectedSlot = 1;
    private bool isMenuOpen = false;
    private List<SaveSlotUI> slotUIList = new List<SaveSlotUI>();
    
    void Start()
{
    // Debug.Log("🎮 SaveLoadUI Iniciado");
    
    // ⭐ GARANTE QUE O SAVELOADMANAGER EXISTE SEPARADAMENTE
    if (SaveLoadManager.Instance == null)
    {
        Debug.LogError("❌ SaveLoadManager não encontrado!");
        
        // Procura na cena
        SaveLoadManager manager = FindObjectOfType<SaveLoadManager>();
        if (manager != null)
        {
            // Debug.Log($"✅ Encontrado: {manager.gameObject.name}");
        }
        else
        {
            Debug.LogError("❌ Nenhum SaveLoadManager na cena!");
            return;
        }
    }
    
    GenerateSaveSlots();
    SetupButtons();
    UpdateAllSlotDisplays();
    
    // ⭐ VERIFICAÇÃO EXTRA
    VerifyComponentsAreSeparate();
}

void VerifyComponentsAreSeparate()
{
    // Verifica se ambos componentes estão no mesmo GameObject
    SaveLoadManager manager = GetComponent<SaveLoadManager>();
    if (manager != null)
    {
        Debug.LogError("🚨 CRÍTICO: SaveLoadManager e SaveLoadUI no MESMO GameObject!");
        Debug.LogError("   → Mova SaveLoadManager para um GameObject separado!");
        
        // Desabilita para evitar conflito
        manager.enabled = false;
    }
}
    
    void Update()
    {
        if (Input.GetKeyDown(toggleMenuKey))
        {
            TogglePanel();
        }
    }
    
    private bool isPerformingLoad = false;  // ⭐ NOSSA PRÓPRIA FLAG

public void TogglePanel()
{
    // ⭐ NÃO PERMITE ABRIR/FECHAR DURANTE LOAD
    if (isPerformingLoad) 
    {
        // Debug.Log("⚠️ Operação de load em andamento - painel bloqueado");
        return;
    }
    
    isMenuOpen = !isMenuOpen;
    
    if (saveLoadPanel != null)
    {
        saveLoadPanel.SetActive(isMenuOpen);
        
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.OnMenuStateChanged(isMenuOpen);
        }
        
        // ⭐⭐ CORREÇÃO SEGURA: Só pausa se não estiver carregando
        if (pauseGameWhenOpen)
        {
            Time.timeScale = isMenuOpen ? 0f : 1f;
        }
        
        if (isMenuOpen)
        {
            // Debug.Log("📂 Painel Save/Load ABERTO");
            UpdateAllSlotDisplays();
        }
        else
        {
            // Debug.Log("📂 Painel Save/Load FECHADO");
        }
    }
}
    
    void SetupButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSaveClicked);
        }
        
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }
    
    void GenerateSaveSlots()
    {
        if (slotsContainer == null || saveSlotPrefab == null) return;
        
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        slotUIList.Clear();
        
        for (int i = 1; i <= totalSlots; i++) // ⭐ Começa do 1 (slots manuais)
        {
            GameObject slotGO = Instantiate(saveSlotPrefab, slotsContainer);
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            
            if (slotUI != null)
            {
                slotUI.slotNumber = i;
                slotUI.OnSlotSelected += SelectSlot;
                slotUIList.Add(slotUI);
            }
        }
        
        if (slotUIList.Count > 0) SelectSlot(1);
    }
    
    void UpdateAllSlotDisplays()
    {
        foreach (var slot in slotUIList)
        {
            if (slot != null) slot.UpdateSlotDisplay();
        }
        UpdateStatusText();
    }
    
    void SelectSlot(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > totalSlots) return;
        
        currentSelectedSlot = slotNumber;
        
        foreach (var slot in slotUIList)
        {
            if (slot != null)
            {
                slot.isSelected = slot.slotNumber == slotNumber;
                slot.UpdateSelectionVisual();
            }
        }
        
        UpdateStatusText();
    }
    
    void UpdateStatusText()
    {
        if (statusText == null) return;
        
        string filePath = GetSaveFilePath(currentSelectedSlot);
        bool exists = File.Exists(filePath);
        
        if (exists)
        {
            statusText.text = $"Slot {currentSelectedSlot}: Salvo";
            statusText.color = Color.yellow;
            if (deleteButton != null) deleteButton.interactable = true;
        }
        else
        {
            statusText.text = $"Slot {currentSelectedSlot}: Vazio";
            statusText.color = Color.green;
            if (deleteButton != null) deleteButton.interactable = false;
        }
    }
    
    public void OnSaveClicked()
    {
        Debug.Log($"💾 SALVANDO NO SLOT {currentSelectedSlot}");
        
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("❌ SaveLoadManager não encontrado!");
            if (statusText != null) statusText.text = "❌ Erro: Sistema não inicializado";
            return;
        }
        
        // ⭐ AGORA: Apenas save manual (isAutoSave = false)
        SaveLoadManager.Instance.SaveCurrentState(currentSelectedSlot, false);
        
        if (statusText != null) statusText.text = $"✅ Salvo no slot {currentSelectedSlot}";
        UpdateAllSlotDisplays();
        
        StartCoroutine(CloseAfterDelay(0.5f));
    }
    
   public void OnLoadClicked()
{
    if (isPerformingLoad) return; // ⭐ EVITA DUPLO CLIQUE
    
    isPerformingLoad = true; // ⭐ MARCA QUE COMEÇOU LOAD
    
    Debug.Log($"📂 CARREGANDO SLOT {currentSelectedSlot}");
    
    string filePath = GetSaveFilePath(currentSelectedSlot);
    if (!File.Exists(filePath))
    {
        Debug.LogError("❌ Arquivo não existe!");
        if (statusText != null) statusText.text = "❌ Slot vazio";
        isPerformingLoad = false; // ⭐ RESETA FLAG
        return;
    }
    
    if (SaveLoadManager.Instance == null)
    {
        Debug.LogError("❌ SaveLoadManager não encontrado!");
        if (statusText != null) statusText.text = "❌ Erro: Sistema não inicializado";
        isPerformingLoad = false; // ⭐ RESETA FLAG
        return;
    }
    
    if (currentSelectedSlot == 0)
    {
        Debug.LogWarning("⚠️ Não é possível carregar do slot 0 (auto-save)");
        if (statusText != null) statusText.text = "❌ Slot 0 é auto-save";
        isPerformingLoad = false; // ⭐ RESETA FLAG
        return;
    }
    
    // ✅ USA O MÉTODO PÚBLICO DO SAVELOADMANAGER
    SaveLoadManager.Instance.LoadFromSlot(currentSelectedSlot);
    
    if (statusText != null) statusText.text = $"✅ Carregado do slot {currentSelectedSlot}";
    UpdateAllSlotDisplays();
    
    StartCoroutine(CloseAfterLoadComplete(1.0f));
}

// ✅ NOVO: Corrotina que espera load completar
IEnumerator CloseAfterLoadComplete(float delay)
{
    // Aguarda em tempo REAL (ignora timeScale)
    yield return new WaitForSecondsRealtime(delay);
    
    // ⭐ FECHA DIRETO - NÃO USA TogglePanel() PARA EVITAR CONFLITO
    if (saveLoadPanel != null)
    {
        saveLoadPanel.SetActive(false);
        isMenuOpen = false;
        
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.OnMenuStateChanged(false);
        }
    }
    
    // ⭐ GARANTE QUE O JOGO VOLTA AO NORMAL
    Time.timeScale = 1f;
    
    // ⭐ LIBERA PARA PRÓXIMAS OPERAÇÕES
    isPerformingLoad = false;
    
    // Debug.Log("✅ Load completo - painel fechado com sucesso");
}
    
    public void OnDeleteClicked()
    {
        string filePath = GetSaveFilePath(currentSelectedSlot);
        
        if (!File.Exists(filePath))
        {
            // Debug.LogWarning($"Slot {currentSelectedSlot} já está vazio.");
            return;
        }
        
        File.Delete(filePath);
        // Debug.Log($"✅ Slot {currentSelectedSlot} deletado");
        UpdateAllSlotDisplays();
    }
    
    public void OnCloseClicked()
    {
        TogglePanel();
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        TogglePanel();
    }
    
    string GetSaveFilePath(int slot)
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(saveFolderPath)) Directory.CreateDirectory(saveFolderPath);
        return Path.Combine(saveFolderPath, $"save_{slot}.json");
    }
}