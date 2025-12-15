using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;      
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
    /// 🔥🔥🔥 BOTÃO EQUIP
    /// ✅ Remove do slot específico
    /// ❌ NÃO chama TryEquipItem() (ele remove novamente!)
    /// ✅ Equipa DIRETAMENTE no character
    /// </summary>
    public void OnEquipButtonClicked()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🎯 BOTÃO EQUIP - SOLUÇÃO DEFINITIVA ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        // ✅ VALIDAÇÕES BÁSICAS
        if (selectedItem == null)
        {
            Debug.LogError("║  ❌ Nenhum item selecionado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        if (!selectedItem.IsEquipment())
        {
            Debug.LogError($"║  ❌ {selectedItem.itemName} não é equipamento!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📦 Item: {selectedItem.itemName}");
        Debug.Log($"║  📢 Slot Index: {selectedItemSlotIndex}");
        
        if (selectedItemSlotIndex < 0 || selectedInventorySlot == null)
        {
            Debug.LogError("║  ❌ SEM INFORMAÇÃO DE SLOT ESPECÍFICO!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("║  ❌ InventoryManager não encontrado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        if (selectedItemSlotIndex >= allSlots.Count)
        {
            Debug.LogError($"║  ❌ Slot index {selectedItemSlotIndex} inválido!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        var targetSlotToEquip = allSlots[selectedItemSlotIndex];
        
        Debug.Log($"║  🔍 Slot {selectedItemSlotIndex}:");
        Debug.Log($"║     Item: {targetSlotToEquip.item?.itemName ?? "NULL"}");
        Debug.Log($"║     Quantidade: {targetSlotToEquip.quantity}");
        Debug.Log($"║     Equipado: {targetSlotToEquip.isEquipped}");
        
        // VALIDAÇÕES DO SLOT
        if (targetSlotToEquip.item != selectedItem)
        {
            Debug.LogError("║  ❌ Slot não tem o item selecionado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        if (targetSlotToEquip.quantity <= 0)
        {
            Debug.LogError("║  ❌ Slot está vazio!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        if (targetSlotToEquip.isEquipped)
        {
            Debug.LogError("║  ❌ Item já está equipado!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        if (inventoryPaperDollUI == null)
        {
            Debug.LogError("║  ❌ inventoryPaperDollUI é NULL!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        CharacterData activeChar = inventoryPaperDollUI.GetCurrentCharacter();
        
        if (activeChar == null)
        {
            Debug.LogError("║  ❌ Nenhum character ativo!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║");
        Debug.Log($"║  ✅ Validações OK!");
        Debug.Log($"║  👤 Character: {activeChar.characterName}");
        Debug.Log($"║");
        
        // Garantir EquipmentLoadout
        if (activeChar.currentEquipment == null)
        {
            activeChar.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        // 🔥🔥🔥 PASSO 1: MARCAR SLOT COMO "EQUIPADO" (LOCK)
        Debug.Log($"║  🔒 Marcando slot {selectedItemSlotIndex} como 'equipado' (lock)...");
        targetSlotToEquip.isEquipped = true;
        
        // 🔥 PASSO 2: VERIFICAR SE JÁ TEM ALGO EQUIPADO
        ItemData.EquipmentSlot equipSlot = selectedItem.equipmentSlot;
        ItemData currentlyEquipped = activeChar.currentEquipment.GetItemInSlot(equipSlot);
        
        Debug.Log($"║  📍 Target Slot: {equipSlot}");
        Debug.Log($"║  🔍 Currently Equipped: {currentlyEquipped?.itemName ?? "Nothing"}");
        
        if (currentlyEquipped != null)
        {
            Debug.Log($"║  ⚠️ Desequipando {currentlyEquipped.itemName}...");
            
            ItemData unequipped = activeChar.currentEquipment.UnequipItem(equipSlot);
            
            if (unequipped != null)
            {
                // Devolver ao inventário (vai para outro slot)
                if (!InventoryManager.Instance.AddItem(unequipped, 1))
                {
                    Debug.LogError($"║  ❌ Não conseguiu devolver ao inventário!");
                    
                    // ROLLBACK
                    targetSlotToEquip.isEquipped = false;
                    activeChar.currentEquipment.EquipItem(unequipped);
                    Debug.Log("╚═══════════════════════════════════════╝");
                    return;
                }
                
                Debug.Log($"║  ✅ {unequipped.itemName} devolvido ao inventário");
            }
        }
        
        // 🔥 PASSO 3: EQUIPAR NO CHARACTER (ANTES DE REMOVER DO INVENTÁRIO!)
        Debug.Log($"║  🎯 Equipando {selectedItem.itemName} no character...");
        
        activeChar.currentEquipment.EquipItem(selectedItem);
        
        // Verificar se equipou
        ItemData verifyEquipped = activeChar.currentEquipment.GetItemInSlot(equipSlot);
        
        if (verifyEquipped != selectedItem)
        {
            Debug.LogError($"║  ❌ Falha ao equipar!");
            
            // ROLLBACK
            targetSlotToEquip.isEquipped = false;
            
            if (currentlyEquipped != null)
            {
                InventoryManager.Instance.RemoveItem(currentlyEquipped, 1);
                activeChar.currentEquipment.EquipItem(currentlyEquipped);
            }
            
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ {selectedItem.itemName} equipado no character!");
        
        // 🔥 PASSO 4: AGORA SIM, REMOVER DO INVENTÁRIO
        // (Como isEquipped = true, ele não vai aparecer na tabela)
        Debug.Log($"║  📤 Removendo do inventário (slot {selectedItemSlotIndex})...");
        
        bool removed = InventoryManager.Instance.RemoveItemFromSlot(selectedItemSlotIndex, 1);
        
        if (!removed)
        {
            Debug.LogError($"║  ❌ Falha ao remover!");
            
            // ROLLBACK COMPLETO
            targetSlotToEquip.isEquipped = false;
            activeChar.currentEquipment.UnequipItem(equipSlot);
            
            if (currentlyEquipped != null)
            {
                InventoryManager.Instance.RemoveItem(currentlyEquipped, 1);
                activeChar.currentEquipment.EquipItem(currentlyEquipped);
            }
            
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ Removido do inventário!");
        
        // 🔥 PASSO 5: ATUALIZAR UI
        Debug.Log($"║  🎨 Atualizando UI...");
        
        inventoryPaperDollUI.UpdateAllSlots();
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
        
        // Limpar seleção
        selectedItem = null;
        selectedItemSlotIndex = -1;
        selectedInventorySlot = null;
        selectedTableRowIndex = -1;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        RefreshUI();
        UpdateEquipmentDisplay();
        UpdateButtonStates();
        
        Debug.Log($"║  🎉 EQUIPAMENTO CONCLUÍDO COM SUCESSO!");
        Debug.Log("╚═══════════════════════════════════════╝");
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
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(false);
            if (itemInfoPanel != null)
            {
                itemInfoPanel.SetActive(false);
            }
        }
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
        Debug.Log("=== UseSelectedItem() INICIADO ===");
        
        if (selectedItem == null) 
        {
            Debug.LogError("❌ UseSelectedItem: selectedItem é NULL!");
            return;
        }
        
        Debug.Log($"Item: {selectedItem.itemName}");
        Debug.Log($"Tipo: {selectedItem.itemType}");
        Debug.Log($"É equipamento? {selectedItem.IsEquipment()}");
        
        if (selectedItem.IsConsumable())
        {
            Debug.Log($"Using consumable: {selectedItem.itemName}");
            
            // Verifica se tem o item
            if (!InventoryManager.Instance.HasItem(selectedItem, 1))
            {
                Debug.LogError($"❌ Não tem {selectedItem.itemName} no inventário!");
                return;
            }
            
            // Remove one
            InventoryManager.Instance.RemoveItem(selectedItem, 1);
            
            // Update details if still has item
            if (InventoryManager.Instance.HasItem(selectedItem, 1))
            {
                OnItemSelected(selectedItem);
            }
            else
            {
                if (inventoryItemDetailsUI != null)
                    inventoryItemDetailsUI.ClearItemDetails();
            }
            
            // 🔥 CORREÇÃO: Refresh UI DEPOIS
            RefreshUI();
            UpdateButtonStates();
        }
        else if (selectedItem.IsEquipment())
        {
            Debug.Log($"=== Tentando equipar: {selectedItem.itemName} ===");
            Debug.Log($"Equipment Slot: {selectedItem.equipmentSlot}");
            
            // 🔍 DEBUG: Verificar se item está no inventário ANTES de equipar
            int itemCountBefore = InventoryManager.Instance.GetItemCount(selectedItem);
            Debug.Log($"🔍 Item count BEFORE equip: {itemCountBefore}");
            
            // ⭐ NOVO: Usar o Paper Doll System se disponível
            bool equipped = false;
            
            if (inventoryPaperDollUI != null)
            {
                Debug.Log("🎯 Usando Paper Doll System para equipar");
                equipped = inventoryPaperDollUI.TryEquipItem(selectedItem);
                Debug.Log($"Paper Doll TryEquipItem result: {equipped}");
            }
            else
            {
                // Fallback para sistema antigo
                Debug.Log("🎯 Usando InventoryManager direto (fallback)");
                equipped = InventoryManager.Instance.EquipItem(selectedItem);
                Debug.Log($"InventoryManager EquipItem result: {equipped}");
            }
            
            if (equipped)
            {
                // 🔍 DEBUG: Verificar se item foi removido do inventário
                int itemCountAfter = InventoryManager.Instance.GetItemCount(selectedItem);
                Debug.Log($"🔍 Item count AFTER equip: {itemCountAfter}");
                
                // ⭐⭐ NOVO: NÃO limpar seleção! Apenas atualizar detalhes
                if (inventoryItemDetailsUI != null)
                    inventoryItemDetailsUI.ShowItemDetails(selectedItem);
                
                // ⭐ NOVO: Atualizar display do paper doll
                if (inventoryPaperDollUI != null)
                {
                    inventoryPaperDollUI.UpdateAllSlots();
                }
                
                // 🔥 CORREÇÃO CRÍTICA: Refresh UI ANTES de UpdateButtonStates
                RefreshUI();
                UpdateEquipmentDisplay();
                
                // ⭐⭐ DEPOIS atualizar botões (agora com dados corretos)
                UpdateButtonStates();
                
                Debug.Log($"✅ {selectedItem.itemName} equipado com sucesso!");
            }
            else
            {
                Debug.LogError($"❌ Falha ao equipar {selectedItem.itemName}!");
                
                // 🔍 Verificar por que falhou
                if (!InventoryManager.Instance.HasItem(selectedItem, 1))
                {
                    Debug.LogError($"   Razão: Item não está no inventário!");
                }
            }
        }
        else
        {
            Debug.Log($"Item não é usável: {selectedItem.itemName}");
        }
        
        Debug.Log("=== UseSelectedItem() FINALIZADO ===");
    }
    public void UnequipSelectedItem()
    {
        Debug.Log("=== UnequipSelectedItem() INICIADO ===");
        
        // ⭐ NOVO: Desequipar do paper doll
        if (inventoryPaperDollUI != null)
        {
            ItemData unequippedItem = inventoryPaperDollUI.GetItemInSelectedSlot();
            
            if (unequippedItem != null)
            {
                Debug.Log($"Unequipping from paper doll: {unequippedItem.itemName}");
                Debug.Log($"Equipment Slot: {unequippedItem.equipmentSlot}");
                
                // 🔍 DEBUG: Verificar item no inventário antes
                int itemCountBefore = InventoryManager.Instance.GetItemCount(unequippedItem);
                Debug.Log($"🔍 Item count BEFORE unequip: {itemCountBefore}");
                
                inventoryPaperDollUI.UnequipSelectedSlot();
                
                // 🔍 DEBUG: Verificar item no inventário depois
                int itemCountAfter = InventoryManager.Instance.GetItemCount(unequippedItem);
                Debug.Log($"🔍 Item count AFTER unequip: {itemCountAfter}");
                
                // Atualizar UI
                RefreshUI();
                UpdateEquipmentDisplay();
                
                Debug.Log($"✅ {unequippedItem.itemName} desequipado");
            }
            else
            {
                Debug.Log("No item selected in paper doll to unequip");
            }
        }
        else
        {
            Debug.LogWarning("Paper doll system not available");
        }
        
        Debug.Log("=== UnequipSelectedItem() FINALIZADO ===");
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
    
    // 🔥🔥🔥 SUBSTITUIR COMPLETAMENTE EquipSelectedItem() e EquipItemProcess()

    public void EquipSelectedItem()
    {
        if (selectedItem == null)
        {
            Debug.LogError("❌ Nenhum item selecionado para equipar!");
            return;
        }
        
        if (!selectedItem.IsEquipment())
        {
            Debug.LogError($"❌ {selectedItem.itemName} não é equipamento!");
            return;
        }
        
        // Salva referência ao item ANTES de equipar
        ItemData itemToEquip = selectedItem;
        ItemData.EquipmentSlot targetSlot = itemToEquip.equipmentSlot;
        
        // Inicia coroutine
        StartCoroutine(EquipItemProcess(itemToEquip, targetSlot));
    }

    // 🔥🔥🔥 CORREÇÃO: Ordem correta das operações
    private System.Collections.IEnumerator EquipItemProcess(ItemData itemToEquip, ItemData.EquipmentSlot targetSlot)
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 Equipando: {itemToEquip.itemName}");
        Debug.Log($"║  📌 Slot: {targetSlot}");
        
        // 🔥 PASSO 1: Equipar via InventoryManager
        bool equipped = false;
        
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("║  🎯 Usando Paper Doll System");
            equipped = inventoryPaperDollUI.TryEquipItem(itemToEquip);
        }
        else
        {
            Debug.Log("║  🎯 Usando InventoryManager (fallback)");
            equipped = InventoryManager.Instance.EquipItem(itemToEquip);
        }
        
        if (!equipped)
        {
            Debug.LogError("║  ❌ Falha ao equipar!");
            Debug.Log("╚═══════════════════════════════════════╝");
            yield break;
        }
        
        Debug.Log("║  ✅ Item equipado!");
        
        // 🔥 PASSO 2: Aguardar 1 frame
        yield return null;
        
        // 🔥 PASSO 3: Atualizar APENAS a tabela (sem limpar seleções)
        if (inventoryTableUI != null)
        {
            Debug.Log("║  🔄 Atualizando tabela (rápido)...");
            inventoryTableUI.UpdateExistingRowsData();
        }
        
        // 🔥 PASSO 4: Atualizar Paper Doll
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("║  🔄 Atualizando Paper Doll...");
            inventoryPaperDollUI.UpdateAllSlots();
        }
        
        // 🔥 PASSO 5: Aguardar frame
        yield return null;
        
        // 🔥🔥🔥 NOVO PASSO 6: LIMPAR TODAS AS SELEÇÕES
        Debug.Log("║  🧹 Limpando seleções após equipar...");
        
        // Limpar seleção da tabela
        if (inventoryTableUI != null)
        {
            inventoryTableUI.ClearSelection();
        }
        
        // Limpar seleção do Paper Doll (visual apenas, não chama OnItemSelected)
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.ClearVisualSelection();
        }
        
        // Limpar item selecionado local
        selectedItem = null;
        
        // Limpar details UI
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("║  ✅ Seleções limpas - usuário deve clicar no Paper Doll para desequipar");
        
        // 🔥 PASSO 7: Atualizar stats e displays
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.UpdatePartyMemberStats();
        }
        
        UpdateCurrencyDisplay();
        UpdateCapacityDisplay();
        
        // 🔥 PASSO 8: Atualizar botões (agora todos devem estar desabilitados)
        yield return null;
        UpdateButtonStates();
        
        Debug.Log("║  ✅ Processo completo!");
        Debug.Log("╚═══════════════════════════════════════╝");
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




    public void OnUnequipClicked()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("=== OnUnequipClicked() - VERSÃO DEFINITIVA ===");
        
        // 🔥 PASSO 1: VERIFICAR SE TEM PAPER DOLL
        if (inventoryPaperDollUI == null)
        {
            Debug.LogError("❌ inventoryPaperDollUI é NULL!");
            Debug.Log("║  Verifique se atribuiu o PaperDoll no Inspector");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        // 🔥 PASSO 2: PEGAR ITEM DO PAPER DOLL (ÚNICA FONTE)
        ItemData itemToUnequip = inventoryPaperDollUI.GetItemInSelectedSlot();
        
        if (itemToUnequip == null)
        {
            Debug.LogError("❌ Nenhum item selecionado no PaperDoll!");
            Debug.Log("║  ℹ️ Clique em um slot do PaperDoll que contenha um item");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📦 Item para desequipar: {itemToUnequip.itemName}");
        Debug.Log($"║  📍 Slot: {itemToUnequip.equipmentSlot}");
        Debug.Log($"║  👤 Character atual: {inventoryPaperDollUI.GetCurrentCharacter()?.characterName ?? "Unknown"}");
        
        // 🔥 PASSO 3: DESEQUIPAR DIRETAMENTE DO PAPER DOLL
        Debug.Log("║  🔄 Chamando inventoryPaperDollUI.UnequipSelectedSlot()...");
        
        try
        {
            inventoryPaperDollUI.UnequipSelectedSlot();
            Debug.Log("║  ✅ Unequip processado com sucesso!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"║  ❌ Erro ao desequipar: {e.Message}");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        // 🔥 PASSO 4: ATUALIZAR UI
        Debug.Log("║  🎨 Atualizando UI...");
        
        // Atualizar tabela (refresh otimizado)
        if (inventoryTableUI != null)
        {
            inventoryTableUI.RefreshTable(forceRefresh: false);
        }
        
        // Atualizar displays
        UpdateEquipmentDisplay();
        
        // Limpar seleção (item foi desequipado)
        selectedItem = null;
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        // Atualizar botões (agora nenhum item selecionado)
        UpdateButtonStates();
        
        Debug.Log("║  🎉 UNEQUIP COMPLETADO COM SUCESSO!");
        Debug.Log("╚═══════════════════════════════════════╝");
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
        Debug.Log("=== InitializeActionButtons() ===");
        
        try
        {
            // Configurar botão DROP
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
            
            // Configurar botão USE
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
            
            // Configurar botão EQUIP
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(EquipSelectedItem);
                Debug.Log("✅ EquipButton configurado");
            }
            else
            {
                Debug.LogWarning("⚠️ EquipButton não encontrado");
            }
            
            // Configurar botão UNEQUIP
            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveAllListeners();
                unequipButton.onClick.AddListener(OnUnequipClicked);
                Debug.Log("✅ UnequipButton configurado");
            }
            else
            {
                Debug.LogError("❌ UnequipButton não encontrado!");
            }
            
            Debug.Log("🎯 Todos os botões foram configurados!");
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
        // 🔥 LOG INICIAL
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔘 UpdateButtonStates() - INÍCIO    ║");
        Debug.Log($"╠═══════════════════════════════════════╣");
        
        // ✅ 1. INFORMAÇÕES BÁSICAS DO ITEM (MANTIDO DO ORIGINAL)
        bool hasItem = selectedItem != null;
        bool isEquipment = hasItem && selectedItem.IsEquipment();
        bool isConsumable = hasItem && selectedItem.IsConsumable();
        bool isDroppable = hasItem && selectedItem.isDroppable;
        
        // 🔥 2. INFORMAÇÕES DE SLOT ESPECÍFICO (NOVO)
        bool hasSpecificSlot = selectedItemSlotIndex >= 0 && selectedInventorySlot != null;
        bool slotHasItem = hasSpecificSlot && selectedInventorySlot.item == selectedItem;
        bool slotHasQuantity = hasSpecificSlot && selectedInventorySlot.quantity > 0;
        bool slotIsEquipped = hasSpecificSlot && selectedInventorySlot.isEquipped;
        
        // ✅ 3. LOGS DETALHADOS (MELHORADO)
        Debug.Log($"║  📦 Item Selecionado:");
        Debug.Log($"║     Nome: {selectedItem?.itemName ?? "NENHUM"}");
        Debug.Log($"║     Tipo: {selectedItem?.itemType.ToString() ?? "N/A"}");
        Debug.Log($"║     É Equipamento: {isEquipment}");
        Debug.Log($"║     É Consumível: {isConsumable}");
        Debug.Log($"║     Pode Dropar: {isDroppable}");
        
        Debug.Log($"║");
        Debug.Log($"║  🔢 Informações de Slot:");
        Debug.Log($"║     Tem Slot Específico: {hasSpecificSlot}");
        if (hasSpecificSlot)
        {
            Debug.Log($"║     Índice do Slot: {selectedItemSlotIndex}");
            Debug.Log($"║     Item no Slot: {selectedInventorySlot.item?.itemName ?? "NULL"}");
            Debug.Log($"║     Quantidade: {selectedInventorySlot.quantity}");
            Debug.Log($"║     Está Equipado: {selectedInventorySlot.isEquipped}");
            Debug.Log($"║     Slot tem este item: {slotHasItem}");
            Debug.Log($"║     Slot tem quantidade: {slotHasQuantity}");
        }
        else
        {
            Debug.Log($"║     ⚠️ Sem informação de slot específico");
        }
        
        Debug.Log($"║");
        Debug.Log($"║  🎮 Estados dos Botões:");
        
        // ✅ 4. BOTÃO EQUIP (MANTIDO + MELHORADO)
        if (equipButton != null)
        {
            // ✅ LÓGICA ORIGINAL (MANTIDA)
            bool canEquipOriginal = hasItem && isEquipment;
            
            // 🔥 ADIÇÃO: Verificação de slot específico
            bool canEquipWithSlot = canEquipOriginal;
            
            if (hasSpecificSlot)
            {
                // Só pode equipar se o slot específico ainda tem o item E não está equipado
                canEquipWithSlot = canEquipWithSlot && 
                                slotHasItem && 
                                slotHasQuantity && 
                                !slotIsEquipped;
            }
            
            equipButton.interactable = canEquipWithSlot;
            
            // ✅ LOGS ORIGINAIS (MANTIDOS)
            if (equipButton.interactable)
            {
                string slotInfo = hasSpecificSlot ? $" (Slot {selectedItemSlotIndex})" : "";
                Debug.Log($"║     🎯 EQUIPAR: ATIVO{slotInfo}");
            }
            else
            {
                string reason = "";
                if (!hasItem) reason = "Sem item";
                else if (!isEquipment) reason = "Não é equipamento";
                else if (hasSpecificSlot && !slotHasItem) reason = "Slot não tem mais este item";
                else if (hasSpecificSlot && !slotHasQuantity) reason = "Slot sem quantidade";
                else if (hasSpecificSlot && slotIsEquipped) reason = "Item já está equipado";
                Debug.Log($"║     ❌ EQUIPAR: INATIVO - {reason}");
            }
        }
        
        // ✅ 5. BOTÃO USE (MANTIDO DO ORIGINAL)
        if (useButton != null)
        {
            // ✅ LÓGICA ORIGINAL (MANTIDA)
            bool canUseOriginal = hasItem && isConsumable;
            
            // 🔥 ADIÇÃO: Verificação de slot específico
            bool canUseWithSlot = canUseOriginal;
            
            if (hasSpecificSlot)
            {
                canUseWithSlot = canUseWithSlot && slotHasItem && slotHasQuantity;
            }
            
            useButton.interactable = canUseWithSlot;
            
            // ✅ LOG ORIGINAL (MANTIDO)
            if (useButton.interactable)
            {
                Debug.Log($"║     💊 USAR: ATIVO");
            }
            else if (hasItem && !isConsumable)
            {
                Debug.Log($"║     ❌ USAR: INATIVO - Não é consumível");
            }
        }
        
        // ✅ 6. BOTÃO DROP (MANTIDO DO ORIGINAL)
        if (dropButton != null)
        {
            // ✅ LÓGICA ORIGINAL (MANTIDA)
            bool canDropOriginal = hasItem && isDroppable;
            
            // 🔥 ADIÇÃO: Verificação de slot específico
            bool canDropWithSlot = canDropOriginal;
            
            if (hasSpecificSlot)
            {
                canDropWithSlot = canDropWithSlot && slotHasItem && slotHasQuantity && !slotIsEquipped;
            }
            
            dropButton.interactable = canDropWithSlot;
            
            // ✅ LOG ORIGINAL (MANTIDO)
            if (dropButton.interactable)
            {
                Debug.Log($"║     🗑️ DROPAR: ATIVO");
            }
            else if (hasItem && !isDroppable)
            {
                Debug.Log($"║     ❌ DROPAR: INATIVO - Não pode ser dropado");
            }
            else if (hasSpecificSlot && slotIsEquipped)
            {
                Debug.Log($"║     ❌ DROPAR: INATIVO - Item está equipado");
            }
        }
        
        // ✅ 7. BOTÃO UNEQUIP (MANTIDO DO ORIGINAL)
        if (unequipButton != null)
        {
            // ✅ LÓGICA ORIGINAL (MANTIDA) - Verificar se item está equipado
            bool isCurrentlyEquipped = false;
            
            if (hasItem && isEquipment)
            {
                // Verifica no PaperDollUI se o item está equipado
                if (inventoryPaperDollUI != null)
                {
                    var equippedItem = inventoryPaperDollUI.GetItemInSelectedSlot();
                    isCurrentlyEquipped = (equippedItem == selectedItem);
                }
                
                // 🔥 ADIÇÃO: Também verifica no slot específico
                if (hasSpecificSlot)
                {
                    isCurrentlyEquipped = isCurrentlyEquipped || slotIsEquipped;
                }
            }
            
            bool canUnequip = hasItem && isEquipment && isCurrentlyEquipped;
            unequipButton.interactable = canUnequip;
            
            // ✅ LOG ORIGINAL (MANTIDO)
            if (unequipButton.interactable)
            {
                Debug.Log($"║     🔓 DESEQUIPAR: ATIVO");
            }
            else if (hasItem && isEquipment && !isCurrentlyEquipped)
            {
                Debug.Log($"║     ❌ DESEQUIPAR: INATIVO - Não está equipado");
            }
        }
        
        // ✅ 8. ATUALIZAÇÃO VISUAL DOS BOTÕES (MANTIDO DO ORIGINAL)
        UpdateButtonVisuals();
        
        Debug.Log($"║");
        Debug.Log($"║  ✅ UpdateButtonStates() COMPLETO");
        Debug.Log($"╚═══════════════════════════════════════╝");
    }

    /// <summary>
    /// ✅ MÉTODO ORIGINAL MANTIDO - Atualiza visual dos botões
    /// </summary>
    private void UpdateButtonVisuals()
    {
        // 🔥 BOTÃO EQUIP - COM INFORMAÇÃO DE SLOT
        if (equipButton != null)
        {
            TextMeshProUGUI equipText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (equipText != null)
            {
                if (equipButton.interactable)
                {
                    equipText.color = Color.white;
                    
                    // 🔥 ADIÇÃO: Mostra slot se disponível
                    if (selectedItemSlotIndex >= 0)
                    {
                        equipText.text = $"EQUIP\n(Slot {selectedItemSlotIndex})";
                    }
                    else
                    {
                        equipText.text = "EQUIP"; // ✅ TEXTO ORIGINAL
                    }
                }
                else
                {
                    equipText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // ✅ COR ORIGINAL
                    equipText.text = "EQUIP"; // ✅ TEXTO ORIGINAL
                }
            }
        }
        
        // ✅ BOTÃO USE - MANTIDO IGUAL
        if (useButton != null)
        {
            TextMeshProUGUI useText = useButton.GetComponentInChildren<TextMeshProUGUI>();
            if (useText != null)
            {
                useText.color = useButton.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                useText.text = "USE"; // ✅ TEXTO ORIGINAL
            }
        }
        
        // ✅ BOTÃO DROP - MANTIDO IGUAL
        if (dropButton != null)
        {
            TextMeshProUGUI dropText = dropButton.GetComponentInChildren<TextMeshProUGUI>();
            if (dropText != null)
            {
                dropText.color = dropButton.interactable ? Color.red : new Color(0.5f, 0.2f, 0.2f, 0.7f);
                dropText.text = "DROP"; // ✅ TEXTO ORIGINAL
            }
        }
        
        // ✅ BOTÃO UNEQUIP - MANTIDO IGUAL
        if (unequipButton != null)
        {
            TextMeshProUGUI unequipText = unequipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (unequipText != null)
            {
                unequipText.color = unequipButton.interactable ? Color.yellow : new Color(0.5f, 0.5f, 0.2f, 0.7f);
                unequipText.text = "UNEQUIP"; // ✅ TEXTO ORIGINAL
            }
        }
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

    public System.Collections.IEnumerator RefreshUIAfterDrag()
    {
        // Aguarda 1 frame para garantir que:
        // 1. DraggableItem terminou OnEndDrag()
        // 2. Eventos foram processados
        // 3. Estado do inventário está consistente
        yield return null;
        
        Debug.Log("🚀 RefreshUIAfterDrag - Refresh OTIMIZADO (não recria linhas)");
        
        // 🔥 OTIMIZAÇÃO 1: Usa RefreshTable(false) ao invés de ForceRefresh()
        // Isso reutiliza as linhas existentes ao invés de recriar tudo
        if (inventoryTableUI != null)
        {
            // false = não força recriação, apenas atualiza dados
            inventoryTableUI.RefreshTable(forceRefresh: false);
        }
        
        // 🔥 OTIMIZAÇÃO 2: Usa versão rápida do UpdateEquipmentDisplay
        UpdateEquipmentDisplayFast();
        
        // Limpa seleção (item foi equipado, não está mais na tabela)
        selectedItem = null;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("✅ UI atualizada após drag & drop (otimizado)");
    }

    // ============================================
    // FIM DOS MÉTODOS DE DRAG & DROP
    // ============================================

    // ============================================
    // Métodos de DEBUG
    // ============================================

    [ContextMenu("🔍 Debug: Check Button States")]
    public void DebugCheckButtonStates()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🔍 BUTTON STATES DIAGNOSTIC          ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        // 1. Estado básico
        Debug.Log($"║  📦 selectedItem: {selectedItem?.itemName ?? "NULL"}");
        
        if (selectedItem != null)
        {
            Debug.Log($"║     ├─ Type: {selectedItem.itemType}");
            Debug.Log($"║     ├─ Is Equipment: {selectedItem.IsEquipment()}");
            Debug.Log($"║     ├─ Slot: {selectedItem.equipmentSlot}");
            Debug.Log($"║     ├─ In Inventory: {InventoryManager.Instance?.GetItemCount(selectedItem) ?? 0}x");
            
            if (selectedItem.IsEquipment())
            {
                var equippedItem = InventoryManager.Instance?.GetEquippedItem(selectedItem.equipmentSlot);
                Debug.Log($"║     └─ Equipped in slot: {equippedItem?.itemName ?? "Empty"}");
            }
        }
        
        // 2. Paper Doll
        Debug.Log($"║");
        Debug.Log($"║  🎨 Paper Doll:");
        if (inventoryPaperDollUI != null)
        {
            var pdItem = inventoryPaperDollUI.GetItemInSelectedSlot();
            Debug.Log($"║     └─ Selected: {pdItem?.itemName ?? "None"}");
        }
        else
        {
            Debug.Log($"║     └─ NULL");
        }
        
        // 3. Estado dos botões
        Debug.Log($"║");
        Debug.Log($"║  🎮 Button States:");
        Debug.Log($"║     ├─ DROP:    {(dropButton?.interactable ?? false ? "✅ ACTIVE" : "❌ INACTIVE")}");
        Debug.Log($"║     ├─ USE:     {(useButton?.interactable ?? false ? "✅ ACTIVE" : "❌ INACTIVE")}");
        Debug.Log($"║     ├─ EQUIP:   {(equipButton?.interactable ?? false ? "✅ ACTIVE" : "❌ INACTIVE")}");
        Debug.Log($"║     └─ UNEQUIP: {(unequipButton?.interactable ?? false ? "✅ ACTIVE" : "❌ INACTIVE")}");
        
        // 4. Verificação de equipamento completa
        Debug.Log($"║");
        Debug.Log($"║  🎯 Equipment Verification:");
        
        if (InventoryManager.Instance != null)
        {
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            bool hasAnyEquipped = false;
            
            foreach (ItemData.EquipmentSlot slot in slotTypes)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var equippedItem = InventoryManager.Instance.GetEquippedItem(slot);
                if (equippedItem != null)
                {
                    hasAnyEquipped = true;
                    Debug.Log($"║     ├─ [{slot}]: {equippedItem.itemName}");
                }
            }
            
            if (!hasAnyEquipped)
            {
                Debug.Log($"║     └─ No items equipped");
            }
        }
        
        // 5. Análise de problema
        Debug.Log($"║");
        Debug.Log($"║  🚨 Problem Analysis:");
        
        if (unequipButton != null && unequipButton.interactable)
        {
            // UNEQUIP está ativo - verificar se deveria estar
            bool shouldBeActive = false;
            string reason = "";
            
            if (selectedItem != null && selectedItem.IsEquipment())
            {
                var equipped = InventoryManager.Instance?.GetEquippedItem(selectedItem.equipmentSlot);
                
                if (equipped != null)
                {
                    bool isEquipped = false;
                    if (!string.IsNullOrEmpty(selectedItem.itemID) && !string.IsNullOrEmpty(equipped.itemID))
                    {
                        isEquipped = selectedItem.itemID == equipped.itemID;
                    }
                    else
                    {
                        isEquipped = selectedItem.itemName == equipped.itemName;
                    }
                    
                    if (isEquipped)
                    {
                        shouldBeActive = true;
                        reason = "Item is equipped";
                    }
                    else
                    {
                        reason = $"Item NOT equipped (slot has: {equipped.itemName})";
                    }
                }
                else
                {
                    reason = "Equipment slot is empty";
                }
            }
            
            if (shouldBeActive)
            {
                Debug.Log($"║     ✅ UNEQUIP is correctly ACTIVE");
                Debug.Log($"║        Reason: {reason}");
            }
            else
            {
                Debug.LogError($"║     ❌❌❌ BUG DETECTED! ❌❌❌");
                Debug.LogError($"║     UNEQUIP is ACTIVE but should be INACTIVE!");
                Debug.LogError($"║     Reason: {reason}");
            }
        }
        else if (unequipButton != null && !unequipButton.interactable)
        {
            Debug.Log($"║     ✅ UNEQUIP is correctly INACTIVE");
        }
        
        Debug.Log("╚═══════════════════════════════════════╝");
    }
}

