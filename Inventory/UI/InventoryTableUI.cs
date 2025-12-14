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
        
        var inventorySlots = InventoryManager.Instance.GetAllSlots();
        var stackableItemsMap = new Dictionary<ItemData, int>();
        var nonStackableItems = new List<ItemData>();
        
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item != null)
            {
                if (slot.item.stackLimit == 1)
                {
                    for (int i = 0; i < slot.quantity; i++)
                    {
                        nonStackableItems.Add(slot.item);
                    }
                }
                else
                {
                    if (!stackableItemsMap.ContainsKey(slot.item))
                        stackableItemsMap[slot.item] = 0;
                    stackableItemsMap[slot.item] += slot.quantity;
                }
            }
        }
        
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
        
        // 1. Tentar reutilizar se já temos o número exato de linhas
        if (activePooledRows.Count == allItemsToDisplay.Count)
        {
            Debug.Log("✅ Reutilizando linhas existentes (mesmo count)...");
            
            for (int i = 0; i < allItemsToDisplay.Count; i++)
            {
                ItemData item = allItemsToDisplay[i];
                int quantity = itemQuantities[item];
                
                var pooledRow = activePooledRows[i];
                
                // Apenas atualizar dados
                FillRowWithData(pooledRow, item, quantity);

                SetupDragAndDrop(pooledRow.rowObject, item);
                
                // Atualizar click handler
                SetupSimpleClickHandler(pooledRow.rowObject, item, i);
                
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
            }
            
            sw.Stop();
            Debug.Log($"RenderWithPooling (reuse): {sw.ElapsedMilliseconds}ms");
            return;
        }
        
        // 2. Se precisar de número diferente de linhas
        Debug.Log($"🔄 Recriando {allItemsToDisplay.Count} linhas (tinha {activePooledRows.Count})");
        
        // Return existing rows FIRST
        if (activePooledRows.Count > 0)
        {
            foreach (var pooledRow in activePooledRows)
            {
                if (pooledRow != null)
                {
                    rowPool.ReturnRow(pooledRow);
                }
            }
            activePooledRows.Clear();
        }
        
        // Create rows for ALL items
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            ItemData item = allItemsToDisplay[i];
            int quantity = itemQuantities[item];
            
            var pooledRow = rowPool.GetRow();
            if (pooledRow == null) 
            {
                Debug.LogError($"Failed to get row for item {i}: {item.itemName}");
                continue;
            }
            
            pooledRow.rowObject.transform.SetParent(tableContentContainer);
            pooledRow.rowObject.transform.localScale = Vector3.one;
            
            // Position
            float yPos = -i * rowHeight;
            var rectTransform = pooledRow.rowObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, yPos);
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rowHeight);
            }
            
            // Fill data
            FillRowWithData(pooledRow, item, quantity);
            //Drag and Drop
            SetupDragAndDrop(pooledRow.rowObject, item);

            // Setup click - SIMPLE version
            SetupSimpleClickHandler(pooledRow.rowObject, item, i);
            
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
            OnRowClicked(rowObj, item);
        });
    }
    
    private void OnRowClicked(GameObject clickedRow, ItemData item)
    {
        Debug.Log($"Clicked: {item.itemName}");
        
        // 1. Deselect previous
        if (lastSelectedRow != null && lastSelectedRow != clickedRow)
        {
            SetRowSelected(lastSelectedRow, false);
        }
        
        // 2. Select new
        SetRowSelected(clickedRow, true);
        
        // 3. Update references
        selectedItem = item;
        lastSelectedRow = clickedRow;
        
        // 4. Notify InventoryUI
        if (inventoryUI != null)
        {
            inventoryUI.OnItemSelected(item);
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
                OnRowClicked(activePooledRows[randomIndex].rowObject, randomItem);
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
/// Configura o sistema de Drag & Drop para uma linha da tabela
/// </summary>
    private void SetupDragAndDrop(GameObject rowObj, ItemData item)
    {
        if (rowObj == null || item == null) return;
        
        // Só equipamentos podem ser arrastados
        if (!item.IsEquipment())
        {
            // Remove draggable se existir
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null)
            {
                Destroy(existingDraggable);
            }
            return;
        }
        
        // 1. Adicionar DraggableItem se não existir
        var draggable = rowObj.GetComponent<DraggableItem>();
        if (draggable == null)
        {
            draggable = rowObj.AddComponent<DraggableItem>();
        }
        
        // 2. Configurar o draggable
        draggable.SetupDraggable(
            item, 
            DraggableItem.DragSource.InventoryTable, 
            ItemData.EquipmentSlot.None
        );
        
        // 3. Garantir que tem Image (necessário para drag visual)
        var image = rowObj.GetComponent<Image>();
        if (image == null)
        {
            image = rowObj.AddComponent<Image>();
            image.color = normalRowColor;
        }
        
        // 4. Garantir que tem CanvasGroup (para controle de alpha durante drag)
        var canvasGroup = rowObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rowObj.AddComponent<CanvasGroup>();
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
}