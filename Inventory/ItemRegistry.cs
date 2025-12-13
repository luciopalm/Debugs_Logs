using UnityEngine;
using System.Collections.Generic;

public static class ItemRegistry
{
    private static Dictionary<string, ItemData> itemRegistry;
    private static bool isInitialized = false;
    
    // Propriedade pública para acesso
    public static int RegisteredItemCount => itemRegistry?.Count ?? 0;
    
    // ⭐ Inicialização automática quando o jogo começa
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        Debug.Log("[ItemRegistry] Initializing item registry...");
        
        itemRegistry = new Dictionary<string, ItemData>();
        
        // Carrega TODOS os ItemData da pasta Resources
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items/");
        
        int registered = 0;
        int skipped = 0;
        
        foreach (ItemData item in allItems)
        {
            if (item == null) continue;
            
            // Usa itemID se existir, senão usa o nome do asset
            string id = !string.IsNullOrEmpty(item.itemID) ? item.itemID : item.name;
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Item has no ID: {item.name}");
                skipped++;
                continue;
            }
            
            if (!itemRegistry.ContainsKey(id))
            {
                itemRegistry.Add(id, item);
                registered++;
            }
            else
            {
                Debug.LogError($"Duplicate item ID: '{id}' (Asset: {item.name})");
                skipped++;
            }
        }
        
        isInitialized = true;
        Debug.Log($"[ItemRegistry] Registered {registered} items, skipped {skipped}");
    }
    
    // ⭐ Método principal para obter itens
    public static ItemData GetItem(string itemID)
    {
        EnsureInitialized();
        
        if (itemRegistry.ContainsKey(itemID))
            return itemRegistry[itemID];
        
        Debug.LogWarning($"[ItemRegistry] Item not found: '{itemID}'");
        return null;
    }
    
    // ⭐ Método para obter por nome (fallback)
    public static ItemData GetItemByName(string itemName)
    {
        EnsureInitialized();
        
        foreach (var kvp in itemRegistry)
        {
            if (kvp.Value.itemName == itemName)
                return kvp.Value;
        }
        
        Debug.LogWarning($"[ItemRegistry] Item with name not found: '{itemName}'");
        return null;
    }
    
    // ⭐ Lista todos os itens
    public static List<ItemData> GetAllItems()
    {
        EnsureInitialized();
        return new List<ItemData>(itemRegistry.Values);
    }
    
    // ⭐ Verifica se item existe
    public static bool ItemExists(string itemID)
    {
        EnsureInitialized();
        return itemRegistry.ContainsKey(itemID);
    }
    
    // ⭐ Recarrega registry (útil durante desenvolvimento)
    public static void Reload()
    {
        isInitialized = false;
        Initialize();
    }
    
    private static void EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }
    
    // ⭐ DEBUG: Lista todos os itens registrados
    public static void DebugPrintAllItems()
    {
        EnsureInitialized();
        
        Debug.Log("[ItemRegistry] === REGISTERED ITEMS ===");
        foreach (var kvp in itemRegistry)
        {
            Debug.Log($"  {kvp.Key} -> {kvp.Value.itemName} ({kvp.Value.itemType})");
        }
        Debug.Log($"Total: {itemRegistry.Count} items");
    }
}