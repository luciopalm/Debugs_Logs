using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Sistema de Drag & Drop CORRIGIDO
/// ✅ Ghost visual funcional
/// ✅ Raycast configurado corretamente
/// ✅ Drop detection funcionando
/// ✅ Proteção contra travamento se Canvas for null
/// </summary>
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum DragSource
    {
        InventoryTable,
        PaperDollSlot,
        EquipmentSlot
    }
    
    [Header("Visual Feedback")]
    [SerializeField] private float dragAlpha = 0.8f;
    [SerializeField] private Vector2 ghostOffset = new Vector2(32f, -32f);
    
    // Drag state
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    
    // Original state
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    
    // Item info
    private ItemData itemData;
    private DragSource source;
    private ItemData.EquipmentSlot sourceEquipmentSlot;
    
    // Drag result
    private bool wasDroppedSuccessfully = false;
    
    // 🔥 GHOST VISUAL
    private GameObject ghostObject;
    private Image ghostImage;
    private RectTransform ghostRect;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 🔥 BUSCA CANVAS - VERSÃO SIMPLIFICADA E RÁPIDA
        canvas = GetComponentInParent<Canvas>();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void SetupDraggable(ItemData item, DragSource dragSource, ItemData.EquipmentSlot equipSlot = ItemData.EquipmentSlot.None)
    {
        itemData = item;
        source = dragSource;
        sourceEquipmentSlot = equipSlot;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null)
        {
            Debug.LogError("❌ Tentativa de arrastar item NULL!");
            return;
        }
        
        // 🔥 BUSCA INTELIGENTE DO CANVAS
        if (canvas == null)
        {
            // Tenta pelo parent primeiro
            canvas = GetComponentInParent<Canvas>();
            
            // Se não achou, busca na cena
            if (canvas == null)
            {
                GameObject inventoryPanel = GameObject.Find("InventoryPanel");
                if (inventoryPanel != null)
                {
                    canvas = inventoryPanel.GetComponentInParent<Canvas>();
                }
                
                // Último recurso
                if (canvas == null)
                {
                    canvas = FindFirstObjectByType<Canvas>();
                }
            }
            
            if (canvas != null)
            {
                Debug.Log($"✅ Canvas encontrado: {canvas.name}");
            }
        }
        
        if (canvas == null)
        {
            Debug.LogWarning("⚠️ Canvas não encontrado - drag pode não funcionar corretamente");
            // NÃO retorna - tenta continuar
        }
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 BEGIN DRAG: {itemData.itemName}");
        Debug.Log($"║  📍 Source: {source}");
        Debug.Log($"║  🎰 Slot: {itemData.equipmentSlot}");
        Debug.Log($"║  ℹ️ Arraste para o Paper Doll →");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        // Salva estado original
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // 🔥 CRIAR GHOST VISUAL (com proteção)
        CreateGhostVisual();
        
        // Torna objeto original semi-transparente
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.3f;
            canvasGroup.blocksRaycasts = false;
        }
        
        wasDroppedSuccessfully = false;
        
        // Notifica InventoryUI
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnItemDragBegin(itemData, source, sourceEquipmentSlot);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // 🔥 PROTEÇÃO: Se não tem ghost ou canvas, não faz nada
        if (ghostObject == null || canvas == null || ghostRect == null) 
        {
            return;
        }
        
        // Move ghost com cursor
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint
        );
        
        ghostRect.anchoredPosition = localPoint + ghostOffset;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"╔═══════════════════════════════════════════╗");
        Debug.Log($"║  🏁 END DRAG: {itemData?.itemName ?? "NULL"}");
        Debug.Log($"║  ✅ Success: {wasDroppedSuccessfully}");
        Debug.Log($"╚═══════════════════════════════════════════╝");
        
        // 🔥 DESTRUIR GHOST COM PROTEÇÃO
        if (ghostObject != null)
        {
            try
            {
                Destroy(ghostObject);
                ghostObject = null;
                ghostImage = null;
                ghostRect = null;
                Debug.Log("👻 Ghost destruído");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao destruir ghost: {e.Message}");
            }
        }
        
        // Restaura visual original COM PROTEÇÃO
        if (canvasGroup != null)
        {
            try
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("🎨 CanvasGroup restaurado");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao restaurar CanvasGroup: {e.Message}");
            }
        }
        
        if (wasDroppedSuccessfully)
        {
            Debug.Log("   ✅ Drop bem-sucedido - item equipado");
            
            // 🔥🔥🔥 CORREÇÃO CRÍTICA: Esconder/desativar o item original
            // Depois que o item foi equipado via drag & drop,
            // ele não deve mais aparecer na tabela
            
            try
            {
                // 1. Desativa este GameObject (item na tabela)
                if (gameObject != null && gameObject.activeSelf)
                {
                    // 🔥 IMPORTANTE: Não destruir imediatamente, apenas desativar
                    // A destruição será feita pelo Refresh da tabela
                    gameObject.SetActive(false);
                    Debug.Log("🎭 Item original desativado da tabela");
                }
                
                // 2. Atualiza UI
                StartCoroutine(SafeRefreshUIAfterDrag());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao processar drop bem-sucedido: {e.Message}");
            }
        }
        else
        {
            Debug.Log("   ↩️ Drop falhou - voltando ao lugar");
            
            // Se drop falhou, volta à posição original
            if (originalParent != null)
            {
                try
                {
                    transform.SetParent(originalParent, false);
                    transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
                    rectTransform.anchoredPosition = originalPosition;
                    Debug.Log("   ✅ Posição original restaurada");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao restaurar posição: {e.Message}");
                }
            }
        }
        
        // Notifica InventoryUI
        if (InventoryUI.Instance != null)
        {
            try
            {
                InventoryUI.Instance.OnItemDragEnd(itemData, wasDroppedSuccessfully);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao notificar InventoryUI: {e.Message}");
            }
        }
        
        // Limpa estado
        wasDroppedSuccessfully = false;
    }

    private System.Collections.IEnumerator SafeRefreshUIAfterDrag()
    {
        Debug.Log("🔄 Atualizando UI após drag bem-sucedido...");
        
        // Aguarda 1 frame para garantir que o equipamento foi processado
        yield return null;
        
        try
        {
            // 🔥 USAR FindFirstObjectByType em vez de acessar campos privados
            InventoryTableUI tableUI = FindFirstObjectByType<InventoryTableUI>();
            if (tableUI != null)
            {
                tableUI.ForceRefresh();
                Debug.Log("✅ Tabela atualizada (item removido)");
            }
            
            InventoryPaperDollUI paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
            if (paperDollUI != null)
            {
                paperDollUI.UpdateAllSlots();
                
                // 🔥 Usar reflexão para chamar método se existir
                if (itemData != null && paperDollUI.GetType().GetMethod("SelectSlotWithItem") != null)
                {
                    // Chama com delay usando Invoke
                    paperDollUI.Invoke("SelectSlotWithItem", 0.1f);
                    Debug.Log($"🎯 Seleção de slot agendada para {itemData.itemName}");
                }
            }
            
            // Atualiza botões via InventoryUI
            if (InventoryUI.Instance != null)
            {
                // 🔥 Agora pode acessar porque o campo é público
                InventoryUI.Instance.OnItemSelected(itemData);
                
                // Usa o método público
                if (InventoryUI.Instance.GetType().GetMethod("PublicUpdateButtonStates") != null)
                {
                    InventoryUI.Instance.Invoke("PublicUpdateButtonStates", 0);
                }
                
                Debug.Log("✅ Botões atualizados");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro durante atualização: {e.Message}");
        }
        
        Debug.Log("✅ Atualização após drag completa!");
    }

    
    // 🔥 NOVO MÉTODO: Atualização segura da UI
    private System.Collections.IEnumerator SafeRefreshUI()
    {
        Debug.Log("🔄 Iniciando atualização segura da UI...");
        
        // Aguarda 2 frames para garantir que tudo foi processado
        yield return null;
        yield return null;
        
        try
        {
            // 🔥 BUSCAR UI COMPONENTS NA CENA (método mais confiável)
            InventoryPaperDollUI paperDollUI = FindFirstObjectByType<InventoryPaperDollUI>();
            if (paperDollUI != null)
            {
                paperDollUI.UpdateAllSlots();
                Debug.Log("✅ PaperDoll atualizado");
            }
            
            InventoryTableUI tableUI = FindFirstObjectByType<InventoryTableUI>();
            if (tableUI != null)
            {
                tableUI.ForceRefresh();
                Debug.Log("✅ Tabela atualizada");
            }
            
            // Atualiza InventoryUI
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.RefreshUI();
                Debug.Log("✅ InventoryUI atualizado");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro durante atualização: {e.Message}");
        }
        
        Debug.Log("✅ Atualização segura completa!");
    }
    
    /// <summary>
    /// 🔥 CRIA GHOST VISUAL - VERSÃO COM PROTEÇÃO CONTRA TRAVAMENTO
    /// </summary>
    private void CreateGhostVisual()
    {
        // 🔥 VERIFICAÇÃO CRÍTICA: Se não tem Canvas, NÃO cria ghost
        if (canvas == null)
        {
            Debug.LogWarning($"⚠️ Não foi possível criar ghost para {itemData?.itemName} - Canvas é NULL");
            return;
        }
        
        if (itemData == null)
        {
            Debug.LogError("❌ itemData null! Não pode criar ghost.");
            return;
        }
        
        try
        {
            // 1. Criar GameObject
            ghostObject = new GameObject("DragGhost", typeof(RectTransform));
            ghostRect = ghostObject.GetComponent<RectTransform>();
            
            // 2. Parent no Canvas
            ghostObject.transform.SetParent(canvas.transform, false);
            ghostObject.transform.SetAsLastSibling();
            
            // 3. Configurar RectTransform
            ghostRect.sizeDelta = new Vector2(64f, 64f);
            ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
            ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            
            // 4. Adicionar Image com ícone
            ghostImage = ghostObject.AddComponent<Image>();
            ghostImage.sprite = itemData.icon;
            ghostImage.color = new Color(1f, 1f, 1f, dragAlpha);
            ghostImage.raycastTarget = false;
            
            // 5. Adicionar CanvasGroup
            CanvasGroup ghostGroup = ghostObject.AddComponent<CanvasGroup>();
            ghostGroup.alpha = 1f;
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            
            // 6. Adicionar borda
            GameObject border = new GameObject("Border", typeof(RectTransform));
            border.transform.SetParent(ghostObject.transform, false);
            
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(1f, 1f, 1f, 0.3f);
            borderImage.raycastTarget = false;
            
            border.transform.SetAsFirstSibling();
            
            Debug.Log($"   👻 Ghost criado para {itemData.itemName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao criar ghost: {e.Message}");
            
            // Limpa qualquer objeto parcialmente criado
            if (ghostObject != null)
            {
                Destroy(ghostObject);
                ghostObject = null;
                ghostImage = null;
                ghostRect = null;
            }
        }
    }
    
    /// <summary>
    /// Marca que o drop foi bem-sucedido (chamado por DropZone)
    /// </summary>
    public void MarkDropSuccess()
    {
        wasDroppedSuccessfully = true;
        Debug.Log($"   ✅ Drop marcado como sucesso para {itemData?.itemName}");
    }
    
    // Getters
    public ItemData GetItemData() => itemData;
    public DragSource GetSource() => source;
    public ItemData.EquipmentSlot GetSourceSlot() => sourceEquipmentSlot;
    
    // Debug: Visualizar ghost na Scene view
    private void OnDrawGizmos()
    {
        if (ghostRect != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 worldPos = ghostRect.position;
            Gizmos.DrawWireCube(worldPos, new Vector3(64f, 64f, 0f));
        }
    }
}