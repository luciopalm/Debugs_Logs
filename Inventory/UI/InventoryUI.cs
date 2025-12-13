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
    [SerializeField] public InventoryTableUI inventoryTableUI;
    
    [Header("New Details System")]
    [SerializeField] public InventoryItemDetailsUI inventoryItemDetailsUI;
    
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
    [SerializeField] public InventoryPaperDollUI inventoryPaperDollUI;
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
            
            // ⭐⭐ CRÍTICO: Se está abrindo, atualiza os dados!
            if (newState)
            {
                Debug.Log("Inventário ABERTO - Atualizando dados...");
                RefreshUI(); // ⭐⭐ ESTA LINHA ESTAVA FALTANDO!
                UpdateCurrencyDisplay();
                UpdateEquipmentDisplay();
            }
            else
            {
                Debug.Log("Inventário FECHADO");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERRO CRÍTICO no ToggleInventory: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
        
        Debug.Log("=== ToggleInventory FINALIZADO ===");
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
                if (inventoryPaperDollUI != null)
                {
                    // Se houver método TryEquipItem, usar
                    if (inventoryPaperDollUI.GetType().GetMethod("TryEquipItem") != null)
                    {
                        equipped = inventoryPaperDollUI.TryEquipItem(selectedItem);
                    }
                    else
                    {
                        // Fallback
                        equipped = InventoryManager.Instance.EquipItem(selectedItem);
                    }
                }
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
    
    public void EquipSelectedItem()
    {
        Debug.Log("=== EquipSelectedItem() INICIADO ===");
        
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
        
        Debug.Log($"🎯 Tentando equipar: {selectedItem.itemName}");
        
        // Salva referência ao item ANTES de equipar
        ItemData itemToEquip = selectedItem;
        
        // Tenta equipar
        bool equipped = InventoryManager.Instance.EquipItem(itemToEquip);
        
        if (equipped)
        {
            Debug.Log($"✅ {itemToEquip.itemName} equipado com sucesso!");
            
            // 🔥🔥🔥 CORREÇÃO CRÍTICA: NÃO limpar a seleção!
            // Em vez disso, garantir que o paper doll atualiza a seleção
            
            // 1. Atualiza visual do paper doll
            if (inventoryPaperDollUI != null)
            {
                inventoryPaperDollUI.UpdateAllSlots();
                
                // 🔥 NOVO: Seleciona automaticamente o slot onde o item foi equipado
                inventoryPaperDollUI.SelectSlotWithItem(itemToEquip);
                Debug.Log($"🎯 Item {itemToEquip.itemName} selecionado no paper doll");
            }
            
            // 2. Atualiza detalhes com o mesmo item (ainda selecionado)
            if (inventoryItemDetailsUI != null)
            {
                inventoryItemDetailsUI.ShowItemDetails(itemToEquip);
            }
            
            // 3. Atualiza tabela (item será removido da lista)
            if (inventoryTableUI != null)
            {
                inventoryTableUI.ForceRefresh();
            }
            
            // 4. 🔥🔥🔥 CORREÇÃO: NÃO definir selectedItem = null!
            // Mantém o item selecionado para que UNEQUIP funcione
            
            // 5. Atualiza botões
            UpdateButtonStates();
            
            Debug.Log($"✅ Equipamento concluído. Item ainda selecionado: {selectedItem?.itemName}");
        }
        else
        {
            Debug.LogError($"❌ Falha ao equipar {itemToEquip.itemName}!");
        }
        
        Debug.Log("=== EquipSelectedItem() FINALIZADO ===");
    }

    // EM InventoryUI.cs, SUBSTITUIR OnUnequipClicked():

    public void OnUnequipClicked()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("=== OnUnequipClicked() INICIADO ===");
        
        if (selectedItem == null)
        {
            Debug.LogError("❌ Nenhum item selecionado!");
            return;
        }
        
        if (!selectedItem.IsEquipment())
        {
            Debug.LogError($"❌ {selectedItem.itemName} não é equipamento!");
            return;
        }
        
        Debug.Log($"🎯 Item: {selectedItem.itemName}");
        Debug.Log($"📌 Slot: {selectedItem.equipmentSlot}");
        
        // 1. Verificar se está equipado
        var equippedItem = InventoryManager.Instance?.GetEquippedItem(selectedItem.equipmentSlot);
        
        if (equippedItem != selectedItem)
        {
            Debug.LogError($"❌ {selectedItem.itemName} não está equipado!");
            return;
        }
        
        Debug.Log($"✅ Confirmado como equipado");
        
        // 2. Salvar referência ANTES de desequipar
        ItemData itemToReselect = selectedItem;
        
        // 3. Desequipar
        ItemData unequipped = InventoryManager.Instance.UnequipItem(selectedItem.equipmentSlot);
        
        if (unequipped == null)
        {
            Debug.LogError($"❌ Falha ao desequipar!");
            return;
        }
        
        Debug.Log($"✅ {unequipped.itemName} desequipado com sucesso");
        
        // 4. 🔥 CORREÇÃO: Usar coroutine COMPLETA
        StartCoroutine(CompleteUnequipProcess(itemToReselect));
    }

    // 🔥 NOVO MÉTODO: Processo completo de unequip
    private System.Collections.IEnumerator CompleteUnequipProcess(ItemData item)
    {
        Debug.Log("🔄 Iniciando processo completo de unequip...");
        
        // PASSO 1: Limpar seleção do Paper Doll
        if (inventoryPaperDollUI != null)
        {
            inventoryPaperDollUI.ClearVisualSelection();
        }
        
        // PASSO 2: Aguardar 1 frame (eventos processados)
        yield return null;
        
        // PASSO 3: Forçar refresh da tabela
        if (inventoryTableUI != null)
        {
            Debug.Log("📊 Forçando refresh da tabela...");
            inventoryTableUI.ForceRefresh();
        }
        
        // PASSO 4: Aguardar outro frame (tabela atualizada)
        yield return null;
        
        // PASSO 5: Re-selecionar item na tabela
        Debug.Log($"🎯 Re-selecionando {item.itemName}...");
        OnItemSelected(item);
        
        // PASSO 6: Aguardar frame final
        yield return null;
        
        // PASSO 7: Atualizar equipment display
        UpdateEquipmentDisplay();
        
        // PASSO 8: 🔥 CRÍTICO - Atualizar botões por último
        Debug.Log("🔘 Atualizando estados dos botões...");
        UpdateButtonStates();
        
        // PASSO 9: Verificação final
        if (equipButton != null)
        {
            bool shouldBeActive = item != null && 
                                item.IsEquipment() && 
                                InventoryManager.Instance.HasItem(item, 1) &&
                                InventoryManager.Instance.GetEquippedItem(item.equipmentSlot) != item;
            
            Debug.Log($"🔍 Verificação final:");
            Debug.Log($"   Item no inventário: {InventoryManager.Instance.GetItemCount(item)}");
            Debug.Log($"   Item equipado: {InventoryManager.Instance.GetEquippedItem(item.equipmentSlot)?.itemName ?? "None"}");
            Debug.Log($"   EQUIP deveria estar: {(shouldBeActive ? "ATIVO ✅" : "INATIVO ❌")}");
            Debug.Log($"   EQUIP realmente está: {(equipButton.interactable ? "ATIVO ✅" : "INATIVO ❌")}");
            
            // 🔥 ÚLTIMA GARANTIA
            if (shouldBeActive && !equipButton.interactable)
            {
                Debug.LogWarning("⚠️ Forçando EQUIP ativo!");
                equipButton.interactable = true;
            }
        }
        
        Debug.Log("✅ Processo de unequip completo!");
        Debug.Log("════════════════════════════════════════");
    }

    // 🔥 NOVO MÉTODO: Atualiza UI após desequipar (com delay)
    private System.Collections.IEnumerator UpdateUIAfterUnequip(ItemData item)
    {
        // Aguarda próximo frame para garantir que:
        // - Item foi adicionado ao inventário
        // - Eventos OnInventoryChanged foram disparados
        // - Tabela foi atualizada
        yield return null;
        
        Debug.Log("📄 Atualizando UI após desequipar...");
        
        // 1. Força refresh completo da tabela
        if (inventoryTableUI != null)
        {
            Debug.Log("📄 Forçando refresh COMPLETO da tabela...");
            inventoryTableUI.ForceRefresh();
        }
        
        // 2. Atualiza equipment display
        UpdateEquipmentDisplay();
        
        // 🔥🔥🔥 CORREÇÃO CRÍTICA: RE-SELECIONA O ITEM **ANTES** DE LIMPAR PAPER DOLL
        // Isso garante que selectedItem está preenchido quando UpdateButtonStates() rodar
        Debug.Log($"🎯 Re-selecionando item na tabela: {item.itemName}");
        OnItemSelected(item);
        
        // 🔥 Aguarda mais 1 frame para garantir que OnItemSelected processou
        yield return null;
        
        // 3. 🔥 AGORA SIM limpa Paper Doll (mas selectedItem JÁ está definido)
        if (inventoryPaperDollUI != null)
        {
            Debug.Log("🧹 Limpando seleção visual do Paper Doll...");
            
            // 🔥 IMPORTANTE: Limpa APENAS a seleção visual, não chama OnItemSelected(null)
            inventoryPaperDollUI.ClearVisualSelection();
            
            Debug.Log("   ✅ Paper Doll desselecionado visualmente");
        }
        
        // 4. 🔥 FORÇA atualização dos botões DEPOIS de tudo
        Debug.Log("📘 Atualizando estados dos botões...");
        UpdateButtonStates();
        
        // 5. 🔥 VERIFICAÇÃO FINAL (não deveria mais precisar forçar)
        if (equipButton != null)
        {
            bool shouldBeActive = item != null && 
                                item.IsEquipment() && 
                                InventoryManager.Instance.HasItem(item, 1) &&
                                InventoryManager.Instance.GetEquippedItem(item.equipmentSlot) != item;
            
            if (shouldBeActive && !equipButton.interactable)
            {
                Debug.LogError("❌ EQUIP deveria estar ativo mas não está! Isso não deveria acontecer mais!");
                equipButton.interactable = true;
            }
            
            Debug.Log($"✅ EQUIP botão: {(equipButton.interactable ? "ATIVO ✅" : "INATIVO ❌")}");
        }
        
        Debug.Log("✅ UI atualizada após desequipar!");
        Debug.Log("=== OnUnequipClicked() FINALIZADO ===");
        Debug.Log("╚══════════════════════════════════════════╝");
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
    private void UpdateButtonStates()
    {
        if (!IsInventoryOpen()) return;
        
        bool hasSelectedItem = selectedItem != null;
        bool isEquipment = hasSelectedItem && selectedItem.IsEquipment();
        bool isConsumable = hasSelectedItem && selectedItem.IsConsumable();
        bool isEquipped = false;
        bool hasItemInInventory = false;
        
        // Verificar se item está no inventário
        if (hasSelectedItem && InventoryManager.Instance != null)
        {
            hasItemInInventory = InventoryManager.Instance.HasItem(selectedItem, 1);
        }
        
        // Verificar se item está equipado
        if (hasSelectedItem && isEquipment && InventoryManager.Instance != null)
        {
            var equippedItem = InventoryManager.Instance.GetEquippedItem(selectedItem.equipmentSlot);
            isEquipped = equippedItem == selectedItem;
        }
        
        // 🔥 LÓGICA CORRETA DOS BOTÕES:
        
        // USE: Só se for consumível E estiver no inventário
        if (useButton != null)
            useButton.interactable = hasSelectedItem && isConsumable && hasItemInInventory;
        
        // EQUIP: Só se for equipamento E estiver no inventário E NÃO estiver equipado
        if (equipButton != null)
            equipButton.interactable = hasSelectedItem && isEquipment && hasItemInInventory && !isEquipped;
        
        // UNEQUIP: Só se for equipamento E ESTIVER equipado
        if (unequipButton != null)
            unequipButton.interactable = hasSelectedItem && isEquipment && isEquipped;
        
        // DROP: Só se estiver no inventário E for dropável
        if (dropButton != null)
            dropButton.interactable = hasSelectedItem && hasItemInInventory && selectedItem.isDroppable;
        
        // 📊 Debug detalhado
        Debug.Log($"🔘 Button States:");
        Debug.Log($"   Item: {selectedItem?.itemName ?? "None"}");
        Debug.Log($"   In Inventory: {hasItemInInventory}");
        Debug.Log($"   Is Equipped: {isEquipped}");
        Debug.Log($"   EQUIP enabled: {equipButton?.interactable ?? false}");
        Debug.Log($"   UNEQUIP enabled: {unequipButton?.interactable ?? false}");
        Debug.Log($"   USE enabled: {useButton?.interactable ?? false}");
        Debug.Log($"   DROP enabled: {dropButton?.interactable ?? false}");
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

    /// <summary>
    /// Aguarda 1 frame e depois força refresh completo da UI
    /// Usado após drag & drop para garantir sincronização
    /// </summary>
    public System.Collections.IEnumerator RefreshUIAfterDrag()
    {
        // Aguarda 1 frame para garantir que:
        // 1. DraggableItem terminou OnEndDrag()
        // 2. Eventos foram processados
        // 3. Estado do inventário está consistente
        yield return null;
        
        Debug.Log("🔄 RefreshUIAfterDrag - Forçando refresh completo");
        
        // Força refresh COMPLETO da tabela (não usa cache)
        if (inventoryTableUI != null)
        {
            inventoryTableUI.ForceRefresh();
        }
        
        // Atualiza equipment display
        UpdateEquipmentDisplay();
        
        // Limpa seleção (item foi equipado, não está mais na tabela)
        selectedItem = null;
        
        if (inventoryItemDetailsUI != null)
        {
            inventoryItemDetailsUI.ClearItemDetails();
        }
        
        Debug.Log("✅ UI atualizada após drag & drop");
    }


    // ============================================
    // FIM DOS MÉTODOS DE DRAG & DROP
    // ============================================
    // ============================================
    // PROPRIEDADES PÚBLICAS 
    // ============================================
    public void PublicUpdateButtonStates() => UpdateButtonStates();
    public InventoryPaperDollUI PaperDollUI => inventoryPaperDollUI;
    public InventoryTableUI TableUI => inventoryTableUI;
    public InventoryItemDetailsUI DetailsUI => inventoryItemDetailsUI;
    public ItemData SelectedItem => selectedItem;

}