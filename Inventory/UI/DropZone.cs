using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// DropZone CORRIGIDA - Detecção de drop melhorada
/// ✅ OnPointerEnter detecta corretamente
/// ✅ OnDrop funciona com ghost
/// ✅ Visual feedback melhorado
/// </summary>
public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum DropType
    {
        PaperDollSlot,
        InventoryTable,
        TrashBin
    }
    
    [Header("Drop Zone Configuration")]
    [SerializeField] private DropType dropType = DropType.PaperDollSlot;
    [SerializeField] private ItemData.EquipmentSlot acceptedEquipmentSlot = ItemData.EquipmentSlot.None;
    
    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color hoverValidColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
    [SerializeField] private Color hoverInvalidColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
    
    [Header("🆕 Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    
    private bool isDraggingOver = false;
    private DraggableItem currentDragItem = null;
    private Color originalColor;
    
    // 🆕 Raycast target reference
    private Graphic raycastTarget;
    
    private void Awake()
    {
        // 🔥 BUSCAR OU CRIAR IMAGE PARA RAYCAST
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        // 🔥 SE NÃO TEM IMAGE, CRIAR UM INVISÍVEL
        if (backgroundImage == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Criando Image invisível para raycast...");
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.01f); // Quase invisível mas aceita raycast
        }
        
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
            raycastTarget = backgroundImage;
            
            // 🔥 GARANTIR RAYCAST TARGET
            raycastTarget.raycastTarget = true;
            Debug.Log($"✅ {gameObject.name}: RaycastTarget ATIVO");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: FALHA ao criar raycast target!");
        }
        
        Debug.Log($"🎯 DropZone configurado: {gameObject.name}");
        Debug.Log($"   Tipo: {dropType}");
        Debug.Log($"   Aceita slot: {acceptedEquipmentSlot}");
    }
    
    private void OnValidate()
    {
        // Auto-setup no editor
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        if (backgroundImage != null)
            backgroundImage.raycastTarget = true;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 🔥 VERIFICAÇÃO MELHORADA
        if (eventData.pointerDrag == null) return;
        
        currentDragItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (currentDragItem == null) return;
        
        isDraggingOver = true;
        
        bool canAccept = CanAcceptItem(currentDragItem);
        
        Debug.Log($"📍 OnPointerEnter: {gameObject.name}");
        Debug.Log($"   Item: {currentDragItem.GetItemData()?.itemName}");
        Debug.Log($"   Pode aceitar: {canAccept}");
        
        // Visual feedback
        if (backgroundImage != null)
        {
            backgroundImage.color = canAccept ? hoverValidColor : hoverInvalidColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isDraggingOver = false;
        currentDragItem = null;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = originalColor;
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log($"║  📦 DROP EVENT em: {gameObject.name}");
        Debug.Log($"║  🎯 Tipo: {dropType}");
        
        // 🔥 VERIFICAÇÃO ROBUSTA
        if (eventData.pointerDrag == null)
        {
            Debug.LogError("║  ❌ eventData.pointerDrag is NULL!");
            Debug.Log("╚═══════════════════════════════════════════╝");
            return;
        }
        
        var draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            Debug.LogError("║  ❌ Sem componente DraggableItem!");
            Debug.Log("╚═══════════════════════════════════════════╝");
            return;
        }
        
        ItemData item = draggableItem.GetItemData();
        if (item == null)
        {
            Debug.LogError("║  ❌ Item data is NULL!");
            Debug.Log("╚═══════════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📦 Item: {item.itemName}");
        Debug.Log($"║  📍 Source: {draggableItem.GetSource()}");
        
        // Verificar se pode aceitar
        if (!CanAcceptItem(draggableItem))
        {
            Debug.LogWarning("║  ⚠️ Este drop zone NÃO aceita este item!");
            Debug.Log("╚═══════════════════════════════════════════╝");
            
            if (backgroundImage != null)
                backgroundImage.color = originalColor;
            
            return;
        }
        
        // 🔥 EXECUTAR AÇÃO
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
        
        if (success)
        {
            Debug.Log("║  ✅ Drop SUCESSO!");
            draggableItem.MarkDropSuccess();
        }
        else
        {
            Debug.LogError("║  ❌ Drop FALHOU!");
        }
        
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
        
        isDraggingOver = false;
        currentDragItem = null;
        
        Debug.Log("╚═══════════════════════════════════════════╝");
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
                // Só aceita itens da tabela de inventário
                if (source != DraggableItem.DragSource.InventoryTable)
                    return false;
                
                // Deve ser equipamento
                if (!item.IsEquipment())
                    return false;
                
                // Verifica compatibilidade de slot
                return IsCompatibleEquipmentSlot(item.equipmentSlot, acceptedEquipmentSlot);
                
            case DropType.InventoryTable:
                // Aceita unequip do paper doll
                return source == DraggableItem.DragSource.PaperDollSlot;
                
            case DropType.TrashBin:
                // Aceita itens droppable
                return item.isDroppable;
                
            default:
                return false;
        }
    }
    
    private bool HandleEquipDrop(DraggableItem draggableItem)
    {
        try
        {
            ItemData item = draggableItem.GetItemData();
            
            Debug.Log($"╔═══════════════════════════════════════════╗");
            Debug.Log($"║  🎯 DROP ZONE: Tentando equipar");
            Debug.Log($"║  📦 Item: {item?.itemName ?? "NULL"}");
            Debug.Log($"║  📍 No slot: {acceptedEquipmentSlot}");
            Debug.Log($"╚═══════════════════════════════════════════╝");
            
            if (item == null)
            {
                Debug.LogError("❌ Item data is NULL!");
                return false;
            }
            
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("❌ InventoryManager não encontrado!");
                return false;
            }
            
            // Verifica se item está no inventário
            int itemCount = InventoryManager.Instance.GetItemCount(item);
            Debug.Log($"🔍 Item no inventário: {itemCount}x");
            
            if (itemCount <= 0)
            {
                Debug.LogError($"❌ {item.itemName} não está no inventário!");
                return false;
            }
            
            // Verifica compatibilidade de slot
            if (!IsCompatibleEquipmentSlot(item.equipmentSlot, acceptedEquipmentSlot))
            {
                Debug.LogError($"❌ Slot incompatível: {item.equipmentSlot} → {acceptedEquipmentSlot}");
                return false;
            }
            
            // 🔥 USAR InventoryManager diretamente
            Debug.Log("🎯 Usando InventoryManager para equipar...");
            bool success = InventoryManager.Instance.EquipItem(item);
            
            if (success)
            {
                Debug.Log($"✅ {item.itemName} equipado com sucesso no slot {acceptedEquipmentSlot}!");
                
                // 🔥🔥🔥 CORREÇÃO: Esconder o item original da tabela
                if (draggableItem != null)
                {
                    GameObject draggableGameObject = draggableItem.gameObject;
                    if (draggableGameObject != null && draggableGameObject.activeSelf)
                    {
                        draggableGameObject.SetActive(false);
                        Debug.Log("🎭 Item original da tabela desativado");
                    }
                }
                
                // 🔥🔥🔥 CORREÇÃO: Buscar componentes dinamicamente
                StartCoroutine(DelayedUIRefresh(item, draggableItem));
                
                return true;
            }
            else
            {
                Debug.LogError($"❌ Falha ao equipar {item.itemName}!");
                
                // Se falhou, reativa o item original
                if (draggableItem != null && draggableItem.gameObject != null)
                {
                    draggableItem.gameObject.SetActive(true);
                    Debug.Log("🔄 Item original reativado (equip falhou)");
                }
                
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ EXCEÇÃO em HandleEquipDrop: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            
            return false;
        }
    }

    // 🔥 MÉTODO AUXILIAR SIMPLIFICADO
    private System.Collections.IEnumerator DelayedUIRefresh(ItemData item, DraggableItem draggableItem)
    {
        // Aguarda 2 frames
        yield return null;
        yield return null;
        
        try
        {
            // 1. Atualizar tabela
            InventoryTableUI tableUI = FindFirstObjectByType<InventoryTableUI>();
            if (tableUI != null)
            {
                tableUI.ForceRefresh();
                Debug.Log("✅ Tabela atualizada");
            }
            
            // 2. Atualizar paper doll
            InventoryPaperDollUI paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
            if (paperDollUI != null)
            {
                paperDollUI.UpdateAllSlots();
                Debug.Log("✅ PaperDoll atualizado");
            }
            
            // 3. Atualizar InventoryUI se disponível
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.OnItemSelected(item);
                Debug.Log($"🎯 {item.itemName} selecionado");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao atualizar UI: {e.Message}");
        }
    }
    
    private bool HandleUnequipDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        ItemData.EquipmentSlot sourceSlot = draggableItem.GetSourceSlot();
        
        Debug.Log($"   🔧 Tentando DESEQUIPAR: {item.itemName}");
        Debug.Log($"   📍 Do slot: {sourceSlot}");
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("   ❌ InventoryManager não encontrado!");
            return false;
        }
        
        ItemData unequipped = InventoryManager.Instance.UnequipItem(sourceSlot);
        
        if (unequipped != null)
        {
            Debug.Log($"   ✅ {unequipped.itemName} desequipado com sucesso!");
            return true;
        }
        else
        {
            Debug.LogError($"   ❌ Falha ao desequipar do slot {sourceSlot}");
            return false;
        }
    }
    
    private bool HandleTrashDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        
        Debug.Log($"   🗑️ Deletando: {item.itemName}");
        
        if (InventoryManager.Instance != null)
        {
            bool removed = InventoryManager.Instance.RemoveItem(item, 1);
            
            if (removed)
            {
                Debug.Log($"   ✅ {item.itemName} deletado!");
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsCompatibleEquipmentSlot(ItemData.EquipmentSlot itemSlot, ItemData.EquipmentSlot targetSlot)
    {
        if (itemSlot == targetSlot) return true;
        
        // Mapeamento de compatibilidade
        switch (targetSlot)
        {
            case ItemData.EquipmentSlot.MainHand:
                return itemSlot == ItemData.EquipmentSlot.Weapon;
                
            case ItemData.EquipmentSlot.Weapon:
                return itemSlot == ItemData.EquipmentSlot.MainHand;
                
            default:
                return false;
        }
    }
    
    // Getters
    public DropType GetDropType() => dropType;
    public ItemData.EquipmentSlot GetAcceptedEquipmentSlot() => acceptedEquipmentSlot;
    
    public void SetAcceptedSlot(ItemData.EquipmentSlot slot)
    {
        acceptedEquipmentSlot = slot;
    }
    
    // 🆕 DEBUG: Visualizar drop zone na Scene view
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;
        
        // Cor baseada no tipo
        Color gizmoColor = Color.white;
        switch (dropType)
        {
            case DropType.PaperDollSlot:
                gizmoColor = Color.cyan;
                break;
            case DropType.InventoryTable:
                gizmoColor = Color.green;
                break;
            case DropType.TrashBin:
                gizmoColor = Color.red;
                break;
        }
        
        if (isDraggingOver)
            gizmoColor = Color.yellow;
        
        Gizmos.color = gizmoColor;
        
        Vector3 worldPos = rect.position;
        Vector3 size = new Vector3(rect.rect.width, rect.rect.height, 0f);
        
        Gizmos.DrawWireCube(worldPos, size);
    }
}