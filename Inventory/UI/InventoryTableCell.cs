using UnityEngine;

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
    
    public string GetIdentifier()
    {
        if (!string.IsNullOrEmpty(customIdentifier))
            return customIdentifier;
        
        return cellType.ToString();
    }
}