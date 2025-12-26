using UnityEngine;
using System.Collections.Generic;

public class InventoryTableCell : MonoBehaviour
{
    public enum CellType
    {
        Item,
        Price,
        Attack,
        Defense,
        Magic,
        Speed,
        Crit,
        Evasion,
        Weight
    }
    
    public CellType cellType = CellType.Item;
    
    // Optional: Set in inspector for quick identification
    [SerializeField] private string customIdentifier = "";
    
    // 🔥 NOVO: Referência para o slot do inventário
    private InventoryManager.InventorySlot linkedSlot = null;
    private ItemData cellItem = null;
    private int cellSlotIndex = -1;
    
    public string GetIdentifier()
    {
        if (!string.IsNullOrEmpty(customIdentifier))
            return customIdentifier;
        
        return cellType.ToString();
    }
    
    // 🔥 NOVO: Métodos para vincular dados do slot
    public void LinkToSlot(InventoryManager.InventorySlot slot, ItemData item, int slotIndex)
    {
        linkedSlot = slot;
        cellItem = item;
        cellSlotIndex = slotIndex;
    }
    
    public bool HasLinkedSlot() => linkedSlot != null;
    public InventoryManager.InventorySlot GetLinkedSlot() => linkedSlot;
    public ItemData GetCellItem() => cellItem;
    public int GetCellSlotIndex() => cellSlotIndex;
    
    // 🔥 Verifica se está vazio (para compatibilidade)
    public bool IsEmpty => linkedSlot == null || cellItem == null;
    
    // 🔥 Propriedade item para compatibilidade
    public ItemData item => cellItem;
    
    // 🔥 Propriedade slotIndex para compatibilidade
    public int slotIndex => cellSlotIndex;
}