using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 🔍 SISTEMA DE DIAGNÓSTICO COMPLETO
/// Rastreia estado de cada linha da tabela para identificar bug de "linhas cinza"
/// </summary>
public class DiagnosticHelper : MonoBehaviour
{
    private static DiagnosticHelper _instance;
    public static DiagnosticHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DiagnosticHelper");
                _instance = go.AddComponent<DiagnosticHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    // Estrutura para rastrear estado de cada linha
    [System.Serializable]
    public class RowState
    {
        public int rowIndex;
        public string itemName;
        public bool hasDraggable;
        public bool draggableEnabled;
        public bool hasImage;
        public bool imageRaycast;
        public bool hasCanvasGroup;
        public bool canvasGroupInteractable;
        public float canvasGroupAlpha;
        public Color imageColor;
        public string parentName;
        public bool isActive;
        public int siblingIndex;
        public float timestamp;
        
        // Estados específicos do bug
        public bool isGray; // Detecta se está cinza
        public string bugReason; // Razão provável
    }
    
    private Dictionary<int, List<RowState>> rowHistory = new Dictionary<int, List<RowState>>();
    private int maxHistoryPerRow = 10;
    
    /// <summary>
    /// 🔍 CAPTURA ESTADO COMPLETO DE UMA LINHA
    /// </summary>
    public RowState CaptureRowState(GameObject rowObject, int rowIndex)
    {
        if (rowObject == null) return null;
        
        var state = new RowState
        {
            rowIndex = rowIndex,
            timestamp = Time.time,
            isActive = rowObject.activeSelf,
            parentName = rowObject.transform.parent?.name ?? "NULL",
            siblingIndex = rowObject.transform.GetSiblingIndex()
        };
        
        // 1. VERIFICAR DRAGGABLE
        var draggable = rowObject.GetComponent<DraggableItem>();
        state.hasDraggable = draggable != null;
        if (draggable != null)
        {
            state.draggableEnabled = draggable.enabled;
            state.itemName = draggable.GetItemData()?.itemName ?? "NULL";
        }
        
        // 2. VERIFICAR IMAGE
        var image = rowObject.GetComponent<Image>();
        state.hasImage = image != null;
        if (image != null)
        {
            state.imageRaycast = image.raycastTarget;
            state.imageColor = image.color;
            
            // 🔥 DETECTAR SE ESTÁ CINZA
            // Cinza = R,G,B próximos e Alpha baixo (~0.5)
            float avgRGB = (image.color.r + image.color.g + image.color.b) / 3f;
            bool isGrayish = avgRGB > 0.4f && avgRGB < 0.6f;
            bool hasLowAlpha = image.color.a > 0.3f && image.color.a < 0.7f;
            
            state.isGray = isGrayish && hasLowAlpha;
            
            if (state.isGray)
            {
                state.bugReason = $"Image cinza detectada! Color: {image.color}";
            }
        }
        
        // 3. VERIFICAR CANVAS GROUP
        var canvasGroup = rowObject.GetComponent<CanvasGroup>();
        state.hasCanvasGroup = canvasGroup != null;
        if (canvasGroup != null)
        {
            state.canvasGroupInteractable = canvasGroup.interactable;
            state.canvasGroupAlpha = canvasGroup.alpha;
            
            // 🔥 DETECTAR ALPHA BAIXO (possível bug)
            if (canvasGroup.alpha < 0.9f && canvasGroup.alpha > 0.1f)
            {
                state.isGray = true;
                state.bugReason = $"CanvasGroup com alpha suspeito: {canvasGroup.alpha}";
            }
        }
        
        // 4. VERIFICAR BUTTON
        var button = rowObject.GetComponent<UnityEngine.UI.Button>();
        if (button != null && !button.interactable)
        {
            state.isGray = true;
            state.bugReason = "Button desabilitado";
        }
        
        return state;
    }
    
    /// <summary>
    /// 🔍 REGISTRA ESTADO NO HISTÓRICO
    /// </summary>
    public void LogRowState(RowState state, string context)
    {
        if (state == null) return;
        
        // Adiciona ao histórico
        if (!rowHistory.ContainsKey(state.rowIndex))
        {
            rowHistory[state.rowIndex] = new List<RowState>();
        }
        
        var history = rowHistory[state.rowIndex];
        history.Add(state);
        
        // Limita tamanho do histórico
        if (history.Count > maxHistoryPerRow)
        {
            history.RemoveAt(0);
        }
        
        // 🔥 LOG APENAS SE DETECTAR PROBLEMA
        if (state.isGray)
        {
            Debug.LogError($"╔══════════════════════════════════════════════╗");
            Debug.LogError($"║  🚨 BUG DETECTADO - LINHA CINZA              ║");
            Debug.LogError($"╠══════════════════════════════════════════════╣");
            Debug.LogError($"║  Context: {context}");
            Debug.LogError($"║  Row Index: {state.rowIndex}");
            Debug.LogError($"║  Item: {state.itemName ?? "NULL"}");
            Debug.LogError($"║  Reason: {state.bugReason}");
            Debug.LogError($"║");
            Debug.LogError($"║  📊 ESTADO COMPLETO:");
            Debug.LogError($"║    • Active: {state.isActive}");
            Debug.LogError($"║    • Draggable: {state.hasDraggable} (enabled: {state.draggableEnabled})");
            Debug.LogError($"║    • Image Color: {state.imageColor}");
            Debug.LogError($"║    • CanvasGroup Alpha: {state.canvasGroupAlpha}");
            Debug.LogError($"║    • Interactable: {state.canvasGroupInteractable}");
            Debug.LogError($"║    • Raycast: {state.imageRaycast}");
            Debug.LogError($"╚══════════════════════════════════════════════╝");
            
            // 🔥 IMPRIME HISTÓRICO RECENTE
            PrintRowHistory(state.rowIndex, 3);
        }
    }
    
    /// <summary>
    /// 📜 IMPRIME HISTÓRICO DE UMA LINHA
    /// </summary>
    private void PrintRowHistory(int rowIndex, int lastN = 5)
    {
        if (!rowHistory.ContainsKey(rowIndex)) return;
        
        var history = rowHistory[rowIndex];
        int startIdx = Mathf.Max(0, history.Count - lastN);
        
        Debug.LogWarning($"📜 HISTÓRICO DA LINHA {rowIndex} (últimos {lastN}):");
        
        for (int i = startIdx; i < history.Count; i++)
        {
            var state = history[i];
            string grayMark = state.isGray ? " ⚠️ CINZA" : "";
            
            Debug.LogWarning($"  [{i}] T={state.timestamp:F2}s | " +
                           $"Alpha={state.canvasGroupAlpha:F2} | " +
                           $"Color={state.imageColor} | " +
                           $"Draggable={state.draggableEnabled}{grayMark}");
        }
    }
    
    /// <summary>
    /// 🔍 ESCANEIA TODAS AS LINHAS ATIVAS
    /// </summary>
    public void ScanAllRows(string context)
    {
        var tableUI = FindFirstObjectByType<InventoryTableUI>();
        if (tableUI == null) return;
        
        var activeRows = GetActiveRowsFromTable(tableUI);
        
        Debug.Log($"╔══════════════════════════════════════════════╗");
        Debug.Log($"║  🔍 SCAN COMPLETO - {context}");
        Debug.Log($"╠══════════════════════════════════════════════╣");
        Debug.Log($"║  Total linhas ativas: {activeRows.Count}");
        
        int grayCount = 0;
        
        for (int i = 0; i < activeRows.Count; i++)
        {
            var rowObj = activeRows[i];
            var state = CaptureRowState(rowObj, i);
            
            if (state != null && state.isGray)
            {
                grayCount++;
                Debug.LogError($"║  ⚠️  Linha {i}: {state.itemName} - {state.bugReason}");
            }
        }
        
        Debug.Log($"║");
        Debug.Log($"║  🚨 Linhas cinza encontradas: {grayCount}/{activeRows.Count}");
        Debug.Log($"╚══════════════════════════════════════════════╝");
    }
    
    /// <summary>
    /// 🔍 OBTÉM LISTA DE LINHAS ATIVAS
    /// </summary>
    private List<GameObject> GetActiveRowsFromTable(InventoryTableUI tableUI)
    {
        var rows = new List<GameObject>();
        
        // Acessa campo privado via reflection
        var activeRowsField = typeof(InventoryTableUI).GetField(
            "activePooledRows", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (activeRowsField != null)
        {
            var activePooledRows = activeRowsField.GetValue(tableUI) as System.Collections.IList;
            
            if (activePooledRows != null)
            {
                foreach (var pooledRow in activePooledRows)
                {
                    var rowObjectField = pooledRow.GetType().GetField("rowObject");
                    if (rowObjectField != null)
                    {
                        var rowObj = rowObjectField.GetValue(pooledRow) as GameObject;
                        if (rowObj != null)
                        {
                            rows.Add(rowObj);
                        }
                    }
                }
            }
        }
        
        return rows;
    }
    
    /// <summary>
    /// 🔍 COMPARA ESTADO ANTES/DEPOIS
    /// </summary>
    public void CompareStates(RowState before, RowState after, string operation)
    {
        if (before == null || after == null) return;
        
        bool changedToGray = !before.isGray && after.isGray;
        
        if (changedToGray)
        {
            Debug.LogError($"╔══════════════════════════════════════════════╗");
            Debug.LogError($"║  🚨 LINHA FICOU CINZA APÓS: {operation}");
            Debug.LogError($"╠══════════════════════════════════════════════╣");
            Debug.LogError($"║  Row: {after.rowIndex} ({after.itemName})");
            Debug.LogError($"║");
            Debug.LogError($"║  MUDANÇAS:");
            
            if (before.canvasGroupAlpha != after.canvasGroupAlpha)
            {
                Debug.LogError($"║    • Alpha: {before.canvasGroupAlpha:F2} → {after.canvasGroupAlpha:F2}");
            }
            
            if (before.imageColor != after.imageColor)
            {
                Debug.LogError($"║    • Color: {before.imageColor} → {after.imageColor}");
            }
            
            if (before.draggableEnabled != after.draggableEnabled)
            {
                Debug.LogError($"║    • Draggable: {before.draggableEnabled} → {after.draggableEnabled}");
            }
            
            Debug.LogError($"╚══════════════════════════════════════════════╝");
        }
    }
    
    /// <summary>
    /// 📊 RELATÓRIO COMPLETO
    /// </summary>
    [ContextMenu("📊 Gerar Relatório Completo")]
    public void GenerateFullReport()
    {
        ScanAllRows("RELATÓRIO MANUAL");
        
        StringBuilder report = new StringBuilder();
        report.AppendLine("╔══════════════════════════════════════════════╗");
        report.AppendLine("║  📊 RELATÓRIO COMPLETO DE DIAGNÓSTICO       ║");
        report.AppendLine("╠══════════════════════════════════════════════╣");
        
        int totalRows = 0;
        int totalGray = 0;
        
        foreach (var kvp in rowHistory)
        {
            var lastState = kvp.Value[kvp.Value.Count - 1];
            totalRows++;
            
            if (lastState.isGray)
            {
                totalGray++;
                report.AppendLine($"║  ⚠️  Row {kvp.Key}: {lastState.itemName}");
                report.AppendLine($"║      Razão: {lastState.bugReason}");
            }
        }
        
        report.AppendLine($"║");
        report.AppendLine($"║  Total linhas rastreadas: {totalRows}");
        report.AppendLine($"║  Linhas com problema: {totalGray}");
        report.AppendLine($"║  Taxa de erro: {(totalGray * 100f / Mathf.Max(1, totalRows)):F1}%");
        report.AppendLine($"╚══════════════════════════════════════════════╝");
        
        Debug.Log(report.ToString());
    }
    
    /// <summary>
    /// 🔍 CONTEXT MENU PARA TESTE MANUAL
    /// </summary>
    [ContextMenu("🔍 Escanear Linhas Agora")]
    public void ManualScan()
    {
        ScanAllRows("SCAN MANUAL");
    }
    
    [ContextMenu("📜 Mostrar Histórico Completo")]
    public void ShowFullHistory()
    {
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("📜 HISTÓRICO COMPLETO DE TODAS AS LINHAS");
        Debug.Log("═══════════════════════════════════════════════");
        
        foreach (var kvp in rowHistory)
        {
            Debug.Log($"\n🔹 LINHA {kvp.Key}:");
            PrintRowHistory(kvp.Key, 10);
        }
    }
}