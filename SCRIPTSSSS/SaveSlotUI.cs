using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI slotNumberText;
    public TextMeshProUGUI saveInfoText;
    public TextMeshProUGUI emptyText;
    public TextMeshProUGUI timeText;        // NOVO: Hora do save
    public GameObject selectedHighlight;
    public Button slotButton;               // Botão do slot
    
    [Header("Slot Data")]
    public int slotNumber = 1;
    public bool isSelected = false;
    
    // Evento quando slot é selecionado
    public delegate void SlotSelectedHandler(int slotNumber);
    public event SlotSelectedHandler OnSlotSelected;
    
    void Start()
    {
        // Configura botão se não estiver configurado
        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
        }
        
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        
        // Configura textos iniciais
        if (slotNumberText != null)
        {
            slotNumberText.text = $"SLOT {slotNumber}";
        }
        
        // Atualiza display
        UpdateSlotDisplay();
        UpdateSelectionVisual();
    }
    
    public void UpdateSlotDisplay()
    {
        string filePath = GetSaveFilePath();
        bool saveExists = File.Exists(filePath);
        
        if (saveExists)
        {
            // Tenta ler informações do save
            try
            {
                string json = File.ReadAllText(filePath);
                GameData data = JsonUtility.FromJson<GameData>(json);
                
                if (saveInfoText != null && data != null)
                {
                    saveInfoText.text = $"{data.playerData.playerName}\n" +
                                       $"Nível: {data.playerData.level}\n" +
                                       $"Vida: {data.playerData.currentHealth}/{data.playerData.maxHealth}";
                    saveInfoText.gameObject.SetActive(true);
                }
                
                if (timeText != null && data != null)
                {
                    // Tenta parsear a data
                    if (DateTime.TryParse(data.saveDate, out DateTime saveDate))
                    {
                        timeText.text = saveDate.ToString("dd/MM HH:mm");
                    }
                    else
                    {
                        timeText.text = data.saveDate;
                    }
                    timeText.gameObject.SetActive(true);
                }
                
                if (emptyText != null)
                {
                    emptyText.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Erro ao ler save slot {slotNumber}: {e.Message}");
                
                // Fallback
                if (saveInfoText != null)
                {
                    saveInfoText.text = "Save Corrompido";
                    saveInfoText.gameObject.SetActive(true);
                }
                
                if (timeText != null)
                {
                    timeText.text = "Erro";
                    timeText.gameObject.SetActive(true);
                }
                
                if (emptyText != null)
                {
                    emptyText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Slot vazio
            if (saveInfoText != null)
            {
                saveInfoText.gameObject.SetActive(false);
            }
            
            if (timeText != null)
            {
                timeText.gameObject.SetActive(false);
            }
            
            if (emptyText != null)
            {
                emptyText.text = "VAGO";
                emptyText.gameObject.SetActive(true);
            }
        }
    }
    
    public void UpdateSelectionVisual()
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(isSelected);
        }
        
        // Muda cor de fundo se selecionado
        Image bg = GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isSelected ? new Color(0.2f, 0.4f, 0.8f, 0.5f) : new Color(0.1f, 0.1f, 0.1f, 0.3f);
        }
        
        // Destaque no texto do número
        if (slotNumberText != null)
        {
            slotNumberText.color = isSelected ? Color.yellow : Color.white;
            slotNumberText.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
    }
    
    // Chamado quando clica no slot
    public void OnSlotClicked()
    {
        Debug.Log($"🎯 Slot {slotNumber} clicado");
        
        // Dispara evento de seleção
        OnSlotSelected?.Invoke(slotNumber);
    }
    
    private string GetSaveFilePath()
    {
        return SavePathUtility.GetSaveFilePath(slotNumber);
    }
    
    // Método para debug
    [ContextMenu("[Debug] Testar Slot")]
    public void DebugTestSlot()
    {
        Debug.Log($"🧪 Testando Slot {slotNumber}");
        UpdateSlotDisplay();
        UpdateSelectionVisual();
    }
}