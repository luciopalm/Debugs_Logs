using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryTableUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform tableContentContainer;
    [SerializeField] private GameObject itemRowPrefab;
    
    [Header("Pool System")]
    [SerializeField] private InventoryRowPool rowPool;
    [SerializeField] private bool useObjectPooling = true;
    
    [Header("Selection System")]
    [SerializeField] private Color selectedRowColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);
    [SerializeField] private Color normalRowColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);

    private Dictionary<int, int> tableRowToInventorySlot = new Dictionary<int, int>();
    
    
    // Reference to main UI
    private InventoryUI inventoryUI;
    
    // Data structures
    private List<ItemData> allItemsToDisplay = new List<ItemData>();
    private Dictionary<ItemData, int> itemQuantities = new Dictionary<ItemData, int>();
    private List<InventoryRowPool.PooledRow> activePooledRows = new List<InventoryRowPool.PooledRow>();
    
    // Simple Selection System
    private ItemData selectedItem = null;
    private GameObject lastSelectedRow = null;
    
    // Performance
    [SerializeField] private float rowHeight = 40f;
    private ScrollRect scrollRect;
    
    //Category Header
    [Header("Category System")]
    [SerializeField] private GameObject categoryHeaderPrefab;
    [SerializeField] private bool enableCategoryCollapse = true;

    // Cache de headers ativos
    private Dictionary<string, GameObject> activeCategoryHeaders = new Dictionary<string, GameObject>();

    // Estado de collapse (persistente)
    private Dictionary<string, bool> categoryExpandedState = new Dictionary<string, bool>()
    {
        ["Weapons"] = true,
        ["Armor"] = true,
        ["Accessories"] = true,
        ["Consumables"] = true,
        ["Materials"] = true,
        ["Key Items"] = true,
        ["Miscellaneous"] = true
    };

    // Ordem das categorias
    private static readonly string[] categoryOrder = new string[]
    {
        "Weapons",
        "Armor",
        "Accessories",
        "Consumables",
        "Materials",
        "Key Items",
        "Miscellaneous"
    };

    // ⭐⭐ CACHE SYSTEM - NOVO
    private int cachedItemCount = -1;
    private bool forceRefresh = false;
    
    private void Start()
    {
        inventoryUI = FindFirstObjectByType<InventoryUI>();
        
        // Initialize pool
        if (useObjectPooling && rowPool != null)
        {
            rowPool.Initialize();
        }
        
        // Get scroll rect
        scrollRect = GetComponentInChildren<ScrollRect>();
        
        Debug.Log("InventoryTableUI initialized with SIMPLE selection + CACHE SYSTEM");
        
        // Criar DropZone para a tabela
        CreateTableDropZone();
    }
    
    public void RefreshTable(bool forceRefresh = false)
    {
        this.forceRefresh = forceRefresh;
        
        // ⭐ INÍCIO: Medição de performance
        System.Diagnostics.Stopwatch totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log($"=== PERFORMANCE DIAGNOSIS ===");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // 1. Verificações básicas
            if (tableContentContainer == null)
            {
                Debug.LogError("❌ ERRO: tableContentContainer é NULL!");
                ShowErrorMessage("tableContentContainer not configured!");
                totalStopwatch.Stop();
                return;
            }
            
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("❌ InventoryManager.Instance é NULL!");
                ShowErrorMessage("InventoryManager not found!");
                totalStopwatch.Stop();
                return;
            }
            
            Debug.Log("✅ Componentes básicos OK");
            
            // 2. ETAPA 1: Coleta de dados
            stopwatch.Restart();
            ProcessInventoryData();
            Debug.Log($"1. Data Collection: {stopwatch.ElapsedMilliseconds}ms");
            
            // 3. Se inventário vazio
            if (allItemsToDisplay.Count == 0)
            {
                Debug.Log("Inventário vazio");
                ShowInfoMessage("Inventory is empty!\nAdd items to get started.");
                
                // ⭐⭐ CACHE: Atualizar cache
                cachedItemCount = 0;
                ClearTableVisuals();
                
                totalStopwatch.Stop();
                Debug.Log($"TOTAL TIME: {totalStopwatch.ElapsedMilliseconds}ms (empty)");
                return;
            }
            
            // ⭐⭐ CACHE SYSTEM: Verificar se podemos REUTILIZAR linhas
            if (!forceRefresh && cachedItemCount == allItemsToDisplay.Count && activePooledRows.Count == allItemsToDisplay.Count)
            {
                Debug.Log("🔄 REUSING existing rows (inventory unchanged)");
                
                // Apenas atualizar dados nas linhas existentes (MUCH FASTER!)
                UpdateExistingRows();
                
                totalStopwatch.Stop();
                Debug.Log($"=== CACHE HIT! TOTAL: {totalStopwatch.ElapsedMilliseconds}ms ===");
                return;
            }
            
            // 4. Mostrar contagem
            Debug.Log($"Itens para mostrar: {allItemsToDisplay.Count} unidades");
            
            // 5. ETAPA 2: Limpeza (só se necessário)
            stopwatch.Restart();
            ClearTableVisuals();
            Debug.Log($"2. Cleanup: {stopwatch.ElapsedMilliseconds}ms");
            
            // 6. ETAPA 3: Renderização
            stopwatch.Restart();
            RenderAllItems();
            Debug.Log($"3. Rendering: {stopwatch.ElapsedMilliseconds}ms");
            
            // ⭐⭐ CACHE: Atualizar cache count
            cachedItemCount = allItemsToDisplay.Count;
            
            // 7. FINAL: Resumo de performance
            totalStopwatch.Stop();
            Debug.Log($"=== PERFORMANCE SUMMARY ===");
            Debug.Log($"TOTAL TIME: {totalStopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"ITEMS: {allItemsToDisplay.Count}");
            Debug.Log($"ACTIVE ROWS: {activePooledRows?.Count ?? 0}");
            Debug.Log($"=== RefreshTable() COMPLETE ===");
        }
        catch (System.Exception e)
        {
            totalStopwatch.Stop();
            Debug.LogError($"❌ ERRO ({totalStopwatch.ElapsedMilliseconds}ms): {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            ShowErrorMessage($"Error: {e.Message}");
        }
    }
    
    // ⭐⭐ NOVO MÉTODO: Atualizar linhas existentes (MUITO MAIS RÁPIDO)
    private void UpdateExistingRows()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            ItemData item = allItemsToDisplay[i];
            int quantity = itemQuantities[item];
            
            if (i < activePooledRows.Count)
            {
                var pooledRow = activePooledRows[i];
                
                // Apenas atualizar dados
                FillRowWithData(pooledRow, item, quantity);
                
                // Atualizar seleção
                if (item == selectedItem)
                {
                    SetRowSelected(pooledRow.rowObject, true);
                    lastSelectedRow = pooledRow.rowObject;
                }
                else
                {
                    SetRowSelected(pooledRow.rowObject, false);
                }
            }
        }
        
        sw.Stop();
        Debug.Log($"UpdateExistingRows: {sw.ElapsedMilliseconds}ms (REUSED {activePooledRows.Count} rows)");
    }
    
        /// <summary>
    /// ⚡ ULTRA OTIMIZADO: Apenas atualiza dados visuais
    /// NÃO recria linhas, NÃO chama GetRow/ReturnRow
    /// Usado após Drag & Drop para zero lag
    /// </summary>

    public void UpdateExistingRowsData()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        Debug.Log("⚡ UpdateExistingRowsData - Início");
        
        // 1. Re-processar dados do inventário
        ProcessInventoryData();
        
        // 2. Verificar se número de itens mudou
        if (allItemsToDisplay.Count != activePooledRows.Count)
        {
            Debug.LogWarning($"⚠️ Item count changed ({activePooledRows.Count} → {allItemsToDisplay.Count}), calling full refresh");
            RefreshTable(true);
            return;
        }
        
        // 3. Atualizar APENAS dados nas células existentes
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            ItemData item = allItemsToDisplay[i];
            int quantity = itemQuantities[item];
            
            if (i >= activePooledRows.Count)
            {
                Debug.LogError($"❌ Index {i} out of bounds (rows: {activePooledRows.Count})");
                break;
            }
            
            var pooledRow = activePooledRows[i];
            
            // ⚡ SUPER RÁPIDO: Apenas atualiza texto/sprite
            UpdateRowDataOnly(pooledRow, item, quantity);
            
            // Atualizar seleção se necessário
            if (item == selectedItem)
            {
                SetRowSelected(pooledRow.rowObject, true);
                lastSelectedRow = pooledRow.rowObject;
            }
            else
            {
                SetRowSelected(pooledRow.rowObject, false);
            }
        }
        
        stopwatch.Stop();
        Debug.Log($"⚡ UpdateExistingRowsData: {stopwatch.ElapsedMilliseconds}ms (ZERO recreations)");
    }

    /// <summary>
    /// ⚡ Atualiza APENAS dados de uma linha (não cria/destrói nada)
    /// </summary>
    private void UpdateRowDataOnly(InventoryRowPool.PooledRow pooledRow, ItemData item, int quantity)
    {
        if (pooledRow.cells == null || pooledRow.cells.Length == 0) return;
        
        // Cache valores para evitar recalcular
        string quantityText = quantity > 1 ? $" x{quantity}" : "";
        int sellPrice = item.GetCalculatedSellPrice();
        
        // Atualizar cada célula
        foreach (var cell in pooledRow.cells)
        {
            if (cell == null) continue;
            
            TMP_Text[] textComponents = cell.GetComponentsInChildren<TMP_Text>();
            if (textComponents.Length == 0) continue;
            
            TMP_Text textComp = textComponents[0];
            
            switch (cell.cellType)
            {
                case InventoryTableCell.CellType.Item:
                    // Atualizar ícone se necessário
                    Image[] images = cell.GetComponentsInChildren<Image>();
                    if (images.Length > 1)
                    {
                        Image iconImage = images[1];
                        if (iconImage.sprite != item.icon)
                        {
                            iconImage.sprite = item.icon;
                            iconImage.color = item.GetRarityColor();
                        }
                    }
                    
                    // Atualizar texto
                    textComp.text = $"{item.itemName}{quantityText}";
                    break;
                    
                case InventoryTableCell.CellType.Price:
                    textComp.text = $"{sellPrice}";
                    break;
                    
                case InventoryTableCell.CellType.Attack:
                    textComp.text = item.attackBonus > 0 ? $"+{item.attackBonus}" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Defense:
                    textComp.text = item.defenseBonus > 0 ? $"+{item.defenseBonus}" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Magic:
                    textComp.text = item.magicAttackBonus > 0 ? $"+{item.magicAttackBonus}" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Speed:
                    textComp.text = item.speedBonus > 0 ? $"+{item.speedBonus}" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Crit:
                    textComp.text = item.criticalRateBonus > 0 ? $"{item.criticalRateBonus}%" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Evasion:
                    textComp.text = item.evasionBonus > 0 ? $"{item.evasionBonus}%" : "-";
                    break;
                    
                case InventoryTableCell.CellType.Weight:
                    textComp.text = $"{item.weight:F1}";
                    break;
            }
        }
    }

    
    private void ProcessInventoryData()
    {
        allItemsToDisplay.Clear();
        itemQuantities.Clear();
        tableRowToInventorySlot.Clear();
        
        var inventorySlots = InventoryManager.Instance.GetAllSlots();
        
        // 🔥 SUA LÓGICA ORIGINAL (PRESERVADA!)
        var stackableItemsMap = new Dictionary<ItemData, int>();
        var nonStackableItems = new List<ItemData>();
        
        // ⚠️ IMPORTANTE: logicalItemIndex representa o índice LÓGICO do item (sem headers)
        // Será usado depois para criar o mapeamento correto
        int logicalItemIndex = 0;
        
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  📊 ProcessInventoryData INICIADO    ║");
        Debug.Log("╚═══════════════════════════════════════╝");
        
        // 🔥🔥🔥 PASSO 1: PROCESSAR APENAS ITENS NÃO-EQUIPADOS (SUA LÓGICA ORIGINAL)
        foreach (var slot in inventorySlots)
        {
            // 🔥 SKIP itens equipados OU vazios!
            if (slot.IsEmpty || slot.item == null)
            {
                continue;
            }
            
            // 🔥🔥🔥 VERIFICAÇÃO CRÍTICA: NÃO MOSTRAR EQUIPADOS
            if (slot.isEquipped)
            {
                Debug.Log($"   ⏭️ SKIP Slot {slot.slotIndex}: {slot.item.itemName} (EQUIPADO)");
                continue; // Pula este slot
            }
            
            Debug.Log($"   ✅ Processando Slot {slot.slotIndex}: {slot.item.itemName} x{slot.quantity}");
            
            if (slot.item.stackLimit == 1)
            {
                // 🔥 Items não-stackable: uma linha por unidade
                for (int i = 0; i < slot.quantity; i++)
                {
                    nonStackableItems.Add(slot.item);
                    
                    // 🔥🔥🔥 MAPEAR: Este item lógico → Este slot do inventário
                    tableRowToInventorySlot[logicalItemIndex] = slot.slotIndex;
                    
                    Debug.Log($"      📋 Map: Logical Item {logicalItemIndex} → Slot {slot.slotIndex}");
                    
                    logicalItemIndex++;
                }
            }
            else
            {
                // Items stackable: uma linha para todos
                if (!stackableItemsMap.ContainsKey(slot.item))
                {
                    stackableItemsMap[slot.item] = 0;
                    
                    // 🔥 MAPEAR: Item lógico → Primeiro slot físico não-equipado
                    tableRowToInventorySlot[logicalItemIndex] = slot.slotIndex;
                    Debug.Log($"      📋 Map (stackable): Logical Item {logicalItemIndex} → Slot {slot.slotIndex}");
                    
                    logicalItemIndex++;
                }
                stackableItemsMap[slot.item] += slot.quantity;
            }
        }
        
        // 🔥 PASSO 2: Adiciona à lista de exibição (SUA LÓGICA ORIGINAL)
        foreach (var item in nonStackableItems)
        {
            allItemsToDisplay.Add(item);
            itemQuantities[item] = 1;
        }
        
        foreach (var kvp in stackableItemsMap)
        {
            allItemsToDisplay.Add(kvp.Key);
            itemQuantities[kvp.Key] = kvp.Value;
        }
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  ✅ ProcessInventoryData COMPLETO    ║");
        Debug.Log($"╞═══════════════════════════════════════╡");
        Debug.Log($"║  📊 Linhas para mostrar: {allItemsToDisplay.Count}");
        Debug.Log($"║  📋 Mapeamentos criados: {tableRowToInventorySlot.Count}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // 🔥🔥🔥 VERIFICAÇÃO: Logs dos mapeamentos finais
        if (tableRowToInventorySlot.Count > 0)
        {
            Debug.Log("=== MAPEAMENTO INICIAL (lógico) ===");
            foreach (var map in tableRowToInventorySlot)
            {
                var slot = inventorySlots[map.Value];
                Debug.Log($"   Logical Item {map.Key} → Slot {map.Value}: {slot.item?.itemName ?? "NULL"} x{slot.quantity}");
            }
        }
    }

    private void RenderAllItems()
    {
        if (useObjectPooling && rowPool != null)
        {
            RenderWithPooling();
        }
        else
        {
            RenderLegacy();
        }
    }
    
    private void RenderWithPooling()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        
        // 🧹 LIMPAR HEADERS ANTIGOS
        ClearCategoryHeaders();
        
        // 1. Se já temos o número exato de linhas, reutilizar (SUA OTIMIZAÇÃO ORIGINAL)
        if (activePooledRows.Count == allItemsToDisplay.Count)
        {
            Debug.Log("✅ Reutilizando linhas existentes (mesmo count)...");
            
            // 🆕 AGRUPAR POR CATEGORIA
            var itemsByCategory = GroupItemsByCategory();
            
            float currentY = 0f;
            int logicalItemIndex = 0; // 🔥 Índice do item (ignora headers)
            
            // 🆕 RENDERIZAR COM HEADERS
            foreach (string category in categoryOrder)
            {
                if (!itemsByCategory.ContainsKey(category))
                    continue;
                
                var itemsInCategory = itemsByCategory[category];
                bool isExpanded = categoryExpandedState.ContainsKey(category) ? categoryExpandedState[category] : true;
                
                // 🏷️ CRIAR HEADER
                GameObject headerObj = CreateCategoryHeader(category, isExpanded, currentY);
                currentY -= 40f; // Header height
                
                // 📦 RENDERIZAR ITENS (se expandido)
                if (isExpanded)
                {
                    foreach (var item in itemsInCategory)
                    {
                        if (logicalItemIndex >= activePooledRows.Count)
                        {
                            Debug.LogError($"❌ Logical index {logicalItemIndex} out of bounds!");
                            break;
                        }
                        
                        var pooledRow = activePooledRows[logicalItemIndex];
                        
                        // Position
                        var rectTransform = pooledRow.rowObject.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = new Vector2(0, currentY);
                        }
                        
                        currentY -= rowHeight;
                        
                        // Apenas atualizar dados (SUA LÓGICA ORIGINAL)
                        int quantity = itemQuantities[item];
                        FillRowWithData(pooledRow, item, quantity);
                        
                        // 🔥 USAR ÍNDICE LÓGICO PARA DRAG & DROP
                        SetupDragAndDrop(pooledRow.rowObject, item, logicalItemIndex);
                        
                        // Atualizar click handler COM ÍNDICE LÓGICO
                        SetupSimpleClickHandler(pooledRow.rowObject, item, logicalItemIndex);
                        
                        // Aplicar seleção
                        if (item == selectedItem)
                        {
                            SetRowSelected(pooledRow.rowObject, true);
                            lastSelectedRow = pooledRow.rowObject;
                        }
                        else
                        {
                            SetRowSelected(pooledRow.rowObject, false);
                        }
                        
                        logicalItemIndex++; // 🔥 Incrementa APENAS quando renderiza item
                    }
                }
            }
            
            sw.Stop();
            Debug.Log($"RenderWithPooling (reuse): {sw.ElapsedMilliseconds}ms");
            return;
        }
        
        // 2. Se precisar de número diferente de linhas (SUA LÓGICA ORIGINAL)
        Debug.Log($"🔄 Recriando {allItemsToDisplay.Count} linhas (tinha {activePooledRows.Count})");
        
        // Return existing rows FIRST
        if (activePooledRows.Count > 0)
        {
            foreach (var pooledRow in activePooledRows)
            {
                if (pooledRow != null)
                {
                    // 🔥🔥🔥 NOVA LINHA: LIMPAR antes de retornar ao pool!
                    CleanupRowComponents(pooledRow.rowObject);
                    
                    rowPool.ReturnRow(pooledRow);
                }
            }
            activePooledRows.Clear();
        }
        
        // 🆕 AGRUPAR POR CATEGORIA
        var itemsByCategoryNew = GroupItemsByCategory();
        
        float currentYPos = 0f;
        int logicalItemIdx = 0; // 🔥 Contador lógico (só itens)
        
        // 🆕 RENDERIZAR COM HEADERS
        foreach (string category in categoryOrder)
        {
            if (!itemsByCategoryNew.ContainsKey(category))
                continue;
            
            var itemsInCategory = itemsByCategoryNew[category];
            bool isExpanded = categoryExpandedState.ContainsKey(category) ? categoryExpandedState[category] : true;
            
            // 🏷️ CRIAR HEADER
            GameObject headerObj = CreateCategoryHeader(category, isExpanded, currentYPos);
            currentYPos -= 40f; // Header height
            
            // 📦 RENDERIZAR ITENS (se expandido)
            if (isExpanded)
            {
                foreach (var item in itemsInCategory)
                {
                    int quantity = itemQuantities[item];
                    
                    var pooledRow = rowPool.GetRow();
                    if (pooledRow == null) 
                    {
                        Debug.LogError($"Failed to get row for logical item {logicalItemIdx}: {item.itemName}");
                        continue;
                    }
                    
                    pooledRow.rowObject.transform.SetParent(tableContentContainer);
                    pooledRow.rowObject.transform.localScale = Vector3.one;
                    
                    // Position
                    var rectTransform = pooledRow.rowObject.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchoredPosition = new Vector2(0, currentYPos);
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rowHeight);
                    }
                    
                    currentYPos -= rowHeight;
                    
                    // Fill data
                    FillRowWithData(pooledRow, item, quantity);
                    
                    // 🔥 Setup Drag & Drop COM ÍNDICE LÓGICO
                    SetupDragAndDrop(pooledRow.rowObject, item, logicalItemIdx);
                    
                    // Setup click - SIMPLE version COM ÍNDICE LÓGICO
                    SetupSimpleClickHandler(pooledRow.rowObject, item, logicalItemIdx);
                    
                    // Apply selection if this is the selected item
                    if (item == selectedItem)
                    {
                        SetRowSelected(pooledRow.rowObject, true);
                        lastSelectedRow = pooledRow.rowObject;
                    }
                    else
                    {
                        SetRowSelected(pooledRow.rowObject, false);
                    }
                    
                    activePooledRows.Add(pooledRow);
                    logicalItemIdx++; // 🔥 Incrementa APENAS quando renderiza item
                }
            }
        }
        
        sw.Stop();
        Debug.Log($"RenderWithPooling: {sw.ElapsedMilliseconds}ms");
    }
    
    private void FillRowWithData(InventoryRowPool.PooledRow pooledRow, ItemData item, int quantity)
    {
        if (pooledRow.cells == null || pooledRow.cells.Length == 0) return;
        
        // ⭐⭐ OTIMIZAÇÃO: Pré-calcular valores usados múltiplas vezes
        string quantityText = quantity > 1 ? $" x{quantity}" : "";
        int sellPrice = item.GetCalculatedSellPrice();
        
        foreach (var cell in pooledRow.cells)
        {
            FillCellWithData(cell, item, quantityText, sellPrice);
        }
    }
    
    private void FillCellWithData(InventoryTableCell cell, ItemData item, string quantityText, int sellPrice)
    {
        TMP_Text[] textComponents = cell.GetComponentsInChildren<TMP_Text>();
        Image[] imageComponents = cell.GetComponentsInChildren<Image>();
        
        if (textComponents.Length == 0) return;
        
        TMP_Text textComp = textComponents[0];
        
        switch (cell.cellType)
        {
            case InventoryTableCell.CellType.Item:
                if (imageComponents.Length > 1)
                {
                    // ⭐⭐ OTIMIZAÇÃO: Só atualizar se mudou
                    if (imageComponents[1].sprite != item.icon)
                    {
                        imageComponents[1].sprite = item.icon;
                        imageComponents[1].color = item.GetRarityColor();
                    }
                }
                textComp.text = $"{item.itemName}{quantityText}";
                break;
                
            case InventoryTableCell.CellType.Price:
                textComp.text = $"{sellPrice}";
                break;
                
            case InventoryTableCell.CellType.Attack:
                textComp.text = item.attackBonus > 0 ? $"+{item.attackBonus}" : "-";
                break;
                
            case InventoryTableCell.CellType.Defense:
                textComp.text = item.defenseBonus > 0 ? $"+{item.defenseBonus}" : "-";
                break;
                
            case InventoryTableCell.CellType.Magic:
                textComp.text = item.magicAttackBonus > 0 ? $"+{item.magicAttackBonus}" : "-";
                break;
                
            case InventoryTableCell.CellType.Speed:
                textComp.text = item.speedBonus > 0 ? $"+{item.speedBonus}" : "-";
                break;
                
            case InventoryTableCell.CellType.Crit:
                textComp.text = item.criticalRateBonus > 0 ? $"{item.criticalRateBonus}%" : "-";
                break;
                
            case InventoryTableCell.CellType.Evasion:
                textComp.text = item.evasionBonus > 0 ? $"{item.evasionBonus}%" : "-";
                break;
                
            case InventoryTableCell.CellType.Weight:
                textComp.text = $"{item.weight:F1}";
                break;
        }
    }
    
    private void SetupSimpleClickHandler(GameObject rowObj, ItemData item, int rowIndex)
    {
        Button rowButton = rowObj.GetComponent<Button>();
        if (rowButton == null)
        {
            rowButton = rowObj.AddComponent<Button>();
        }
        
        // Clean button setup
        ColorBlock colors = rowButton.colors;
        colors.normalColor = normalRowColor;
        colors.highlightedColor = new Color(0.7f, 0.7f, 0.9f, 0.3f); // Light blue on hover
        colors.pressedColor = new Color(0.5f, 0.5f, 0.8f, 0.5f); // Darker on click
        colors.selectedColor = selectedRowColor; // SELECTED COLOR (important!)
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        rowButton.colors = colors;
        
        rowButton.transition = Selectable.Transition.ColorTint;
        rowButton.navigation = new Navigation() { mode = Navigation.Mode.None };
        
        // Remove old listeners
        rowButton.onClick.RemoveAllListeners();
        
        // SIMPLE click handler
    rowButton.onClick.AddListener(() =>
    {
        OnRowClicked(rowObj, item, rowIndex); // Adicionado rowIndex
    });
    }
    
    private void OnRowClicked(GameObject clickedRow, ItemData item, int logicalItemIndex)
    {
        Debug.Log($"🖱️ Linha clicada: {item.itemName} (Índice LÓGICO: {logicalItemIndex})");
        
        // 🔥 PEGAR SLOT ESPECÍFICO DO MAPEAMENTO (usando índice LÓGICO)
        int inventorySlotIndex = -1;
        InventoryManager.InventorySlot specificSlot = null;
        
        // 🔥 PRIMEIRO: Tentar mapeamento direto COM ÍNDICE LÓGICO
        if (tableRowToInventorySlot.TryGetValue(logicalItemIndex, out inventorySlotIndex))
        {
            Debug.Log($"   🎯 Mapeamento direto: Logical Item {logicalItemIndex} → Slot {inventorySlotIndex}");
            
            
            if (InventoryManager.Instance != null)
            {
                var allSlots = InventoryManager.Instance.GetAllSlots();
                if (inventorySlotIndex >= 0 && inventorySlotIndex < allSlots.Count)
                {
                    specificSlot = allSlots[inventorySlotIndex];
                    
                    // 🔥 VALIDAR SE É O ITEM CORRETO
                    if (specificSlot.item == item && !specificSlot.IsEmpty)
                    {
                        Debug.Log($"   ✅ Slot {inventorySlotIndex} confirmado: {specificSlot.item.itemName} x{specificSlot.quantity}");
                    }
                    else
                    {
                        Debug.LogWarning($"   ⚠️ Slot {inventorySlotIndex} não corresponde! Buscando manualmente...");
                        inventorySlotIndex = -1;
                        specificSlot = null;
                    }
                }
            }
        }
        
        // 🔥 FALLBACK: Se mapeamento falhou, buscar manualmente
        if (inventorySlotIndex < 0 && InventoryManager.Instance != null)
        {
            Debug.LogWarning($"   ⚠️ Mapeamento não encontrado para linha {logicalItemIndex}. Buscando primeiro slot não-equipado...");
            
            var allSlots = InventoryManager.Instance.GetAllSlots();
            
            // 🔥 BUSCAR PRIMEIRO SLOT NÃO-EQUIPADO COM ESTE ITEM
            for (int i = 0; i < allSlots.Count; i++)
            {
                var slot = allSlots[i];
                
                if (!slot.IsEmpty && 
                    slot.item == item && 
                    !slot.isEquipped && 
                    slot.quantity > 0)
                {
                    inventorySlotIndex = i;
                    specificSlot = slot;
                    Debug.Log($"   🔍 Encontrado manualmente: Slot {i}");
                    break;
                }
            }
        }
        
        // 🔥 VALIDAÇÃO FINAL
        if (inventorySlotIndex < 0 || specificSlot == null)
        {
            Debug.LogError($"   ❌ FALHA CRÍTICA: Não conseguiu identificar slot para {item.itemName}!");
            
            // Limpar seleção
            if (lastSelectedRow != null && lastSelectedRow != clickedRow)
            {
                SetRowSelected(lastSelectedRow, false);
            }
            
            SetRowSelected(clickedRow, true);
            selectedItem = item;
            lastSelectedRow = clickedRow;
            
            // Notifica SEM slot específico (vai falhar no equip, mas é melhor que equipar errado)
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected(item);
            }
            return;
        }
        
        // 🔥 LOG FINAL ANTES DE NOTIFICAR
        Debug.Log($"");
        Debug.Log($"   📊 INFORMAÇÃO FINAL:");
        Debug.Log($"   • Item: {item.itemName}");
        Debug.Log($"   • Linha: {logicalItemIndex}");
        Debug.Log($"   • Slot Index: {inventorySlotIndex}");
        Debug.Log($"   • Slot válido: {specificSlot != null}");
        Debug.Log($"   • Quantidade: {specificSlot?.quantity ?? 0}");
        Debug.Log($"");
        
        // Desmarcar anterior
        if (lastSelectedRow != null && lastSelectedRow != clickedRow)
        {
            SetRowSelected(lastSelectedRow, false);
        }
        
        // Marcar nova
        SetRowSelected(clickedRow, true);
        
        // Atualizar referências
        selectedItem = item;
        lastSelectedRow = clickedRow;
        
        // 🔥 NOTIFICAR COM INFORMAÇÃO CORRETA
        if (inventoryUI != null)
        {
            inventoryUI.OnItemSelectedWithSlot(item, inventorySlotIndex, specificSlot, logicalItemIndex);
        }
    }
    
    private void SetRowSelected(GameObject rowObj, bool selected)
    {
        Image bgImage = rowObj.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = selected ? selectedRowColor : normalRowColor;
        }
        
        // Also update Button state
        Button rowButton = rowObj.GetComponent<Button>();
        if (rowButton != null)
        {
            if (selected)
                rowButton.Select();
            else
                rowButton.OnDeselect(null);
        }
    }
    
    public void ClearSelection()
    {
        if (lastSelectedRow != null)
        {
            SetRowSelected(lastSelectedRow, false);
        }
        
        selectedItem = null;
        lastSelectedRow = null;
        
        if (inventoryUI != null)
        {
            inventoryUI.OnItemSelected(null);
        }
        
        Debug.Log("Selection cleared");
    }
    
    public ItemData GetSelectedItem()
    {
        return selectedItem;
    }

    /// <summary>
    /// 📊 Agrupa itens por categoria (PRESERVA ordem do allItemsToDisplay)
    /// </summary>
    private Dictionary<string, List<ItemData>> GroupItemsByCategory()
    {
        var grouped = new Dictionary<string, List<ItemData>>();
        
        foreach (var item in allItemsToDisplay)
        {
            string category = item.GetCategoryName();
            
            if (!grouped.ContainsKey(category))
            {
                grouped[category] = new List<ItemData>();
            }
            
            grouped[category].Add(item);
        }
        
        return grouped;
    }

    /// <summary>
    /// 🏷️ Cria header de categoria
    /// </summary>
    private GameObject CreateCategoryHeader(string categoryName, bool isExpanded, float yPosition)
    {
        if (categoryHeaderPrefab == null)
        {
            Debug.LogError("❌ categoryHeaderPrefab não configurado!");
            return null;
        }
        
        GameObject headerObj = Instantiate(categoryHeaderPrefab, tableContentContainer);
        headerObj.name = $"CategoryHeader_{categoryName}";
        
        // Position
        var rectTransform = headerObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, yPosition);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 40f);
        }
        
        // Setup component
        var headerUI = headerObj.GetComponent<CategoryHeaderUI>();
        if (headerUI == null)
        {
            headerUI = headerObj.AddComponent<CategoryHeaderUI>();
        }
        
        headerUI.Initialize(categoryName, isExpanded);
        
        // Callback para toggle
        headerUI.OnToggleCategory = OnCategoryToggled;
        
        // Cache
        activeCategoryHeaders[categoryName] = headerObj;
        
        Debug.Log($"   🏷️ Header criado: {categoryName} (Expanded: {isExpanded})");
        
        return headerObj;
    }

    /// <summary>
    /// 🧹 Limpa headers antigos
    /// </summary>
    private void ClearCategoryHeaders()
    {
        foreach (var kvp in activeCategoryHeaders)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        
        activeCategoryHeaders.Clear();
    }

    /// <summary>
    /// 🔄 Callback quando categoria é expandida/colapsada
    /// </summary>
    private void OnCategoryToggled(string categoryName, bool isExpanded)
    {
        Debug.Log($"🔄 OnCategoryToggled: {categoryName} = {isExpanded}");
        
        // Salvar estado
        categoryExpandedState[categoryName] = isExpanded;
        
        // Re-renderizar
        RefreshTable(forceRefresh: false);
    }
    
    private void RenderLegacy()
    {
        // Legacy render (without pooling)
        foreach (Transform child in tableContentContainer)
        {
            Destroy(child.gameObject);
        }
        
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            ItemData item = allItemsToDisplay[i];
            int quantity = itemQuantities[item];
            
            GameObject rowObj = Instantiate(itemRowPrefab, tableContentContainer);
            rowObj.name = $"Row_{item.itemName}_{i}";
            
            float yPos = -i * rowHeight;
            var rectTransform = rowObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, yPos);
            
            // Fill cells
            InventoryTableCell[] cells = rowObj.GetComponentsInChildren<InventoryTableCell>();
            foreach (var cell in cells)
            {
                FillCellWithData(cell, item, quantity > 1 ? $" x{quantity}" : "", item.GetCalculatedSellPrice());
            }
            
            SetupSimpleClickHandler(rowObj, item, i);
            
            if (item == selectedItem)
            {
                SetRowSelected(rowObj, true);
                lastSelectedRow = rowObj;
            }
        }
    }
    
    private void ClearTableVisuals()
    {
        // ⭐⭐ OTIMIZAÇÃO: Se vamos reutilizar (cache hit), não limpar!
        if (!forceRefresh && cachedItemCount == allItemsToDisplay.Count && activePooledRows.Count == allItemsToDisplay.Count)
        {
            Debug.Log("⏩ Skipping cleanup (reusing rows)");
            return;
        }
        
        // Medir performance
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        
        if (useObjectPooling && rowPool != null)
        {
            // ⭐⭐ OTIMIZAÇÃO: Usar ReturnAllRows otimizado
            rowPool.ReturnAllRows();
            activePooledRows.Clear();
        }
        else if (tableContentContainer != null)
        {
            // Legacy system
            foreach (Transform child in tableContentContainer)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
        
        sw.Stop();
        Debug.Log($"ClearTableVisuals: {sw.ElapsedMilliseconds}ms (limpou {activePooledRows.Count} linhas)");
    }
    
    [ContextMenu("Debug: Test Selection")]
    public void DebugTestSelection()
    {
        if (allItemsToDisplay.Count > 0)
        {
            int randomIndex = Random.Range(0, allItemsToDisplay.Count);
            ItemData randomItem = allItemsToDisplay[randomIndex];
            
            Debug.Log($"Testing selection on: {randomItem.itemName}");
            
            // Simulate click on first row
            if (activePooledRows.Count > randomIndex)
            {
                OnRowClicked(activePooledRows[randomIndex].rowObject, randomItem, randomIndex);
            }
        }
    }
    public void InvalidateCache()
    {
        Debug.Log("🔄 Cache invalidado - próximo refresh será completo");
        cachedItemCount = -1;
        forceRefresh = true;
    }
    private void ShowErrorMessage(string message)
    {
        if (tableContentContainer == null) return;
        
        GameObject errorObj = new GameObject("ErrorMessage");
        errorObj.transform.SetParent(tableContentContainer);
        
        TMP_Text textComp = errorObj.AddComponent<TextMeshProUGUI>();
        textComp.text = $"ERROR: {message}";
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.red;
        textComp.fontSize = 14;
        
        RectTransform rect = errorObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 40f);
        
        Debug.LogError($"[InventoryTableUI] {message}");
    }

    private void ShowInfoMessage(string message)
    {
        if (tableContentContainer == null) return;
        
        GameObject infoObj = new GameObject("InfoMessage");
        infoObj.transform.SetParent(tableContentContainer);
        
        TMP_Text textComp = infoObj.AddComponent<TextMeshProUGUI>();
        textComp.text = message;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.yellow;
        textComp.fontSize = 16;
        
        RectTransform rect = infoObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 60f);
        
        Debug.Log($"[InventoryTableUI] {message}");
    }
    
    // ⭐⭐ NOVO: Método para forçar refresh completo
    public void ForceRefresh()
    {
        forceRefresh = true;
        cachedItemCount = -1;
        RefreshTable(true);
    }
    
    // ⭐⭐ NOVO: Chamado quando inventário muda (adiciona/remove item)
    public void OnInventoryChanged()
    {
        Debug.Log("📢 OnInventoryChanged() - Invalidando cache");
        
        // Marcar que precisa de refresh completo
        InvalidateCache();
        
        // Refresh imediato
        RefreshTable(true);
    }
    /// <summary>
    /// 🔥 CRIA UMA DROPZONE PARA TODA A ÁREA DA TABELA
    /// Para receber itens desequipados do PaperDoll
    /// </summary>
    private void CreateTableDropZone()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🎯 Criando DropZone para Tabela     ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        if (tableContentContainer == null)
        {
            Debug.LogError("║  ❌ tableContentContainer é NULL!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        // Verificar se já tem DropZone
        var existingDropZone = tableContentContainer.GetComponent<DropZone>();
        if (existingDropZone != null)
        {
            Debug.Log("║  ✅ DropZone já existe na tabela");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  🎯 Container: {tableContentContainer.name}");
        
        // 🔥 PASSO 1: ADICIONAR IMAGE (PARA RAYCAST)
        var image = tableContentContainer.GetComponent<Image>();
        if (image == null)
        {
            Debug.Log("║  🖼️ Adicionando Image...");
            image = tableContentContainer.gameObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.01f); // Quase invisível
        }
        else
        {
            Debug.Log("║  ✅ Image já existe");
        }
        
        image.raycastTarget = true;
        Debug.Log($"║  🎯 Raycast Target: {image.raycastTarget}");
        
        // 🔥 PASSO 2: ADICIONAR DROPZONE
        Debug.Log("║  📦 Adicionando DropZone...");
        var dropZone = tableContentContainer.gameObject.AddComponent<DropZone>();
        
        // 🔥 PASSO 3: CONFIGURAR VIA REFLEXÃO (já que os campos são privados)
        try
        {
            // Configurar dropType = InventoryTable
            var dropTypeField = typeof(DropZone).GetField("dropType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (dropTypeField != null)
            {
                dropTypeField.SetValue(dropZone, DropZone.DropType.InventoryTable);
                Debug.Log("║  ✅ DropType configurado: InventoryTable");
            }
            
            // Configurar acceptedEquipmentSlot = None
            var acceptedSlotField = typeof(DropZone).GetField("acceptedEquipmentSlot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (acceptedSlotField != null)
            {
                acceptedSlotField.SetValue(dropZone, ItemData.EquipmentSlot.None);
                Debug.Log("║  ✅ Accepted Slot: None (aceita qualquer)");
            }
            
            // Configurar backgroundImage
            var bgImageField = typeof(DropZone).GetField("backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (bgImageField != null)
            {
                bgImageField.SetValue(dropZone, image);
                Debug.Log("║  ✅ BackgroundImage configurado");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"║  ⚠️ Não conseguiu configurar via reflexão: {e.Message}");
            Debug.Log("║  ℹ️ Configure manualmente no Inspector");
        }
        
        Debug.Log("║  ✅ DropZone criada para toda a tabela!");
        Debug.Log("╚═══════════════════════════════════════╝");
    }

    /// <summary>
    ///  CONFIGURA DRAG & Drop COM VALIDAÇÃO TRIPLA
    /// </summary>
    private void SetupDragAndDrop(GameObject rowObj, ItemData item, int logicalItemIndex)
    {
        if (rowObj == null || item == null) return;
        
        // Só equipamentos podem ser arrastados
        if (!item.IsEquipment())
        {
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null) Destroy(existingDraggable);
            return;
        }
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 SetupDragAndDrop                  ║");
        Debug.Log($"╞═══════════════════════════════════════╣");
        Debug.Log($"║  📦 Item: {item.itemName}");
        Debug.Log($"║  📋 Logical Item Index: {logicalItemIndex}");
        
        // 🔥 PEGAR SLOT ESPECÍFICO DO MAPEAMENTO (usando índice LÓGICO)
        InventoryManager.InventorySlot specificSlot = null;
        int inventorySlotIndex = -1;
        
        // 🔥🔥🔥 VALIDAÇÃO TRIPLA DO MAPEAMENTO (agora com índice lógico)
        if (tableRowToInventorySlot.TryGetValue(logicalItemIndex, out inventorySlotIndex))
        {
            Debug.Log($"║  ✅ Mapeamento encontrado: Logical Item {logicalItemIndex} → Slot {inventorySlotIndex}");          
            
            if (InventoryManager.Instance != null)
            {
                var allSlots = InventoryManager.Instance.GetAllSlots();
                
                if (inventorySlotIndex >= 0 && inventorySlotIndex < allSlots.Count)
                {
                    specificSlot = allSlots[inventorySlotIndex];
                    
                    // 🔥🔥🔥 VALIDAÇÃO CRÍTICA: O ITEM DO SLOT DEVE SER O MESMO DA LINHA!
                    if (specificSlot.item == item && !specificSlot.IsEmpty)
                    {
                        Debug.Log($"║  ✅ VALIDAÇÃO OK:");
                        Debug.Log($"║     Slot {inventorySlotIndex}: {specificSlot.item.itemName} x{specificSlot.quantity}");
                        Debug.Log($"║     Equipado: {specificSlot.isEquipped}");
                    }
                    else
                    {
                        // ⚠️ MAPEAMENTO INVÁLIDO - BUSCAR CORRETO
                        Debug.LogError($"║  ❌ MAPEAMENTO INVÁLIDO!");
                        Debug.LogError($"║     Esperado: {item.itemName}");
                        Debug.LogError($"║     Encontrado: {specificSlot.item?.itemName ?? "NULL"}");
                        Debug.LogError($"║  🔍 Buscando slot correto...");
                        
                        // Buscar manualmente
                        specificSlot = null;
                        inventorySlotIndex = -1;
                        
                        for (int i = 0; i < allSlots.Count; i++)
                        {
                            var slot = allSlots[i];
                            
                            if (!slot.IsEmpty && 
                                slot.item == item && 
                                !slot.isEquipped && 
                                slot.quantity > 0)
                            {
                                specificSlot = slot;
                                inventorySlotIndex = i;
                                Debug.Log($"║  ✅ Slot correto encontrado: {i}");
                                break;
                            }
                        }
                        
                        if (specificSlot == null)
                        {
                            Debug.LogError($"║  ❌ NENHUM SLOT VÁLIDO ENCONTRADO!");
                            Debug.Log($"╚═══════════════════════════════════════╝");
                            return; // 🔥 ABORTA SE NÃO ENCONTRAR
                        }
                    }
                }
                else
                {
                    Debug.LogError($"║  ❌ Índice {inventorySlotIndex} fora do range!");
                    specificSlot = null;
                    inventorySlotIndex = -1;
                }
            }
        }
        else
        {
            Debug.LogWarning($"║  ⚠️ Mapeamento não encontrado para row {logicalItemIndex}");
        }
        
        // 🔥 FALLBACK: Se não tem mapeamento, buscar primeiro slot não-equipado
        if (specificSlot == null || inventorySlotIndex < 0)
        {
            Debug.LogWarning($"║  🔍 FALLBACK: Buscando primeiro slot não-equipado...");
            
            var allSlots = InventoryManager.Instance.GetAllSlots();
            
            for (int i = 0; i < allSlots.Count; i++)
            {
                var slot = allSlots[i];
                
                if (!slot.IsEmpty && 
                    slot.item == item && 
                    !slot.isEquipped && 
                    slot.quantity > 0)
                {
                    specificSlot = slot;
                    inventorySlotIndex = i;
                    Debug.Log($"║  ✅ Fallback encontrado: Slot {i}");
                    break;
                }
            }
        }
        
        // 🔥 VALIDAÇÃO FINAL
        if (specificSlot == null || inventorySlotIndex < 0)
        {
            Debug.LogError($"║  ❌ FALHA CRÍTICA: Nenhum slot válido para {item.itemName}!");
            Debug.Log($"╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  ✅ CONFIGURAÇÃO FINAL:");
        Debug.Log($"║     Slot Index: {inventorySlotIndex}");
        Debug.Log($"║     Item: {specificSlot.item.itemName}");
        Debug.Log($"║     Quantidade: {specificSlot.quantity}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // Criar/Atualizar DraggableItem
        var draggable = rowObj.GetComponent<DraggableItem>();
        if (draggable == null)
        {
            draggable = rowObj.AddComponent<DraggableItem>();
        }
        
        // 🔥 Configurar com slot VALIDADO
        draggable.SetupDraggable(
            item,  // 🔥 Item da linha
            DraggableItem.DragSource.InventoryTable, 
            ItemData.EquipmentSlot.None,
            inventorySlotIndex,   // 🔥 Índice VALIDADO
            specificSlot          // 🔥 Slot VALIDADO
        );
        
        // Garantir componentes visuais
        var image = rowObj.GetComponent<Image>();
        if (image == null)
        {
            image = rowObj.AddComponent<Image>();
            image.color = normalRowColor;
        }
        
        var canvasGroup = rowObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rowObj.AddComponent<CanvasGroup>();
        }
    }
    private void CleanupRowComponents(GameObject rowObject)
    {
        if (rowObject == null) return;
        
        // 1. Destruir DraggableItem (será recriado)
        var draggable = rowObject.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            Destroy(draggable);
        }
        
        // 2. Limpar Button listeners
        var button = rowObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
        
        // 3. Resetar visual
        var image = rowObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = normalRowColor;
        }
        
        // 4. Resetar CanvasGroup
        var canvasGroup = rowObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public int GetMappedInventorySlot(int logicalItemIndex, ItemData item)
    {
        // 🎯 ESTRATÉGIA 1: Mapeamento direto (usando índice lógico)
        if (tableRowToInventorySlot.TryGetValue(logicalItemIndex, out int slotIndex))
        {
            Debug.Log($"   🎯 Mapeamento encontrado: Logical Item {logicalItemIndex} → Slot {slotIndex}");
            
            // Validar se o slot ainda tem o item correto
            if (InventoryManager.Instance != null)
            {
                var allSlots = InventoryManager.Instance.GetAllSlots();
                if (slotIndex >= 0 && slotIndex < allSlots.Count)
                {
                    var slot = allSlots[slotIndex];
                    if (slot.item == item && !slot.IsEmpty && !slot.isEquipped)
                    {
                        return slotIndex;
                    }
                    else
                    {
                        Debug.LogWarning($"   ⚠️ Slot {slotIndex} não corresponde mais!");
                    }
                }
            }
        }
        
        // 🎯 ESTRATÉGIA 2: Buscar primeiro não-equipado
        Debug.LogWarning($"   ⚠️ Buscando manualmente primeiro slot não-equipado...");
        
        if (InventoryManager.Instance != null)
        {
            var allSlots = InventoryManager.Instance.GetAllSlots();
            
            for (int i = 0; i < allSlots.Count; i++)
            {
                var slot = allSlots[i];
                if (!slot.IsEmpty && 
                    slot.item == item && 
                    !slot.isEquipped && 
                    slot.quantity > 0)
                {
                    Debug.Log($"   🔍 Encontrado: Slot {i}");
                    return i;
                }
            }
        }
        
        Debug.LogError($"   ❌ Nenhum slot válido encontrado para {item?.itemName}!");
        return -1;
    }
    /// <summary>
    /// 🔥 MAPEIA LINHA DA TABELA PARA SLOT ESPECÍFICO DO INVENTÁRIO
    /// </summary>
    private int FindSpecificInventorySlotForTableRow(int tableRowIndex, ItemData targetItem)
    {
        if (InventoryManager.Instance == null || targetItem == null)
            return -1;
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        // 🔥 ESTRATÉGIA 1: Se temos exatamente o mesmo número de slots que linhas
        if (tableRowIndex < allSlots.Count && 
            allSlots[tableRowIndex].item == targetItem && 
            !allSlots[tableRowIndex].isEquipped)
        {
            return tableRowIndex;
        }
        
        // 🔥 ESTRATÉGIA 2: Percorrer todos os slots para encontrar
        List<int> matchingSlots = new List<int>();
        
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            
            if (!slot.IsEmpty && 
                slot.item == targetItem && 
                !slot.isEquipped)
            {
                matchingSlots.Add(i);
            }
        }
        
        // 🔥 Se encontrou slots compatíveis
        if (matchingSlots.Count > 0)
        {
            // Para múltiplos itens iguais, tentar usar o primeiro não-rastreado
            if (matchingSlots.Count > 1)
            {
                Debug.Log($"🔍 {targetItem.itemName} encontrado em {matchingSlots.Count} slots: [{string.Join(", ", matchingSlots)}]");
                
                // Tentar usar o slot que corresponde à posição na tabela
                if (tableRowIndex < matchingSlots.Count)
                {
                    return matchingSlots[tableRowIndex];
                }
                
                // Fallback: primeiro slot da lista
                return matchingSlots[0];
            }
            else
            {
                return matchingSlots[0];
            }
        }
        
        return -1;
    }

    [ContextMenu("🔍 Debug: Check Mapping")]
    public void DebugCheckMapping()
    {
        Debug.Log("=== MAPEAMENTO LINHA → SLOT ===");
        
        foreach (var mapping in tableRowToInventorySlot)
        {
            var allSlots = InventoryManager.Instance.GetAllSlots();
            var slot = allSlots[mapping.Value];
            
            Debug.Log($"Linha {mapping.Key} → Slot {mapping.Value}: {slot.item?.itemName} x{slot.quantity}");
        }
    }

    [ContextMenu("🔍 Debug: Verificar Drag & Drop Setup")]
    public void DebugCheckDragDropSetup()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO DRAG & DROP SETUP       ║");
        Debug.Log("╚═══════════════════════════════════════════╝");
        
        Debug.Log($"\n📊 Estatísticas:");
        Debug.Log($"   Total de linhas ativas: {activePooledRows.Count}");
        Debug.Log($"   Total de itens para mostrar: {allItemsToDisplay.Count}");
        
        int rowsWithDraggable = 0;
        int rowsWithImage = 0;
        int rowsWithCanvasGroup = 0;
        
        foreach (var row in activePooledRows)
        {
            if (row != null && row.rowObject != null)
            {
                if (row.rowObject.GetComponent<DraggableItem>() != null)
                    rowsWithDraggable++;
                
                if (row.rowObject.GetComponent<Image>() != null)
                    rowsWithImage++;
                
                if (row.rowObject.GetComponent<CanvasGroup>() != null)
                    rowsWithCanvasGroup++;
            }
        }
        
        Debug.Log($"\n🎯 Componentes de Drag:");
        Debug.Log($"   Linhas com DraggableItem: {rowsWithDraggable}/{activePooledRows.Count}");
        Debug.Log($"   Linhas com Image: {rowsWithImage}/{activePooledRows.Count}");
        Debug.Log($"   Linhas com CanvasGroup: {rowsWithCanvasGroup}/{activePooledRows.Count}");
        
        // Verificar DropZones na cena
        var dropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        Debug.Log($"\n📍 DropZones encontradas na cena: {dropZones.Length}");
        
        foreach (var dz in dropZones)
        {
            Debug.Log($"   - {dz.gameObject.name}");
            Debug.Log($"     Tipo: {dz.GetDropType()}");
            Debug.Log($"     Aceita slot: {dz.GetAcceptedEquipmentSlot()}");
        }
        
        // Verificar se tem EventSystem
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"\n🎮 EventSystem: {(eventSystem != null ? "✅ OK" : "❌ FALTANDO!")}");
        
        // Verificar Canvas
        if (tableContentContainer != null)
        {
            var canvas = tableContentContainer.GetComponentInParent<Canvas>();
            Debug.Log($"\n🎨 Canvas:");
            Debug.Log($"   Encontrado: {(canvas != null ? "✅" : "❌")}");
            
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log($"   GraphicRaycaster: {(raycaster != null ? "✅" : "❌")}");
            }
        }
        
        Debug.Log("\n═══════════════════════════════════════════");
    }

    [ContextMenu("🔍 Debug: Check DropZones")]
    public void DebugCheckDropZones()
    {
        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO DE DROPZONES NA TABELA             ║");
        Debug.Log("╠══════════════════════════════════════════════════════╣");
        
        if (tableContentContainer == null)
        {
            Debug.LogError("║  ❌ tableContentContainer é NULL!");
            Debug.Log("╚══════════════════════════════════════════════════════╝");
            return;
        }
        
        // 1. Verificar DropZone no container principal
        var containerDropZone = tableContentContainer.GetComponent<DropZone>();
        if (containerDropZone != null)
        {
            Debug.Log("║  ✅ DropZone encontrada no tableContentContainer");
            
            // Tentar acessar propriedades via reflexão
            var dropTypeField = typeof(DropZone).GetField("dropType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (dropTypeField != null)
            {
                var dropType = (DropZone.DropType)dropTypeField.GetValue(containerDropZone);
                Debug.Log($"║     Tipo: {dropType}");
            }
        }
        else
        {
            Debug.LogError("║  ❌ Nenhuma DropZone no tableContentContainer!");
        }
        
        // 2. Verificar Image (raycast)
        var containerImage = tableContentContainer.GetComponent<Image>();
        if (containerImage != null)
        {
            Debug.Log($"║  ✅ Image encontrada: RaycastTarget = {containerImage.raycastTarget}");
        }
        else
        {
            Debug.LogError("║  ❌ Nenhuma Image no tableContentContainer!");
        }
        
        // 3. Verificar DropZones nas linhas individuais
        int rowDropZones = 0;
        if (activePooledRows != null)
        {
            foreach (var row in activePooledRows)
            {
                if (row != null && row.rowObject != null)
                {
                    var rowDropZone = row.rowObject.GetComponent<DropZone>();
                    if (rowDropZone != null) rowDropZones++;
                }
            }
        }
        
        Debug.Log($"║  📊 DropZones nas linhas: {rowDropZones}/{activePooledRows?.Count ?? 0}");
        
        // 4. Verificar configuração geral
        Debug.Log("║");
        Debug.Log("║  ⚙️ CONFIGURAÇÃO:");
        Debug.Log($"║     Use Object Pooling: {useObjectPooling}");
        Debug.Log($"║     Row Pool: {(rowPool != null ? "✅" : "❌")}");
        Debug.Log($"║     Scroll Rect: {(scrollRect != null ? "✅" : "❌")}");
        
        // 5. Testar raycast
        if (containerImage != null && !containerImage.raycastTarget)
        {
            Debug.LogError("║  ⚠️ AVISO: RaycastTarget está FALSE!");
            Debug.Log("║     DropZone não vai receber eventos de mouse!");
        }
        
        Debug.Log("╚══════════════════════════════════════════════════════╝");
    }

    [ContextMenu("🔍 Debug: Verificar Mapeamento COMPLETO")]
    public void DebugVerifyCompleteMapping()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║  🔍 VERIFICAÇÃO COMPLETA DE MAPEAMENTO                    ║");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");
        
        // 1. Itens na tabela
        Debug.Log($"║  📊 Itens para mostrar: {allItemsToDisplay.Count}");
        Debug.Log($"║  📋 Linhas ativas: {activePooledRows.Count}");
        Debug.Log($"║  🗺️ Mapeamentos: {tableRowToInventorySlot.Count}");
        Debug.Log($"║");
        
        // 2. Listar itens da tabela
        Debug.Log($"║  📦 ITENS NA TABELA:");
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            var item = allItemsToDisplay[i];
            int qty = itemQuantities.ContainsKey(item) ? itemQuantities[item] : 0;
            Debug.Log($"║    Row {i}: {item.itemName} x{qty}");
        }
        Debug.Log($"║");
        
        // 3. Verificar mapeamentos
        Debug.Log($"║  🗺️ MAPEAMENTOS (Row → Slot):");
        foreach (var mapping in tableRowToInventorySlot)
        {
            var allSlots = InventoryManager.Instance.GetAllSlots();
            
            if (mapping.Value >= 0 && mapping.Value < allSlots.Count)
            {
                var slot = allSlots[mapping.Value];
                string equippedMark = slot.isEquipped ? " [EQUIPADO]" : "";
                
                Debug.Log($"║    Row {mapping.Key} → Slot {mapping.Value}: {slot.item?.itemName ?? "VAZIO"} x{slot.quantity}{equippedMark}");
            }
            else
            {
                Debug.LogError($"║    Row {mapping.Key} → Slot {mapping.Value}: ❌ ÍNDICE INVÁLIDO!");
            }
        }
        Debug.Log($"║");
        
        // 4. Verificar DraggableItems
        Debug.Log($"║  🎯 DRAGGABLE ITEMS:");
        for (int i = 0; i < activePooledRows.Count; i++)
        {
            var row = activePooledRows[i];
            if (row == null || row.rowObject == null) continue;
            
            var draggable = row.rowObject.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                var itemData = draggable.GetItemData();
                int slotIndex = draggable.GetSourceInventorySlotIndex();
                var specificSlot = draggable.GetSourceInventorySlot();
                
                Debug.Log($"║    Row {i}:");
                Debug.Log($"║      Item: {itemData?.itemName ?? "NULL"}");
                Debug.Log($"║      Slot Index: {slotIndex}");
                Debug.Log($"║      Specific Slot: {specificSlot?.item?.itemName ?? "NULL"}");
            }
            else
            {
                Debug.Log($"║    Row {i}: ❌ SEM DRAGGABLE");
            }
        }
        
        Debug.Log($"╚═══════════════════════════════════════════════════════════╝");
    }
}