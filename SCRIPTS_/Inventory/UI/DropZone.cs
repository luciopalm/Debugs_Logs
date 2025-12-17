using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum DropType { PaperDollSlot, InventoryTable, TrashBin }
    
    [Header("Drop Zone Configuration")]
    [SerializeField] private DropType dropType = DropType.PaperDollSlot;
    [SerializeField] private ItemData.EquipmentSlot acceptedEquipmentSlot = ItemData.EquipmentSlot.None;
    
    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color hoverValidColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
    [SerializeField] private Color hoverInvalidColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
    
    [Header("Advanced Transparency")]
    [SerializeField] private bool useAdvancedControl = true;
    [SerializeField] private Color normalColorRGB = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] [Range(0, 100)] private int normalAlphaPercent = 5;
    [SerializeField] private Color hoverValidColorRGB = Color.green;
    [SerializeField] [Range(0, 100)] private int hoverValidAlphaPercent = 30;
    [SerializeField] private Color hoverInvalidColorRGB = Color.red;
    [SerializeField] [Range(0, 100)] private int hoverInvalidAlphaPercent = 30;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    private bool isDraggingOver = false;
    private DraggableItem currentDragItem = null;
    private Color originalColor;
    private Graphic raycastTarget;
    
    private void Awake()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.01f);
        }
        
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
            raycastTarget = backgroundImage;
            raycastTarget.raycastTarget = true;
        }
        
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        
        currentDragItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (currentDragItem == null) return;
        
        isDraggingOver = true;
        bool canAccept = CanAcceptItem(currentDragItem);
        
        if (backgroundImage != null)
        {
            backgroundImage.color = useAdvancedControl 
                ? (canAccept ? GetHoverValidColor() : GetHoverInvalidColor())
                : (canAccept ? hoverValidColor : hoverInvalidColor);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isDraggingOver = false;
        currentDragItem = null;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = useAdvancedControl ? GetNormalColor() : originalColor;
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        
        var draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggableItem == null) return;
        
        ItemData item = draggableItem.GetItemData();
        if (item == null) return;
        
        if (!CanAcceptItem(draggableItem))
        {
            if (backgroundImage != null) backgroundImage.color = originalColor;
            return;
        }
        
        bool success = false;
        
        switch (dropType)
        {
            case DropType.PaperDollSlot:
                success = HandleEquipDrop(draggableItem);
                break;
            case DropType.InventoryTable:
                success = HandleUnequipDrop(draggableItem);
                break;
            case DropType.TrashBin:
                success = HandleTrashDrop(draggableItem);
                break;
        }
        
        if (success) draggableItem.MarkDropSuccess();
        if (backgroundImage != null) backgroundImage.color = originalColor;
        
        isDraggingOver = false;
        currentDragItem = null;
    }
    
    private bool CanAcceptItem(DraggableItem draggableItem)
    {
        if (draggableItem == null) return false;
        
        ItemData item = draggableItem.GetItemData();
        if (item == null) return false;
        
        DraggableItem.DragSource source = draggableItem.GetSource();
        
        switch (dropType)
        {
            case DropType.PaperDollSlot:
                if (source != DraggableItem.DragSource.InventoryTable) return false;
                if (!item.IsEquipment()) return false;
                return IsCompatibleEquipmentSlot(item.equipmentSlot, acceptedEquipmentSlot);
                
            case DropType.InventoryTable:
                return source == DraggableItem.DragSource.PaperDollSlot;
                
            case DropType.TrashBin:
                return item.isDroppable;
                
            default:
                return false;
        }
    }
    
    // ✅ CORRIGIDO: Mapeamento de compatibilidade atualizado
    private bool IsCompatibleEquipmentSlot(ItemData.EquipmentSlot itemSlot, ItemData.EquipmentSlot targetSlot)
    {
        // Correspondência direta
        if (itemSlot == targetSlot) return true;
        
        // Sem mapeamentos adicionais necessários (enum está correta agora)
        return false;
    }
    
    private bool HandleEquipDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        if (InventoryManager.Instance == null) return false;
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        InventoryManager.InventorySlot validSlot = null;
        int validSlotIndex = -1;
        
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            if (!slot.IsEmpty && slot.item == item && !slot.isEquipped && slot.quantity > 0)
            {
                validSlot = slot;
                validSlotIndex = i;
                break;
            }
        }
        
        if (validSlot == null || validSlotIndex < 0) return false;
        
        var paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
        if (paperDollUI == null) return false;
        
        var activeChar = paperDollUI.GetCurrentCharacter();
        if (activeChar == null || activeChar.currentEquipment == null) return false;
        
        validSlot.isEquipped = true;
        
        ItemData.EquipmentSlot targetSlot = item.equipmentSlot;
        var currentlyEquipped = activeChar.currentEquipment.GetItemInSlot(targetSlot);
        
        if (currentlyEquipped != null)
        {
            var unequipped = activeChar.currentEquipment.UnequipItem(targetSlot);
            if (unequipped != null)
            {
                if (!InventoryManager.Instance.AddItem(unequipped, 1))
                {
                    validSlot.isEquipped = false;
                    activeChar.currentEquipment.EquipItem(unequipped);
                    return false;
                }
            }
        }
        
        activeChar.currentEquipment.EquipItem(item);
        
        bool removed = InventoryManager.Instance.RemoveItemFromSlot(validSlotIndex, 1);
        if (!removed)
        {
            validSlot.isEquipped = false;
            activeChar.currentEquipment.UnequipItem(targetSlot);
            return false;
        }
        
        paperDollUI.UpdateAllSlots();
        
        var tableUI = FindFirstObjectByType<InventoryTableUI>();
        if (tableUI != null) tableUI.UpdateExistingRowsData();
        
        var detailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (detailsUI != null) detailsUI.UpdatePartyMemberStats();
        
        return true;
    }
    
    private bool HandleUnequipDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        ItemData.EquipmentSlot sourceSlot = draggableItem.GetSourceSlot();
        
        var paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
        if (paperDollUI == null) return false;
        
        CharacterData currentCharacter = paperDollUI.GetCurrentCharacter();
        if (currentCharacter == null) return false;
        
        if (currentCharacter.currentEquipment == null)
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        
        ItemData equippedInCharacter = currentCharacter.currentEquipment.GetItemInSlot(sourceSlot);
        if (equippedInCharacter != item) return false;
        
        ItemData unequipped = currentCharacter.currentEquipment.UnequipItem(sourceSlot);
        if (unequipped == null) return false;
        
        if (InventoryManager.Instance == null)
        {
            currentCharacter.currentEquipment.EquipItem(unequipped);
            return false;
        }
        
        if (!InventoryManager.Instance.CanCarryWeight(unequipped.weight))
        {
            currentCharacter.currentEquipment.EquipItem(unequipped);
            return false;
        }
        
        InventoryManager.Instance.MarkItemAsUnequipped(unequipped);
        
        bool added = InventoryManager.Instance.AddItem(unequipped, 1);
        if (!added)
        {
            currentCharacter.currentEquipment.EquipItem(unequipped);
            if (paperDollUI != null) paperDollUI.UpdateAllSlots();
            return false;
        }
        
        InventoryManager.Instance.SyncFromActiveCharacter();
        
        if (paperDollUI != null)
        {
            paperDollUI.UpdateAllSlots();
            paperDollUI.ClearAllSelections();
        }
        
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.StartCoroutine(UpdateUIAfterUnequip(paperDollUI));
        
        return true;
    }
    
    private System.Collections.IEnumerator UpdateUIAfterUnequip(InventoryPaperDollUI paperDollUI)
    {
        yield return null;
        
        if (paperDollUI != null)
        {
            paperDollUI.ResetAllSlotsSelection();
            yield return null;
            
            paperDollUI.FixDropZones();
            yield return null;
            
            paperDollUI.UpdateAllSlots();
            yield return null;
            
            paperDollUI.FixDropZones();
        }
        
        var tableUI = FindFirstObjectByType<InventoryTableUI>();
        if (tableUI != null) tableUI.RefreshTable(false);
        
        var detailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (detailsUI != null) detailsUI.UpdatePartyMemberStats();
    }
    
    private bool HandleTrashDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        if (InventoryManager.Instance != null)
            return InventoryManager.Instance.RemoveItem(item, 1);
        return false;
    }
    
    // Getters
    public DropType GetDropType() => dropType;
    public ItemData.EquipmentSlot GetAcceptedEquipmentSlot() => acceptedEquipmentSlot;
    public void SetAcceptedSlot(ItemData.EquipmentSlot slot) => acceptedEquipmentSlot = slot;
    
    // Advanced Color Helpers
    private Color GetNormalColor() => new Color(normalColorRGB.r, normalColorRGB.g, normalColorRGB.b, normalAlphaPercent / 100f);
    private Color GetHoverValidColor() => new Color(hoverValidColorRGB.r, hoverValidColorRGB.g, hoverValidColorRGB.b, hoverValidAlphaPercent / 100f);
    private Color GetHoverInvalidColor() => new Color(hoverInvalidColorRGB.r, hoverInvalidColorRGB.g, hoverInvalidColorRGB.b, hoverInvalidAlphaPercent / 100f);
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;
        
        Color gizmoColor = dropType == DropType.PaperDollSlot ? Color.cyan :
                           dropType == DropType.InventoryTable ? Color.green : Color.red;
        
        if (isDraggingOver) gizmoColor = Color.yellow;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(rect.position, new Vector3(rect.rect.width, rect.rect.height, 0f));
    }
}