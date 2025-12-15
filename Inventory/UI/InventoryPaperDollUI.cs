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
        [HideInInspector] public DraggableItem draggableComponent; // 🔥 NOVO
    }
    
    [Header("Paper Doll Slots References")]
    [SerializeField] private PaperDollSlot[] paperDollSlots;
    
    [Header("Slot Configuration")]
    [SerializeField] private Color emptySlotColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    [SerializeField] private Color occupiedSlotColor = Color.white;
    [SerializeField] private Color selectedSlotColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    
    [Header("Drag & Drop")]
    [SerializeField] private bool enableDragDrop = true;
    
    // References
    private InventoryUI inventoryUI;
    private InventoryManager inventoryManager;
    private PartyManager partyManager;
    private CharacterData currentCharacter;
    
    private PaperDollSlot selectedSlot;
    
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
        
        // 🔥 CONECTAR O EVENTO
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
    
    // 🔥 ADICIONE ESTE MÉTODO SE NÃO EXISTIR
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 PaperDollUI: OnActiveMemberChanged");
        Debug.Log($"║  👤 Novo personagem: {newActiveMember?.characterName ?? "NULL"}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        if (newActiveMember == null) return;
        
        currentCharacter = newActiveMember;
        
        // 🔥 GARANTIR QUE TEM EquipmentLoadout
        if (currentCharacter.currentEquipment == null)
        {
            Debug.Log($"   🔧 Criando EquipmentLoadout para {currentCharacter.characterName}");
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        UpdateAllSlots();
        ClearAllSelections();
        
        // 🔥 FORÇAR UPDATE DO InventoryUI também
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UpdateEquipmentDisplay();
        }
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
            
            // 🔥 NOVO: Setup DraggableItem
            SetupDraggableForSlot(slot);
        }
    }
    
    // 🔥🔥🔥 NOVO MÉTODO: Configura Drag & Drop para um slot
    private void SetupDraggableForSlot(PaperDollSlot slot)
    {
        if (!enableDragDrop) return;
        if (slot.slotObject == null) return;
        
        // 1. Garantir que tem DraggableItem
        slot.draggableComponent = slot.slotObject.GetComponent<DraggableItem>();
        if (slot.draggableComponent == null)
        {
            slot.draggableComponent = slot.slotObject.AddComponent<DraggableItem>();
        }
        
        // 2. Garantir que tem Image (necessário para drag visual)
        var image = slot.slotObject.GetComponent<Image>();
        if (image == null)
        {
            image = slot.slotObject.AddComponent<Image>();
            image.color = emptySlotColor;
        }
        
        // 3. Garantir que tem CanvasGroup
        var canvasGroup = slot.slotObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = slot.slotObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void UpdateAllSlots()
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔄 PaperDollUI: UpdateAllSlots()    ║");
        Debug.Log($"║  👤 Character: {currentCharacter?.characterName ?? "NULL"}");
        Debug.Log($"║  📍 Index: {partyManager?.GetActiveIndex() ?? -1}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        if (partyManager == null || currentCharacter == null)
        {
            Debug.LogWarning("⚠️ PartyManager ou currentCharacter é null");
            ClearAllSlotsToEmpty();
            return;
        }
        
        // 🔥 GARANTIR QUE currentCharacter É O PERSONAGEM ATUAL
        currentCharacter = partyManager.GetActiveMember();
        
        if (currentCharacter == null)
        {
            Debug.LogError("❌ Não conseguiu obter active member!");
            ClearAllSlotsToEmpty();
            return;
        }
        
        Debug.Log($"   ✅ Character atualizado: {currentCharacter.characterName}");
        
        // 🔥 GARANTIR QUE TEM currentEquipment
        if (currentCharacter.currentEquipment == null)
        {
            Debug.Log($"   🔧 Criando EquipmentLoadout para {currentCharacter.characterName}");
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        // 🔥 LIMPAR TODOS OS SLOTS ANTES DE ATUALIZAR
        foreach (var slot in paperDollSlots)
        {
            if (slot != null)
            {
                ClearSlot(slot);
            }
        }
        
        // 🔥 ATUALIZAR CADA SLOT COM OS EQUIPAMENTOS DO CHARACTER ATUAL
        foreach (var slot in paperDollSlots)
        {
            if (slot != null)
            {
                UpdateSlot(slot);
            }
        }
        
        // 🔥 VERIFICAÇÃO DE DEBUG
        Debug.Log($"   📊 Equipamentos de {currentCharacter.characterName}:");
        if (currentCharacter.currentEquipment != null)
        {
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            foreach (ItemData.EquipmentSlot slotType in slotTypes)
            {
                if (slotType == ItemData.EquipmentSlot.None) continue;
                
                var item = currentCharacter.currentEquipment.GetItemInSlot(slotType);
                if (item != null)
                {
                    Debug.Log($"      [{slotType}]: {item.itemName}");
                }
            }
        }
    }
    
    private void UpdateSlot(PaperDollSlot slot)
    {
        Debug.Log($"   🔄 UpdateSlot: {slot.slotType}");
        
        if (currentCharacter == null)
        {
            Debug.LogWarning("      ❌ currentCharacter é null - limpando slot");
            ClearSlot(slot);
            return;
        }
        
        if (currentCharacter.currentEquipment == null)
        {
            Debug.LogWarning($"      ❌ {currentCharacter.characterName} não tem currentEquipment");
            ClearSlot(slot);
            return;
        }
        
        ItemData foundItem = null;
        ItemData.EquipmentSlot[] compatibleSlots = GetCompatibleSlotsReverse(slot.slotType);
        
        // 🔥 BUSCAR ITEM NO currentEquipment DO CHARACTER ATUAL
        foreach (var compatibleSlot in compatibleSlots)
        {
            foundItem = currentCharacter.currentEquipment.GetItemInSlot(compatibleSlot);
            if (foundItem != null) 
            {
                Debug.Log($"      ✅ Encontrou {foundItem.itemName} no slot {compatibleSlot}");
                break;
            }
        }
        
        slot.equippedItem = foundItem;
        
        if (slot.equippedItem != null)
        {
            Debug.Log($"      🎯 Atualizando slot {slot.slotType} com {slot.equippedItem.itemName}");
            
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
            
            // 🔥 ATUALIZAR DraggableItem
            UpdateDraggableForSlot(slot);
        }
        else
        {
            Debug.Log($"      🟡 Slot {slot.slotType} está vazio");
            ClearSlot(slot);
        }
    }
    
    // 🔥🔥🔥 NOVO MÉTODO: Atualiza DraggableItem de um slot
    private void UpdateDraggableForSlot(PaperDollSlot slot)
    {
        if (!enableDragDrop) return;
        if (slot.draggableComponent == null) return;
        
        if (slot.equippedItem != null)
        {
            // Configurar para arrastar DO paper doll
            slot.draggableComponent.SetupDraggable(
                slot.equippedItem,
                DraggableItem.DragSource.PaperDollSlot,
                slot.equippedItem.equipmentSlot
            );
            
            slot.draggableComponent.enabled = true;
        }
        else
        {
            // Desabilitar drag se slot vazio
            slot.draggableComponent.enabled = false;
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
        
        // 🔥 NOVO: Desabilitar drag quando limpar
        if (slot.draggableComponent != null)
        {
            slot.draggableComponent.enabled = false;
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
    
    private void OnSlotClicked(PaperDollSlot clickedSlot)
    {
        Debug.Log($"╔═══════════════════════════════════════════╗");
        Debug.Log($"║  🖱️ OnSlotClicked: {clickedSlot.slotType}");
        
        if (currentCharacter == null)
        {
            Debug.LogError("║  ❌ No character!");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  👤 Character: {currentCharacter.characterName}");
        
        if (inventoryUI == null)
        {
            inventoryUI = InventoryUI.Instance;
            if (inventoryUI == null)
            {
                Debug.LogError("║  ❌ InventoryUI not found!");
                Debug.Log($"╚═══════════════════════════════════════════╝");
                return;
            }
        }
        
        // : Atualizar slot ANTES de pegar item
        UpdateSlot(clickedSlot);
        
        Debug.Log($"║  📦 Item no slot: {clickedSlot.equippedItem?.itemName ?? "Empty"}");
        
        //  Clear previous selection
        if (selectedSlot != null && selectedSlot != clickedSlot)
        {
            Debug.Log($"║  🧹 Limpando seleção anterior: {selectedSlot.slotType}");
            SetSlotSelected(selectedSlot, false);
        }
        
        //  Select new slot
        selectedSlot = clickedSlot;
        SetSlotSelected(clickedSlot, true);
        
        Debug.Log($"║  ✅ selectedSlot agora é: {selectedSlot.slotType}");
        
        // 🔥 Notify InventoryUI
        if (clickedSlot.equippedItem != null)
        {
            Debug.Log($"║  📢 Notificando InventoryUI: {clickedSlot.equippedItem.itemName}");
            inventoryUI.OnItemSelected(clickedSlot.equippedItem);
        }
        else
        {
            Debug.Log($"║  📢 Notificando InventoryUI: NULL (slot vazio)");
            inventoryUI.OnItemSelected(null);
        }
        
        Debug.Log($"╚═══════════════════════════════════════════╝");
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
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🛠️ TryEquipItem - EQUIP INDIVIDUAL");
        Debug.Log($"╠═══════════════════════════════════════╣");
        
        if (currentCharacter == null)
        {
            Debug.LogError("║  ❌ Nenhum character selecionado!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  👤 Character: {currentCharacter.characterName}");
        Debug.Log($"║  📦 Item: {item?.itemName}");
        Debug.Log($"║  📍 Slot: {item?.equipmentSlot}");
        
        // 🔥 VALIDAÇÕES BÁSICAS
        if (item == null || !item.IsEquipment())
        {
            Debug.LogError($"║  ❌ Item inválido!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        if (!currentCharacter.CanEquipItem(item))
        {
            Debug.LogError($"║  ❌ {currentCharacter.characterName} não pode equipar {item.itemName}!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        // 🔥 1. VERIFICAR SE ITEM ESTÁ NO INVENTÁRIO (compartilhado)
        if (!InventoryManager.Instance.HasItem(item, 1))
        {
            Debug.LogError($"║  ❌ {item.itemName} não está no inventário!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ Item está no inventário compartilhado");
        
        // 🔥 2. GARANTIR QUE O CHARACTER TEM EquipmentLoadout
        if (currentCharacter.currentEquipment == null)
        {
            Debug.Log($"║  🔧 Criando EquipmentLoadout para {currentCharacter.characterName}");
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        // 🔥 3. VERIFICAR ITEM ATUALMENTE EQUIPADO NESTE CHARACTER
        ItemData currentlyEquipped = currentCharacter.currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (currentlyEquipped != null)
        {
            Debug.Log($"║  ⚠️ {currentCharacter.characterName} já tem {currentlyEquipped.itemName} equipado");
            
            // 🔥 4. DESEQUIPAR DO CHARACTER (não do InventoryManager!)
            Debug.Log($"║  🔄 Desequipando {currentlyEquipped.itemName}...");
            
            // Remove do character
            ItemData unequipped = currentCharacter.currentEquipment.UnequipItem(item.equipmentSlot);
            
            if (unequipped != null)
            {
                // 🔥 ADICIONAR AO INVENTÁRIO COMPARTILHADO
                if (!InventoryManager.Instance.AddItem(unequipped, 1))
                {
                    Debug.LogError($"║  ❌ Não conseguiu devolver {unequipped.itemName} ao inventário!");
                    // Re-equipar no character
                    currentCharacter.currentEquipment.EquipItem(unequipped);
                    Debug.Log($"╚═══════════════════════════════════════╝");
                    return false;
                }
                
                Debug.Log($"║  ✅ {unequipped.itemName} devolvido ao inventário");
            }
        }
        
        // 🔥 5. REMOVER ITEM DO INVENTÁRIO COMPARTILHADO
        if (!InventoryManager.Instance.RemoveItem(item, 1))
        {
            Debug.LogError($"║  ❌ Não conseguiu remover {item.itemName} do inventário!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ {item.itemName} removido do inventário");
        
        // 🔥 6. EQUIPAR NO CHARACTER (APENAS NELE!)
        currentCharacter.currentEquipment.EquipItem(item);
        
        // 🔥 7. VERIFICAR SE REALMENTE FOI EQUIPADO
        ItemData verifyEquipped = currentCharacter.currentEquipment.GetItemInSlot(item.equipmentSlot);
        
        if (verifyEquipped != item)
        {
            Debug.LogError($"║  ❌ FALHA: {item.itemName} não foi equipado em {currentCharacter.characterName}!");
            // Devolver ao inventário
            InventoryManager.Instance.AddItem(item, 1);
            Debug.Log($"╚═══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ {item.itemName} equipado em {currentCharacter.characterName}");
        
        // 🔥 8. ATUALIZAR UI
        UpdateAllSlots();
        
        if (inventoryUI != null)
        {
            inventoryUI.UpdateEquipmentDisplay();
        }
        
        // Atualizar stats
        var itemDetailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (itemDetailsUI != null)
        {
            itemDetailsUI.UpdatePartyMemberStats();
        }
        
        Debug.Log($"║  🎉 EQUIPAMENTO INDIVIDUAL CONCLUÍDO!");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
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
    

    /// <summary>
    /// 🎯 Seleciona um slot específico no Paper Doll
    /// Usado após equipar via botão EQUIP para manter consistência
    /// </summary>
    public bool SelectSlotByType(ItemData.EquipmentSlot targetSlot)
    {
        Debug.Log($"   🔍 SelectSlotByType({targetSlot}) - Início");
        
        if (paperDollSlots == null || paperDollSlots.Length == 0)
        {
            Debug.LogError("   ❌ paperDollSlots não configurado!");
            return false;
        }
        
        Debug.Log($"   📊 Verificando {paperDollSlots.Length} slots...");
        
        // Procurar slot compatível
        foreach (var slot in paperDollSlots)
        {
            if (slot == null)
            {
                Debug.LogWarning("   ⚠️ Slot null");
                continue;
            }
            
            if (slot.slotObject == null)
            {
                Debug.LogWarning($"   ⚠️ {slot.slotType}: slotObject null");
                continue;
            }
            
            Debug.Log($"   🔍 Verificando slot: {slot.slotType}");
            
            // Verificar compatibilidade
            ItemData.EquipmentSlot[] compatibleSlots = GetCompatibleSlotsReverse(slot.slotType);
            
            Debug.Log($"      Compatible com: {string.Join(", ", compatibleSlots)}");
            
            bool isCompatible = false;
            foreach (var compatSlot in compatibleSlots)
            {
                if (compatSlot == targetSlot)
                {
                    isCompatible = true;
                    break;
                }
            }
            
            if (isCompatible)
            {
                Debug.Log($"   ✅ MATCH! Slot {slot.slotType} é compatível com {targetSlot}");
                
                // 🔥 Verificar se tem item equipado
                if (slot.equippedItem != null)
                {
                    Debug.Log($"      Item equipado: {slot.equippedItem.itemName}");
                }
                else
                {
                    Debug.LogWarning($"      ⚠️ Slot está VAZIO!");
                }
                
                // Simular clique no slot
                Debug.Log($"      🖱️ Chamando OnSlotClicked()...");
                OnSlotClicked(slot);
                
                Debug.Log($"   ✅ SelectSlotByType SUCESSO");
                return true;
            }
            else
            {
                Debug.Log($"      ❌ NÃO compatível");
            }
        }
        
        Debug.LogError($"   ❌ Não encontrou slot compatível para {targetSlot}!");
        return false;
    }
    

    public void UnequipSelectedSlot()
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔓 UnequipSelectedSlot - INDIVIDUAL");
        Debug.Log($"╠═══════════════════════════════════════╣");
        
        if (currentCharacter == null)
        {
            Debug.LogError("║  ❌ Nenhum character selecionado!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        if (selectedSlot == null)
        {
            Debug.LogError("║  ❌ Nenhum slot selecionado no PaperDoll!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  👤 Character: {currentCharacter.characterName}");
        Debug.Log($"║  📍 Slot: {selectedSlot.slotType}");
        
        // 🔥 ATUALIZAR SLOT ANTES DE CONTINUAR
        UpdateSlot(selectedSlot);
        
        if (selectedSlot.equippedItem == null)
        {
            Debug.LogError($"║  ❌ Slot {selectedSlot.slotType} está vazio!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        ItemData itemToUnequip = selectedSlot.equippedItem;
        Debug.Log($"║  📦 Item para desequipar: {itemToUnequip.itemName}");
        
        // 🔥 1. VERIFICAR SE O CHARACTER TEM O ITEM EQUIPADO
        if (currentCharacter.currentEquipment == null)
        {
            Debug.LogError($"║  ❌ {currentCharacter.characterName} não tem currentEquipment!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        ItemData itemInCharacter = currentCharacter.currentEquipment.GetItemInSlot(itemToUnequip.equipmentSlot);
        
        if (itemInCharacter == null)
        {
            Debug.LogError($"║  ❌ {itemToUnequip.itemName} não está equipado em {currentCharacter.characterName}!");
            // Limpar slot visual mesmo assim
            ClearSlot(selectedSlot);
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        // 🔥 2. DESEQUIPAR DO CHARACTER (APENAS DELE!)
        ItemData unequippedItem = currentCharacter.currentEquipment.UnequipItem(itemToUnequip.equipmentSlot);
        
        if (unequippedItem == null)
        {
            Debug.LogError($"║  ❌ Falha ao desequipar {itemToUnequip.itemName}!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ {unequippedItem.itemName} desequipado de {currentCharacter.characterName}");
        
        // 🔥 3. ADICIONAR AO INVENTÁRIO COMPARTILHADO
        if (!InventoryManager.Instance.AddItem(unequippedItem, 1))
        {
            Debug.LogError($"║  ❌ Não conseguiu adicionar {unequippedItem.itemName} ao inventário!");
            // Re-equipar no character
            currentCharacter.currentEquipment.EquipItem(unequippedItem);
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ {unequippedItem.itemName} adicionado ao inventário compartilhado");
        
        // 🔥 4. LIMPAR SLOT VISUAL
        ClearSlot(selectedSlot);
        selectedSlot = null;
        
        // 🔥 5. ATUALIZAR TODOS OS SLOTS
        UpdateAllSlots();
        
        // 🔥 6. NOTIFICAR InventoryUI
        if (inventoryUI != null)
        {
            inventoryUI.OnItemSelected(null);
            inventoryUI.UpdateEquipmentDisplay();
        }
        
        // Atualizar stats
        var itemDetailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (itemDetailsUI != null)
        {
            itemDetailsUI.UpdatePartyMemberStats();
        }
        
        Debug.Log($"║  🎉 DESEQUIPAMENTO INDIVIDUAL CONCLUÍDO!");
        Debug.Log($"╚═══════════════════════════════════════╝");
    }

    // 🔥 NOVO MÉTODO AUXILIAR: Encontra slot por tipo de equipamento
    private PaperDollSlot FindSlotByEquipmentSlot(ItemData.EquipmentSlot targetSlot)
    {
        if (paperDollSlots == null) return null;
        
        foreach (var slot in paperDollSlots)
        {
            if (slot == null) continue;
            
            // Verifica compatibilidade
            var compatibleSlots = GetCompatibleSlotsReverse(slot.slotType);
            
            foreach (var compatSlot in compatibleSlots)
            {
                if (compatSlot == targetSlot)
                {
                    return slot;
                }
            }
        }
        
        return null;
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
        Debug.Log("🔍 GetItemInSelectedSlot() chamado");
        
        // Verifica se tem slot selecionado
        if (selectedSlot == null)
        {
            Debug.Log("   ❌ selectedSlot é NULL - retornando NULL");
            return null;
        }
        
        Debug.Log($"   ✅ selectedSlot: {selectedSlot.slotType}");
        Debug.Log($"   📦 equippedItem no selectedSlot: {selectedSlot.equippedItem?.itemName ?? "NULL"}");
        
        // 🔥 ATUALIZAR O SLOT ANTES DE RETORNAR
        UpdateSlot(selectedSlot);
        
        Debug.Log($"   📦 equippedItem APÓS UpdateSlot: {selectedSlot.equippedItem?.itemName ?? "NULL"}");
        
        if (selectedSlot.equippedItem != null)
        {
            Debug.Log($"   ✅ Retornando: {selectedSlot.equippedItem.itemName}");
        }
        else
        {
            Debug.Log($"   ❌ Retornando NULL (slot vazio)");
        }
        
        return selectedSlot?.equippedItem;
    }
    public CharacterData GetCurrentCharacter()
    {
        return currentCharacter;
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
            string dragStatus = slot.draggableComponent != null && slot.draggableComponent.enabled ? " [DRAGGABLE]" : " [NO DRAG]";
            Debug.Log($"- {slot.slotType}: {(slot.equippedItem != null ? slot.equippedItem.itemName : "Empty")}{status}{dragStatus}");
        }
    }

    [ContextMenu("🔍 Debug: Check Paper Doll State")]
    public void DebugCheckPaperDollState()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🎨 PAPER DOLL STATE DIAGNOSTIC       ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        // 1. Estado básico
        Debug.Log($"║  👤 Current Character: {currentCharacter?.characterName ?? "NULL"}");
        Debug.Log($"║  📌 Selected Slot: {selectedSlot?.slotType.ToString() ?? "NULL"}");
        
        if (selectedSlot != null)
        {
            Debug.Log($"║     └─ Item: {selectedSlot.equippedItem?.itemName ?? "Empty"}");
        }
        
        Debug.Log($"║");
        
        // 2. Todos os slots
        Debug.Log($"║  📦 All Slots ({paperDollSlots?.Length ?? 0} total):");
        
        if (paperDollSlots != null)
        {
            foreach (var slot in paperDollSlots)
            {
                if (slot == null)
                {
                    Debug.Log($"║     ├─ NULL slot");
                    continue;
                }
                
                string selectedMark = slot == selectedSlot ? " [SELECTED]" : "";
                string itemName = slot.equippedItem?.itemName ?? "Empty";
                
                Debug.Log($"║     ├─ {slot.slotType}: {itemName}{selectedMark}");
            }
        }
        
        Debug.Log($"║");
        
        // 3. Character Equipment
        Debug.Log($"║  🎯 Character Equipment:");
        
        if (currentCharacter != null && currentCharacter.currentEquipment != null)
        {
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            
            foreach (ItemData.EquipmentSlot slot in slotTypes)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var item = currentCharacter.currentEquipment.GetItemInSlot(slot);
                
                if (item != null)
                {
                    Debug.Log($"║     ├─ [{slot}]: {item.itemName}");
                }
            }
        }
        else
        {
            Debug.Log($"║     └─ Character ou Equipment é NULL");
        }
        
        Debug.Log($"║");
        
        // 4. InventoryManager Equipment
        Debug.Log($"║  📊 InventoryManager Equipment:");
        
        if (InventoryManager.Instance != null)
        {
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            bool hasAny = false;
            
            foreach (ItemData.EquipmentSlot slot in slotTypes)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var item = InventoryManager.Instance.GetEquippedItem(slot);
                
                if (item != null)
                {
                    hasAny = true;
                    Debug.Log($"║     ├─ [{slot}]: {item.itemName}");
                }
            }
            
            if (!hasAny)
            {
                Debug.Log($"║     └─ No items equipped");
            }
        }
        else
        {
            Debug.Log($"║     └─ InventoryManager is NULL");
        }
        
        Debug.Log($"║");
        
        // 5. Verificação de inconsistências
        Debug.Log($"║  🚨 Inconsistency Check:");
        
        bool foundInconsistency = false;
        
        if (currentCharacter != null && currentCharacter.currentEquipment != null)
        {
            var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
            
            foreach (ItemData.EquipmentSlot slot in slotTypes)
            {
                if (slot == ItemData.EquipmentSlot.None) continue;
                
                var charItem = currentCharacter.currentEquipment.GetItemInSlot(slot);
                var mgrItem = InventoryManager.Instance?.GetEquippedItem(slot);
                
                bool matches = false;
                if (charItem == null && mgrItem == null)
                {
                    matches = true;
                }
                else if (charItem != null && mgrItem != null)
                {
                    if (!string.IsNullOrEmpty(charItem.itemID) && !string.IsNullOrEmpty(mgrItem.itemID))
                    {
                        matches = charItem.itemID == mgrItem.itemID;
                    }
                    else
                    {
                        matches = charItem.itemName == mgrItem.itemName;
                    }
                }
                
                if (!matches)
                {
                    foundInconsistency = true;
                    Debug.LogError($"║     ❌ Slot {slot}:");
                    Debug.LogError($"║        Character: {charItem?.itemName ?? "Empty"}");
                    Debug.LogError($"║        Manager:   {mgrItem?.itemName ?? "Empty"}");
                }
            }
        }
        
        if (!foundInconsistency)
        {
            Debug.Log($"║     ✅ No inconsistencies found");
        }
        
        Debug.Log($"║");
        
        // 6. Teste GetItemInSelectedSlot()
        Debug.Log($"║  🧪 Test GetItemInSelectedSlot():");
        
        var testItem = GetItemInSelectedSlot();
        
        if (testItem != null)
        {
            Debug.Log($"║     ✅ Returned: {testItem.itemName}");
        }
        else
        {
            Debug.Log($"║     ❌ Returned: NULL");
        }
        
        Debug.Log("╚═══════════════════════════════════════╝");
    }

    [ContextMenu("🔄 Force Sync Character ↔ Manager")]
    public void DebugForceSyncEquipment()
    {
        Debug.Log("🔄 Forcing sync between Character and InventoryManager...");
        
        if (currentCharacter == null)
        {
            Debug.LogError("❌ No current character!");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ No InventoryManager!");
            return;
        }
        
        var slotTypes = System.Enum.GetValues(typeof(ItemData.EquipmentSlot));
        int syncCount = 0;
        
        foreach (ItemData.EquipmentSlot slot in slotTypes)
        {
            if (slot == ItemData.EquipmentSlot.None) continue;
            
            var charItem = currentCharacter.currentEquipment?.GetItemInSlot(slot);
            var mgrItem = InventoryManager.Instance.GetEquippedItem(slot);
            
            if (charItem != mgrItem)
            {
                Debug.Log($"Syncing {slot}: {charItem?.itemName ?? "Empty"} → Manager");
                
                // Limpa slot no Manager
                InventoryManager.Instance.Equipment.UnequipItem(slot);
                
                // Equipa item do Character
                if (charItem != null)
                {
                    InventoryManager.Instance.Equipment.EquipItem(charItem);
                }
                
                syncCount++;
            }
        }
        
        if (syncCount > 0)
        {
            Debug.Log($"✅ Synced {syncCount} slots");
            UpdateAllSlots();
        }
        else
        {
            Debug.Log("✅ Already in sync");
        }
    }
}