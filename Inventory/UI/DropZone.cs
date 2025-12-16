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
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 DropZone.Awake(): {gameObject.name}");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // 🔥 BUSCAR OU CRIAR IMAGE PARA RAYCAST
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        // 🔥🔥🔥 CORREÇÃO CRÍTICA: SE NÃO TEM IMAGE, CRIAR UM
        if (backgroundImage == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Criando Image para raycast...");
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.01f); // Quase invisível mas aceita raycast
        }
        
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
            raycastTarget = backgroundImage;
            
            // 🔥🔥🔥 GARANTIR RAYCAST TARGET ATIVO
            raycastTarget.raycastTarget = true;
            
            // 🔥🔥🔥 REMOVIDO: NÃO ALTERAR SIBLING INDEX!
            // transform.SetAsLastSibling(); ← ISTO CAUSAVA O BUG DE REORDENAÇÃO!
            
            Debug.Log($"✅ {gameObject.name}: RaycastTarget ATIVO");
            Debug.Log($"   Sibling Index (não modificado): {transform.GetSiblingIndex()}");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: FALHA ao criar raycast target!");
        }
        
        // 🔥🔥🔥 ADICIONAR CANVAS GROUP PARA CONTROLE DE RAYCASTS
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Garantir que CanvasGroup não bloqueia raycasts
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f;
        
        Debug.Log($"🎯 DropZone configurado: {gameObject.name}");
        Debug.Log($"   Tipo: {dropType}");
        Debug.Log($"   Aceita slot: {acceptedEquipmentSlot}");
        Debug.Log($"   CanvasGroup blocksRaycasts: {canvasGroup.blocksRaycasts}");
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
        Debug.Log($"🎯 OnPointerEnter CHAMADO em: {gameObject.name}");
        Debug.Log($"   Event Position: {eventData.position}");
        Debug.Log($"   Pointer Drag: {(eventData.pointerDrag != null ? eventData.pointerDrag.name : "NULL")}");
        
        if (eventData.pointerDrag == null)
        {
            Debug.LogWarning($"   ⚠️ pointerDrag é NULL - saindo");
            return;
        }
        
        currentDragItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (currentDragItem == null)
        {
            Debug.LogWarning($"   ⚠️ Nenhum DraggableItem encontrado em {eventData.pointerDrag.name}");
            return;
        }
        
        Debug.Log($"   ✅ DraggableItem encontrado: {currentDragItem.GetItemData()?.itemName}");
        
        isDraggingOver = true;
        
        bool canAccept = CanAcceptItem(currentDragItem);
        
        Debug.Log($"   Can Accept: {canAccept}");
        Debug.Log($"   Drop Type: {dropType}");
        Debug.Log($"   Accepted Slot: {acceptedEquipmentSlot}");
        
        if (backgroundImage != null)
        {
            if (useAdvancedControl)
            {
                backgroundImage.color = canAccept ? GetHoverValidColor() : GetHoverInvalidColor();
            }
            else
            {
                backgroundImage.color = canAccept ? hoverValidColor : hoverInvalidColor;
            }
            
            Debug.Log($"   🎨 Background color alterado para: {(canAccept ? "VALID (verde)" : "INVALID (vermelho)")}");
        }
        else
        {
            Debug.LogWarning($"   ⚠️ backgroundImage é NULL!");
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isDraggingOver = false;
        currentDragItem = null;
        
        if (backgroundImage != null)
        {
            if (useAdvancedControl)
            {
                backgroundImage.color = GetNormalColor();
            }
            else
            {
                backgroundImage.color = originalColor;
            }
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
        
        // 🔥🔥🔥 CORREÇÃO: BUSCAR SLOT DINAMICAMENTE
        if (InventoryManager.Instance == null)
        {
            Debug.LogError($"   ❌ InventoryManager não encontrado!");
            return false;
        }
        
        // 🔥 PASSO 1: ENCONTRAR O SLOT CORRETO NO INVENTÁRIO
        var allSlots = InventoryManager.Instance.GetAllSlots();
        
        InventoryManager.InventorySlot validSlot = null;
        int validSlotIndex = -1;
        
        // Buscar primeiro slot NÃO-EQUIPADO com este item
        for (int i = 0; i < allSlots.Count; i++)
        {
            var slot = allSlots[i];
            
            if (!slot.IsEmpty && 
                slot.item == item && 
                !slot.isEquipped && 
                slot.quantity > 0)
            {
                validSlot = slot;
                validSlotIndex = i;
                Debug.Log($"   ✅ Slot encontrado: {i} ({item.itemName} x{slot.quantity})");
                break;
            }
        }
        
        // 🔥 VALIDAR SE ENCONTROU
        if (validSlot == null || validSlotIndex < 0)
        {
            Debug.LogError($"   ❌ Nenhum slot não-equipado encontrado para {item.itemName}!");
            
            // Verificar se já está equipado
            var paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
            if (paperDollUI != null)
            {
                var currentChar = paperDollUI.GetCurrentCharacter();
                if (currentChar != null && currentChar.currentEquipment != null)
                {
                    var alreadyEquipped = currentChar.currentEquipment.GetItemInSlot(item.equipmentSlot);
                    if (alreadyEquipped == item)
                    {
                        Debug.LogWarning($"   ⚠️ {item.itemName} já está equipado!");
                    }
                }
            }
            
            return false;
        }
        
        Debug.Log($"   📍 Usando Slot: {validSlotIndex}");
        
        // 🔥 PASSO 2: EQUIPAR VIA PAPER DOLL
        var paperDollUI2 = FindFirstObjectByType<InventoryPaperDollUI>();
        if (paperDollUI2 == null)
        {
            Debug.LogError("   ❌ PaperDollUI não encontrado!");
            return false;
        }
        
        var activeChar = paperDollUI2.GetCurrentCharacter();
        if (activeChar == null || activeChar.currentEquipment == null)
        {
            Debug.LogError("   ❌ Character inválido!");
            return false;
        }
        
        // 🔥 PASSO 3: MARCAR COMO EQUIPADO (LOCK)
        validSlot.isEquipped = true;
        
        // 🔥 PASSO 4: DESEQUIPAR ITEM ATUAL (se houver)
        ItemData.EquipmentSlot targetSlot = item.equipmentSlot;
        var currentlyEquipped = activeChar.currentEquipment.GetItemInSlot(targetSlot);
        
        if (currentlyEquipped != null)
        {
            Debug.Log($"   🔄 Desequipando {currentlyEquipped.itemName}...");
            var unequipped = activeChar.currentEquipment.UnequipItem(targetSlot);
            if (unequipped != null)
            {
                if (!InventoryManager.Instance.AddItem(unequipped, 1))
                {
                    // ROLLBACK
                    validSlot.isEquipped = false;
                    activeChar.currentEquipment.EquipItem(unequipped);
                    Debug.LogError($"   ❌ Falha ao devolver {unequipped.itemName} ao inventário!");
                    return false;
                }
                Debug.Log($"   ✅ {unequipped.itemName} devolvido ao inventário");
            }
        }
        
        // 🔥 PASSO 5: EQUIPAR NO CHARACTER
        activeChar.currentEquipment.EquipItem(item);
        
        // 🔥 PASSO 6: VERIFICAR SE REALMENTE FOI EQUIPADO
        var verify = activeChar.currentEquipment.GetItemInSlot(targetSlot);
        if (verify != item)
        {
            // ROLLBACK
            validSlot.isEquipped = false;
            Debug.LogError($"   ❌ Verificação falhou após equipar!");
            return false;
        }
        
        // 🔥 PASSO 7: REMOVER DO INVENTÁRIO (UMA VEZ SÓ!)
        bool removed = InventoryManager.Instance.RemoveItemFromSlot(validSlotIndex, 1);
        if (!removed)
        {
            // ROLLBACK COMPLETO
            validSlot.isEquipped = false;
            activeChar.currentEquipment.UnequipItem(targetSlot);
            Debug.LogError($"   ❌ Falha ao remover do inventário!");
            return false;
        }
        
        Debug.Log($"   ✅ {item.itemName} equipado com sucesso via drag!");
        
        // 🔥 PASSO 8: ATUALIZAR UI
        paperDollUI2.UpdateAllSlots();
        
        var tableUI = FindFirstObjectByType<InventoryTableUI>();
        if (tableUI != null)
        {
            tableUI.UpdateExistingRowsData();
        }
        
        var detailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (detailsUI != null)
        {
            detailsUI.UpdatePartyMemberStats();
        }
        
        return true;
    }
    
    
    private bool HandleUnequipDrop(DraggableItem draggableItem)
    {
        ItemData item = draggableItem.GetItemData();
        ItemData.EquipmentSlot sourceSlot = draggableItem.GetSourceSlot();
        
        Debug.Log($"╔══════════════════════════════════════╗");
        Debug.Log($"║  🔧 DESEQUIPAR VIA DRAG & DROP       ║");
        Debug.Log($"╠══════════════════════════════════════╣");
        Debug.Log($"║  📦 Item: {item.itemName}");
        Debug.Log($"║  📍 Slot: {sourceSlot}");
        
        // 🔥 1. ENCONTRAR PAPER DOLL UI
        var paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
        if (paperDollUI == null)
        {
            Debug.LogError("║  ❌ InventoryPaperDollUI não encontrado!");
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        // 🔥 2. PEGAR CHARACTER ATUAL
        CharacterData currentCharacter = paperDollUI.GetCurrentCharacter();
        if (currentCharacter == null)
        {
            Debug.LogError("║  ❌ Nenhum character ativo!");
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  👤 Character: {currentCharacter.characterName}");
        
        // 🔥 3. VERIFICAR SE ITEM ESTÁ EQUIPADO NO CHARACTER
        if (currentCharacter.currentEquipment == null)
        {
            Debug.LogWarning($"║  ⚠️ Character sem equipment - criando novo");
            currentCharacter.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        ItemData equippedInCharacter = currentCharacter.currentEquipment.GetItemInSlot(sourceSlot);
        
        if (equippedInCharacter != item)
        {
            Debug.LogError($"║  ❌ Item não está equipado no character!");
            Debug.Log($"║     Character tem: {equippedInCharacter?.itemName ?? "NULL"}");
            Debug.Log($"║     Item arrastado: {item.itemName}");
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        // 🔥 4. DESEQUIPAR DO CHARACTER
        Debug.Log($"║  🔄 Desequipando do character...");
        ItemData unequipped = currentCharacter.currentEquipment.UnequipItem(sourceSlot);
        
        if (unequipped == null)
        {
            Debug.LogError($"║  ❌ Falha ao desequipar do character!");
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ Desequipado do character: {unequipped.itemName}");
        
        // 🔥 5. VERIFICAR SE TEM ESPAÇO NO INVENTÁRIO COMPARTILHADO
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("║  ❌ InventoryManager não encontrado!");
            // Re-equipar para não perder o item
            currentCharacter.currentEquipment.EquipItem(unequipped);
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        // Verificar peso
        if (!InventoryManager.Instance.CanCarryWeight(unequipped.weight))
        {
            Debug.LogError($"║  ❌ Peso máximo excedido!");
            Debug.Log($"║     Peso do item: {unequipped.weight}");
            Debug.Log($"║     Peso atual: {InventoryManager.Instance.CurrentWeight}");
            Debug.Log($"║     Peso máximo: {InventoryManager.Instance.MaxWeight}");
            
            // Re-equipar para não perder o item
            currentCharacter.currentEquipment.EquipItem(unequipped);
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ Verificação de peso: OK");
        
        // 🔥 6. PRIMEIRO: DESMARCAR ITEM COMO EQUIPADO NO INVENTÁRIO
        Debug.Log($"║  🔧 Desmarcando {unequipped.itemName} como equipado no inventário...");
        bool markedAsUnequipped = InventoryManager.Instance.MarkItemAsUnequipped(unequipped);
        
        if (!markedAsUnequipped)
        {
            Debug.LogWarning($"║  ⚠️ Não conseguiu desmarcar como equipado - continuando mesmo assim");
        }
        else
        {
            Debug.Log($"║  ✅ Item desmarcado como equipado no inventário");
        }
        
        // 🔥 7. AGORA ADICIONAR AO INVENTÁRIO COMPARTILHADO
        Debug.Log($"║  📥 Adicionando ao inventário compartilhado...");
        
        // ⭐⭐ ADICIONE ESTE LOG DE DEBUG ANTES DO AddItem
        Debug.Log($"║  🧪 DEBUG ANTES DO AddItem:");
        Debug.Log($"║     Item: {unequipped.itemName}");
        Debug.Log($"║     Peso: {unequipped.weight}");
        Debug.Log($"║     Stack limit: {unequipped.stackLimit}");
        Debug.Log($"║     Slots vazios: {InventoryManager.Instance.GetEmptySlotCount()}");
        
        bool added = InventoryManager.Instance.AddItem(unequipped, 1);
        
        if (!added)
        {
            Debug.LogError($"║  ❌ Não conseguiu adicionar ao inventário!");
            
            // 🔥🔥🔥 CORREÇÃO CRÍTICA: MESMO SE FALHAR, ATUALIZA O PAPER DOLL!
            Debug.Log($"║  🔄 AddItem FALHOU - fazendo rollback e atualizando UI...");
            
            // Re-equipar para não perder o item
            currentCharacter.currentEquipment.EquipItem(unequipped);
            
            // 🔥 ATUALIZAR PAPER DOLL DE QUALQUER JEITO (EVITA SLOT BLOQUEADO)
            Debug.Log($"║  🎨 Atualizando Paper Doll (mesmo com falha)...");
            if (paperDollUI != null)
            {
                paperDollUI.UpdateAllSlots();
            }
            
            // 🔥 GARANTIR QUE O SLOT ESTÁ ATIVO COMO DROPZONE
            Debug.Log($"║  🔧 Verificando DropZone do slot...");
            if (paperDollUI != null)
            {
                paperDollUI.FixDropZones();
            }
            
            Debug.Log("╚══════════════════════════════════════╝");
            return false;
        }
        
        Debug.Log($"║  ✅ Adicionado ao inventário compartilhado");
        Debug.Log($"║     Quantidade agora: {InventoryManager.Instance.GetItemCount(unequipped)}");
        
        // 🔥 8. SINCRONIZAR COM INVENTORYMANAGER (para manter consistência)
        Debug.Log($"║  🔄 Sincronizando com InventoryManager...");
        InventoryManager.Instance.SyncFromActiveCharacter();
        
        // 🔥 9. ATUALIZAR UI
        Debug.Log($"║  🎨 Atualizando UI...");

        // 🔥🔥🔥 ADICIONE ESTAS LINHAS AQUI - GARANTIR ATUALIZAÇÃO IMEDIATA
        Debug.Log($"║  🔧 Atualização IMEDIATA do PaperDoll...");
        if (paperDollUI != null)
        {
            // 1. Atualizar slots
            paperDollUI.UpdateAllSlots();
            
            // 2. 🔥 CORREÇÃO CRÍTICA: Limpar seleção para evitar bugs
            paperDollUI.ClearAllSelections();
            
            // 3. 🔥 VERIFICAR ESTADO DOS SLOTS
            paperDollUI.DebugCheckSlotsActiveState();
        }
        
        // Aguardar 1 frame antes de atualizar UI completa
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.StartCoroutine(UpdateUIAfterUnequip(paperDollUI));
        }
        else
        {
            // Fallback: atualizar manualmente
            Debug.Log($"║  ⚠️ InventoryUI.Instance é null - fallback manual");
            if (paperDollUI != null)
            {
                paperDollUI.UpdateAllSlots();
            }
            
            var tableUI = FindFirstObjectByType<InventoryTableUI>();
            if (tableUI != null) 
            {
                tableUI.RefreshTable(false);
                tableUI.ClearSelection();
            }
            
            var detailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
            if (detailsUI != null)
            {
                detailsUI.UpdatePartyMemberStats();
                detailsUI.ClearItemDetails();
            }
        }
        
        Debug.Log($"║  🎉 DESEQUIPAMENTO CONCLUÍDO!");
        Debug.Log("╚══════════════════════════════════════╝");
        
        return true;
    }


      
    /// Ordem correta de atualização
    /// 1. Reset selections PRIMEIRO
    /// 2. Fix DropZones ANTES de UpdateAllSlots
    /// 3. UpdateAllSlots por último
    /// </summary>
    private System.Collections.IEnumerator UpdateUIAfterUnequip(InventoryPaperDollUI paperDollUI)
    {
        // Aguarda 1 frame para garantir que todas as operações finalizaram
        yield return null;
        
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🔄 UpdateUIAfterUnequip - FIXED     ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        // 🔥🔥🔥 PASSO 1: RESETAR SELEÇÃO DOS SLOTS PRIMEIRO (antes de qualquer update)
        if (paperDollUI != null)
        {
            Debug.Log($"║  🧹 PASSO 1: Resetando seleções...");
            paperDollUI.ResetAllSlotsSelection();
            Debug.Log($"║  ✅ Seleções resetadas");
        }
        
        // 🔥🔥🔥 AGUARDAR 1 FRAME EXTRA
        yield return null;
        
        // 🔥🔥🔥 PASSO 2: FIXAR DROPZONES **ANTES** DE ATUALIZAR VISUAL
        if (paperDollUI != null)
        {
            Debug.Log($"║  🔧 PASSO 2: Fixando DropZones...");
            paperDollUI.FixDropZones();
            Debug.Log($"║  ✅ DropZones fixadas");
            
            // Verificação extra
            Debug.Log($"║  🔍 PASSO 2.5: Verificando estado...");
            paperDollUI.DebugCheckSlotsActiveState();
        }
        
        // 🔥🔥🔥 AGUARDAR 1 FRAME EXTRA
        yield return null;
        
        // 🔥🔥🔥 PASSO 3: AGORA SIM ATUALIZAR VISUAL DO PAPER DOLL
        if (paperDollUI != null)
        {
            Debug.Log($"║  🎨 PASSO 3: Atualizando visual do Paper Doll...");
            paperDollUI.UpdateAllSlots();
            Debug.Log($"║  ✅ Paper Doll atualizado");
        }
        else
        {
            Debug.LogError($"║  ❌ paperDollUI é null!");
        }
        
        // 🔥🔥🔥 AGUARDAR 1 FRAME EXTRA
        yield return null;
        
        // 🔥🔥🔥 PASSO 4: VERIFICAÇÃO FINAL PÓS-UPDATE
        if (paperDollUI != null)
        {
            Debug.Log($"║  🔍 PASSO 4: Verificação final...");
            paperDollUI.DebugCheckSlotsActiveState();
            
            // 🔥 GARANTIA EXTRA: Se ainda houver problema, fixar novamente
            Debug.Log($"║  🛡️ PASSO 4.5: Garantia extra - fixando novamente...");
            paperDollUI.FixDropZones();
        }
        
        // 2. Atualizar tabela SEM forçar refresh completo
        var tableUI = FindFirstObjectByType<InventoryTableUI>();
        if (tableUI != null)
        {
            Debug.Log($"║  📊 Atualizando tabela (modo leve)...");
            tableUI.RefreshTable(false); // 🔥 FALSE = não forçar recriação
            Debug.Log($"║  ✅ Tabela atualizada");
        }
        else
        {
            Debug.LogWarning($"║  ⚠️ tableUI não encontrado");
        }
        
        // 3. Atualizar stats
        var detailsUI = FindFirstObjectByType<InventoryItemDetailsUI>();
        if (detailsUI != null)
        {
            Debug.Log($"║  📈 Atualizando stats...");
            detailsUI.UpdatePartyMemberStats();
            Debug.Log($"║  ✅ Stats atualizados");
        }
        else
        {
            Debug.LogWarning($"║  ⚠️ detailsUI não encontrado");
        }
        
        // 4. Limpar seleções da tabela também
        if (tableUI != null)
        {
            Debug.Log($"║  🧹 Limpando seleção da tabela...");
            tableUI.ClearSelection();
            Debug.Log($"║  ✅ Seleção da tabela limpa");
        }
        
        Debug.Log($"║  ✅ UI COMPLETAMENTE ATUALIZADA!");
        Debug.Log($"║  🎯 DropZones devem estar funcionais!");
        Debug.Log("╚═══════════════════════════════════════╝");
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
    [ContextMenu("🔍 Debug: DropZone Configuration")]
    public void DebugDropZoneConfig()
    {
        Debug.Log($"=== DROPZONE CONFIG: {gameObject.name} ===");
        Debug.Log($"Drop Type: {dropType}");
        Debug.Log($"Accepted Slot: {acceptedEquipmentSlot}");
        Debug.Log($"Background Image: {(backgroundImage != null ? "✅" : "❌")}");
        
        if (backgroundImage != null)
        {
            Debug.Log($"Raycast Target: {backgroundImage.raycastTarget}");
        }
        
        // Verificar parent e hierarquia
        Debug.Log($"Parent: {transform.parent?.name ?? "None"}");
        Debug.Log($"Hierarchy: {GetHierarchyPath()}");
    }

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform current = transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }

    




#region 🎨 ADVANCED TRANSPARENCY CONTROL
[Header("🎨 Advanced Transparency Control")]
[SerializeField] private bool useAdvancedControl = true;

[Header("Normal State")]
[SerializeField] private Color normalColorRGB = new Color(0.2f, 0.2f, 0.2f);
[SerializeField] [Range(0, 100)] private int normalAlphaPercent = 5; // 5%

[Header("Hover Valid State")]
[SerializeField] private Color hoverValidColorRGB = Color.green;
[SerializeField] [Range(0, 100)] private int hoverValidAlphaPercent = 30; // 30%

[Header("Hover Invalid State")]
[SerializeField] private Color hoverInvalidColorRGB = Color.red;
[SerializeField] [Range(0, 100)] private int hoverInvalidAlphaPercent = 30; // 30%

// Métodos auxiliares para obter cores com alpha
private Color GetNormalColor()
{
    return new Color(
        normalColorRGB.r,
        normalColorRGB.g,
        normalColorRGB.b,
        normalAlphaPercent / 100f
    );
}

private Color GetHoverValidColor()
{
    return new Color(
        hoverValidColorRGB.r,
        hoverValidColorRGB.g,
        hoverValidColorRGB.b,
        hoverValidAlphaPercent / 100f
    );
}

private Color GetHoverInvalidColor()
{
    return new Color(
        hoverInvalidColorRGB.r,
        hoverInvalidColorRGB.g,
        hoverInvalidColorRGB.b,
        hoverInvalidAlphaPercent / 100f
    );
}


#endregion
}