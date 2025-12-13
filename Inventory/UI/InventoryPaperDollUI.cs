using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryPaperDollUI : MonoBehaviour
{
    [System.Serializable]
    public class PaperDollSlot
    {
        public ItemData.EquipmentSlot slotType;
        public GameObject slotObject;
        public Image itemIcon;
        public TMP_Text slotNameText;
        public Image backgroundImage;
        
        [HideInInspector] public ItemData equippedItem;
        [HideInInspector] public Button slotButton;
    }
    
    [Header("Paper Doll Slots References")]
    [SerializeField] private PaperDollSlot[] paperDollSlots;
    
    [Header("Slot Configuration")]
    [SerializeField] private Color emptySlotColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    [SerializeField] private Color occupiedSlotColor = Color.white;
    [SerializeField] private Color selectedSlotColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    
    [Header("Drag & Drop")]
    [SerializeField] private bool enableDragDrop = true;
    [SerializeField] private GameObject dragItemPrefab;
    
    // References
    private InventoryUI inventoryUI;
    private InventoryManager inventoryManager;
    private PartyManager partyManager;
    private CharacterData currentCharacter;
    
    private PaperDollSlot selectedSlot;
    private GameObject currentDragItem;
    
    private void Start()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI == null)
            inventoryUI = InventoryUI.Instance;
        
        inventoryManager = InventoryManager.Instance;
        partyManager = PartyManager.Instance;
        
        if (partyManager == null)
        {
            Debug.LogError("[PaperDollUI] PartyManager not found!");
            return;
        }
        
        partyManager.OnActiveMemberChanged += OnActiveMemberChanged;
        currentCharacter = partyManager.GetActiveMember();
        
        if (currentCharacter != null)
        {
            if (currentCharacter.currentEquipment == null)
            {
                currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
            }
        }
        
        InitializePaperDollSlots();
        UpdateAllSlots();
    }
    
    private void OnDestroy()
    {
        if (partyManager != null)
        {
            partyManager.OnActiveMemberChanged -= OnActiveMemberChanged;
        }
    }
    
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        if (newActiveMember == null) return;
        
        currentCharacter = newActiveMember;
        UpdateAllSlots();
        ClearAllSelections();
    }
    
    private void InitializePaperDollSlots()
    {
        foreach (var slot in paperDollSlots)
        {
            if (slot.slotObject == null) continue;
            
            slot.slotButton = slot.slotObject.GetComponent<Button>();
            if (slot.slotButton == null)
            {
                slot.slotButton = slot.slotObject.AddComponent<Button>();
            }
            
            ColorBlock colors = slot.slotButton.colors;
            colors.normalColor = emptySlotColor;
            colors.highlightedColor = new Color(0.7f, 0.7f, 0.9f, 0.3f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);
            colors.selectedColor = selectedSlotColor;
            slot.slotButton.colors = colors;
            slot.slotButton.transition = Selectable.Transition.ColorTint;
            slot.slotButton.navigation = new Navigation() { mode = Navigation.Mode.None };
            
            slot.slotButton.onClick.RemoveAllListeners();
            slot.slotButton.onClick.AddListener(() => OnSlotClicked(slot));
            
            if (slot.itemIcon == null)
            {
                Image[] allImages = slot.slotObject.GetComponentsInChildren<Image>();
                foreach (Image img in allImages)
                {
                    if (img.gameObject != slot.slotObject)
                    {
                        slot.itemIcon = img;
                        break;
                    }
                }
            }
            
            if (slot.backgroundImage == null)
            {
                slot.backgroundImage = slot.slotObject.GetComponent<Image>();
            }
            
            if (slot.slotNameText != null)
            {
                slot.slotNameText.text = slot.slotType.ToString();
            }
        }
    }
    
    public void UpdateAllSlots()
    {
        if (partyManager == null || currentCharacter == null)
        {
            ClearAllSlotsToEmpty();
            return;
        }
        
        if (currentCharacter.currentEquipment == null)
        {
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        foreach (var slot in paperDollSlots)
        {
            UpdateSlot(slot);
        }
        
        if (selectedSlot != null && selectedSlot.equippedItem != null)
        {
            bool itemStillEquipped = false;
            if (currentCharacter.currentEquipment != null)
            {
                var equippedItem = currentCharacter.currentEquipment.GetItemInSlot(selectedSlot.equippedItem.equipmentSlot);
                itemStillEquipped = equippedItem == selectedSlot.equippedItem;
            }
            
            if (itemStillEquipped)
            {
                SetSlotSelected(selectedSlot, true);
            }
            else
            {
                selectedSlot = null;
            }
        }
    }
    
    private void UpdateSlot(PaperDollSlot slot)
    {
        // 🔥 DIAGNÓSTICO: Log inicial
        Debug.Log($"   🔍 UpdateSlot({slot.slotType}) - DIAGNÓSTICO:");
        
        if (currentCharacter == null)
        {
            Debug.LogError($"      ❌ currentCharacter is NULL!");
            ClearSlot(slot);
            return;
        }
        
        Debug.Log($"      ✅ currentCharacter: {currentCharacter.characterName}");
        
        if (currentCharacter.currentEquipment == null)
        {
            Debug.LogError($"      ❌ currentEquipment is NULL!");
            ClearSlot(slot);
            return;
        }
        
        Debug.Log($"      ✅ currentEquipment exists");
        
        ItemData foundItem = null;
        ItemData.EquipmentSlot[] compatibleSlots = GetCompatibleSlotsReverse(slot.slotType);
        
        Debug.Log($"      📋 Compatible slots: {string.Join(", ", compatibleSlots)}");
        
        foreach (var compatibleSlot in compatibleSlots)
        {
            foundItem = currentCharacter.currentEquipment.GetItemInSlot(compatibleSlot);
            Debug.Log($"      🔎 Checking {compatibleSlot}: {foundItem?.itemName ?? "NULL"}");
            if (foundItem != null) break;
        }
        
        // 🔥 CORREÇÃO: Sempre atualiza equippedItem AQUI
        slot.equippedItem = foundItem;
        
        Debug.Log($"      🎯 RESULT: {slot.equippedItem?.itemName ?? "NULL"}");
        
        if (slot.equippedItem != null)
        {
            if (slot.itemIcon != null)
            {
                slot.itemIcon.sprite = slot.equippedItem.icon;
                slot.itemIcon.color = slot.equippedItem.GetRarityColor();
                slot.itemIcon.gameObject.SetActive(true);
            }
            
            if (slot.backgroundImage != null)
            {
                if (slot != selectedSlot)
                {
                    slot.backgroundImage.color = occupiedSlotColor;
                }
            }
        }
        else
        {
            ClearSlot(slot);
        }
    }
    
    private void ClearSlot(PaperDollSlot slot)
    {
        slot.equippedItem = null;
        
        if (slot.itemIcon != null)
        {
            slot.itemIcon.sprite = null;
            slot.itemIcon.gameObject.SetActive(false);
        }
        
        if (slot.backgroundImage != null)
        {
            if (slot != selectedSlot)
            {
                slot.backgroundImage.color = emptySlotColor;
            }
        }
    }
    
    private void ClearAllSlotsToEmpty()
    {
        if (paperDollSlots == null) return;
        
        foreach (var slot in paperDollSlots)
        {
            ClearSlot(slot);
        }
    }
    
    // 🔥 CORREÇÃO CRÍTICA: OnSlotClicked
    private void OnSlotClicked(PaperDollSlot clickedSlot)
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 PAPER DOLL SLOT CLICKED");
        Debug.Log($"║  📍 Slot: {clickedSlot.slotType}");
        
        if (currentCharacter == null)
        {
            Debug.Log($"║  ❌ No character!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        if (inventoryUI == null)
        {
            inventoryUI = InventoryUI.Instance;
            if (inventoryUI == null)
            {
                Debug.Log($"║  ❌ InventoryUI not found!");
                Debug.Log($"╚═══════════════════════════════════════╝");
                return;
            }
        }
        
        // 🔥 CORREÇÃO: SEMPRE re-fetch do equipamento ANTES de decidir
        UpdateSlot(clickedSlot);
        
        Debug.Log($"║  📦 Item: {clickedSlot.equippedItem?.itemName ?? "Empty"}");
        
        // Clear previous selection
        if (selectedSlot != null && selectedSlot != clickedSlot)
        {
            SetSlotSelected(selectedSlot, false);
        }
        
        // Select new slot
        selectedSlot = clickedSlot;
        SetSlotSelected(clickedSlot, true);
        
        // Notify InventoryUI
        if (clickedSlot.equippedItem != null)
        {
            Debug.Log($"║  ✅ Notifying InventoryUI: {clickedSlot.equippedItem.itemName}");
            inventoryUI.OnItemSelected(clickedSlot.equippedItem);
        }
        else
        {
            Debug.Log($"║  ℹ️ Slot is empty - clearing selection");
            inventoryUI.OnItemSelected(null);
        }
        
        Debug.Log($"╚═══════════════════════════════════════╝");
    }
    
    private void SetSlotSelected(PaperDollSlot slot, bool selected)
    {
        if (slot.backgroundImage != null)
        {
            if (selected)
            {
                slot.backgroundImage.color = selectedSlotColor;
            }
            else
            {
                if (slot.equippedItem != null)
                {
                    slot.backgroundImage.color = occupiedSlotColor;
                }
                else
                {
                    slot.backgroundImage.color = emptySlotColor;
                }
            }
        }
        
        if (slot.slotButton != null)
        {
            if (selected)
            {
                slot.slotButton.Select();
            }
            else
            {
                slot.slotButton.OnDeselect(null);
            }
        }
    }

    public bool TryEquipItem(ItemData item)
    {
        if (currentCharacter == null)
        {
            Debug.LogError("[PaperDollUI] No active character!");
            return false;
        }
        
        if (item == null || !item.IsEquipment()) return false;
        
        if (!currentCharacter.CanEquipItem(item)) return false;
        
        if (!inventoryManager.HasItem(item, 1)) return false;
        
        if (!inventoryManager.RemoveItem(item, 1)) return false;
        
        ItemData currentlyEquipped = currentCharacter.currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (currentlyEquipped != null)
        {
            ItemData unequipped = UnequipItemFromCharacter(item.equipmentSlot);
            
            if (unequipped == null)
            {
                inventoryManager.AddItem(item, 1);
                return false;
            }
        }
        
        currentCharacter.currentEquipment.EquipItem(item);
        
        ItemData verifyEquipped = currentCharacter.currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (verifyEquipped != item)
        {
            inventoryManager.AddItem(item, 1);
            return false;
        }
        
        UpdateAllSlots();
        
        if (inventoryUI != null)
        {
            inventoryUI.UpdateEquipmentDisplay();
        }
        
        var itemDetailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (itemDetailsUI != null)
        {
            itemDetailsUI.UpdatePartyMemberStats();
        }
        
        return true;
    }
    
    private ItemData UnequipItemFromCharacter(ItemData.EquipmentSlot slot)
    {
        if (currentCharacter == null || currentCharacter.currentEquipment == null)
            return null;
        
        ItemData unequipped = currentCharacter.currentEquipment.UnequipItem(slot);
        
        if (unequipped != null)
        {
            if (!inventoryManager.CanCarryWeight(unequipped.weight))
            {
                currentCharacter.currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            bool added = inventoryManager.AddItem(unequipped, 1);
            
            if (!added)
            {
                currentCharacter.currentEquipment.EquipItem(unequipped);
                return null;
            }
            
            return unequipped;
        }
        
        return null;
    }
    
    private ItemData.EquipmentSlot[] GetCompatibleSlotsReverse(ItemData.EquipmentSlot paperDollSlot)
    {
        switch (paperDollSlot)
        {
            case ItemData.EquipmentSlot.MainHand:
                return new ItemData.EquipmentSlot[] 
                { 
                    ItemData.EquipmentSlot.Weapon,
                    ItemData.EquipmentSlot.MainHand,
                };
                
            case ItemData.EquipmentSlot.OffHand:
                return new ItemData.EquipmentSlot[] 
                { 
                    ItemData.EquipmentSlot.OffHand 
                };
                
            case ItemData.EquipmentSlot.LongRange:
                return new ItemData.EquipmentSlot[] 
                { 
                    ItemData.EquipmentSlot.LongRange 
                };
                
            default:
                return new ItemData.EquipmentSlot[] { paperDollSlot };
        }
    }
    
    public void UnequipSelectedSlot()
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔓 UNEQUIP SELECTED SLOT");
        
        if (selectedSlot == null)
        {
            Debug.LogError("║  ❌ No slot selected!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        // 🔥 CORREÇÃO: Re-fetch antes de desequipar
        UpdateSlot(selectedSlot);
        
        if (selectedSlot.equippedItem == null)
        {
            Debug.LogError("║  ❌ Selected slot is empty!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        if (currentCharacter == null)
        {
            Debug.LogError("║  ❌ No character!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📦 Item: {selectedSlot.equippedItem.itemName}");
        Debug.Log($"║  🎰 Slot: {selectedSlot.slotType}");
        
        ItemData.EquipmentSlot originalSlot = selectedSlot.equippedItem.equipmentSlot;
        ItemData unequippedItem = UnequipItemFromCharacter(originalSlot);
        
        if (unequippedItem != null)
        {
            Debug.Log($"║  ✅ Unequipped successfully!");
            
            if (selectedSlot != null)
            {
                SetSlotSelected(selectedSlot, false);
                selectedSlot.equippedItem = null;
                ClearSlot(selectedSlot);
            }
            
            selectedSlot = null;
            
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected(null);
                inventoryUI.UpdateEquipmentDisplay();
            }
            
            var itemDetailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
            if (itemDetailsUI != null)
            {
                itemDetailsUI.UpdatePartyMemberStats();
            }
        }
        else
        {
            Debug.LogError("║  ❌ Failed to unequip!");
        }
        
        Debug.Log($"╚═══════════════════════════════════════╝");
    }
    public void SelectSlotWithItem(ItemData item)
    {
        if (item == null) return;
        
        Debug.Log($"🎯 Procurando slot com item: {item.itemName}");
        
        // 🔥 PRECISO SABER QUAL É O NOME DO SEU ARRAY DE SLOTS
        // Pode ser: paperDollSlots, equipmentSlots, ou equipmentSlotUIs
        // Vou tentar os mais comuns:
        
        // Tentativa 1: paperDollSlots
        if (paperDollSlots != null && paperDollSlots.Length > 0)
        {
            foreach (var slot in paperDollSlots)
            {
                if (slot != null && slot.GetItemInSlot() == item)
                {
                    Debug.Log($"✅ Slot encontrado para {item.itemName} (paperDollSlots)");
                    SelectSlot(slot);
                    return;
                }
            }
        }
        
        // Tentativa 2: equipmentSlotUIs
        if (equipmentSlotUIs != null && equipmentSlotUIs.Length > 0)
        {
            foreach (var slot in equipmentSlotUIs)
            {
                if (slot != null && slot.GetItemInSlot() == item)
                {
                    Debug.Log($"✅ Slot encontrado para {item.itemName} (equipmentSlotUIs)");
                    SelectSlot(slot);
                    return;
                }
            }
        }
        
        Debug.LogWarning($"⚠️ Nenhum slot encontrado com {item.itemName}");
    }

    // 🔥 Método auxiliar para selecionar slot
    private void SelectSlot(Component slot)
    {
        // Simula seleção - depende da sua implementação
        // Você pode precisar adaptar esta parte
        Debug.Log($"🎯 Slot selecionado: {slot.name}");
        
        // Notifica InventoryUI
        if (InventoryUI.Instance != null)
        {
            // Chama método de seleção se existir
            var method = slot.GetType().GetMethod("GetItemInSlot");
            if (method != null)
            {
                ItemData slotItem = (ItemData)method.Invoke(slot, null);
                if (slotItem != null)
                {
                    InventoryUI.Instance.OnItemSelected(slotItem);
                }
            }
        }
    }
    public void ClearAllSelections()
    {
        selectedSlot = null;
        
        if (paperDollSlots != null)
        {
            foreach (var slot in paperDollSlots)
            {
                if (slot != null)
                {
                    SetSlotSelected(slot, false);
                }
            }
        }
        
        if (inventoryUI != null)
        {
            inventoryUI.OnItemSelected(null);
        }
    }

    public void ClearVisualSelection()
    {
        selectedSlot = null;
        
        if (paperDollSlots != null)
        {
            foreach (var slot in paperDollSlots)
            {
                if (slot != null)
                {
                    SetSlotSelected(slot, false);
                }
            }
        }
    }
    
    public ItemData GetItemInSelectedSlot()
    {
        // 🔥 CORREÇÃO: Re-fetch antes de retornar
        if (selectedSlot != null)
        {
            UpdateSlot(selectedSlot);
        }
        
        return selectedSlot?.equippedItem;
    }
    
    public CharacterData GetCurrentCharacter()
    {
        return currentCharacter;
    }
    
    public void BeginDrag(ItemData item)
    {
        if (!enableDragDrop || dragItemPrefab == null) return;
        
        currentDragItem = Instantiate(dragItemPrefab, transform.root);
        Image dragImage = currentDragItem.GetComponent<Image>();
        if (dragImage != null && item != null)
        {
            dragImage.sprite = item.icon;
            dragImage.color = item.GetRarityColor();
        }
    }
    
    public void EndDrag()
    {
        if (currentDragItem != null)
        {
            Destroy(currentDragItem);
            currentDragItem = null;
        }
    }
    
    [ContextMenu("Debug: Print Slot Info")]
    public void DebugPrintSlotInfo()
    {
        Debug.Log($"=== PAPER DOLL SLOTS INFO ===");
        Debug.Log($"Total slots: {paperDollSlots.Length}");
        Debug.Log($"Selected slot: {selectedSlot?.slotType.ToString() ?? "None"}");
        Debug.Log($"Current character: {currentCharacter?.characterName ?? "None"}");
        
        foreach (var slot in paperDollSlots)
        {
            string status = slot == selectedSlot ? " [SELECTED]" : "";
            Debug.Log($"- {slot.slotType}: {(slot.equippedItem != null ? slot.equippedItem.itemName : "Empty")}{status}");
        }
    }
    [ContextMenu("🔍 Debug: Print Paper Doll State")]
    public void DebugPrintPaperDollState()
    {
        Debug.Log("╔════════════════════════════════════════════╗");
        Debug.Log("║  🎯 PAPER DOLL STATE                      ║");
        Debug.Log("╠════════════════════════════════════════════╣");
        
        if (currentCharacter == null)
        {
            Debug.LogError("❌ currentCharacter is NULL!");
            Debug.Log("╚════════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  Character: {currentCharacter.characterName}");
        Debug.Log($"║  Equipment reference: {(currentCharacter.currentEquipment != null ? "✅ EXISTS" : "❌ NULL")}");
        Debug.Log("╠════════════════════════════════════════════╣");
        
        if (paperDollSlots == null || paperDollSlots.Length == 0)
        {
            Debug.LogError("❌ paperDollSlots is NULL or empty!");
            Debug.Log("╚════════════════════════════════════════════╝");
            return;
        }
        
        foreach (var slot in paperDollSlots)
        {
            if (slot == null) continue;
            
            Debug.Log($"║  Slot [{slot.slotType}]:");
            Debug.Log($"║    → equippedItem: {slot.equippedItem?.itemName ?? "NULL"}");
            
            // Check what's actually in character equipment
            if (currentCharacter.currentEquipment != null)
            {
                var compatibleSlots = GetCompatibleSlotsReverse(slot.slotType);
                foreach (var compatSlot in compatibleSlots)
                {
                    var actualItem = currentCharacter.currentEquipment.GetItemInSlot(compatSlot);
                    Debug.Log($"║    → {compatSlot} in character: {actualItem?.itemName ?? "NULL"}");
                }
            }
        }
        
        Debug.Log("╚════════════════════════════════════════════╝");
    }

    
    
}