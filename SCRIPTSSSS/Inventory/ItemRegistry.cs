using UnityEngine;
using System.Collections.Generic;

public static class ItemRegistry
{
    private static Dictionary<string, ItemData> itemRegistry;
    private static bool isInitialized = false;
    
    public static int RegisteredItemCount 
    { 
        get 
        { 
            if (!isInitialized) Initialize();
            return itemRegistry?.Count ?? 0; 
        } 
    }

    // ✅ MANTIDO: Runtime initialize (importante para outros sistemas)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        // ⭐⭐ REDUZIDO: Log apenas se demorar
        var startTime = System.DateTime.Now;
        
        itemRegistry = new Dictionary<string, ItemData>();
        
        // 🔥🔥🔥 MUDANÇA CRÍTICA: Não carregar TUDO de uma vez
        // Em vez de LoadAll, carregamos apenas quando necessário
        InitializeLazy();
        
        isInitialized = true;
        
        var loadTime = (System.DateTime.Now - startTime).TotalSeconds;
        if (loadTime > 1.0f) // Só loga se demorar mais de 1 segundo
            Debug.Log($"[ItemRegistry] Inicializado em {loadTime:F2}s (carregamento lento detectado)");
    }
    
    // ⭐⭐ NOVO: Carregamento sob demanda (LAZY LOADING)
    private static void InitializeLazy()
    {
        // Passo 1: Primeiro, apenas INDEXAMOS os itens disponíveis
        // Não carregamos os assets ainda
        TextAsset[] indexFiles = Resources.LoadAll<TextAsset>("Items/Index/");
        
        if (indexFiles != null && indexFiles.Length > 0)
        {
            Debug.Log($"[ItemRegistry] Usando sistema de indexação ({indexFiles.Length} índices)");
            // Sistema profissional com indexação - vamos implementar depois
        }
        else
        {
            // ⭐⭐ SOLUÇÃO TEMPORÁRIA: Carrega apenas os itens CRÍTICOS
            Debug.LogWarning($"[ItemRegistry] Carregamento lento detectado - usando modo otimizado");
            
            // Carrega apenas alguns itens essenciais primeiro
            ItemData[] essentialItems = Resources.LoadAll<ItemData>("Items/Essential/");
            
            if (essentialItems != null && essentialItems.Length > 0)
            {
                foreach (ItemData item in essentialItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.itemID))
                    {
                        itemRegistry[item.itemID] = item;
                    }
                }
                Debug.Log($"[ItemRegistry] {essentialItems.Length} itens essenciais carregados");
            }
            else
            {
                // ⭐⭐ MODO DE EMERGÊNCIA: Nenhum item carregado na inicialização
                // Os itens serão carregados sob demanda quando GetItem() for chamado
                Debug.LogWarning($"[ItemRegistry] Nenhum item carregado na inicialização - modo Lazy Ativado");
            }
        }
    }
    
    // ⭐⭐ GetItem OTIMIZADO: Carrega sob demanda
    public static ItemData GetItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("[ItemRegistry] itemID vazio!");
            return null;
        }
        
        // Garante inicialização
        if (!isInitialized) Initialize();
        
        // Se já está no registry, retorna
        if (itemRegistry.ContainsKey(itemID))
            return itemRegistry[itemID];
        
        // ⭐⭐ CARREGAMENTO SOB DEMANDA: Só carrega quando precisa
        Debug.Log($"[ItemRegistry] Carregando item sob demanda: {itemID}");
        
        // Tenta carregar o item específico (MUITO mais rápido que LoadAll)
        ItemData item = Resources.Load<ItemData>($"Items/{itemID}");
        
        if (item != null)
        {
            itemRegistry[itemID] = item;
            return item;
        }
        
        // Tenta carregar por nome se não encontrar por ID
        item = Resources.Load<ItemData>($"Items/{itemID}_ItemData");
        
        if (item != null)
        {
            itemRegistry[itemID] = item;
            return item;
        }
        
        Debug.LogWarning($"[ItemRegistry] Item não encontrado: '{itemID}'");
        return null;
    }
    
    // ⭐⭐ Para compatibilidade: Método antigo ainda funciona
    public static ItemData GetItemByName(string itemName)
    {
        if (!isInitialized) Initialize();
        
        foreach (var kvp in itemRegistry)
        {
            if (kvp.Value.itemName == itemName)
                return kvp.Value;
        }
        
        // Tenta carregar sob demanda
        ItemData[] foundItems = Resources.LoadAll<ItemData>("");
        foreach (ItemData item in foundItems)
        {
            if (item.itemName == itemName)
            {
                itemRegistry[item.itemID] = item;
                return item;
            }
        }
        
        return null;
    }
    
    // Outros métodos permanecem semelhantes, mas chamam GetItem()
    public static bool ItemExists(string itemID)
    {
        if (!isInitialized) Initialize();
        
        if (itemRegistry.ContainsKey(itemID))
            return true;
        
        // Verifica se pode ser carregado
        ItemData item = Resources.Load<ItemData>($"Items/{itemID}");
        if (item != null)
        {
            itemRegistry[itemID] = item;
            return true;
        }
        
        return false;
    }
    
    [ContextMenu("🔍 Debug: Verificar Performance")]
    public static void DebugCheckPerformance()
    {
        Debug.Log($"[ItemRegistry] Itens carregados: {itemRegistry?.Count ?? 0}");
        Debug.Log($"[ItemRegistry] Modo Lazy: {(itemRegistry?.Count < 50 ? "✅ ATIVADO" : "⚠️ Parcial")}");
    }

    // ⭐⭐ MANTIDO: Método para compatibilidade
    public static List<ItemData> GetAllItems()
    {
        if (!isInitialized) Initialize();
        
        // Se o registry está vazio (modo lazy), carrega todos agora
        if (itemRegistry.Count == 0)
        {
            Debug.LogWarning("[ItemRegistry] GetAllItems() forçando carregamento completo...");
            ForceLoadAllItems();
        }
        
        return new List<ItemData>(itemRegistry.Values);
    }

    // ⭐⭐ NOVO: Força carregamento completo (apenas quando necessário)
    private static void ForceLoadAllItems()
    {
        Debug.Log("[ItemRegistry] Carregando todos os itens (operação lenta)...");
        
        var startTime = System.DateTime.Now;
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items/");
        
        foreach (ItemData item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID))
            {
                if (!itemRegistry.ContainsKey(item.itemID))
                {
                    itemRegistry[item.itemID] = item;
                }
            }
        }
        
        var loadTime = (System.DateTime.Now - startTime).TotalSeconds;
        Debug.Log($"[ItemRegistry] {allItems.Length} itens carregados em {loadTime:F2}s");
    }

    // ⭐⭐ MANTIDO: Método de debug
    public static void DebugPrintAllItems()
    {
        if (!isInitialized) Initialize();
        
        Debug.Log("[ItemRegistry] === ITENS REGISTRADOS ===");
        
        if (itemRegistry.Count == 0)
        {
            Debug.Log("  Nenhum item carregado (modo lazy ativo)");
            return;
        }
        
        foreach (var kvp in itemRegistry)
        {
            Debug.Log($"  {kvp.Key} -> {kvp.Value.itemName} ({kvp.Value.itemType})");
        }
        
        Debug.Log($"Total: {itemRegistry.Count} itens na memória");
    }

    // ⭐⭐ MANTIDO: Recarrega registry (útil durante desenvolvimento)
    public static void Reload()
    {
        isInitialized = false;
        itemRegistry?.Clear();
        Initialize();
    }

    // ⭐⭐ NOVO: Método para saber se está no modo lazy
    public static bool IsLazyModeActive()
    {
        return isInitialized && itemRegistry.Count < 50; // Se tem menos de 50 itens, está no modo lazy
    }

}