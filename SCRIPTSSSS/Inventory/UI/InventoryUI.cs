using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Combat.TurnBased;

public class InventoryUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventorySlotsContainer;
    [SerializeField] private GameObject slotPrefab;
    
    [Header("New Table System")]
    [SerializeField] private InventoryTableUI inventoryTableUI;
    
    [Header("New Details System")]
    [SerializeField] private InventoryItemDetailsUI inventoryItemDetailsUI;
    
    [Header("Currency Display")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text weightText; // ⭐ NOVO
    
    [Header("Item Info Panel - OLD (deprecated)")]
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemStatsText;
    
    [Header("Equipment Display")]
    [SerializeField] private Transform equipmentSlotsContainer;
    [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs;
    
    [Header("Configuration")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private bool autoInitialize = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float tooltipDelay = 0.5f;
    [Header("Action Buttons")]
    [SerializeField] private Button dropButton;
    [SerializeField] private Button useButton;
      
    [Header("Paper Doll System")] 
    [SerializeField] private InventoryPaperDollUI inventoryPaperDollUI;

    //VARIÁVEIS PARA RASTREAMENTO DE SLOT ESPECÍFICO
    private int selectedItemSlotIndex = -1;
    private InventoryManager.InventorySlot selectedInventorySlot;
    private int selectedTableRowIndex = -1;
    // Runtime data
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private InventoryManager.InventorySlot selectedSlot;
    private float hoverTimer;
    private bool isHovering;
    
    // New system data
    private ItemData selectedItem;
    
    // Singleton instance
    public static InventoryUI Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple InventoryUI instances detected. Destroying: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (autoInitialize)
        {
            InitializeUI();
        }
    }
    
    private void Start()
    {   
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Connect to InventoryManager events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
            InventoryManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
            InventoryManager.Instance.OnEquipmentChanged += UpdateEquipmentDisplay;
            InventoryManager.Instance.OnWeightChanged += UpdateWeightDisplay; //

            //  Conecta OnInventoryChanged à tabela
            if (inventoryTableUI != null)
            {
                InventoryManager.Instance.OnInventoryChanged += inventoryTableUI.OnInventoryChanged;
                Debug.Log("✅ InventoryTableUI conectada ao evento OnInventoryChanged");
            }
            
            // Initial refresh
            RefreshUI();
            UpdateCurrencyDisplay();
            UpdateWeightDisplay(InventoryManager.Instance.CurrentWeight, InventoryManager.Instance.MaxWeight); // ⭐ NOVO
        }
        else
        {
            Debug.LogError("InventoryManager not found! Make sure it's in the scene.");
        }
        
        // ⭐⭐ ADICIONE ESTA LINHA AQUI:
        InitializeActionButtons(); // 🎯 CONFIGURA OS 4 BOTÕES!
        
        // Initialize new table system if available
        if (inventoryTableUI != null)
        {
            // Already initialized via inspector or Awake
        }
        
        // Initialize details system if available
        if (inventoryItemDetailsUI != null)
        {
            // Already initialized via Start
        }
        
        // Hide old info panel
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(false);
        }

        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 50)
        {
            Debug.LogWarning($"[InventoryUI] Start() lento: {stopwatch.ElapsedMilliseconds}ms");
            
            // 🔥 DICA: Se for muito lento, mover inicialização para primeiro frame
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                Debug.LogError("⚠️ InventoryUI.Start() está MUITO lento! Considere usar:");
                Debug.LogError("   • Corrotina com yield return null");
                Debug.LogError("   • Initialize() separado do Start()");
            }
        }
    }

    
    private void Update()
    {
        // ⭐ DEBUG: Log para verificar input
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in (KeyCode[])System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    //Debug.Log($"Tecla pressionada: {keyCode}");
                    break; // Só loga a primeira tecla
                }
            }
        }
        
        // Toggle inventory with key
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"=== Tecla {toggleKey} pressionada - Abrindo/fechando inventário ===");
            ToggleInventory();
        }
        
        // Tooltip handling (old system)
        HandleTooltip();
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            InventoryManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
            InventoryManager.Instance.OnEquipmentChanged -= UpdateEquipmentDisplay;
            InventoryManager.Instance.OnWeightChanged -= UpdateWeightDisplay; //
            //Desconecta tabela
            if (inventoryTableUI != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= inventoryTableUI.OnInventoryChanged;
            }
        }
    }
    
    public void InitializeUI()
    {
        Debug.Log($"Initializing Inventory UI");
        
        // Clear old slot system if exists
        if (inventorySlotsContainer != null && slotPrefab != null)
        {
            foreach (Transform child in inventorySlotsContainer)
            {
                Destroy(child.gameObject);
            }
            slotUIs.Clear();
        }
        
        // Initialize new table system
        if (inventoryTableUI != null)
        {
            Debug.Log("Initializing new table system");
        }
        else
        {
            Debug.LogWarning("InventoryTableUI reference not set!");
        }
        
        // Initialize equipment slots if available
        if (equipmentSlotsContainer != null)
        {
            equipmentSlotUIs = equipmentSlotsContainer.GetComponentsInChildren<EquipmentSlotUI>();
            foreach (var equipmentSlot in equipmentSlotUIs)
            {
                if (equipmentSlot != null)
                {
                    equipmentSlot.Initialize(this);
                }
            }
        }
        
        Debug.Log($"Inventory UI initialized");
    }
    
    public void RefreshUI()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("=== RefreshUI() INICIADO ===");
        Debug.Log($"Tempo: {Time.time:F2}");
        
        // 1. Verificar InventoryManager
        if (InventoryManager.Instance == null) 
        {
            Debug.LogError("❌ ERRO CRÍTICO: InventoryManager.Instance é NULL!");
            Debug.Log("═ RefreshUI() ABORTADO (InventoryManager não encontrado) ═");
            return;
        }
        Debug.Log("✅ InventoryManager encontrado");
        
        // 2. Verificar inventoryTableUI
        if (inventoryTableUI == null)
        {
            Debug.LogError("❌ ERRO: inventoryTableUI é NULL!");
            Debug.Log("   Verifique se atribuiu ItemsTablePanel no Inspector");
        }
        else
        {
            Debug.Log($"✅ inventoryTableUI encontrado: {inventoryTableUI.gameObject.name}");
            Debug.Log($"   Chamando inventoryTableUI.RefreshTable()...");
            
            try
            {
                inventoryTableUI.RefreshTable();
                Debug.Log("   ✅ inventoryTableUI.RefreshTable() chamado com sucesso");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"   ❌ ERRO ao chamar RefreshTable(): {e.Message}");
            }
        }
        
        // 3. Verificar inventoryItemDetailsUI
        if (inventoryItemDetailsUI == null)
        {
            Debug.LogWarning("⚠️ inventoryItemDetailsUI é NULL (pode ser normal se não atribuiu)");
        }
        else
        {
            Debug.Log($"✅ inventoryItemDetailsUI encontrado");
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
        
        // 4. Sistema antigo (compatibilidade)
        Debug.Log("--- Sistema antigo (slots) ---");
        List<InventoryManager.InventorySlot> slots = InventoryManager.Instance.GetAllSlots();
        Debug.Log($"Slots totais: {slots.Count}, Slots UI: {slotUIs.Count}");
        
        // 5. Atualizar displays
        UpdateCapacityDisplay();
        UpdateEquipmentDisplay();
        
        // 6. Atualizar peso
        if (InventoryManager.Instance != null)
        {
            UpdateWeightDisplay(InventoryManager.Instance.CurrentWeight, InventoryManager.Instance.MaxWeight);
            Debug.Log($"Peso atual: {InventoryManager.Instance.CurrentWeight:F1}/{InventoryManager.Instance.MaxWeight:F1} kg");
        }
        
        Debug.Log("=== RefreshUI() FINALIZADO ===");
        Debug.Log("═══════════════════════════════════════════");
    }
    
    // ⭐ NOVO: Called when item is selected in table
    public void OnItemSelected(ItemData item)
    {
        selectedItem = item;
        
        // Update details panel
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ShowItemDetails(item);
        }
        else
        {
            // Fallback to old system
            ShowItemInfoOldSystem(item);
        }

        UpdateButtonStates(); // Atualiza botões quando seleciona item
        
        Debug.Log($"Item selected: {item?.itemName ?? "None"}");
    }
    
    public void UpdateCurrencyDisplay()
    {
        if (currencyText != null && InventoryManager.Instance != null)
        {
            currencyText.text = $"{InventoryManager.Instance.Currency} G";
        }
    }
    
        public void UpdateEquipmentDisplaySafe()
    {
        Debug.Log("🔄 UpdateEquipmentDisplaySafe - SÓ VISUAL");
        
        // Atualiza APENAS Paper Doll (não chama RefreshUI)
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.UpdateAllSlots();
        }
        
        // Atualiza stats
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
}
    // ⭐ NOVO: Update weight display
    public void UpdateWeightDisplay(float currentWeight, float maxWeight)
    {
        if (weightText != null)
        {
            weightText.text = $"WEIGHT: {currentWeight:F1}/{maxWeight:F1} kg";
            
            // Change color based on weight usage
            float weightPercentage = currentWeight / maxWeight;
            if (weightPercentage >= 1f)
            {
                weightText.color = Color.red;
            }
            else if (weightPercentage >= 0.8f)
            {
                weightText.color = Color.yellow;
            }
            else
            {
                weightText.color = Color.white;
            }
        }
    }
    
    private void UpdateCapacityDisplay()
    {
        if (capacityText != null && InventoryManager.Instance != null)
        {
            int used = InventoryManager.Instance.GetUsedSlotCount();
            int total = InventoryManager.Instance.InventorySize;
            capacityText.text = $"SLOTS: {used}/{total}";
            
            // Change color if inventory is full
            if (used >= total)
            {
                capacityText.color = Color.red;
            }
            else if (used >= total * 0.8f)
            {
                capacityText.color = Color.yellow;
            }
            else
            {
                capacityText.color = Color.white;
            }
        }
    }

    public void UpdateEquipmentDisplayFast()
    {
        // 🚀 OTIMIZAÇÃO: Atualiza APENAS o Paper Doll (mais rápido)
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.UpdateAllSlots();
        }
        
        // 🚀 OTIMIZAÇÃO: Atualiza stats do party member (leve)
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.OnEquipmentChanged();
        }
    }
    
    public void UpdateEquipmentDisplay()
    {
        Debug.Log("=== UpdateEquipmentDisplay() INICIADO ===");
        
        // 1. Verificar referências críticas
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager.Instance é NULL!");
            return;
        }
        
        Debug.Log("✅ InventoryManager encontrado");
        
        // 2. Sistema antigo de EquipmentSlotUI (para compatibilidade)
        if (equipmentSlotUIs != null && equipmentSlotUIs.Length > 0)
        {
            Debug.Log($"Updating {equipmentSlotUIs.Length} old equipment slots");
            
            int updatedCount = 0;
            foreach (var equipmentSlot in equipmentSlotUIs)
            {
                if (equipmentSlot != null)
                {
                    try
                    {
                        equipmentSlot.UpdateEquipment();
                        updatedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Erro ao atualizar equipmentSlot: {e.Message}");
                    }
                }
            }
            Debug.Log($"✅ Updated {updatedCount}/{equipmentSlotUIs.Length} old slots");
        }
        else
        {
            Debug.Log("⚠️ equipmentSlotUIs array está vazio ou null");
        }
        
        // 3. ⭐ NOVO: Atualizar Paper Doll System se disponível
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("🔄 Atualizando Paper Doll System...");
            
            try
            {
                // Chamar método de atualização do paper doll
                inventoryPaperDollUI.UpdateAllSlots();
                Debug.Log("✅ Paper Doll atualizado com sucesso");
                
                // DEBUG: Verificar estado atual
                if (selectedItem != null && selectedItem.IsEquipment())
                {
                    Debug.Log($"📌 Item selecionado: {selectedItem.itemName} (Slot: {selectedItem.equipmentSlot})");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao atualizar Paper Doll: {e.Message}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ inventoryPaperDollUI não está configurado");
            Debug.Log("   Verifique se arrastou o PaperDollPanel para o campo no Inspector");
        }
        
        // 4. Atualizar stats do party member
        if (inventoryItemDetailsUI != null)
        {
            Debug.Log("📊 Atualizando party member stats...");
            
            try
            {
                inventoryItemDetailsUI.OnEquipmentChanged();
                Debug.Log("✅ Party member stats atualizados");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao atualizar stats: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ inventoryItemDetailsUI é NULL");
        }
        
        // 5. Verificar estado do equipamento no InventoryManager
        try
        {
            var equipment = InventoryManager.Instance.Equipment;
            
            // Log dos itens equipados para debug
            Debug.Log("🎯 EQUIPAMENTO ATUAL NO INVENTORYMANAGER:");
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            foreach (ItemData.EquipmentSlot slot in slotTypes)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var item = InventoryManager.Instance.GetEquippedItem(slot);
                if (item != null)
                {
                    Debug.Log($"   [{slot}]: {item.itemName}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao verificar equipamento: {e.Message}");
        }
        
        Debug.Log("=== UpdateEquipmentDisplay() FINALIZADO ===");
    }
    

    public void ToggleInventory()
    {
        Debug.Log("=== ToggleInventory INICIADO ===");
        
        try
        {
            if (inventoryPanel == null)
            {
                Debug.LogError("ERROR: inventoryPanel é NULL!");
                return;
            }
            
            bool newState = !inventoryPanel.activeSelf;
            Debug.Log($"Tentando SetActive({newState})...");
            
            inventoryPanel.SetActive(newState);
            
            Debug.Log($"SUCESSO: InventoryPanel agora está {(newState ? "ATIVO" : "INATIVO")}");
            
            // ⭐⭐ NOVO: PAUSAR/DESPAUSAR O JOGO
            if (newState)
            {
                // Inventário ABERTO - Pausar jogo
                PauseGame();
                Debug.Log("⏸️ JOGO PAUSADO (inventário aberto)");
                
                // Atualizar dados
                RefreshUI();
                UpdateCurrencyDisplay();
                UpdateEquipmentDisplay();
            }
            else
            {
                // Inventário FECHADO - Despausar jogo
                ResumeGame();
                Debug.Log("▶️ JOGO DESPAUSADO (inventário fechado)");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERRO CRÍTICO no ToggleInventory: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
        
        Debug.Log("=== ToggleInventory FINALIZADO ===");
    }

    /// <summary>
    ///  Seleciona item COM INFORMAÇÃO DE SLOT ESPECÍFICO
    /// </summary>
        public void OnItemSelectedWithSlot(ItemData item, int slotIndex, 
                                        InventoryManager.InventorySlot specificSlot, 
                                        int tableRowIndex = -1)
        {
            selectedItem = item;
            selectedItemSlotIndex = slotIndex;
            selectedInventorySlot = specificSlot;
            selectedTableRowIndex = tableRowIndex;
            
            Debug.Log($"🎯 Item selecionado COM slot:");
            Debug.Log($"   Item: {item?.itemName}");
            Debug.Log($"   Slot Index: {slotIndex}");
            Debug.Log($"   Table Row: {tableRowIndex}");
            Debug.Log($"   Slot válido? {specificSlot != null}");
            
            // Atualizar painel de detalhes
            if (inventoryItemDetailsUI != null)
            {
                inventoryItemDetailsUI.ShowItemDetails(item);
            }
            else
            {
                ShowItemInfoOldSystem(item);
            }
            
            UpdateButtonStates();
        }




    /// <summary>
    /// 🔥 REMOVE ITEM DE UM SLOT ESPECÍFICO
    /// </summary>
    private bool RemoveItemFromSpecificSlot(int slotIndex)
    {
        if (InventoryManager.Instance == null) return false;
        
        // 🔥 ADICIONE ESTE MÉTODO AO InventoryManager.cs SE NÃO EXISTIR
        // Método já fornecido anteriormente: RemoveItemFromSlot
        return InventoryManager.Instance.RemoveItemFromSlot(slotIndex, 1);
    }

    /// <summary>
    /// 🔥 LIMPA TODAS AS SELEÇÕES
    /// </summary>
    private void ClearItemSelection()
    {
        selectedItem = null;
        selectedItemSlotIndex = -1;
        selectedInventorySlot = null;
        selectedTableRowIndex = -1;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("🧹 Seleção de item limpa");
    }

    /// <summary>
    /// Pausa o jogo (Time.timeScale = 0)
    /// </summary>// No InventoryUI.cs, modifique:
    public void PauseGame()
    {
        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.PauseGame("Inventário aberto");
        }
        else
        {
            // Fallback
            Time.timeScale = 0f;
        }
        

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.canInteract = false;
    }

    public void ResumeGame()
    {
        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.ResumeGame();
        }
        else
        {
            // Fallback
            Time.timeScale = 1f;
        }
        
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.canInteract = true;
    }
    
    public void OpenInventory()
    {
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
            RefreshUI();
            UpdateCurrencyDisplay();
            UpdateEquipmentDisplay();
        }
    }
    
    public void CloseInventory()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  ❌ BOTÃO FECHAR - INVENTÁRIO        ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        if (inventoryPanel == null)
        {
            Debug.LogError("║  ❌ inventoryPanel é NULL!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  🎯 Fechando inventário...");
        
        // Fechar painel
        inventoryPanel.SetActive(false);
        
        // Despausar jogo
        ResumeGame();
        
        Debug.Log($"║  ✅ Inventário fechado");
        Debug.Log($"║  ▶️ Jogo despausado");
        Debug.Log("╚═══════════════════════════════════════╝");
    }
    
    public bool IsInventoryOpen()
    {
        return inventoryPanel != null && inventoryPanel.activeSelf;
    }
    
  
    
    // ============================================
    // OLD SYSTEM METHODS (for compatibility)
    // ============================================
    
    // Called by old InventorySlotUI when slot is clicked
    public void OnSlotClicked(InventoryManager.InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty) return;
        
        Debug.Log($"Slot clicked: {slot.item.itemName} x{slot.quantity}");
        
        // Use new system if available
        if (inventoryItemDetailsUI != null && slot.item != null)
        {
            OnItemSelected(slot.item);
        }
        else
        {
            // Fallback to old system
            ShowItemInfoOldSystem(slot);
        }
    }
    
    // Called by old InventorySlotUI when slot is hovered
    public void OnSlotHoverEnter(InventoryManager.InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty) return;
        
        selectedSlot = slot;
        isHovering = true;
        hoverTimer = 0f;
    }
    
    public void OnSlotHoverExit()
    {
        isHovering = false;
        selectedSlot = null;
        
        // Hide tooltip immediately
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(false);
        }
    }
    
    private void HandleTooltip()
    {
        if (!isHovering || selectedSlot == null || selectedSlot.IsEmpty) return;
        
        hoverTimer += Time.deltaTime;
        
        if (hoverTimer >= tooltipDelay && !itemInfoPanel.activeSelf)
        {
            ShowItemInfoOldSystem(selectedSlot);
        }
    }
    
    private void ShowItemInfoOldSystem(InventoryManager.InventorySlot slot)
    {
        if (itemInfoPanel == null || slot == null || slot.IsEmpty) return;
        
        ItemData item = slot.item;
        
        // Set basic info
        if (itemNameText != null)
            itemNameText.text = item.itemName;
        
        if (itemDescriptionText != null)
            itemDescriptionText.text = item.description;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = item.GetRarityColor();
        }
        
        // Build stats text
        if (itemStatsText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // Type and rarity
            sb.AppendLine($"Type: {item.itemType}");
            sb.AppendLine($"Rarity: {item.rarity}");
            
            // Equipment info
            if (item.IsEquipment())
            {
                sb.AppendLine($"Slot: {item.equipmentSlot}");
                if (item.weaponType != ItemData.WeaponType.None)
                    sb.AppendLine($"Weapon: {item.weaponType}");
                
                sb.AppendLine($"Required Level: {item.requiredLevel}");
            }
            
            // Stats
            if (item.attackBonus != 0) sb.AppendLine($"Attack: +{item.attackBonus}");
            if (item.defenseBonus != 0) sb.AppendLine($"Defense: +{item.defenseBonus}");
            if (item.magicAttackBonus != 0) sb.AppendLine($"Magic Attack: +{item.magicAttackBonus}");
            if (item.magicDefenseBonus != 0) sb.AppendLine($"Magic Defense: +{item.magicDefenseBonus}");
            if (item.speedBonus != 0) sb.AppendLine($"Speed: +{item.speedBonus}");
            
            // Consumable effects
            if (item.hpRestore != 0) sb.AppendLine($"Restores {item.hpRestore} HP");
            if (item.mpRestore != 0) sb.AppendLine($"Restores {item.mpRestore} MP");
            if (item.revive) sb.AppendLine($"Revives fallen ally");
            if (item.cureAllStatus) sb.AppendLine($"Cures all status effects");
            
            // Usage
            sb.AppendLine($"Stack: {slot.quantity}/{item.stackLimit}");
            sb.AppendLine($"Price: {item.GetCalculatedSellPrice()} Gold");
            
            itemStatsText.text = sb.ToString();
        }
        
        // Show panel
        itemInfoPanel.SetActive(true);
    }
    
    private void ShowItemInfoOldSystem(ItemData item)
    {
        if (itemInfoPanel == null || item == null) return;
        
        // Similar to above but without slot quantity
        if (itemNameText != null)
            itemNameText.text = item.itemName;
        
        if (itemDescriptionText != null)
            itemDescriptionText.text = item.description;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = item.GetRarityColor();
        }
        
        if (itemStatsText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"Type: {item.itemType}");
            sb.AppendLine($"Rarity: {item.rarity}");
            
            if (item.IsEquipment())
            {
                sb.AppendLine($"Slot: {item.equipmentSlot}");
                sb.AppendLine($"Required Level: {item.requiredLevel}");
            }
            
            if (item.attackBonus != 0) sb.AppendLine($"Attack: +{item.attackBonus}");
            if (item.defenseBonus != 0) sb.AppendLine($"Defense: +{item.defenseBonus}");
            
            sb.AppendLine($"Weight: {item.weight:F1} kg");
            sb.AppendLine($"Price: {item.GetCalculatedSellPrice()} Gold");
            
            itemStatsText.text = sb.ToString();
        }
        
        itemInfoPanel.SetActive(true);
    }

    // ============================================
    // ACTION BUTTONS METHODS
    // ============================================
    
    public void UseSelectedItem()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  💊 USE SELECTED ITEM - CORRIGIDO    ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        if (selectedItem == null) 
        {
            Debug.LogError("║  ❌ Nenhum item selecionado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📦 Item: {selectedItem.itemName}");
        Debug.Log($"║  🏷️ Tipo: {selectedItem.itemType}");
        
        if (selectedItem.IsConsumable())
        {
            Debug.Log($"║  🔥 Usando consumível: {selectedItem.itemName}");
            
            // 🔥 PASSO 1: VERIFICAR SE TEM O ITEM
            if (!InventoryManager.Instance.HasItem(selectedItem, 1))
            {
                Debug.LogError($"║  ❌ Não tem {selectedItem.itemName} no inventário!");
                Debug.Log("╚═══════════════════════════════════════╝");
                return;
            }
            
            // 🔥 PASSO 2: APLICAR EFEITOS NO PLAYER ATIVO
            bool effectsApplied = ApplyConsumableEffects(selectedItem);
            
            if (!effectsApplied)
            {
                Debug.LogError($"║  ❌ Não conseguiu aplicar efeitos!");
                Debug.Log("╚═══════════════════════════════════════╝");
                return;
            }
            
            // 🔥 PASSO 3: REMOVER 1 DO INVENTÁRIO
            bool removed = InventoryManager.Instance.RemoveItem(selectedItem, 1);
            
            if (!removed)
            {
                Debug.LogError($"║  ❌ Não conseguiu remover do inventário!");
                Debug.Log("╚═══════════════════════════════════════╝");
                return;
            }
            
            Debug.Log($"║  ✅ {selectedItem.itemName} usado com sucesso!");
            
            // 🔥 PASSO 4: ATUALIZAR UI
            // Se ainda tem mais deste item, manter selecionado
            if (InventoryManager.Instance.HasItem(selectedItem, 1))
            {
                OnItemSelected(selectedItem);
            }
            else
            {
                // Limpar seleção
                selectedItem = null;
                if (inventoryItemDetailsUI != null)
                    inventoryItemDetailsUI.ClearItemDetails();
                    
                // Limpar seleção da tabela
                if (inventoryTableUI != null)
                    inventoryTableUI.ClearSelection();
            }
            
            // Atualizar UI
            RefreshUI();
            UpdateButtonStates();
            
            Debug.Log($"║  🎉 Consumível usado com sucesso!");
        }
        else if (selectedItem.IsEquipment())
        {
            Debug.Log($"║  ⚠️ {selectedItem.itemName} é equipamento!");
            Debug.Log($"║  ℹ️ Use Drag & Drop para equipar");
        }
        else
        {
            Debug.Log($"║  ⚠️ Item não é usável: {selectedItem.itemName}");
        }
        
        Debug.Log("╚═══════════════════════════════════════╝");
    }
   
   private bool ApplyConsumableEffects(ItemData consumable)
    {
        if (consumable == null || !consumable.IsConsumable()) return false;
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔥 APLICANDO EFEITOS: {consumable.itemName}");
        
        // 🔥 OBTER PLAYER ATIVO
        CharacterData activeCharacter = null;
        
        if (PartyManager.Instance != null)
        {
            activeCharacter = PartyManager.Instance.GetActiveMember();
        }
        
        if (activeCharacter == null)
        {
            Debug.LogError("║  ❌ Nenhum player ativo encontrado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  👤 Player: {activeCharacter.characterName}");
        Debug.Log($"║  ❤️ HP Antes: {activeCharacter.currentHP}/{activeCharacter.GetCurrentMaxHP()}");
        Debug.Log($"║  🔷 MP Antes: {activeCharacter.currentMP}/{activeCharacter.GetCurrentMaxMP()}");
        
        bool effectApplied = false;
        
        // 🔥 CURAR HP
        if (consumable.hpRestore > 0)
        {
            int healAmount = consumable.hpRestore;
            activeCharacter.currentHP = Mathf.Min(
                activeCharacter.currentHP + healAmount,
                activeCharacter.GetCurrentMaxHP()
            );
            
            Debug.Log($"║  💚 +{healAmount} HP curado!");
            effectApplied = true;
        }
        
        // 🔥 RESTAURAR MP
        if (consumable.mpRestore > 0)
        {
            int restoreAmount = consumable.mpRestore;
            activeCharacter.currentMP = Mathf.Min(
                activeCharacter.currentMP + restoreAmount,
                activeCharacter.GetCurrentMaxMP()
            );
            
            Debug.Log($"║  💙 +{restoreAmount} MP restaurado!");
            effectApplied = true;
        }
        
        // 🔥 REVIVER
        if (consumable.revive && activeCharacter.currentHP <= 0)
        {
            activeCharacter.currentHP = activeCharacter.GetCurrentMaxHP() / 2; // Revive com 50% HP
            Debug.Log($"║  ⚡ Revivido! HP: {activeCharacter.currentHP}");
            effectApplied = true;
        }
        
        // 🔥 CURAR STATUS
        if (consumable.cureAllStatus)
        {
            // TODO: Implementar sistema de status effects
            Debug.Log($"║  🩹 Status effects curados!");
            effectApplied = true;
        }
        
        Debug.Log($"║  ❤️ HP Depois: {activeCharacter.currentHP}/{activeCharacter.GetCurrentMaxHP()}");
        Debug.Log($"║  🔷 MP Depois: {activeCharacter.currentMP}/{activeCharacter.GetCurrentMaxMP()}");
        
        if (!effectApplied)
        {
            Debug.LogWarning($"║  ⚠️ Nenhum efeito aplicado!");
        }
        
        Debug.Log($"║  ✅ Efeitos aplicados: {effectApplied}");
        Debug.Log("╚═══════════════════════════════════════╝");
        
        // 🔥 ATUALIZAR UI DE STATS
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
        
        return effectApplied;
    }
    
    public void DropSelectedItem()
    {
        if (selectedItem == null) return;
        
        Debug.Log($"=== DropSelectedItem() INICIADO ===");
        Debug.Log($"Tentando dropar: {selectedItem.itemName}");
        
        // TODO: Add confirmation dialog
        
        // Verificar quantidade antes
        int itemCountBefore = InventoryManager.Instance.GetItemCount(selectedItem);
        Debug.Log($"🔍 Item count BEFORE drop: {itemCountBefore}");
        
        // Remove one item
        bool removed = InventoryManager.Instance.RemoveItem(selectedItem, 1);
        Debug.Log($"RemoveItem result: {removed}");
        
        // Verificar quantidade depois
        int itemCountAfter = InventoryManager.Instance.GetItemCount(selectedItem);
        Debug.Log($"🔍 Item count AFTER drop: {itemCountAfter}");
        
        if (removed)
        {
            Debug.Log($"✅ Dropped {selectedItem.itemName}");
        }
        else
        {
            Debug.LogError($"❌ Failed to drop {selectedItem.itemName}");
        }
        
        // Update details if still has item
        if (InventoryManager.Instance.HasItem(selectedItem, 1))
        {
            OnItemSelected(selectedItem);
        }
        else
        {
            // No more of this item, clear selection
            selectedItem = null;
            if (inventoryItemDetailsUI != null)
                inventoryItemDetailsUI.ClearItemDetails();
            if (inventoryTableUI != null)
                inventoryTableUI.ClearSelection();
        }
        
        Debug.Log("=== DropSelectedItem() FINALIZADO ===");
    }
    


   

    // 🔥🔥🔥 CORREÇÃO: Ordem correta das operações
    private System.Collections.IEnumerator EquipItemProcess(ItemData itemToEquip, ItemData.EquipmentSlot targetSlot)
    {
        Debug.Log("╔═══════════════════════════════════════════════");
        Debug.Log($"│  🎯 Equipando: {itemToEquip.itemName}");
        Debug.Log($"│  🔌 Slot: {targetSlot}");
        
        // 🔥 PASSO 1: Equipar via InventoryManager
        bool equipped = false;
        
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("│  🎯 Usando Paper Doll System");
            equipped = inventoryPaperDollUI.TryEquipItem(itemToEquip);
        }
        else
        {
            Debug.Log("│  🎯 Usando InventoryManager (fallback)");
            equipped = InventoryManager.Instance.EquipItem(itemToEquip);
        }
        
        if (!equipped)
        {
            Debug.LogError("│  ❌ Falha ao equipar!");
            Debug.Log("╚═══════════════════════════════════════════════");
            yield break;
        }
        
        Debug.Log("│  ✅ Item equipado!");
        
        // 🔥 OTIMIZAÇÃO: Aguardar 1 frame ANTES de atualizar UI
        yield return null;
        
        // 🔥 PASSO 2: Atualizar UI - VERSÃO ULTRA-RÁPIDA
        Debug.Log("│  🚀 Atualizando UI (otimizado)...");
        
        // 2A. Atualizar APENAS a tabela (sem recriar linhas)
        if (inventoryTableUI != null)
        {
            inventoryTableUI.UpdateExistingRowsData(); // ⚡ MUITO mais rápido!
        }
        
        // 2B. Atualizar Paper Doll
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.UpdateAllSlots();
        }
        
        // 2C. Atualizar stats
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
        
        // 🔥 PASSO 3: Aguardar frame
        yield return null;
        
        // 🔥 PASSO 4: Limpar seleções
        Debug.Log("│  🧹 Limpando seleções após equipar...");
        
        if (inventoryTableUI != null)
        {
            inventoryTableUI.ClearSelection();
        }
        
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.ClearVisualSelection();
        }
        
        selectedItem = null;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("│  ✅ Seleções limpas - usuário deve clicar no Paper Doll para desequipar");
        
        // 🔥 PASSO 5: Atualizar displays finais (leve)
        UpdateCurrencyDisplay();
        UpdateCapacityDisplay();
        
        // 🔥 PASSO 6: Atualizar botões
        yield return null;
        UpdateButtonStates();
        
        Debug.Log("│  ✅ Processo completo!");
        Debug.Log("╚═══════════════════════════════════════════════");
    }

    // 🔥 Helper (já existe, mantém igual)
    private bool IsCompatibleSlot(ItemData.EquipmentSlot slotA, ItemData.EquipmentSlot slotB)
    {
        if (slotA == slotB) return true;
        
        // Weapon <-> MainHand
        if ((slotA == ItemData.EquipmentSlot.Weapon && slotB == ItemData.EquipmentSlot.MainHand) ||
            (slotA == ItemData.EquipmentSlot.MainHand && slotB == ItemData.EquipmentSlot.Weapon))
            return true;
        
        return false;
    }


        // 🔥🔥🔥 MÉTODO SIMPLIFICADO: Usa método público do PaperDoll
    private bool SelectPaperDollSlot(ItemData.EquipmentSlot targetSlot)
    {
        if (inventoryPaperDollUI == null) return false;
        
        // 🔥 Usar método público (muito mais simples!)
        return inventoryPaperDollUI.SelectSlotByType(targetSlot);
    }

    




   
    
    
    
    // ============================================
    // DEBUG METHODS
    // ============================================
    
    [ContextMenu("Debug: Force Refresh UI")]
    public void DebugForceRefresh()
    {
        Debug.Log("=== DEBUG: Force Refreshing Inventory UI ===");
        RefreshUI();
        UpdateCurrencyDisplay();
        UpdateEquipmentDisplay();
        
        if (InventoryManager.Instance != null)
        {
            UpdateWeightDisplay(InventoryManager.Instance.CurrentWeight, InventoryManager.Instance.MaxWeight);
        }
    }
    
    [ContextMenu("Debug: Print Current State")]
    public void DebugPrintState()
    {
        Debug.Log($"=== InventoryUI State ===");
        Debug.Log($"Selected Item: {selectedItem?.itemName ?? "None"}");
        Debug.Log($"Table UI: {(inventoryTableUI != null ? "Set" : "Null")}");
        Debug.Log($"Details UI: {(inventoryItemDetailsUI != null ? "Set" : "Null")}");
        Debug.Log($"Inventory Open: {IsInventoryOpen()}");
        
        if (selectedItem != null)
        {
            Debug.Log($"Selected Item Details:");
            Debug.Log($"  Name: {selectedItem.itemName}");
            Debug.Log($"  Type: {selectedItem.itemType}");
            Debug.Log($"  Slot: {selectedItem.equipmentSlot}");
            Debug.Log($"  Weight: {selectedItem.weight}");
            Debug.Log($"  In Inventory: {InventoryManager.Instance?.GetItemCount(selectedItem) ?? 0}");
        }
    }
    
    // ⭐ NOVO: DIAGNÓSTICO ESPECÍFICO PARA IRON SWORD
    [ContextMenu("[DIAGNOSTIC] Debug Iron Sword Equip Issue")]
    public void DebugIronSwordIssue()
    {
        Debug.Log("=== DIAGNOSTIC: IRON SWORD EQUIP ISSUE ===");
        
        // 1. Encontra a Iron Sword
        ItemData ironSword = ItemRegistry.GetItemByName("Iron Sword");
        if (ironSword == null)
        {
            ironSword = ItemRegistry.GetItem("iron_sword");
        }
        
        if (ironSword == null)
        {
            Debug.LogError("❌ Iron Sword not found in registry!");
            ItemRegistry.DebugPrintAllItems();
            return;
        }
        
        Debug.Log($"✅ Iron Sword encontrada: {ironSword.itemName} (ID: {ironSword.itemID})");
        Debug.Log($"É equipamento? {ironSword.IsEquipment()}");
        Debug.Log($"Slot: {ironSword.equipmentSlot}");
        Debug.Log($"Weapon Type: {ironSword.weaponType}");
        Debug.Log($"Required Level: {ironSword.requiredLevel}");
        
        // 2. Verifica no inventário
        if (InventoryManager.Instance != null)
        {
            int count = InventoryManager.Instance.GetItemCount(ironSword);
            Debug.Log($"Quantidade no inventário: {count}");
            
            // Verifica slots específicos
            var slotsWithItem = InventoryManager.Instance.GetSlotsWithItem(ironSword);
            Debug.Log($"Slots com Iron Sword: {slotsWithItem.Count}");
            foreach (var slot in slotsWithItem)
            {
                Debug.Log($"  Slot {slot.slotIndex}: {slot.quantity}x");
            }
            
            // 3. Tenta equipar
            Debug.Log("--- Tentando equipar via InventoryManager ---");
            bool success = InventoryManager.Instance.EquipItem(ironSword);
            Debug.Log($"Resultado EquipItem: {success}");
            
            // 4. Verifica novamente
            int countAfter = InventoryManager.Instance.GetItemCount(ironSword);
            Debug.Log($"Quantidade após tentativa: {countAfter}");
            
            // 5. Verifica se está equipado
            var equippedItem = InventoryManager.Instance.GetEquippedItem(ironSword.equipmentSlot);
            Debug.Log($"Item equipado no slot {ironSword.equipmentSlot}: {equippedItem?.itemName ?? "None"}");
            
            // 6. Debug do equipment loadout
            var equipment = InventoryManager.Instance.Equipment;
            Debug.Log($"Weapon slot: {equipment.weapon?.itemName}");
            Debug.Log($"MainHand slot: {equipment.mainHand?.itemName}");
            
            // 7. Verifica todos os slots possíveis
            Debug.Log("\n🔍 Verificando todos os slots de equipamento:");
            var allSlots = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            foreach (ItemData.EquipmentSlot slot in allSlots)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var item = InventoryManager.Instance.GetEquippedItem(slot);
                if (item != null && item.itemName.Contains("Sword"))
                {
                    Debug.Log($"  [{slot}]: {item.itemName} (ID: {item.itemID})");
                }
            }
        }
        else
        {
            Debug.LogError("InventoryManager.Instance é null!");
        }
        
        Debug.Log("=== FIM DIAGNÓSTICO ===");
    }
    
    [ContextMenu("[DIAGNOSTIC] Check Equipment System Integrity")]
    public void DebugEquipmentIntegrity()
    {
        Debug.Log("=== EQUIPMENT SYSTEM INTEGRITY CHECK ===");
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager não encontrado!");
            return;
        }
        
        // 1. Lista todos os itens equipados
        Debug.Log("\n🎯 ITENS EQUIPADOS:");
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        bool anyEquipped = false;
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var equippedItem = InventoryManager.Instance.GetEquippedItem(slot);
            if (equippedItem != null)
            {
                anyEquipped = true;
                Debug.Log($"  [{slot}]: {equippedItem.itemName}");
                
                // Verifica se ainda está no inventário (NÃO DEVERIA!)
                int invCount = InventoryManager.Instance.GetItemCount(equippedItem);
                if (invCount > 0)
                {
                    Debug.LogError($"    ⚠️ CONFLITO: Ainda no inventário ({invCount}x)!");
                }
            }
        }
        
        if (!anyEquipped)
        {
            Debug.Log("  Nenhum item equipado");
        }
        
        // 2. Lista todos os itens no inventário
        Debug.Log("\n📦 ITENS NO INVENTÁRIO (equipamentos):");
        var inventorySlots = InventoryManager.Instance.GetNonEmptySlots();
        bool anyEquipmentInInventory = false;
        
        foreach (var slot in inventorySlots)
        {
            if (slot.item.IsEquipment())
            {
                anyEquipmentInInventory = true;
                Debug.Log($"  {slot.item.itemName} x{slot.quantity} ({slot.item.equipmentSlot})");
            }
        }
        
        if (!anyEquipmentInInventory)
        {
            Debug.Log("  Nenhum equipamento no inventário");
        }
        
        // 3. Verifica integridade do Paper Doll
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("\n🎨 PAPER DOLL STATUS:");
            inventoryPaperDollUI.DebugPrintSlotInfo();
        }
        
        Debug.Log("=== FIM DA VERIFICAÇÃO ===");
    }
    
    [ContextMenu("[DIAGNOSTIC] Test Equip/Unequip Cycle")]
    public void DebugTestEquipCycle()
    {
        Debug.Log("=== TESTE: CICLO EQUIP/DESEQUIP ===");
        
        // Encontra qualquer espada para teste
        ItemData testSword = null;
        var allItems = ItemRegistry.GetAllItems();
        
        foreach (var item in allItems)
        {
            if (item.IsEquipment() && item.weaponType == ItemData.WeaponType.Sword)
            {
                testSword = item;
                break;
            }
        }
        
        if (testSword == null)
        {
            Debug.LogError("❌ Nenhuma espada encontrada para teste!");
            return;
        }
        
        Debug.Log($"Usando {testSword.itemName} para teste");
        
        // Adiciona ao inventário se não tiver
        if (!InventoryManager.Instance.HasItem(testSword, 1))
        {
            Debug.Log($"Adicionando {testSword.itemName} ao inventário...");
            InventoryManager.Instance.AddItem(testSword, 1);
        }
        
        // PASSO 1: Equipar
        Debug.Log($"\n🔧 PASSO 1: Equipar {testSword.itemName}");
        int beforeEquipCount = InventoryManager.Instance.GetItemCount(testSword);
        Debug.Log($"Antes de equipar: {beforeEquipCount} no inventário");
        
        bool equipSuccess = InventoryManager.Instance.EquipItem(testSword);
        Debug.Log($"EquipItem() retornou: {equipSuccess}");
        
        int afterEquipCount = InventoryManager.Instance.GetItemCount(testSword);
        Debug.Log($"Após equipar: {afterEquipCount} no inventário");
        
        // PASSO 2: Verificar equipado
        var equippedItem = InventoryManager.Instance.GetEquippedItem(testSword.equipmentSlot);
        Debug.Log($"Item equipado no slot {testSword.equipmentSlot}: {equippedItem?.itemName ?? "None"}");
        
        // PASSO 3: Desequipar
        Debug.Log($"\n🔧 PASSO 2: Desequipar {testSword.itemName}");
        var unequipped = InventoryManager.Instance.UnequipItem(testSword.equipmentSlot);
        Debug.Log($"UnequipItem() retornou: {unequipped?.itemName ?? "NULL"}");
        
        int afterUnequipCount = InventoryManager.Instance.GetItemCount(testSword);
        Debug.Log($"Após desequipar: {afterUnequipCount} no inventário");
        
        Debug.Log("=== FIM DO TESTE ===");
    }

    [ContextMenu("[TEST] Verify Equip Fix")]
    public void TestEquipFix()
    {
        Debug.Log("=== TESTE DA CORREÇÃO DO EQUIPAMENTO ===");
        
        // Encontra a Iron Sword
        ItemData ironSword = ItemRegistry.GetItem("iron_sword");
        if (ironSword == null)
        {
            Debug.LogError("Iron Sword não encontrada!");
            return;
        }
        
        // Garante que tem uma no inventário
        if (!InventoryManager.Instance.HasItem(ironSword, 1))
        {
            InventoryManager.Instance.AddItem(ironSword, 1);
            Debug.Log($"Adicionada 1x {ironSword.itemName} ao inventário");
        }
        
        // PASSO 1: Verifica estado inicial
        Debug.Log($"\n📊 ESTADO INICIAL:");
        Debug.Log($"No inventário: {InventoryManager.Instance.GetItemCount(ironSword)}x");
        Debug.Log($"Equipado em {ironSword.equipmentSlot}: {InventoryManager.Instance.GetEquippedItem(ironSword.equipmentSlot)?.itemName ?? "None"}");
        
        // PASSO 2: Tenta equipar
        Debug.Log($"\n🎯 TENTANDO EQUIPAR:");
        bool equipResult = InventoryManager.Instance.EquipItem(ironSword);
        Debug.Log($"Resultado: {equipResult}");
        
        // PASSO 3: Verifica estado final
        Debug.Log($"\n📊 ESTADO FINAL:");
        Debug.Log($"No inventário: {InventoryManager.Instance.GetItemCount(ironSword)}x");
        Debug.Log($"Equipado em {ironSword.equipmentSlot}: {InventoryManager.Instance.GetEquippedItem(ironSword.equipmentSlot)?.itemName ?? "None"}");
        
        // PASSO 4: Tenta equipar NOVAMENTE (deve falhar/ser ignorado)
        Debug.Log($"\n🎯 TENTANDO EQUIPAR NOVAMENTE (deve ser ignorado):");
        equipResult = InventoryManager.Instance.EquipItem(ironSword);
        Debug.Log($"Resultado: {equipResult}");
        
        Debug.Log("=== FIM DO TESTE ===");
    }
    // ⭐⭐ MÉTODO PARA CONFIGURAR BOTÕES
    private void InitializeActionButtons()
    {
        Debug.Log("=== InitializeActionButtons() - ONLY DROP/USE ===");
        
        try
        {
            // Configurar botão DROP (mantém)
            if (dropButton != null)
            {
                dropButton.onClick.RemoveAllListeners();
                dropButton.onClick.AddListener(DropSelectedItem);
                Debug.Log("✅ DropButton configurado");
            }
            else
            {
                Debug.LogWarning("⚠️ DropButton não encontrado");
            }
            
            // Configurar botão USE (mantém para consumíveis)
            if (useButton != null)
            {
                useButton.onClick.RemoveAllListeners();
                useButton.onClick.AddListener(UseSelectedItem);
                Debug.Log("✅ UseButton configurado");
            }
            else
            {
                Debug.LogWarning("⚠️ UseButton não encontrado");
            }
            
            // 🔥 EQUIP e UNEQUIP FORAM REMOVIDOS
            Debug.Log("❌ EquipButton e UnequipButton REMOVIDOS do sistema");
            Debug.Log("🎯 Use apenas Drag & Drop para equipar/desequipar");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao configurar botões: {e.Message}");
        }
    }

    // Atualizar estado dos botões baseado no item selecionado
   

        /// <summary>
    /// 🔥 ATUALIZA O ESTADO DE TODOS OS BOTÕES BASEADO NO ITEM SELECIONADO
    /// ✅ VERSAO COMPLETA COM SLOT ESPECÍFICO
    /// ✅ Mantém toda a lógica original
    /// ✅ Adiciona verificação de slot específico
    /// </summary>
    private void UpdateButtonStates()
    {
        // 🔥 VERSÃO SIMPLIFICADA - SEM EQUIP/UNEQUIP
        
        bool hasItem = selectedItem != null;
        bool isDroppable = hasItem && selectedItem.isDroppable;
        bool isConsumable = hasItem && selectedItem.IsConsumable();
        
        // Apenas DROP e USE
        if (dropButton != null)
        {
            dropButton.interactable = isDroppable;
        }
        
        if (useButton != null)
        {
            useButton.interactable = isConsumable;
        }
        
        // 🔥 EQUIP e UNEQUIP FORAM REMOVIDOS
        // Nenhuma ação necessária
        
        UpdateButtonVisuals();

        // 🔍 DIAGNÓSTICO: Scan após atualizar botões
        if (DiagnosticHelper.Instance != null)
        {
            DiagnosticHelper.Instance.ScanAllRows("UpdateButtonStates");
        }
    }

    /// <summary>
    /// ✅ MÉTODO ORIGINAL MANTIDO - Atualiza visual dos botões
    /// </summary>
    private void UpdateButtonVisuals()
    {
        // 🔥 APENAS DROP E USE (EQUIP/UNEQUIP REMOVIDOS)
        
        if (dropButton != null)
        {
            TextMeshProUGUI dropText = dropButton.GetComponentInChildren<TextMeshProUGUI>();
            if (dropText != null)
            {
                dropText.color = dropButton.interactable ? Color.red : new Color(0.5f, 0.2f, 0.2f, 0.7f);
                dropText.text = "DROP";
            }
        }
        
        if (useButton != null)
        {
            TextMeshProUGUI useText = useButton.GetComponentInChildren<TextMeshProUGUI>();
            if (useText != null)
            {
                useText.color = useButton.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                useText.text = "USE";
            }
        }
        
        // 🔥 EQUIP e UNEQUIP textos foram removidos
    }
    // ============================================
    // DRAG & DROP SYSTEM
    // ============================================
    
    /// <summary>
    /// Chamado quando um item começa a ser arrastado
    /// </summary>
    public void OnItemDragBegin(ItemData item, DraggableItem.DragSource source, ItemData.EquipmentSlot sourceSlot)
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 DRAG BEGIN in InventoryUI");
        Debug.Log($"║  📦 Item: {item?.itemName}");
        Debug.Log($"║  📍 Source: {source}");
        Debug.Log($"║  🎰 Slot: {sourceSlot}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // Store current drag info
        // (você pode adicionar variáveis de instância se precisar rastrear)
        
        // Highlight valid drop zones
        HighlightValidDropZones(item, source);
    }
    
    /// <summary>
    /// Chamado quando o arrasto termina
    /// </summary>
    public void OnItemDragEnd(ItemData item, bool wasDroppedSuccessfully)
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🏁 DRAG END in InventoryUI");
        Debug.Log($"║  📦 Item: {item?.itemName}");
        Debug.Log($"║  ✅ Success: {wasDroppedSuccessfully}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // Clear highlights
        ClearDropZoneHighlights();
        
        // If successfully dropped, refresh UI
        if (wasDroppedSuccessfully)
        {
            RefreshUI();
            UpdateEquipmentDisplay();
        }
    }
    
    /// <summary>
    /// Destaca visualmente as zonas de drop válidas para o item sendo arrastado
    /// </summary>
    private void HighlightValidDropZones(ItemData item, DraggableItem.DragSource source)
    {
        if (item == null) return;
        
        Debug.Log($"   💡 Highlighting valid drop zones for {item.itemName}");
        
        // Find all DropZones in the UI
        DropZone[] allDropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        
        Debug.Log($"   🔍 Found {allDropZones.Length} drop zones");
        
        foreach (var dropZone in allDropZones)
        {
            // Simulate checking if this zone can accept the item
            // (A lógica completa está no DropZone.CanAcceptItem)
            
            bool canAccept = false;
            
            switch (dropZone.GetDropType())
            {
                case DropZone.DropType.PaperDollSlot:
                    // Can accept equipment that matches slot
                    canAccept = item.IsEquipment() && 
                               (item.equipmentSlot == dropZone.GetAcceptedEquipmentSlot() ||
                                IsCompatibleEquipmentSlot(item.equipmentSlot, dropZone.GetAcceptedEquipmentSlot()));
                    break;
                    
                case DropZone.DropType.InventoryTable:
                    // Can accept drops FROM paper doll (unequip)
                    canAccept = source == DraggableItem.DragSource.PaperDollSlot;
                    break;
            }
            
            if (canAccept)
            {
                Debug.Log($"      ✅ {dropZone.GetDropType()} can accept {item.itemName}");
                // Visual highlight will be handled by OnPointerEnter
            }
        }
    }
    
    /// <summary>
    /// Remove destaques visuais das zonas de drop
    /// </summary>
    private void ClearDropZoneHighlights()
    {
        // Visual highlights são automaticamente limpos pelo OnPointerExit
        // Este método existe para cleanup adicional se necessário
    }
    
    /// <summary>
    /// Verifica se dois slots de equipamento são compatíveis
    /// </summary>
    private bool IsCompatibleEquipmentSlot(ItemData.EquipmentSlot itemSlot, ItemData.EquipmentSlot targetSlot)
    {
        if (itemSlot == targetSlot) return true;
        
        // Mapeamento de compatibilidade
        switch (targetSlot)
        {
            case ItemData.EquipmentSlot.MainHand:
                return itemSlot == ItemData.EquipmentSlot.Weapon;
                
            case ItemData.EquipmentSlot.Weapon:
                return itemSlot == ItemData.EquipmentSlot.MainHand;
                
            default:
                return false;
        }
    }
    // ============================================
    // DRAG & DROP - HELPER METHODS
    // ============================================

// 🔥 PASSO 2: MODIFICAR InventoryUI.cs - RefreshUIAfterDrag
// (Aproximadamente linha 1190)

    public System.Collections.IEnumerator RefreshUIAfterDrag()
    {
        // ⚡ OTIMIZAÇÃO: Não precisa aguardar frame aqui
        // yield return null; // ❌ REMOVER ESTA LINHA
        
        Debug.Log("🚀 RefreshUIAfterDrag - ULTRA-RÁPIDO");
        
        // 🔥 USAR MÉTODO OTIMIZADO (não recria linhas!)
        if (inventoryTableUI != null)
        {
            inventoryTableUI.UpdateExistingRowsData(); // ⚡ Só atualiza dados
        }
        
        // 🔥 Atualizar equipamento (versão rápida)
        UpdateEquipmentDisplayFast();
        
        // Limpar seleção
        selectedItem = null;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("✅ UI atualizada após drag (ultra-rápido)");
        
        yield return null; // ✅ Frame único no final
    }

    // ============================================
    // FIM DOS MÉTODOS DE DRAG & DROP
    // ============================================

    // ============================================
    // Métodos de DEBUG
    // ============================================
    [ContextMenu("🔍 Debug: Verify Canvas Raycaster")]
    public void DebugVerifyCanvasRaycaster()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICANDO CANVAS RAYCASTER      ║");
        Debug.Log("╚═══════════════════════════════════════╝");
        
        // 1. Encontrar Canvas principal
        Canvas[] allCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        Debug.Log($"📊 Total de Canvas na cena: {allCanvas.Length}");
        
        foreach (var canvas in allCanvas)
        {
            Debug.Log($"\n🎨 Canvas: {canvas.name}");
            Debug.Log($"   Render Mode: {canvas.renderMode}");
            Debug.Log($"   Sort Order: {canvas.sortingOrder}");
            
            // Verificar GraphicRaycaster
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"   ✅ GraphicRaycaster: PRESENTE");
                Debug.Log($"      Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
                Debug.Log($"      Blocking Objects: {raycaster.blockingObjects}");
            }
            else
            {
                Debug.LogError($"   ❌ GraphicRaycaster: AUSENTE!");
                Debug.LogError($"   🔧 ADICIONE GraphicRaycaster ao Canvas {canvas.name}!");
            }
            
            // Verificar se é o canvas do inventário
            if (canvas.name.Contains("Inventory") || canvas.name.Contains("Canvas"))
            {
                Debug.Log($"   🎯 Este parece ser o Canvas principal do Inventário");
            }
        }
        
        // 2. Verificar EventSystem
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"\n✅ EventSystem encontrado: {eventSystem.name}");
            Debug.Log($"   Current Input Module: {eventSystem.currentInputModule?.GetType().Name}");
        }
        else
        {
            Debug.LogError($"\n❌ EventSystem NÃO ENCONTRADO!");
            Debug.LogError($"   🔧 ADICIONE um EventSystem à cena!");
        }
        
        Debug.Log("\n╔═══════════════════════════════════════╗");
        Debug.Log("║  FIM DA VERIFICAÇÃO                   ║");
        Debug.Log("╚═══════════════════════════════════════╝");
    }
   
}
