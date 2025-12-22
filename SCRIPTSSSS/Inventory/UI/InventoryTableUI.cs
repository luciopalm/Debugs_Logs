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
    private Dictionary<ItemData, int> itemToListIndex = new Dictionary<ItemData, int>();
    private List<InventoryRowPool.PooledRow> activePooledRows = new List<InventoryRowPool.PooledRow>();
    
    // Simple Selection System
    private ItemData selectedItem = null;
    private GameObject lastSelectedRow = null;
    
    // Performance
    [SerializeField] private float rowHeight = 40f;
    private ScrollRect scrollRect;
    
    // Category Header
    [Header("Category System")]
    [SerializeField] private GameObject categoryHeaderPrefab;
    [SerializeField] private bool enableCategoryCollapse = true;

    private GameObject currentInfoMessageObject = null;
    
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

    // Cache System
    private int cachedItemCount = -1;
    private bool forceRefresh = false;

    private bool enableDiagnostics = false; // Desabilitado para performance

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
        
        // Criar DropZone para a tabela
        CreateTableDropZone();
    }
    
    public void RefreshTable(bool forceRefresh = false)
    {
        this.forceRefresh = forceRefresh;
        
        // Remover mensagem anterior
        ClearInfoMessage();
        
        try
        {
            // Verificações básicas
            if (tableContentContainer == null)
            {
                Debug.LogError("tableContentContainer é NULL!");
                ShowErrorMessage("tableContentContainer not configured!");
                return;
            }
            
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("InventoryManager.Instance é NULL!");
                ShowErrorMessage("InventoryManager not found!");
                return;
            }
            
            // Coleta de dados
            ProcessInventoryData();
            
            // Se inventário vazio
            if (allItemsToDisplay.Count == 0)
            {
                ShowInfoMessage("Inventory is empty!\nAdd items to get started.");
                
                // Atualizar cache
                cachedItemCount = 0;
                ClearTableVisuals();
                return;
            }

            // Remover qualquer mensagem antiga
            ClearInfoMessage();
            
            // Cache System: Verificar se podemos REUTILIZAR linhas
            if (!forceRefresh && cachedItemCount == allItemsToDisplay.Count && activePooledRows.Count == allItemsToDisplay.Count)
            {
                // Apenas atualizar dados nas linhas existentes
                UpdateExistingRows();
                return;
            }
            
            // Limpeza (só se necessário)
            ClearTableVisuals();
            
            // Renderização
            RenderAllItems();
            
            // Atualizar cache count
            cachedItemCount = allItemsToDisplay.Count;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro no RefreshTable: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            ShowErrorMessage($"Error: {e.Message}");
        }
    }
    
    // Atualizar linhas existentes (mais rápido)
    private void UpdateExistingRows()
    {
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
    }
    
    /// <summary>
    /// Atualiza apenas dados visuais sem recriar linhas
    /// Usado após Drag & Drop para zero lag
    /// </summary>
    public void UpdateExistingRowsData()
    {
        // Re-processar dados do inventário
        ProcessInventoryData();
        
        // Verificar se número de itens mudou
        if (allItemsToDisplay.Count != activePooledRows.Count)
        {
            RefreshTable(true);
            return;
        }
        
        // Atualizar APENAS dados nas células existentes
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            ItemData item = allItemsToDisplay[i];
            int quantity = itemQuantities[item];
            
            if (i >= activePooledRows.Count)
            {
                Debug.LogError($"Index {i} out of bounds (rows: {activePooledRows.Count})");
                break;
            }
            
            var pooledRow = activePooledRows[i];
            
            // Apenas atualiza texto/sprite
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
    }

    /// <summary>
    /// Atualiza APENAS dados de uma linha (não cria/destrói nada)
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
        itemToListIndex.Clear();
        tableRowToInventorySlot.Clear();
        
        var inventorySlots = InventoryManager.Instance.GetAllSlots();
        
        // Nova estratégia: Agrupar por categoria PRIMEIRO
        var itemsByCategory = new Dictionary<string, List<(ItemData item, int slotIndex, int unitIndex)>>();
        
        // Coletar todos os itens não-equipados
        foreach (var slot in inventorySlots)
        {
            if (slot.IsEmpty || slot.item == null || slot.isEquipped)
                continue;
            
            string category = slot.item.GetCategoryName();
            
            if (!itemsByCategory.ContainsKey(category))
            {
                itemsByCategory[category] = new List<(ItemData item, int slotIndex, int unitIndex)>();
            }
            
            // Para não-stackable: uma entrada por unidade
            if (slot.item.stackLimit == 1)
            {
                for (int unitIndex = 0; unitIndex < slot.quantity; unitIndex++)
                {
                    itemsByCategory[category].Add((slot.item, slot.slotIndex, unitIndex));
                }
            }
            else
            {
                // Para stackable: uma entrada com todas as unidades
                itemsByCategory[category].Add((slot.item, slot.slotIndex, 0));
            }
        }
        
        // Ordenar pelas categorias definidas (categoryOrder)
        int listIndex = 0;
        
        foreach (string category in categoryOrder)
        {
            if (!itemsByCategory.ContainsKey(category)) continue;
            
            var itemsInCategory = itemsByCategory[category];
            
            // Dentro da categoria, manter ordem de adição (slotIndex)
            itemsInCategory.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
            
            // Adicionar à lista de exibição
            foreach (var itemInfo in itemsInCategory)
            {
                allItemsToDisplay.Add(itemInfo.item);
                
                // Para stackable, acumular quantidade
                if (itemInfo.item.stackLimit > 1)
                {
                    if (!itemQuantities.ContainsKey(itemInfo.item))
                    {
                        // Encontrar quantidade total deste item stackable
                        int totalQty = 0;
                        foreach (var slot in inventorySlots)
                        {
                            if (!slot.IsEmpty && slot.item == itemInfo.item && !slot.isEquipped)
                            {
                                totalQty += slot.quantity;
                            }
                        }
                        itemQuantities[itemInfo.item] = totalQty;
                    }
                }
                else
                {
                    itemQuantities[itemInfo.item] = 1;
                }
                
                // Mapear: linha da tabela → slot específico
                tableRowToInventorySlot[listIndex] = itemInfo.slotIndex;
                
                listIndex++;
            }
        }
    }
    
    /// <summary>
    /// Obter slot específico para item não-stackable
    /// Usado quando há múltiplas unidades do mesmo item
    /// </summary>
    private InventoryManager.InventorySlot GetSpecificSlotForNonStackable(ItemData item, int logicalIndex)
    {
        if (InventoryManager.Instance == null) return null;
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        // Contar quantas vezes já vimos este item
        int occurrenceCount = 0;
        
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            
            if (slot.IsEmpty || slot.item != item || slot.isEquipped)
                continue;
            
            // Para não-stackable, cada quantidade é uma ocorrência separada
            for (int q = 0; q < slot.quantity; q++)
            {
                if (occurrenceCount == logicalIndex)
                {
                    return slot; // Encontrou o slot correto!
                }
                occurrenceCount++;
            }
        }
        
        return null;
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
        ClearCategoryHeaders();
        
        // Agrupar por categoria com índice original
        var itemsByCategory = GroupItemsByCategoryWithIndex();
        
        // Calcular quantos itens visíveis (expandidos)
        int totalVisibleItems = 0;
        foreach (string category in categoryOrder)
        {
            if (!itemsByCategory.ContainsKey(category)) continue;
            
            bool isExpanded = categoryExpandedState.ContainsKey(category) ? categoryExpandedState[category] : true;
            if (isExpanded)
            {
                totalVisibleItems += itemsByCategory[category].Count;
            }
        }
        
        // Ajustar pool se necessário
        bool needsRecreate = (activePooledRows.Count != totalVisibleItems);
        
        if (needsRecreate)
        {
            // Retornar todas as linhas ao pool
            foreach (var pooledRow in activePooledRows)
            {
                if (pooledRow != null)
                {
                    CleanupRowComponents(pooledRow.rowObject);
                    rowPool.ReturnRow(pooledRow);
                }
            }
            activePooledRows.Clear();
        }
        
        // Renderizar com mapeamento correto
        float currentY = 0f;
        int visualRowIndex = 0;
        
        // Criar mapeamento VISUAL → LÓGICO
        Dictionary<int, int> visualToLogicalMap = new Dictionary<int, int>();
        
        foreach (string category in categoryOrder)
        {
            if (!itemsByCategory.ContainsKey(category)) continue;
            
            var itemsInCategory = itemsByCategory[category];
            bool isExpanded = categoryExpandedState.ContainsKey(category) ? categoryExpandedState[category] : true;
            
            // Criar HEADER
            GameObject headerObj = CreateCategoryHeader(category, isExpanded, currentY);
            currentY -= 40f;
            
            // Percorrer itens da categoria
            foreach (var itemPair in itemsInCategory)
            {
                ItemData item = itemPair.item;
                int originalIndex = itemPair.originalIndex;
                
                // Se expandido: RENDERIZAR
                if (isExpanded)
                {
                    InventoryRowPool.PooledRow pooledRow;
                    
                    if (needsRecreate)
                    {
                        pooledRow = rowPool.GetRow();
                        if (pooledRow == null) continue;
                        
                        pooledRow.rowObject.transform.SetParent(tableContentContainer);
                        pooledRow.rowObject.transform.localScale = Vector3.one;
                        
                        var layoutElement = pooledRow.rowObject.GetComponent<LayoutElement>();
                        if (layoutElement == null) layoutElement = pooledRow.rowObject.AddComponent<LayoutElement>();
                        layoutElement.ignoreLayout = true;
                        
                        activePooledRows.Add(pooledRow);
                    }
                    else
                    {
                        // Reutilizar linha existente
                        if (visualRowIndex >= activePooledRows.Count)
                        {
                            Debug.LogError($"Visual index {visualRowIndex} out of bounds!");
                            continue;
                        }
                        pooledRow = activePooledRows[visualRowIndex];
                    }
                    
                    // Posicionar
                    var rectTransform = pooledRow.rowObject.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = new Vector2(0, 1);
                        rectTransform.anchorMax = new Vector2(1, 1);
                        rectTransform.pivot = new Vector2(0.5f, 1);
                        rectTransform.anchoredPosition = new Vector2(0, currentY);
                        rectTransform.sizeDelta = new Vector2(0, rowHeight);
                    }
                    
                    // Garantir que linha está acima dos HEADERS
                    pooledRow.rowObject.transform.SetAsLastSibling();
                    
                    currentY -= rowHeight;
                    
                    // Preencher dados
                    int quantity = itemQuantities[item];
                    FillRowWithData(pooledRow, item, quantity);
                    
                    // Usar originalIndex
                    SetupDragAndDrop(pooledRow.rowObject, item, originalIndex);
                    SetupSimpleClickHandler(pooledRow.rowObject, item, originalIndex);

                    EnsureRowFullyInteractable(pooledRow.rowObject);
                    
                    // Salvar mapeamento VISUAL→ORIGINAL
                    visualToLogicalMap[visualRowIndex] = originalIndex;
                    
                    // Seleção
                    if (item == selectedItem)
                    {
                        SetRowSelected(pooledRow.rowObject, true);
                        lastSelectedRow = pooledRow.rowObject;
                    }
                    else
                    {
                        SetRowSelected(pooledRow.rowObject, false);
                    }
                    
                    visualRowIndex++;
                }
            }
        }
        
        // Ajustar content size
        AdjustContentSize(currentY);
    }

    /// <summary>
    /// Agrupa itens por categoria
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

    // Método auxiliar: Encontrar índice lógico a partir do item
    private int FindLogicalIndexForItem(ItemData targetItem, int occurrence = 0)
    {
        int count = 0;
        
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            if (allItemsToDisplay[i] == targetItem)
            {
                if (count == occurrence)
                    return i;
                count++;
            }
        }
        
        return -1;
    }

    /// <summary>
    /// Ajusta o tamanho do content container
    /// </summary>
    private void AdjustContentSize(float finalY)
    {
        if (tableContentContainer == null) return;
        
        var contentRT = tableContentContainer.GetComponent<RectTransform>();
        if (contentRT != null)
        {
            float totalHeight = Mathf.Abs(finalY);
            contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, totalHeight);
        }
    }

    private void FillRowWithData(InventoryRowPool.PooledRow pooledRow, ItemData item, int quantity)
    {
        if (pooledRow.cells == null || pooledRow.cells.Length == 0) return;
        
        // Pré-calcular valores usados múltiplas vezes
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
                    // Só atualizar se mudou
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
        colors.highlightedColor = new Color(0.7f, 0.7f, 0.9f, 0.3f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);
        colors.selectedColor = selectedRowColor;
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        rowButton.colors = colors;
        
        rowButton.transition = Selectable.Transition.ColorTint;
        rowButton.navigation = new Navigation() { mode = Navigation.Mode.None };
        
        // Remove old listeners
        rowButton.onClick.RemoveAllListeners();

        // Simple click handler
        rowButton.onClick.AddListener(() =>
        {
            OnRowClicked(rowObj, item, rowIndex);
        });
    }
    
    private void OnRowClicked(GameObject clickedRow, ItemData item, int logicalItemIndex)
    {
        // Pegar slot específico do mapeamento (usando índice LÓGICO)
        int inventorySlotIndex = -1;
        InventoryManager.InventorySlot specificSlot = null;
        
        // Primeiro: Tentar mapeamento direto COM ÍNDICE LÓGICO
        if (tableRowToInventorySlot.TryGetValue(logicalItemIndex, out inventorySlotIndex))
        {
            if (InventoryManager.Instance != null)
            {
                var allSlots = InventoryManager.Instance.GetAllSlots();
                if (inventorySlotIndex >= 0 && inventorySlotIndex < allSlots.Count)
                {
                    specificSlot = allSlots[inventorySlotIndex];
                    
                    // Validar se é o item correto
                    if (specificSlot.item == item && !specificSlot.IsEmpty)
                    {
                        // Slot confirmado
                    }
                    else
                    {
                        inventorySlotIndex = -1;
                        specificSlot = null;
                    }
                }
            }
        }
        
        // Fallback: Se mapeamento falhou, buscar manualmente
        if (inventorySlotIndex < 0 && InventoryManager.Instance != null)
        {
            var allSlots = InventoryManager.Instance.GetAllSlots();
            
            // Buscar primeiro slot não-equipado com este item
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
                    break;
                }
            }
        }
        
        // Validação final
        if (inventorySlotIndex < 0 || specificSlot == null)
        {
            // Limpar seleção
            if (lastSelectedRow != null && lastSelectedRow != clickedRow)
            {
                SetRowSelected(lastSelectedRow, false);
            }
            
            SetRowSelected(clickedRow, true);
            selectedItem = item;
            lastSelectedRow = clickedRow;
            
            // Notifica SEM slot específico
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected(item);
            }
            return;
        }
        
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
        
        // Notificar com informação correta
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
    }
    
    public ItemData GetSelectedItem()
    {
        return selectedItem;
    }

    /// <summary>
    /// Agrupa itens por categoria PRESERVANDO índice original
    /// Retorna: Dictionary<categoria, List<(ItemData item, int originalIndex)>>
    /// </summary>
    private Dictionary<string, List<(ItemData item, int originalIndex)>> GroupItemsByCategoryWithIndex()
    {
        var grouped = new Dictionary<string, List<(ItemData item, int originalIndex)>>();
        
        for (int i = 0; i < allItemsToDisplay.Count; i++)
        {
            var item = allItemsToDisplay[i];
            string category = item.GetCategoryName();
            
            if (!grouped.ContainsKey(category))
            {
                grouped[category] = new List<(ItemData item, int originalIndex)>();
            }
            
            grouped[category].Add((item, i)); // Salva o índice original
        }
        
        return grouped;
    }

    /// <summary>
    /// Cria header de categoria - CORRIGIDO: NÃO BLOQUEIA RAYCASTS
    /// </summary>
    private GameObject CreateCategoryHeader(string categoryName, bool isExpanded, float yPosition)
    {
        if (categoryHeaderPrefab == null)
        {
            Debug.LogError("categoryHeaderPrefab não configurado!");
            return null;
        }
        
        GameObject headerObj = Instantiate(categoryHeaderPrefab, tableContentContainer);
        headerObj.name = $"CategoryHeader_{categoryName}";
        
        // Configurar RECT TRANSFORM
        var rectTransform = headerObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = new Vector2(0, yPosition);
            rectTransform.sizeDelta = new Vector2(0, 40f);
        }
        
        // LAYOUT ELEMENT (ignorar LayoutGroup)
        var layoutElement = headerObj.GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = headerObj.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        
        // Configurar BUTTON para não bloquear raycasts dos itens
        var button = headerObj.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
        }
        
        // Configurar IMAGE para não bloquear (mas ainda receber cliques no header)
        var headerImage = headerObj.GetComponent<Image>();
        if (headerImage != null)
        {
            headerImage.raycastTarget = true;
        }
        
        // Garantir que filhos não bloqueiem raycasts desnecessariamente
        var childImages = headerObj.GetComponentsInChildren<Image>();
        foreach (var img in childImages)
        {
            if (img.gameObject != headerObj) // Não mexer no background
            {
                img.raycastTarget = false; // Arrow não precisa receber raycasts
            }
        }
        
        var childTexts = headerObj.GetComponentsInChildren<TMP_Text>();
        foreach (var txt in childTexts)
        {
            txt.raycastTarget = false; // Texto não precisa receber raycasts
        }
        
        var headerUI = headerObj.GetComponent<CategoryHeaderUI>();
        if (headerUI == null) headerUI = headerObj.AddComponent<CategoryHeaderUI>();
        headerUI.Initialize(categoryName, isExpanded);
        headerUI.OnToggleCategory = OnCategoryToggled;
        
        // Posicionar no topo da hierarquia (renderizar atrás dos itens)
        headerObj.transform.SetAsFirstSibling();
        
        activeCategoryHeaders[categoryName] = headerObj;
        
        return headerObj;
    }

    /// <summary>
    /// Limpa headers antigos
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
    /// Callback quando categoria é expandida/colapsada
    /// </summary>
    private void OnCategoryToggled(string categoryName, bool isExpanded)
    {
        // Salvar estado
        categoryExpandedState[categoryName] = isExpanded;
        
        // Invalidar cache para forçar refresh COMPLETO
        InvalidateCache();
        
        // Forçar refresh COMPLETO
        RefreshTable(forceRefresh: true);
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
        // Otimização: Se vamos reutilizar (cache hit), não limpar!
        if (!forceRefresh && cachedItemCount == allItemsToDisplay.Count && activePooledRows.Count == allItemsToDisplay.Count)
        {
            return;
        }
        
        if (useObjectPooling && rowPool != null)
        {
            // Usar ReturnAllRows otimizado
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
    }
    
    public void InvalidateCache()
    {
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
        // Primeiro: Remover mensagem anterior se existir
        ClearInfoMessage();
        
        if (tableContentContainer == null) return;
        
        currentInfoMessageObject = new GameObject("InfoMessage");
        currentInfoMessageObject.transform.SetParent(tableContentContainer);
        
        TMP_Text textComp = currentInfoMessageObject.AddComponent<TextMeshProUGUI>();
        textComp.text = message;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.yellow;
        textComp.fontSize = 16;
        
        RectTransform rect = currentInfoMessageObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 60f);
    }

    private void ClearInfoMessage()
    {
        if (currentInfoMessageObject != null)
        {
            Destroy(currentInfoMessageObject);
            currentInfoMessageObject = null;
        }
    }
    
    // Método para forçar refresh completo
    public void ForceRefresh()
    {
        forceRefresh = true;
        cachedItemCount = -1;
        RefreshTable(true);
    }
    
    // Chamado quando inventário muda (adiciona/remove item)
    public void OnInventoryChanged()
    {
        // Marcar que precisa de refresh completo
        InvalidateCache();
        
        // Refresh imediato
        RefreshTable(true);
    }
    
    /// <summary>
    /// Cria uma DropZone para toda a área da tabela
    /// Para receber itens desequipados do PaperDoll
    /// </summary>
    private void CreateTableDropZone()
    {
        if (tableContentContainer == null)
        {
            Debug.LogError("tableContentContainer é NULL!");
            return;
        }
        
        // Verificar se já tem DropZone
        var existingDropZone = tableContentContainer.GetComponent<DropZone>();
        if (existingDropZone != null)
        {
            return;
        }
        
        // Adicionar IMAGE (para raycast)
        var image = tableContentContainer.GetComponent<Image>();
        if (image == null)
        {
            image = tableContentContainer.gameObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.01f); // Quase invisível
        }
        
        image.raycastTarget = true;
        
        // Adicionar DROPZONE
        var dropZone = tableContentContainer.gameObject.AddComponent<DropZone>();
        
        // Configurar via reflexão (já que os campos são privados)
        try
        {
            // Configurar dropType = InventoryTable
            var dropTypeField = typeof(DropZone).GetField("dropType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (dropTypeField != null)
            {
                dropTypeField.SetValue(dropZone, DropZone.DropType.InventoryTable);
            }
            
            // Configurar acceptedEquipmentSlot = None
            var acceptedSlotField = typeof(DropZone).GetField("acceptedEquipmentSlot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (acceptedSlotField != null)
            {
                acceptedSlotField.SetValue(dropZone, ItemData.EquipmentSlot.None);
            }
            
            // Configurar backgroundImage
            var bgImageField = typeof(DropZone).GetField("backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (bgImageField != null)
            {
                bgImageField.SetValue(dropZone, image);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Não conseguiu configurar DropZone via reflexão: {e.Message}");
        }
    }
    
    /// <summary>
    /// CORREÇÃO: Remove DraggableItem desabilitado e recria
    /// Resolve bug de itens ficarem cinza após equipar
    /// </summary>
    private void CleanupDisabledDraggable(GameObject rowObj)
    {
        if (rowObj == null) return;
        
        var draggable = rowObj.GetComponent<DraggableItem>();
        
        if (draggable != null && !draggable.enabled)
        {
            Destroy(draggable);
        }
    }
    
    /// <summary>
    /// Setup drag & drop com validação
    /// Valida: originalIndex → tableRowToInventorySlot → InventoryManager.slots
    /// </summary>
    private void SetupDragAndDrop(GameObject rowObj, ItemData item, int originalIndex)
    {
        if (rowObj == null || item == null) return;

        CleanupDisabledDraggable(rowObj);
        
        // Só equipamentos podem ser arrastados
        if (!item.IsEquipment())
        {
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null) Destroy(existingDraggable);
            return;
        }
        
        // Buscar slot via mapeamento
        if (!tableRowToInventorySlot.TryGetValue(originalIndex, out int inventorySlotIndex))
        {
            // Não criar DraggableItem se não tem mapeamento
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null) Destroy(existingDraggable);
            return;
        }
        
        // Validar slot no InventoryManager
        if (InventoryManager.Instance == null)
        {
            return;
        }
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        if (inventorySlotIndex < 0 || inventorySlotIndex >= allSlots.Count)
        {
            return;
        }
        
        var specificSlot = allSlots[inventorySlotIndex];
        
        // Validação crítica - Item no slot DEVE ser o da linha!
        if (specificSlot.item != item)
        {
            // Não criar DraggableItem se item não bate!
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null) Destroy(existingDraggable);
            return;
        }
        
        // Verificar se item está equipado
        if (specificSlot.isEquipped)
        {
            // Não criar DraggableItem para itens equipados
            var existingDraggable = rowObj.GetComponent<DraggableItem>();
            if (existingDraggable != null) Destroy(existingDraggable);
            return;
        }
        
        // Criar/Atualizar DraggableItem
        var draggable = rowObj.GetComponent<DraggableItem>();
        if (draggable == null)
        {
            draggable = rowObj.AddComponent<DraggableItem>();
        }
        
        // Configurar com informações validadas
        draggable.SetupDraggable(
            item,
            DraggableItem.DragSource.InventoryTable,
            ItemData.EquipmentSlot.None,
            inventorySlotIndex,   // Índice validado
            specificSlot          // Slot validado
        );

        EnsureRowFullyInteractable(rowObj);
    }
    
    /// <summary>
    /// Limpeza completa de componentes da linha (incluindo DraggableItem)
    /// </summary>
    private void CleanupRowComponents(GameObject rowObject)
    {
        if (rowObject == null) return;
        
        // Destruir DraggableItem completamente
        var draggable = rowObject.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            draggable.enabled = false;
            Destroy(draggable);
        }
        
        // Limpar Button listeners
        var button = rowObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
        
        // Resetar visual
        var image = rowObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = normalRowColor;
        }
        
        // Resetar CanvasGroup completamente
        var canvasGroup = rowObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;              // Alpha completo
            canvasGroup.blocksRaycasts = true;   // Aceita raycasts
            canvasGroup.interactable = true;     // Interagível
        }
    }

    /// <summary>
    /// Garante que CanvasGroup está com configuração correta
    /// Chamado APÓS criar/configurar DraggableItem
    /// </summary>
    private void EnsureRowFullyInteractable(GameObject rowObject)
    {
        if (rowObject == null) return;
        
        var canvasGroup = rowObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rowObject.AddComponent<CanvasGroup>();
        }
        
        // Força valores corretos
        if (canvasGroup.alpha < 0.99f)
        {
            canvasGroup.alpha = 1f;
        }
        
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public int GetMappedInventorySlot(int logicalItemIndex, ItemData item)
    {
        // ESTRATÉGIA 1: Mapeamento direto (usando índice lógico)
        if (tableRowToInventorySlot.TryGetValue(logicalItemIndex, out int slotIndex))
        {
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
                }
            }
        }
        
        // ESTRATÉGIA 2: Buscar primeiro não-equipado
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
                    return i;
                }
            }
        }
        
        return -1;
    }
    
    /// <summary>
    /// Mapeia linha da tabela para slot específico do inventário
    /// </summary>
    private int FindSpecificInventorySlotForTableRow(int tableRowIndex, ItemData targetItem)
    {
        if (InventoryManager.Instance == null || targetItem == null)
            return -1;
        
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        // ESTRATÉGIA 1: Se temos exatamente o mesmo número de slots que linhas
        if (tableRowIndex < allSlots.Count && 
            allSlots[tableRowIndex].item == targetItem && 
            !allSlots[tableRowIndex].isEquipped)
        {
            return tableRowIndex;
        }
        
        // ESTRATÉGIA 2: Percorrer todos os slots para encontrar
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
        
        // Se encontrou slots compatíveis
        if (matchingSlots.Count > 0)
        {
            // Para múltiplos itens iguais, tentar usar o primeiro não-rastreado
            if (matchingSlots.Count > 1)
            {
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

    // Métodos de Debug (mantidos apenas para desenvolvimento)
    [ContextMenu("Debug: Test Selection")]
    public void DebugTestSelection()
    {
        if (allItemsToDisplay.Count > 0)
        {
            int randomIndex = Random.Range(0, allItemsToDisplay.Count);
            ItemData randomItem = allItemsToDisplay[randomIndex];
            
            // Simulate click on first row
            if (activePooledRows.Count > randomIndex)
            {
                OnRowClicked(activePooledRows[randomIndex].rowObject, randomItem, randomIndex);
            }
        }
    }
}