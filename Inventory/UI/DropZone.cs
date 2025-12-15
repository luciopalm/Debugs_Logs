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
    
    // 🔥 SUBSTITUIR O MÉTODO OnDrop() NO DropZone.cs

    public void OnDrop(PointerEventData eventData)
    {
        // 🚀 OTIMIZAÇÃO: Verificações rápidas primeiro
        if (eventData.pointerDrag == null) return;
        
        var draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggableItem == null) return;
        
        ItemData item = draggableItem.GetItemData();
        if (item == null) return;
        
        // 🚀 OTIMIZAÇÃO: Verifica se pode aceitar ANTES de logar
        if (!CanAcceptItem(draggableItem))
        {
            // Silenciosamente rejeita (sem logs pesados)
            if (backgroundImage != null)
                backgroundImage.color = originalColor;
            return;
        }
        
        // Só loga se for aceitar o drop (reduz spam de logs)
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  📦 DROP EVENT em: {gameObject.name}");
        Debug.Log($"║  🎯 Tipo: {dropType}");
        Debug.Log($"║  📦 Item: {item.itemName}");
        Debug.Log($"║  📍 Source: {draggableItem.GetSource()}");
        
        // 🔥 EXECUTAR AÇÃO (já sabemos que pode aceitar)
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
            Debug.Log("║  ❌ Drop FALHOU!");
        }
        
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
        
        isDraggingOver = false;
        currentDragItem = null;
        
        Debug.Log("╚═══════════════════════════════════════╝");
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
        ItemData item = draggableItem.GetItemData();
        
        Debug.Log($"   🎯 Tentando EQUIPAR via Drag & Drop: {item.itemName}");
        
        // 🔥 OBTER INFORMAÇÃO DO SLOT ESPECÍFICO DO DRAGGABLE
        var specificSlot = draggableItem.GetSourceInventorySlot();
        int slotIndex = draggableItem.GetSourceInventorySlotIndex();
        
        Debug.Log($"   📊 Slot info do Draggable: Index={slotIndex}, Slot válido?={specificSlot != null}");
        
        // 🔥 ESTRATÉGIA 1: TEMOS SLOT ESPECÍFICO
        if (slotIndex >= 0 && specificSlot != null && specificSlot.item == item)
        {
            Debug.Log($"   🎯 Removendo do slot específico {slotIndex}");
            
            if (InventoryManager.Instance != null)
            {
                // 🔥 🔥 🔥 CORREÇÃO: VERIFICAR SE O MÉTODO EXISTE
                bool removed = false;
                
                // Verificar se o método RemoveItemFromSlot existe
                System.Reflection.MethodInfo methodInfo = typeof(InventoryManager).GetMethod("RemoveItemFromSlot");
                
                if (methodInfo != null)
                {
                    // Método existe - usar reflection para chamar
                    Debug.Log($"   ✅ Método RemoveItemFromSlot encontrado, usando...");
                    removed = (bool)methodInfo.Invoke(InventoryManager.Instance, new object[] { slotIndex, 1 });
                }
                else
                {
                    // 🔥 FALLBACK: Usar método existente RemoveItem
                    Debug.LogWarning($"   ⚠️ RemoveItemFromSlot não existe, usando RemoveItem como fallback");
                    
                    // Obter todos os slots para verificar
                    var allSlots = InventoryManager.Instance.GetAllSlots();
                    if (slotIndex < allSlots.Count && allSlots[slotIndex].item == item)
                    {
                        // Remove 1 unidade deste item (de qualquer slot)
                        removed = InventoryManager.Instance.RemoveItem(item, 1);
                    }
                }
                
                if (removed)
                {
                    Debug.Log($"   ✅ Item removido do slot {slotIndex}");
                    
                    // Equipar via PaperDollUI
                    var paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
                    if (paperDollUI != null)
                    {
                        return paperDollUI.TryEquipItem(item);
                    }
                }
            }
        }
        
        // 🔥 ESTRATÉGIA 2: FALLBACK
        Debug.Log($"   🔄 Usando fallback (remover qualquer)");
        
        var paperDollUIFallback = FindFirstObjectByType<InventoryPaperDollUI>();
        if (paperDollUIFallback != null && InventoryManager.Instance != null)
        {
            // Remove qualquer unidade
            bool removed = InventoryManager.Instance.RemoveItem(item, 1);
            if (removed)
            {
                return paperDollUIFallback.TryEquipItem(item);
            }
        }
        
        return false;
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