using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 🔍 MENU DE DIAGNÓSTICO NO INSPECTOR
/// Adicione este componente ao GameObject InventoryPanel
/// </summary>
public class InventoryDiagnosticMenu : MonoBehaviour
{
    [Header("Referencias Automáticas")]
    private InventoryTableUI tableUI;
    private InventoryUI inventoryUI;
    
    [Header("Configurações")]
    [SerializeField] private bool autoScanOnEquip = true;
    [SerializeField] private bool autoScanOnRefresh = true;
    [SerializeField] private bool logEveryRowState = false;
    
    [Header("Debug Info (Read-Only)")]
    [SerializeField] private int totalRowsScanned;
    [SerializeField] private int grayRowsFound;
    [SerializeField] private float lastScanTime;
    
    private void Start()
    {
        tableUI = FindFirstObjectByType<InventoryTableUI>();
        inventoryUI = FindFirstObjectByType<InventoryUI>();
        
        if (tableUI == null)
            Debug.LogError("❌ InventoryTableUI não encontrado!");
        
        if (inventoryUI == null)
            Debug.LogError("❌ InventoryUI não encontrado!");
        
        // Subscribe to events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
            InventoryManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        }
        
        Debug.Log("✅ InventoryDiagnosticMenu inicializado");
    }
    
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
            InventoryManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }
    
    private void OnInventoryChanged()
    {
        if (autoScanOnRefresh)
        {
            ScanAllRowsNow("OnInventoryChanged");
        }
    }
    
    private void OnEquipmentChanged()
    {
        if (autoScanOnEquip)
        {
            ScanAllRowsNow("OnEquipmentChanged");
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // CONTEXT MENU - CLIQUE DIREITO NO COMPONENTE NO INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════
    
    [ContextMenu("🔍 1. Escanear Todas as Linhas AGORA")]
    public void ScanAllRowsNow()
    {
        ScanAllRowsNow("MANUAL SCAN");
    }
    
    private void ScanAllRowsNow(string context)
    {
        lastScanTime = Time.time;
        
        if (DiagnosticHelper.Instance == null)
        {
            Debug.LogError("❌ DiagnosticHelper não encontrado!");
            return;
        }
        
        DiagnosticHelper.Instance.ScanAllRows(context);
        
        // Update stats
        UpdateScanStats();
    }
    
    [ContextMenu("📊 2. Relatório Completo")]
    public void GenerateFullReport()
    {
        if (DiagnosticHelper.Instance == null)
        {
            Debug.LogError("❌ DiagnosticHelper não encontrado!");
            return;
        }
        
        DiagnosticHelper.Instance.GenerateFullReport();
        UpdateScanStats();
    }
    
    [ContextMenu("📜 3. Mostrar Histórico Completo")]
    public void ShowFullHistory()
    {
        if (DiagnosticHelper.Instance == null)
        {
            Debug.LogError("❌ DiagnosticHelper não encontrado!");
            return;
        }
        
        DiagnosticHelper.Instance.ShowFullHistory();
    }
    
    [ContextMenu("🔧 4. Inspecionar Linha Específica (Index 0)")]
    public void InspectRow0()
    {
        InspectSpecificRow(0);
    }
    
    [ContextMenu("🔧 5. Inspecionar Linha Específica (Index 1)")]
    public void InspectRow1()
    {
        InspectSpecificRow(1);
    }
    
    [ContextMenu("🔧 6. Inspecionar Linha Específica (Index 2)")]
    public void InspectRow2()
    {
        InspectSpecificRow(2);
    }
    
    private void InspectSpecificRow(int rowIndex)
    {
        if (tableUI == null)
        {
            Debug.LogError("❌ TableUI não encontrado!");
            return;
        }
        
        // Acessa activePooledRows via reflection
        var activeRowsField = typeof(InventoryTableUI).GetField(
            "activePooledRows", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (activeRowsField == null)
        {
            Debug.LogError("❌ Campo activePooledRows não encontrado!");
            return;
        }
        
        var activeRows = activeRowsField.GetValue(tableUI) as System.Collections.IList;
        
        if (activeRows == null || rowIndex >= activeRows.Count)
        {
            Debug.LogError($"❌ Row {rowIndex} não existe! Total: {activeRows?.Count ?? 0}");
            return;
        }
        
        var pooledRow = activeRows[rowIndex];
        var rowObjectField = pooledRow.GetType().GetField("rowObject");
        
        if (rowObjectField == null)
        {
            Debug.LogError("❌ Campo rowObject não encontrado!");
            return;
        }
        
        var rowObj = rowObjectField.GetValue(pooledRow) as GameObject;
        
        if (rowObj == null)
        {
            Debug.LogError($"❌ rowObject da linha {rowIndex} é NULL!");
            return;
        }
        
        // Captura estado detalhado
        Debug.Log($"╔══════════════════════════════════════════════╗");
        Debug.Log($"║  🔍 INSPEÇÃO DETALHADA - LINHA {rowIndex}");
        Debug.Log($"╠══════════════════════════════════════════════╣");
        Debug.Log($"║  GameObject: {rowObj.name}");
        Debug.Log($"║  Ativo: {rowObj.activeSelf}");
        Debug.Log($"║  Parent: {rowObj.transform.parent?.name ?? "NULL"}");
        Debug.Log($"║");
        
        // Componentes
        Debug.Log($"║  📦 COMPONENTES:");
        
        var draggable = rowObj.GetComponent<DraggableItem>();
        Debug.Log($"║    • DraggableItem: {(draggable != null ? "✅" : "❌")}");
        if (draggable != null)
        {
            Debug.Log($"║      - Enabled: {draggable.enabled}");
            Debug.Log($"║      - Item: {draggable.GetItemData()?.itemName ?? "NULL"}");
        }
        
        var image = rowObj.GetComponent<Image>();
        Debug.Log($"║    • Image: {(image != null ? "✅" : "❌")}");
        if (image != null)
        {
            Debug.Log($"║      - Color: {image.color}");
            Debug.Log($"║      - Raycast: {image.raycastTarget}");
        }
        
        var canvasGroup = rowObj.GetComponent<CanvasGroup>();
        Debug.Log($"║    • CanvasGroup: {(canvasGroup != null ? "✅" : "❌")}");
        if (canvasGroup != null)
        {
            Debug.Log($"║      - Alpha: {canvasGroup.alpha}");
            Debug.Log($"║      - Interactable: {canvasGroup.interactable}");
            Debug.Log($"║      - BlocksRaycasts: {canvasGroup.blocksRaycasts}");
        }
        
        var button = rowObj.GetComponent<Button>();
        Debug.Log($"║    • Button: {(button != null ? "✅" : "❌")}");
        if (button != null)
        {
            Debug.Log($"║      - Interactable: {button.interactable}");
            Debug.Log($"║      - Listeners: {button.onClick.GetPersistentEventCount()}");
        }
        
        Debug.Log($"╚══════════════════════════════════════════════╝");
        
        // Usa DiagnosticHelper para captura completa
        if (DiagnosticHelper.Instance != null)
        {
            var state = DiagnosticHelper.Instance.CaptureRowState(rowObj, rowIndex);
            DiagnosticHelper.Instance.LogRowState(state, $"INSPEÇÃO MANUAL - Row {rowIndex}");
        }
    }
    
    [ContextMenu("🧹 7. Limpar Histórico de Diagnóstico")]
    public void ClearDiagnosticHistory()
    {
        if (DiagnosticHelper.Instance == null) return;
        
        // Acessa e limpa o histórico via reflection
        var historyField = typeof(DiagnosticHelper).GetField(
            "rowHistory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (historyField != null)
        {
            var history = historyField.GetValue(DiagnosticHelper.Instance) as System.Collections.IDictionary;
            history?.Clear();
            
            Debug.Log("✅ Histórico de diagnóstico limpo!");
        }
        
        totalRowsScanned = 0;
        grayRowsFound = 0;
        lastScanTime = 0f;
    }
    
    [ContextMenu("🔄 8. Forçar Refresh da Tabela")]
    public void ForceTableRefresh()
    {
        if (tableUI == null)
        {
            Debug.LogError("❌ TableUI não encontrado!");
            return;
        }
        
        Debug.Log("🔄 Forçando refresh completo...");
        
        tableUI.RefreshTable(forceRefresh: true);
        
        // Scan após refresh
        if (DiagnosticHelper.Instance != null)
        {
            DiagnosticHelper.Instance.ScanAllRows("APÓS FORCE REFRESH");
        }
    }
    
    [ContextMenu("🎯 9. Equipar Primeiro Item (Teste)")]
    public void EquipFirstItemTest()
    {
        if (InventoryManager.Instance == null) return;
        
        var slots = InventoryManager.Instance.GetNonEmptySlots();
        
        if (slots.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhum item no inventário!");
            return;
        }
        
        var firstSlot = slots[0];
        
        if (firstSlot.item.IsEquipment())
        {
            Debug.Log($"🎯 Tentando equipar: {firstSlot.item.itemName}");
            
            bool success = InventoryManager.Instance.EquipItem(firstSlot.item);
            
            Debug.Log($"Resultado: {(success ? "✅ SUCESSO" : "❌ FALHOU")}");
            
            // Scan após equipar
            if (DiagnosticHelper.Instance != null && success)
            {
                DiagnosticHelper.Instance.ScanAllRows("APÓS EQUIPAR TESTE");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Primeiro item não é equipamento: {firstSlot.item.itemName}");
        }
    }
    
    [ContextMenu("📸 10. Capturar Screenshot do Estado")]
    public void CaptureScreenshot()
    {
        string filename = $"InventoryDebug_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string path = System.IO.Path.Combine(Application.dataPath, "..", filename);
        
        ScreenCapture.CaptureScreenshot(filename);
        
        Debug.Log($"📸 Screenshot salvo: {filename}");
        Debug.Log($"   Path: {path}");
        
        // Também gera relatório
        if (DiagnosticHelper.Instance != null)
        {
            DiagnosticHelper.Instance.GenerateFullReport();
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE STATS
    // ═══════════════════════════════════════════════════════════════════════
    
    private void UpdateScanStats()
    {
        if (tableUI == null) return;
        
        // Conta linhas via reflection
        var activeRowsField = typeof(InventoryTableUI).GetField(
            "activePooledRows", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (activeRowsField != null)
        {
            var activeRows = activeRowsField.GetValue(tableUI) as System.Collections.IList;
            totalRowsScanned = activeRows?.Count ?? 0;
        }
        
        // Conta cinzas (via DiagnosticHelper)
        grayRowsFound = CountGrayRows();
    }
    
    private int CountGrayRows()
    {
        if (DiagnosticHelper.Instance == null) return 0;
        if (tableUI == null) return 0;
        
        int count = 0;
        
        // Acessa activePooledRows
        var activeRowsField = typeof(InventoryTableUI).GetField(
            "activePooledRows", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (activeRowsField != null)
        {
            var activeRows = activeRowsField.GetValue(tableUI) as System.Collections.IList;
            
            if (activeRows != null)
            {
                foreach (var pooledRow in activeRows)
                {
                    var rowObjectField = pooledRow.GetType().GetField("rowObject");
                    if (rowObjectField != null)
                    {
                        var rowObj = rowObjectField.GetValue(pooledRow) as GameObject;
                        if (rowObj != null)
                        {
                            var state = DiagnosticHelper.Instance.CaptureRowState(rowObj, -1);
                            if (state != null && state.isGray)
                            {
                                count++;
                            }
                        }
                    }
                }
            }
        }
        
        return count;
    }
    
    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE (opcional - para scan contínuo)
    // ═══════════════════════════════════════════════════════════════════════
    
    private float nextAutoScan = 0f;
    [SerializeField] private float autoScanInterval = 5f; // Scan a cada 5s
    [SerializeField] private bool enableAutoScan = false;
    
    private void Update()
    {
        if (!enableAutoScan) return;
        if (Time.time < nextAutoScan) return;
        
        nextAutoScan = Time.time + autoScanInterval;
        
        if (DiagnosticHelper.Instance != null)
        {
            DiagnosticHelper.Instance.ScanAllRows("AUTO SCAN");
            UpdateScanStats();
        }
    }
}