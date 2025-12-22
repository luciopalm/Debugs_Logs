using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Combat.TurnBased;

public class InventoryManager : MonoBehaviour
{
    // ============================================
    // SINGLETON SIMPLIFICADO
    // ============================================
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InventoryManager>();
            }
            return _instance;
        }
    }
    
    // ============================================
    // CLASSES DE DADOS
    // ============================================
    
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;
        public int slotIndex;
        public bool isEquipped;
        
        public InventorySlot(ItemData item, int quantity, int slotIndex)
        {
            this.item = item;
            this.quantity = quantity;
            this.slotIndex = slotIndex;
            this.isEquipped = false;
        }
        
        public bool IsEmpty => item == null || quantity <= 0;
        public bool IsStackFull => item != null && quantity >= item.stackLimit;
        
        public bool CanAddToStack(int amount = 1)
        {
            if (item == null) return true;
            return quantity + amount <= item.stackLimit;
        }
    }
    
    [System.Serializable]
    public class EquipmentLoadout
    {
        public ItemData weapon;
        public ItemData armor;
        public ItemData helmet;
        public ItemData gloves;
        public ItemData boots;
        public ItemData accessory;
        public ItemData ring;
        public ItemData amulet;
        public ItemData body;
        public ItemData offHand;
        public ItemData longRange;
        public ItemData mainHand;
        
        public ItemData GetItemInSlot(ItemData.EquipmentSlot slot)
        {
            switch (slot)
            {
                case ItemData.EquipmentSlot.Weapon: return weapon;
                case ItemData.EquipmentSlot.Armor: return armor;
                case ItemData.EquipmentSlot.Body: return body;
                case ItemData.EquipmentSlot.Helmet: return helmet;
                case ItemData.EquipmentSlot.Gloves: return gloves;
                case ItemData.EquipmentSlot.Boots: return boots;
                case ItemData.EquipmentSlot.Accessory: return accessory;
                case ItemData.EquipmentSlot.Ring: return ring;
                case ItemData.EquipmentSlot.Amulet: return amulet;
                case ItemData.EquipmentSlot.OffHand: return offHand;
                case ItemData.EquipmentSlot.LongRange: return longRange;
                case ItemData.EquipmentSlot.MainHand: return mainHand;
                default: return null;
            }
        }
        
        public void EquipItem(ItemData item)
        {
            if (item == null || !item.IsEquipment()) return;
            
            switch (item.equipmentSlot)
            {
                case ItemData.EquipmentSlot.Weapon: weapon = item; break;
                case ItemData.EquipmentSlot.Armor: armor = item; break;
                case ItemData.EquipmentSlot.Body: body = item; break;
                case ItemData.EquipmentSlot.Helmet: helmet = item; break;
                case ItemData.EquipmentSlot.Gloves: gloves = item; break;
                case ItemData.EquipmentSlot.Boots: boots = item; break;
                case ItemData.EquipmentSlot.Accessory: accessory = item; break;
                case ItemData.EquipmentSlot.Ring: ring = item; break;
                case ItemData.EquipmentSlot.Amulet: amulet = item; break;
                case ItemData.EquipmentSlot.OffHand: offHand = item; break;
                case ItemData.EquipmentSlot.LongRange: longRange = item; break;
                case ItemData.EquipmentSlot.MainHand: mainHand = item; break;
            }
        }

        
        
        public ItemData UnequipItem(ItemData.EquipmentSlot slot)
        {
            ItemData unequipped = GetItemInSlot(slot);
            
            switch (slot)
            {
                case ItemData.EquipmentSlot.Weapon: weapon = null; break;
                case ItemData.EquipmentSlot.Armor: armor = null; break;
                case ItemData.EquipmentSlot.Body: body = null; break;
                case ItemData.EquipmentSlot.Helmet: helmet = null; break;
                case ItemData.EquipmentSlot.Gloves: gloves = null; break;
                case ItemData.EquipmentSlot.Boots: boots = null; break;
                case ItemData.EquipmentSlot.Accessory: accessory = null; break;
                case ItemData.EquipmentSlot.Ring: ring = null; break;
                case ItemData.EquipmentSlot.Amulet: amulet = null; break;
                case ItemData.EquipmentSlot.OffHand: offHand = null; break;
                case ItemData.EquipmentSlot.LongRange: longRange = null; break;
                case ItemData.EquipmentSlot.MainHand: mainHand = null; break;
            }
            
            return unequipped;
        }
        
        public int GetTotalStatBonus(ItemData.StatType statType)
        {
            int total = 0;
            
            ItemData[] equippedItems = new ItemData[] 
            { 
                weapon, armor, body, helmet, gloves, boots, accessory, ring, amulet,
                offHand, longRange, mainHand
            };
            
            foreach (var item in equippedItems)
            {
                if (item == null) continue;
                
                switch (statType)
                {
                    case ItemData.StatType.Attack: 
                        total += item.attackBonus; break;
                    case ItemData.StatType.Defense: 
                        total += item.defenseBonus; break;
                    case ItemData.StatType.MagicAttack: 
                        total += item.magicAttackBonus; break;
                    case ItemData.StatType.MagicDefense: 
                        total += item.magicDefenseBonus; break;
                    case ItemData.StatType.Speed: 
                        total += item.speedBonus; break;
                }
            }
            
            return total;
        }
    }
    
    // ============================================
    // CONFIGURAÇÃO
    // ============================================
    
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 30;
    [SerializeField] private int maxCurrency = 999999;
    
    [Header("Starting Items (Debug)")]
    [SerializeField] private ItemData[] startingItems;
    [SerializeField] private int startingCurrency = 100;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Header("Weight System")]
    [SerializeField] private float maxWeight = 100f;
    
    // ============================================
    // DADOS DO INVENTÁRIO
    // ============================================
    
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private EquipmentLoadout currentEquipment = new EquipmentLoadout();
    private int currentCurrency = 0;
    private float currentWeight = 0f;

    // ============================================
    // CONTROLE DE SAVE/LOAD
    // ============================================
    private List<InventoryItemData> savedItemData = new List<InventoryItemData>();
    private bool isLoadedFromSave = false;  
    
    // ============================================
    // EVENTOS
    // ============================================
    
    public System.Action OnInventoryChanged;
    public System.Action OnCurrencyChanged;
    public System.Action OnEquipmentChanged;
    public System.Action<float, float> OnWeightChanged;
    
    // ============================================
    // INICIALIZAÇÃO
    // ============================================
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeInventory();
    }
    
    private void Start()
    {
        InitializeInventory();
        
        // Try to load from GameDataManager FIRST (using new system)
        if (GameDataManager.Instance != null)
        {
            LoadInventoryFromGameData();
        }
        
        // ⭐⭐ MODIFICADO: Só adiciona startingItems se NÃO TEM ITENS e NÃO É UM LOAD DE SAVE
        bool hasItems = inventorySlots.Any(slot => !slot.IsEmpty);
        
        // DEBUG
        Debug.Log($"InventoryManager.Start() - hasItems: {hasItems}, isLoadedFromSave: {isLoadedFromSave}");
        
        // ⭐⭐ IMPORTANTE: Se o jogo foi criado AGORA (não é load de save), NÃO adiciona startingItems
        // Porque o GameDataManager já adicionou via AddStartingItemsToNewGame()
        if (!hasItems && !isLoadedFromSave)
        {
            Debug.Log("⚠️ Inventory is empty but not loaded from save - checking if this is a fresh new game...");
            // Não faz nada - o GameDataManager cuidará dos itens iniciais
        }
    }
    
    private void InitializeInventory()
    {
        inventorySlots.Clear();
        
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventorySlot(null, 0, i));
        }
        
        currentWeight = 0f;
    }
    
    private void CalculateCurrentWeight()
    {
        float totalWeight = 0f;
        
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item != null)
            {
                totalWeight += slot.item.weight * slot.quantity;
            }
        }
        
        currentWeight = totalWeight;
    }
    
    // ============================================
    // INTEGRAÇÃO COM GAMEDATAMANAGER
    // ============================================
    
    private void LoadFromGameData()
    {
        var inventoryData = GameDataManager.Instance.GetInventoryData();
        if (inventoryData == null) return;
        
        InitializeInventory();
        currentCurrency = inventoryData.currency;
        
        OnInventoryChanged?.Invoke();
        OnCurrencyChanged?.Invoke();
    }
    
    public void SaveToGameData()
    {
        if (GameDataManager.Instance == null) return;
        
        // Save inventory data using new system
        SaveInventoryToGameData();
        
        // Also save to current game data slot
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData != null && gameData.saveSlot > 0)
        {
            GameDataManager.Instance.SaveGame(gameData.saveSlot);
            Debug.Log($"💾 Inventory saved to slot {gameData.saveSlot}");
        }
    }

    /// <summary>
    /// 💾 SALVA o inventário completo para o GameData
    /// </summary>
    public void SaveInventoryToGameData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] GameDataManager not found, cannot save inventory.");
            return;
        }
        
        var inventoryData = GameDataManager.Instance.GetInventoryData();
        if (inventoryData == null)
        {
            Debug.LogError("[InventoryManager] InventoryData is null!");
            return;
        }
        
        Debug.Log($"💾 Saving inventory to GameData: {inventorySlots.Count} slots, {currentCurrency} currency");
        
        // Clear existing saved data
        inventoryData.items.Clear();
        
        // Save all non-empty slots
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item != null)
            {
                var itemData = new InventoryItemData(slot.item, slot.quantity)
                {
                    slotIndex = slot.slotIndex,
                    isEquipped = slot.isEquipped
                };
                inventoryData.items.Add(itemData);
            }
        }
        
        // Save currency
        inventoryData.currency = currentCurrency;
        
        // Save weight and capacity
        inventoryData.currentWeight = currentWeight;
        inventoryData.maxWeight = maxWeight;
        inventoryData.inventorySize = inventorySize;
        
        // Save shared equipment (for compatibility)
        SaveSharedEquipmentToGameData(inventoryData);
        
        Debug.Log($"✅ Inventory saved: {inventoryData.items.Count} items, {currentCurrency} currency");
    }

    /// <summary>
    /// 📂 CARREGA o inventário completo do GameData
    /// </summary>
    public void LoadInventoryFromGameData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] GameDataManager not found, cannot load inventory.");
            isLoadedFromSave = false;
            return;
        }
        
        var inventoryData = GameDataManager.Instance.GetInventoryData();
        if (inventoryData == null)
        {
            Debug.LogWarning("[InventoryManager] InventoryData is null, starting with empty inventory.");
            isLoadedFromSave = false;
            return;
        }

            bool hasValidSave = false;
    
        // 1. Verifica se tem arquivo de save no disco
        if (GameDataManager.Instance.SaveFileExists(1))
        {
            hasValidSave = true;
            Debug.Log("✅ Save file exists on disk");
        }
        
        // 2. Verifica se tem dados no save
        if (inventoryData.items.Count > 0 || inventoryData.currency > 0)
        {
            hasValidSave = true;
            Debug.Log($"✅ Save has data: {inventoryData.items.Count} items, {inventoryData.currency} currency");
        }
        
        if (!hasValidSave)
        {
            Debug.Log("[InventoryManager] No valid save found - will use starting items");
            isLoadedFromSave = false; // ⭐ NÃO é um load de save real
            return; // ⭐ PARA AQUI, não carrega nada
        }
        
        Debug.Log($"📂 Loading inventory from GameData: {inventoryData.items.Count} items");
        
        // Clear current inventory
        InitializeInventory();
        
        // Set basic properties
        currentCurrency = inventoryData.currency;
        currentWeight = inventoryData.currentWeight;
        maxWeight = inventoryData.maxWeight;
        inventorySize = inventoryData.inventorySize;
        
        // Load items
        foreach (var savedItem in inventoryData.items)
        {
            ItemData item = ItemRegistry.GetItem(savedItem.itemID);
            if (item == null)
            {
                Debug.LogWarning($"Item not found in registry: {savedItem.itemID}");
                continue;
            }
            
            // Add item to inventory
            bool added = false;
            
            // Try to add to existing stack first
            if (item.stackLimit > 1)
            {
                foreach (var slot in inventorySlots)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.stackLimit)
                    {
                        int space = item.stackLimit - slot.quantity;
                        int addAmount = Mathf.Min(savedItem.quantity, space);
                        slot.quantity += addAmount;
                        savedItem.quantity -= addAmount;
                        
                        if (savedItem.quantity <= 0)
                        {
                            added = true;
                            break;
                        }
                    }
                }
            }
            
            // Add remaining to empty slots
            if (savedItem.quantity > 0)
            {
                foreach (var slot in inventorySlots)
                {
                    if (slot.IsEmpty)
                    {
                        slot.item = item;
                        slot.quantity = savedItem.quantity;
                        slot.slotIndex = savedItem.slotIndex >= 0 ? savedItem.slotIndex : slot.slotIndex;
                        slot.isEquipped = savedItem.isEquipped;
                        added = true;
                        break;
                    }
                }
            }
            
            if (!added)
            {
                Debug.LogWarning($"Could not add item to inventory: {item.itemName} x{savedItem.quantity}");
            }
        }
        
        // Load shared equipment 
        LoadSharedEquipmentFromGameData(inventoryData);
        
        isLoadedFromSave = true;
        
        // Trigger events
        OnInventoryChanged?.Invoke();
        OnCurrencyChanged?.Invoke();
        OnWeightChanged?.Invoke(currentWeight, maxWeight);
        
        Debug.Log($"✅ Inventory loaded: {GetUsedSlotCount()}/{inventorySize} slots, {currentCurrency} currency");
    }

    // ============================================
    // GERENCIAMENTO DE ITENS
    // ============================================
    
   public bool AddItem(ItemData item, int quantity = 1)
    {
        // ⭐⭐ DEBUG INICIAL
        //Debug.Log($"╔═══════════════════════════════════════╗");
        //Debug.Log($"║  🔧 AddItem() - DIAGNÓSTICO COMPLETO");
        //Debug.Log($"╠═══════════════════════════════════════╣");
        //Debug.Log($"║  📦 Item: {item?.itemName ?? "NULL"}");
        //Debug.Log($"║  🔢 Quantidade: {quantity}");
        //Debug.Log($"║  🆔 ID: {item?.itemID ?? "NO ID"}");
        //Debug.Log($"║  ⚖️  Peso unitário: {item?.weight:F2}");
        
        if (item == null || quantity <= 0)
        {
            //Debug.LogError($"║  ❌ Item null ou quantidade zero!");
            //Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        float addedWeight = item.weight * quantity;
        //Debug.Log($"║  📊 Peso total a adicionar: {addedWeight:F2}");
        //Debug.Log($"║  📊 Peso atual: {currentWeight:F2}/{maxWeight:F2}");
        
        // VERIFICAÇÃO DE PESO (MANTENHA O CÓDIGO ORIGINAL)
        if (currentWeight + addedWeight > maxWeight)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"[InventoryManager] Weight limit exceeded!");
            
        // Debug.LogError($"║  ❌ Limite de peso excedido!");
            //Debug.Log($"║     {currentWeight:F2} + {addedWeight:F2} > {maxWeight:F2}");
            //Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        //Debug.Log($"║  ✅ Verificação de peso: OK");
        
        // ⭐⭐ DEBUG: MOSTRAR ESTADO ATUAL DOS SLOTS
        //Debug.Log($"║  📋 ESTADO ATUAL DOS SLOTS ({inventorySlots.Count} total):");
        
        int emptySlots = 0;
        int matchingStacks = 0;
        int equippedSlotsWithItem = 0;
        
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            
            if (slot.IsEmpty)
            {
                emptySlots++;
                //Debug.Log($"║     Slot {i}: [VAZIO]");
            }
            else
            {
                string equippedMark = slot.isEquipped ? " [EQUIPADO]" : "";
                
                if (slot.item == item)
                {
                    matchingStacks++;
                    if (slot.isEquipped) equippedSlotsWithItem++;
                    //Debug.Log($"║     Slot {i}: ✅ {slot.item.itemName} x{slot.quantity}/{slot.item.stackLimit}{equippedMark}");
                }
                else
                {
                    //Debug.Log($"║     Slot {i}: {slot.item.itemName} x{slot.quantity}{equippedMark}");
                }
            }
        }
        
        //Debug.Log($"║  📊 Slots vazios: {emptySlots}");
        //Debug.Log($"║  📊 Stacks compatíveis: {matchingStacks}");
        //Debug.Log($"║  📊 Slots equipados com este item: {equippedSlotsWithItem}");
        //Debug.Log($"║  📊 Stack limit do item: {item.stackLimit}");
        
        // ⭐⭐ AGORA O CÓDIGO ORIGINAL CONTINUA (COM CORREÇÕES)
        
        // Try to add to existing stacks (INCLUINDO SLOTS EQUIPADOS!)
        if (item.stackLimit > 1)
        {
        // Debug.Log($"║  🔍 Procurando stacks existentes (incluindo equipados)...");
            
            foreach (var slot in inventorySlots)
            {
                // 🔥🔥🔥 CORREÇÃO: Aceita slots NÃO VAZIOS com o MESMO ITEM, mesmo que estejam equipados
                if (!slot.IsEmpty && slot.item == item && !slot.IsStackFull)
                {
                    //Debug.Log($"║     ✅ Stack encontrado: Slot {slot.slotIndex}");
                    //Debug.Log($"║        Equipado: {slot.isEquipped}, Quantidade: {slot.quantity}/{item.stackLimit}");
                    
                    // 🔥 SEMPRE DESMARCA isEquipped ao adicionar a um stack
                    if (slot.isEquipped)
                    {
                        Debug.Log($"║        🔧 DESMARCANDO slot {slot.slotIndex} como equipado (estava marcado)");
                        slot.isEquipped = false;
                    }
                    
                    int canAdd = item.stackLimit - slot.quantity;
                    int addAmount = Mathf.Min(quantity, canAdd);
                    
                    //Debug.Log($"║        Pode adicionar: {addAmount} (de {quantity})");
                    
                    slot.quantity += addAmount;
                    quantity -= addAmount;
                    
                    if (quantity <= 0)
                    {
                        CalculateCurrentWeight();
                        OnInventoryChanged?.Invoke();
                        
                        //Debug.Log($"║  🎉 AddItem SUCESSO (stack existente)!");
                        //Debug.Log($"╚═══════════════════════════════════════╝");
                        return true;
                    }
                    
                    //Debug.Log($"║        Restante: {quantity}");
                }
            }
            
            //Debug.Log($"║  🔍 Nenhum stack existente/completo encontrado");
        }
        
        // 🔥🔥🔥 CORREÇÃO: Fill empty slots OR slots with this item that are marked as equipped
        //Debug.Log($"║  🔍 Procurando slots vazios...");
        
        foreach (var slot in inventorySlots)
        {
            // 🔥 SIMPLIFICADO: Aceita APENAS slots VAZIOS
            if (slot.IsEmpty)
            {
            // Debug.Log($"║     ✅ Slot VAZIO encontrado: {slot.slotIndex}");
                slot.item = item;
                slot.quantity = 0;
                slot.isEquipped = false; // 🔥 GARANTIR que NÃO está equipado
                
                // Calcula quanto podemos adicionar
                int spaceInStack = item.stackLimit - slot.quantity;
                int addAmount = Mathf.Min(quantity, spaceInStack);
                
                slot.quantity += addAmount;
                quantity -= addAmount;
                
                //Debug.Log($"║        Adicionado: {addAmount} unidades");
                //Debug.Log($"║        Nova quantidade: {slot.quantity}/{item.stackLimit}");
                //Debug.Log($"║        Restante para adicionar: {quantity}");
                
                if (quantity <= 0)
                {
                    CalculateCurrentWeight();
                    OnInventoryChanged?.Invoke();
                    
                    //Debug.Log($"║  🎉 AddItem SUCESSO!");
                    //Debug.Log($"╚═══════════════════════════════════════╝");
                    return true;
                }
            }
        }
        
        if (quantity > 0)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"[InventoryManager] Not enough space!");
            
            //Debug.LogError($"║  ❌ ESPAÇO INSUFICIENTE!");
            //Debug.Log($"║     Não conseguiu adicionar {quantity} unidades");
            //Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        //Debug.Log($"║  🎉 AddItem SUCESSO COMPLETO!");
        //Debug.Log($"╚═══════════════════════════════════════╝");
        return true;
    }
    /// <summary>
    /// 🔥 DESMARCA um item como equipado no inventário
    /// Usado quando desequipamos via drag & drop
    /// </summary>
    public bool MarkItemAsUnequipped(ItemData item)
    {
        if (item == null) return false;
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔧 MarkItemAsUnequipped: {item.itemName}");
        
        bool foundAndCleared = false;
        
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item == item && slot.isEquipped)
            {
                Debug.Log($"║  ✅ Slot {slot.slotIndex}: Limpando flag isEquipped");
                slot.isEquipped = false;
                foundAndCleared = true;
                
                // 🔥 ATUALIZAR EVENTO PARA UI
                OnInventoryChanged?.Invoke();
            }
        }
        
        if (!foundAndCleared)
        {
            Debug.LogError($"║  ❌ Nenhum slot com {item.itemName} marcado como equipado!");
        }
        
        Debug.Log($"╚═══════════════════════════════════════╝");
        return foundAndCleared;
    }
    
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;
        
        int remaining = quantity;
        
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            var slot = inventorySlots[i];
            
            if (!slot.IsEmpty && slot.item == item)
            {
                int removeAmount = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= removeAmount;
                remaining -= removeAmount;
                
                if (slot.quantity <= 0)
                {
                    slot.item = null;
                    slot.quantity = 0;
                }
                
                if (remaining <= 0)
                {
                    CalculateCurrentWeight();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        return remaining <= 0;
    }
    
    public bool HasItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;
        
        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item == item)
            {
                total += slot.quantity;
                if (total >= quantity) return true;
            }
        }
        
        return total >= quantity;
    }
    
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        
        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item == item)
            {
                total += slot.quantity;
            }
        }
        
        return total;
    }
    
    public List<InventorySlot> GetSlotsWithItem(ItemData item)
    {
        List<InventorySlot> slots = new List<InventorySlot>();
        
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item == item)
            {
                slots.Add(slot);
            }
        }
        
        return slots;
    }
    
    // ============================================
    // GERENCIAMENTO DE EQUIPAMENTO
    // ============================================
    
    public bool EquipItem(ItemData item)
    {
        if (item == null || !item.IsEquipment()) return false;
        
        ItemData currentlyEquipped = currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (currentlyEquipped == item)
        {
            return true;
        }
        
        if (!HasItem(item, 1)) return false;
        
        if (!RemoveItem(item, 1)) return false;
        
        if (currentlyEquipped != null)
        {
            ItemData.EquipmentSlot targetSlot = item.equipmentSlot;
            ItemData unequippedItem = UnequipItem(targetSlot);
            
            if (unequippedItem == null)
            {
                AddItem(item, 1);
                return false;
            }
        }
        
        //  EQUIPA NO INVENTORYMANAGER
        currentEquipment.EquipItem(item);
        
        //SINCRONIZA COM O CHARACTER ATIVO
        SyncEquipmentWithActiveCharacter();
        
        ItemData verifyEquipped = currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (verifyEquipped != item)
        {
            AddItem(item, 1);
            return false;
        }
        
        OnEquipmentChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        SaveToGameData();

        var slotsWithItem = GetSlotsWithItem(item);
        foreach (var slot in slotsWithItem)
        {
            if (slot.isEquipped)
            {
                Debug.Log($"⚠️ Slot {slot.slotIndex} marcado como equipado - GARANTINDO drag ativo");
                
                // 🔥 NÃO desabilita o DraggableItem! Mantém ativo para poder desequipar via drag
                // O bug era que aqui estava desabilitando o draggable
                
                // Em vez disso, apenas marca visualmente
                // O DraggableItem continua funcionando
            }
        }
        
        return true;
    }

    /// <summary>
    /// 🚀 VERSÃO SILENCIOSA - Equipa sem disparar eventos
    /// Usado quando queremos controlar manualmente os refreshes
    /// </summary>
    public bool EquipItemSilent(ItemData item)
    {
        if (item == null || !item.IsEquipment()) return false;
        
        ItemData currentlyEquipped = currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (currentlyEquipped == item) return true;
        
        if (!HasItem(item, 1)) return false;
        if (!RemoveItem(item, 1)) return false;
        
        if (currentlyEquipped != null)
        {
            ItemData.EquipmentSlot targetSlot = item.equipmentSlot;
            ItemData unequippedItem = UnequipItemSilent(targetSlot); // ✅ Agora existe!
            
            if (unequippedItem == null)
            {
                AddItem(item, 1);
                return false;
            }
        }
        
        currentEquipment.EquipItem(item);
        SyncEquipmentWithActiveCharacter();
        
        // ❌ NÃO dispara eventos aqui!
        // OnEquipmentChanged?.Invoke();
        // OnInventoryChanged?.Invoke();
        
        SaveToGameData();
        
        return true;
    }

    /// <summary>
    /// 🚀 VERSÃO SILENCIOSA - Desequipa sem disparar eventos
    /// </summary>
    public ItemData UnequipItemSilent(ItemData.EquipmentSlot slot)
    {
        ItemData unequipped = currentEquipment.UnequipItem(slot);
        
        if (unequipped != null)
        {
            if (!CanCarryWeight(unequipped.weight))
            {
                currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            bool added = AddItem(unequipped, 1);
            
            if (!added)
            {
                currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            SyncEquipmentWithActiveCharacter();
            
            // ❌ NÃO dispara eventos!
            // OnEquipmentChanged?.Invoke();
            // OnInventoryChanged?.Invoke();
            
            SaveToGameData();
        }
        
        return unequipped;
    }

    public ItemData UnequipItem(ItemData.EquipmentSlot slot)
    {
        ItemData unequipped = currentEquipment.UnequipItem(slot);
        
        if (unequipped != null)
        {
            if (!CanCarryWeight(unequipped.weight))
            {
                currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            bool added = AddItem(unequipped, 1);
            
            if (!added)
            {
                currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            // 🔥🔥🔥 NOVO: SINCRONIZA COM O CHARACTER ATIVO
            SyncEquipmentWithActiveCharacter();
            
            OnEquipmentChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            SaveToGameData();
        }
        
        return unequipped;
    }

    // 🔥🔥🔥 NOVO MÉTODO: Sincroniza equipamento com o character ativo
    private void SyncEquipmentWithActiveCharacter()
    {
        if (PartyManager.Instance == null) return;
        
        var activeCharacter = PartyManager.Instance.GetActiveMember();
        if (activeCharacter == null) return;
        
        // Garante que o character tem um EquipmentLoadout
        if (activeCharacter.currentEquipment == null)
        {
            activeCharacter.currentEquipment = new EquipmentLoadout();
        }
        
        // 🔥 COPIA TODOS OS EQUIPAMENTOS DO INVENTORYMANAGER PARA O CHARACTER
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            // Pega do InventoryManager
            var equippedItem = currentEquipment.GetItemInSlot(slot);
            
            // Limpa o slot do character
            activeCharacter.currentEquipment.UnequipItem(slot);
            
            // Se tem item, equipa
            if (equippedItem != null)
            {
                activeCharacter.currentEquipment.EquipItem(equippedItem);
            }
        }
        
        Debug.Log($"🔄 Equipamento sincronizado com {activeCharacter.characterName}");
    }

    public void SyncFromActiveCharacter()
    {
        Debug.Log("🔄 SyncFromActiveCharacter() - Sincronizando Character → InventoryManager");
        
        if (PartyManager.Instance == null)
        {
            Debug.LogError("❌ PartyManager não encontrado!");
            return;
        }
        
        var activeChar = PartyManager.Instance.GetActiveMember();
        if (activeChar == null)
        {
            Debug.LogError("❌ Nenhum character ativo!");
            return;
        }
        
        if (activeChar.currentEquipment == null)
        {
            Debug.LogWarning("⚠️ Character não tem currentEquipment!");
            return;
        }
        
        // Percorre todos os slots de equipamento
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        int syncCount = 0;
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            // Pega do Character
            var itemInChar = activeChar.currentEquipment.GetItemInSlot(slot);
            
            // Pega do InventoryManager
            var itemInManager = currentEquipment.GetItemInSlot(slot);
            
            // Compara
            bool isDifferent = false;
            
            if (itemInChar == null && itemInManager != null)
            {
                isDifferent = true;
                Debug.LogWarning($"⚠️ Slot {slot}: Manager tem {itemInManager.itemName}, Character tem NULL");
            }
            else if (itemInChar != null && itemInManager == null)
            {
                isDifferent = true;
                Debug.LogWarning($"⚠️ Slot {slot}: Character tem {itemInChar.itemName}, Manager tem NULL");
            }
            else if (itemInChar != null && itemInManager != null)
            {
                // Compara por itemID
                if (!string.IsNullOrEmpty(itemInChar.itemID) && !string.IsNullOrEmpty(itemInManager.itemID))
                {
                    isDifferent = itemInChar.itemID != itemInManager.itemID;
                }
                else
                {
                    isDifferent = itemInChar.itemName != itemInManager.itemName;
                }
                
                if (isDifferent)
                {
                    Debug.LogWarning($"⚠️ Slot {slot}: Character={itemInChar.itemName}, Manager={itemInManager.itemName}");
                }
            }
            
            // Se diferente, sincroniza Character → Manager
            if (isDifferent)
            {
                // Limpa slot no Manager
                currentEquipment.UnequipItem(slot);
                
                // Equipa o que está no Character
                if (itemInChar != null)
                {
                    currentEquipment.EquipItem(itemInChar);
                    syncCount++;
                    Debug.Log($"   ✅ Sincronizado {slot}: {itemInChar.itemName}");
                }
            }
        }
        
        if (syncCount > 0)
        {
            Debug.Log($"✅ Sincronização completa: {syncCount} item(s) sincronizado(s)");
            OnEquipmentChanged?.Invoke();
        }
        else
        {
            Debug.Log("✅ Nenhuma inconsistência encontrada");
        }
    }

    [ContextMenu("🔄 Debug: Force Sync from Character")]
    public void DebugForceSyncFromCharacter()
    {
        SyncFromActiveCharacter();
    }
    
    public ItemData GetEquippedItem(ItemData.EquipmentSlot slot)
    {
        return currentEquipment.GetItemInSlot(slot);
    }
    
    public int GetEquipmentStatBonus(ItemData.StatType statType)
    {
        return currentEquipment.GetTotalStatBonus(statType);
    }

    public bool RemoveItemFromSlot(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count)
        {
            Debug.LogError($"[InventoryManager] Invalid slot index: {slotIndex}");
            return false;
        }
        
        var slot = inventorySlots[slotIndex];
        
        if (slot.IsEmpty || slot.quantity < quantity)
        {
            Debug.LogWarning($"[InventoryManager] Not enough items in slot {slotIndex}");
            return false;
        }
        
        slot.quantity -= quantity;
        
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }
        
        CalculateCurrentWeight();
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"[InventoryManager] Removed {quantity} {slot.item?.itemName} from slot {slotIndex}");
        return true;
    }
    
    // ============================================
    // SISTEMA DE MOEDA
    // ============================================
    
    public bool AddCurrency(int amount, bool autoSave = true)
    {
        if (amount <= 0) return false;
        
        int newAmount = Mathf.Min(currentCurrency + amount, maxCurrency);
        int added = newAmount - currentCurrency;
        
        if (added > 0)
        {
            currentCurrency = newAmount;
            OnCurrencyChanged?.Invoke();
            
            
        }
        
        return false;
    }
    
    public bool RemoveCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            OnCurrencyChanged?.Invoke();
            return true;
        }
        
        return false;
    }
    
    public bool HasCurrency(int amount)
    {
        return currentCurrency >= amount;
    }
    
    // ============================================
    // USO DE ITENS
    // ============================================
    
    public bool UseItem(ItemData item, BattleUnit target = null)
    {
        if (item == null || !item.IsConsumable()) return false;
        if (!HasItem(item, 1)) return false;
        
        bool usedSuccessfully = ApplyItemEffects(item, target);
        
        if (usedSuccessfully)
        {
            RemoveItem(item, 1);
            SaveToGameData();
        }
        
        return usedSuccessfully;
    }
    
    private bool ApplyItemEffects(ItemData item, BattleUnit target)
    {
        bool appliedEffect = false;
        
        if (item.hpRestore > 0 && target != null)
        {
            target.Heal(item.hpRestore);
            appliedEffect = true;
        }
        
        return appliedEffect;
    }
    
    // ============================================
    // MÉTODOS DE UTILIDADE
    // ============================================
    
    public List<InventorySlot> GetAllSlots()
    {
        return new List<InventorySlot>(inventorySlots);
    }
    
    public List<InventorySlot> GetNonEmptySlots()
    {
        return inventorySlots.Where(slot => !slot.IsEmpty).ToList();
    }
    
    public int GetEmptySlotCount()
    {
        return inventorySlots.Count(slot => slot.IsEmpty);
    }
    
    public int GetUsedSlotCount()
    {
        return inventorySlots.Count(slot => !slot.IsEmpty);
    }
    
    public bool IsInventoryFull()
    {
        return GetEmptySlotCount() == 0;
    }

    // ============================================
    // MÉTODOS AUXILIARES PARA SAVE/LOAD DE EQUIPAMENTOS
    // ============================================

    /// <summary>
    /// 🎯 Salva o equipamento compartilhado para o GameData
    /// </summary>
    private void SaveSharedEquipmentToGameData(InventoryData inventoryData)
    {
        if (inventoryData == null) return;
        
        // Clear all slots first
        inventoryData.sharedEquipmentLoadout = new EquipmentLoadoutData();
        
        // Save equipment by item ID
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var equippedItem = currentEquipment.GetItemInSlot(slot);
            if (equippedItem != null)
            {
                inventoryData.sharedEquipmentLoadout.SetItemIDForSlot(slot, equippedItem.itemID);
            }
        }
        
        Debug.Log("🎯 Shared equipment saved to GameData");
    }

    /// <summary>
    /// 🎯 Carrega o equipamento compartilhado do GameData
    /// </summary>
    private void LoadSharedEquipmentFromGameData(InventoryData inventoryData)
    {
        if (inventoryData == null) return;
        
        // Clear current equipment
        currentEquipment = new EquipmentLoadout();
        
        // Load equipment by item ID
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            string itemID = inventoryData.sharedEquipmentLoadout.GetItemIDForSlot(slot);
            if (!string.IsNullOrEmpty(itemID))
            {
                ItemData item = ItemRegistry.GetItem(itemID);
                if (item != null)
                {
                    currentEquipment.EquipItem(item);
                    Debug.Log($"   Loaded equipment: {item.itemName} in slot {slot}");
                }
                else
                {
                    Debug.LogWarning($"   Equipment item not found: {itemID} for slot {slot}");
                }
            }
        }
        
        Debug.Log("🎯 Shared equipment loaded from GameData");
    }
    
    // ============================================
    // DEBUGS METHODS
    // ============================================
    
    [ContextMenu("Debug: Print Inventory")]
    public void DebugPrintInventory()
    {
        Debug.Log("=== INVENTORY DEBUG ===");
        Debug.Log($"Currency: {currentCurrency}");
        Debug.Log($"Slots: {GetUsedSlotCount()}/{inventorySize}");
        Debug.Log($"Weight: {currentWeight:F1}/{maxWeight:F1} kg");
        
        int itemCount = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                itemCount++;
                Debug.Log($"Slot {slot.slotIndex}: {slot.quantity}x {slot.item.itemName}");
            }
        }
        
        if (itemCount == 0)
        {
            Debug.Log("Inventory is empty");
        }
    }
    
    [ContextMenu("Debug: Clear Inventory")]
    public void DebugClearInventory()
    {
        inventorySlots.Clear();
        InitializeInventory();
        currentCurrency = 0;
        
        OnInventoryChanged?.Invoke();
        OnCurrencyChanged?.Invoke();
    }



    [ContextMenu("🔍 Debug: Print Equipment Loadout")]
    public void DebugPrintEquipmentLoadout()
    {
        Debug.Log("╔════════════════════════════════════════════╗");
        Debug.Log("║  🎯 EQUIPMENT LOADOUT DIAGNOSIS           ║");
        Debug.Log("╠════════════════════════════════════════════╣");
        
        if (currentEquipment == null)
        {
            Debug.LogError("❌ currentEquipment is NULL!");
            Debug.Log("╚════════════════════════════════════════════╝");
            return;
        }
        
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var item = currentEquipment.GetItemInSlot(slot);
            
            if (item != null)
            {
                Debug.Log($"║  ✅ [{slot}]: {item.itemName}");
            }
            else
            {
                Debug.Log($"║  ⬜ [{slot}]: Empty");
            }
        }
        
        Debug.Log("╚════════════════════════════════════════════╝");
    }

    [ContextMenu("🧪 Test Save System")]
    public void DebugTestSaveSystem()
    {
        Debug.Log("=== 🧪 TESTANDO SISTEMA DE SAVE ===");
        
        // Adiciona alguns itens manualmente
        Debug.Log("Adicionando 100 moedas...");
        AddCurrency(100);
        
        Debug.Log("Tentando salvar...");
        SaveToGameData();
        
        Debug.Log("=== FIM DO TESTE ===");
    }

    [ContextMenu("🧪 Test REAL Persistence (Save→Reload)")]
    public void DebugTestRealPersistence()
    {
        Debug.Log("=== 🧪 TESTE PERSISTÊNCIA REAL (SAVE→RELOAD) ===");
        
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("GameDataManager.Instance é NULL!");
            return;
        }
        
        // 1. Estado inicial
        int initialCurrency = currentCurrency;
        Debug.Log($"1. Estado inicial: {initialCurrency} moedas");
        
        // 2. Modificar (adicionar 1000 moedas)
        AddCurrency(1000);
        int afterModification = currentCurrency;
        Debug.Log($"2. Após modificação: {afterModification} moedas (+1000)");
        
        // 3. SALVAR NO DISCO (slot 1)
        Debug.Log("3. Salvando NO DISCO (slot 1)...");
        GameDataManager.Instance.SaveGame(1);
        
        // 4. Modificar MAIS (para provar que vai recarregar)
        AddCurrency(500);
        Debug.Log($"4. Modificação extra: {currentCurrency} moedas (+500)");
        
        // 5. RECARREGAR DO DISCO
        Debug.Log("5. Recarregando DO DISCO...");
        bool loaded = GameDataManager.Instance.LoadGame(1);
        Debug.Log($"   LoadGame retornou: {loaded}");
        
        if (loaded)
        {
            // 6. Recarregar inventário do GameData atualizado
            LoadInventoryFromGameData();
            Debug.Log($"6. Estado final: {currentCurrency} moedas");
            
            // 7. Verificar
            if (currentCurrency == afterModification) // Deve ser 23050 (22050 + 1000)
            {
                Debug.Log("✅ PERSISTÊNCIA REAL FUNCIONANDO!");
                Debug.Log($"   Estado salvo: {afterModification}");
                Debug.Log($"   Estado carregado: {currentCurrency}");
            }
            else
            {
                Debug.LogError($"❌ PERSISTÊNCIA FALHOU!");
                Debug.Log($"   Esperado: {afterModification}");
                Debug.Log($"   Recebido: {currentCurrency}");
            }
        }
        else
        {
            Debug.LogError("❌ FALHA AO CARREGAR DO DISCO!");
        }
        
        Debug.Log("=== FIM DO TESTE ===");
    }
    [ContextMenu("🎮 Debug: Add My Test Items")]
    public void DebugAddMyTestItems()
    {
        Debug.Log("=== 🎮 ADICIONANDO SEUS ITENS ===");
        
        // Seus itens exatos
        string[] itemIDs = { "boots_sample", "old_dagger", "silver_ring", "wood" };
        
        int addedCount = 0;
        
        foreach (string itemID in itemIDs)
        {
            ItemData item = ItemRegistry.GetItem(itemID);
            
            if (item != null)
            {
                // Define quantidade baseada no tipo
                int quantity = 1;
                if (item.itemType == ItemData.ItemType.Material) // Material
                    quantity = 5;
                else if (item.stackLimit > 1) // Consumíveis
                    quantity = 3;
                
                bool success = AddItem(item, quantity);
                
                if (success)
                {
                    Debug.Log($"✅ {item.itemName} x{quantity} added");
                    addedCount++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not add {item.itemName} (inventory full?)");
                }
            }
            else
            {
                Debug.LogError($"❌ Item not found: {itemID}");
                Debug.Log("   Check: 1) Item exists in Assets 2) ItemRegistry has loaded it");
            }
        }
        
        // Adiciona moedas também
        AddCurrency(150);
        Debug.Log($"💰 +150 coins added");
        
        Debug.Log($"=== {addedCount}/{itemIDs.Length} itens adicionados ===");
        
        // Mostra status atual
        Debug.Log($"📊 Inventory now: {GetUsedSlotCount()}/{inventorySize} slots, {currentCurrency} coins");
    }

    [ContextMenu("🔍 Debug: Trace Item Source")]
    public void DebugTraceItemSource()
    {
        Debug.Log("=== 🔍 RASTREANDO ORIGEM DOS ITENS ===");
        
        // 1. Estado atual
        Debug.Log($"isLoadedFromSave: {isLoadedFromSave}");
        Debug.Log($"Starting items: {startingItems.Length}");
        Debug.Log($"Current items: {GetUsedSlotCount()}");
        
        // 2. Verifica SE tem save
        if (GameDataManager.Instance != null)
        {
            bool hasSave = GameDataManager.Instance.SaveFileExists(1);
            Debug.Log($"Save file exists: {hasSave}");
            
            var inventoryData = GameDataManager.Instance.GetInventoryData();
            if (inventoryData != null)
            {
                Debug.Log($"Items in save: {inventoryData.items.Count}");
                Debug.Log($"Currency in save: {inventoryData.currency}");
            }
        }
        
        // 3. Lista TODOS os itens atuais
        Debug.Log("📦 Itens atuais no inventário:");
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                Debug.Log($"   Slot {slot.slotIndex}: {slot.item?.itemName} x{slot.quantity}");
            }
        }
        
        Debug.Log("=== FIM ===");
    }
    
    // ============================================
    // PROPRIEDADES PÚBLICAS
    // ============================================
    
    public int Currency => currentCurrency;
    public int MaxCurrency => maxCurrency;
    public int InventorySize => inventorySize;
    public EquipmentLoadout Equipment => currentEquipment;
    public float CurrentWeight => currentWeight;
    public float MaxWeight => maxWeight;
    public bool CanCarryWeight(float additionalWeight) => currentWeight + additionalWeight <= maxWeight;
}