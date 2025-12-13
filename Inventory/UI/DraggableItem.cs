using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 🔥 VERSÃO CORRIGIDA - Ghost sempre é destruído
/// ✅ Ghost cleanup garantido mesmo em drops externos
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
            canvas = GetComponentInParent<Canvas>();
            
            if (canvas == null)
            {
                GameObject inventoryPanel = GameObject.Find("InventoryPanel");
                if (inventoryPanel != null)
                {
                    canvas = inventoryPanel.GetComponentInParent<Canvas>();
                }
                
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
        }
        
        Debug.Log($"╔════════════════════════════════════╗");
        Debug.Log($"║  🎯 BEGIN DRAG: {itemData.itemName}");
        Debug.Log($"║  📍 Source: {source}");
        Debug.Log($"║  🎰 Slot: {itemData.equipmentSlot}");
        Debug.Log($"║  ℹ️ Arraste para o Paper Doll →");
        Debug.Log($"╚════════════════════════════════════╝");
        
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
        Debug.Log($"🏁 END DRAG: {itemData?.itemName} (Success: {wasDroppedSuccessfully})");
        
        // 🔥🔥🔥 CORREÇÃO CRÍTICA: SEMPRE destruir ghost
        DestroyGhost();
        
        // Restaura visual original
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Se drop falhou, volta à posição original
        if (!wasDroppedSuccessfully)
        {
            Debug.Log("   ↩️ Drop falhou - mantendo no lugar");
        }
        else
        {
            Debug.Log("   ✅ Drop bem-sucedido");
            
            // 🔥 FORÇA REFRESH DA UI
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.StartCoroutine(
                    InventoryUI.Instance.RefreshUIAfterDrag()
                );
            }
        }
        
        // Notifica InventoryUI
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnItemDragEnd(itemData, wasDroppedSuccessfully);
        }
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
            DestroyGhost();
        }
    }
    
    /// <summary>
    /// 🔥🔥🔥 NOVO MÉTODO: Garante destruição do ghost
    /// </summary>
    private void DestroyGhost()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
            ghostImage = null;
            ghostRect = null;
            Debug.Log("   🗑️ Ghost destruído");
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
    
    // 🔥🔥🔥 NOVO: Cleanup ao destruir componente
    private void OnDestroy()
    {
        DestroyGhost();
    }
    
    // 🔥🔥🔥 NOVO: Cleanup ao desabilitar
    private void OnDisable()
    {
        DestroyGhost();
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