using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text slotNameText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Slot Configuration")]
    [SerializeField] private ItemData.EquipmentSlot equipmentSlot = ItemData.EquipmentSlot.Weapon;
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color occupiedColor = Color.white;
    
    // Private variables
    private InventoryUI inventoryUI;
    private ItemData equippedItem;
    
    public void Initialize(InventoryUI uiController)
    {
        inventoryUI = uiController;
        
        // Set slot name
        if (slotNameText != null)
        {
            slotNameText.text = equipmentSlot.ToString();
        }
        
        ClearSlot();
        
        Debug.Log($"Equipment slot initialized: {equipmentSlot}");
    }
    
    public void UpdateEquipment()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager not found!");
            return;
        }
        
        // Get currently equipped item from InventoryManager
        equippedItem = InventoryManager.Instance.GetEquippedItem(equipmentSlot);
        
        if (equippedItem != null)
        {
            // Update icon
            if (itemIcon != null)
            {
                itemIcon.sprite = equippedItem.icon;
                itemIcon.color = equippedItem.GetRarityColor();
                itemIcon.gameObject.SetActive(true);
            }
            
            // Update background
            if (backgroundImage != null)
            {
                backgroundImage.color = occupiedColor;
            }
            
            Debug.Log($"Equipment slot {equipmentSlot} updated: {equippedItem.itemName}");
        }
        else
        {
            ClearSlot();
        }
    }
    
    public void ClearSlot()
    {
        equippedItem = null;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (equippedItem == null) return;
        
        Debug.Log($"Equipment slot clicked: {equipmentSlot} - {equippedItem?.itemName}");
        
        // Unequip item on click
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            UnequipItem();
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // This would handle drag & drop from inventory to equipment slot
        Debug.Log($"Item dropped on {equipmentSlot} slot");
        
        // TODO: Implement drag & drop logic
        // For now, just log the drop event
    }
    
    private void UnequipItem()
    {
        if (equippedItem == null || InventoryManager.Instance == null) return;
        
        Debug.Log($"Unequipping {equippedItem.itemName} from {equipmentSlot}");
        
        // Unequip via InventoryManager
        ItemData unequippedItem = InventoryManager.Instance.UnequipItem(equipmentSlot);
        
        if (unequippedItem != null)
        {
            Debug.Log($"Successfully unequipped {unequippedItem.itemName}");
            
            // Update visual
            UpdateEquipment();
            
            // Refresh inventory UI if available
            if (inventoryUI != null)
            {
                inventoryUI.UpdateEquipmentDisplay();
            }
        }
        else
        {
            Debug.LogWarning($"Failed to unequip item from {equipmentSlot}");
        }
    }
    
    // Check if an item can be equipped in this slot
    public bool CanEquipItem(ItemData item)
    {
        if (item == null || !item.IsEquipment()) return false;
        
        return item.equipmentSlot == equipmentSlot;
    }
    
    // Equip an item to this slot
    public bool EquipItem(ItemData item)
    {
        if (!CanEquipItem(item) || InventoryManager.Instance == null) return false;
        
        Debug.Log($"Attempting to equip {item.itemName} to {equipmentSlot}");
        
        return InventoryManager.Instance.EquipItem(item);
    }
    
    // Getters
    public ItemData.EquipmentSlot GetSlotType() => equipmentSlot;
    public ItemData GetEquippedItem() => equippedItem;
    public bool IsOccupied() => equippedItem != null;
    
    [ContextMenu("Debug: Print Equipment Slot Info")]
    public void DebugPrintInfo()
    {
        Debug.Log($"=== Equipment Slot: {equipmentSlot} ===");
        Debug.Log($"Equipped item: {equippedItem?.itemName ?? "None"}");
        Debug.Log($"Can accept: {equipmentSlot} items");
        Debug.Log($"InventoryUI ref: {inventoryUI != null}");
        Debug.Log($"Icon active: {itemIcon?.gameObject.activeSelf}");
    }
}