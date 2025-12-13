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
        
        // Try to load from GameDataManager
        if (GameDataManager.Instance != null)
        {
            LoadFromGameData();
        }
        
        // Add starting items only if inventory is empty
        bool hasItems = inventorySlots.Any(slot => !slot.IsEmpty);
        
        if (!hasItems && startingItems.Length > 0)
        {
            foreach (var item in startingItems)
            {
                if (item != null)
                {
                    AddItem(item, item.stackLimit > 1 ? 3 : 1);
                }
            }
            AddCurrency(startingCurrency);
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
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null) return;
        
        var inventoryData = GameDataManager.Instance.GetInventoryData();
        if (inventoryData == null) return;
        
        inventoryData.currency = currentCurrency;
        
        OnInventoryChanged?.Invoke();
        
        if (gameData.saveSlot > 0)
        {
            GameDataManager.Instance.SaveGame(gameData.saveSlot);
        }
    }

    // ============================================
    // GERENCIAMENTO DE ITENS
    // ============================================
    
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;
        
        float addedWeight = item.weight * quantity;
        if (currentWeight + addedWeight > maxWeight)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[InventoryManager] Weight limit exceeded!");
            return false;
        }
        
        // Try to add to existing stacks
        if (item.stackLimit > 1)
        {
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty && slot.item == item && !slot.IsStackFull)
                {
                    int canAdd = item.stackLimit - slot.quantity;
                    int addAmount = Mathf.Min(quantity, canAdd);
                    
                    slot.quantity += addAmount;
                    quantity -= addAmount;
                    
                    if (quantity <= 0)
                    {
                        CalculateCurrentWeight();
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }
        
        // Fill empty slots
        foreach (var slot in inventorySlots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.quantity = Mathf.Min(quantity, item.stackLimit);
                quantity -= slot.quantity;
                
                if (quantity <= 0)
                {
                    CalculateCurrentWeight();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        if (quantity > 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[InventoryManager] Not enough space!");
            return false;
        }
        
        return true;
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
        
        // 🔥 EQUIPA NO INVENTORYMANAGER
        currentEquipment.EquipItem(item);
        
        // 🔥🔥🔥 NOVO: SINCRONIZA COM O CHARACTER ATIVO
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
        
        return true;
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
    
    // ============================================
    // SISTEMA DE MOEDA
    // ============================================
    
    public bool AddCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        int newAmount = Mathf.Min(currentCurrency + amount, maxCurrency);
        int added = newAmount - currentCurrency;
        
        if (added > 0)
        {
            currentCurrency = newAmount;
            OnCurrencyChanged?.Invoke();
            SaveToGameData();
            return true;
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
            SaveToGameData();
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
    // DEBUG
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

    // ADICIONE ESTE MÉTODO NO InventoryManager.cs

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