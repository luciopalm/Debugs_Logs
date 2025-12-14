using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.3f);
    
    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.2f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f);
    
    // Private variables
    private int slotIndex;
    private InventoryManager.InventorySlot currentSlot;
    private InventoryUI inventoryUI;
    private bool isHighlighted = false;
    
    public void Initialize(int index, InventoryUI uiController)
    {
        slotIndex = index;
        inventoryUI = uiController;
        
        // Setup initial state
        ClearSlot();
        
        // Ensure highlight is hidden initially
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        
        Debug.Log($"Slot {index} initialized");
    }
    
    public void UpdateSlot(InventoryManager.InventorySlot slot)
    {
        currentSlot = slot;
        
        if (slot == null || slot.IsEmpty)
        {
            ClearSlot();
            return;
        }
        
        // Update icon
        if (itemIcon != null)
        {
            itemIcon.sprite = slot.item.icon;
            itemIcon.color = GetRarityColor(slot.item.rarity);
            itemIcon.gameObject.SetActive(true);
        }
        
        // Update quantity text
        if (quantityText != null)
        {
            if (slot.item.stackLimit > 1 && slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        
        // Update background based on equipment status
        if (backgroundImage != null)
        {
            backgroundImage.color = slot.isEquipped ? Color.cyan : normalColor;
        }
        
        Debug.Log($"Slot {slotIndex} updated: {slot.item.itemName} x{slot.quantity}");
    }
    
    public void ClearSlot()
    {
        currentSlot = null;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
        
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
        }
        
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        
        isHighlighted = false;
    }
    
    private Color GetRarityColor(ItemData.ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemData.ItemRarity.Common: return commonColor;
            case ItemData.ItemRarity.Uncommon: return uncommonColor;
            case ItemData.ItemRarity.Rare: return rareColor;
            case ItemData.ItemRarity.Epic: return epicColor;
            case ItemData.ItemRarity.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }
    
    public void SetHighlight(bool highlight)
    {
        isHighlighted = highlight;
        
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(highlight);
            highlightImage.color = highlightColor;
        }
    }
    
    // Pointer event handlers
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty) return;
        
        Debug.Log($"Slot {slotIndex} clicked: {currentSlot.item.itemName}");
        
        // Left click - select item
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (inventoryUI != null)
            {
                inventoryUI.OnSlotClicked(currentSlot);
            }
        }
        // Right click - context menu (TODO: implement)
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"Right click on {currentSlot.item.itemName}");
            // TODO: Open context menu
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty) return;
        
        SetHighlight(true);
        
        if (inventoryUI != null)
        {
            inventoryUI.OnSlotHoverEnter(currentSlot);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
        
        if (inventoryUI != null)
        {
            inventoryUI.OnSlotHoverExit();
        }
    }
    
    // Drag & Drop methods (placeholder for now)
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty) return;
        Debug.Log($"Started dragging {currentSlot.item.itemName}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // Drag logic would go here
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Drag ended");
    }
    
    // Getters
    public InventoryManager.InventorySlot GetCurrentSlot() => currentSlot;
    public int GetSlotIndex() => slotIndex;
    public bool IsEmpty() => currentSlot == null || currentSlot.IsEmpty;
    
    [ContextMenu("Debug: Print Slot Info")]
    public void DebugPrintInfo()
    {
        Debug.Log($"=== Slot {slotIndex} Debug ===");
        Debug.Log($"Has InventoryUI ref: {inventoryUI != null}");
        Debug.Log($"Current slot: {(currentSlot != null ? currentSlot.item?.itemName : "NULL")}");
        Debug.Log($"Is highlighted: {isHighlighted}");
        Debug.Log($"Icon active: {itemIcon?.gameObject.activeSelf}");
        Debug.Log($"Quantity active: {quantityText?.gameObject.activeSelf}");
    }
}