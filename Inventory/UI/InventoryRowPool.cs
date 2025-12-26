using UnityEngine;
using System.Collections.Generic;

public class InventoryRowPool : MonoBehaviour
{
    [System.Serializable]
    public class PooledRow
    {
        public GameObject rowObject;
        public InventoryTableCell[] cells;
        public bool isActive;
    }
    
    [Header("Pool Settings")]
    [SerializeField] private GameObject itemRowPrefab;
    [SerializeField] private int initialPoolSize = 200;
    [SerializeField] private int expandAmount = 50;
    
    [Header("References")]
    [SerializeField] private Transform poolContainer;
    
    private List<PooledRow> pooledRows = new List<PooledRow>();
    private Queue<PooledRow> availableRows = new Queue<PooledRow>();
    
    public void Initialize()
    {
        if (poolContainer == null)
        {
            GameObject container = new GameObject("RowPoolContainer");
            poolContainer = container.transform;
            poolContainer.SetParent(transform);
            poolContainer.localPosition = Vector3.zero;
        }
        
        // Criar pool inicial
        ExpandPool(initialPoolSize);
        
        Debug.Log($"[RowPool] Initialized with {pooledRows.Count} rows");
    }
    
    private void ExpandPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject rowObj = Instantiate(itemRowPrefab, poolContainer);
            rowObj.name = $"PooledRow_{pooledRows.Count}";
            rowObj.SetActive(false);
            
            InventoryTableCell[] cells = rowObj.GetComponentsInChildren<InventoryTableCell>();
            
            PooledRow pooledRow = new PooledRow
            {
                rowObject = rowObj,
                cells = cells,
                isActive = false
            };
            
            pooledRows.Add(pooledRow);
            availableRows.Enqueue(pooledRow);
        }
        
        Debug.Log($"[RowPool] Expanded by {amount} rows. Total: {pooledRows.Count}, Available: {availableRows.Count}");
    }
    
    // Método GetRow() - OTIMIZADO COM EMERGENCY BATCH
    public PooledRow GetRow()
    {
        // ⭐⭐ OTIMIZAÇÃO: Verificar se queue está vazia ANTES de tentar dequeue
        if (availableRows.Count == 0)
        {
            Debug.LogWarning($"[RowPool] Pool exhausted! Available: {availableRows.Count}, Total: {pooledRows.Count}");
            
            // ⭐⭐ CORREÇÃO: Criar múltiplas linhas de emergência
            int emergencyBatch = Mathf.Max(10, expandAmount); // Cria pelo menos 10 linhas
            Debug.LogWarning($"  🔧 Creating emergency batch of {emergencyBatch} rows...");
            
            ExpandPool(emergencyBatch);
            
            // Verificar novamente
            if (availableRows.Count == 0)
            {
                Debug.LogError("❌ CRITICAL: Pool failed to expand!");
                return CreateEmergencyRowDirectly();
            }
        }
        
        PooledRow row = availableRows.Dequeue();
        row.isActive = true;
        row.rowObject.SetActive(true);
        
        // Debug.Log($"[RowPool] Got row. Remaining: {availableRows.Count}");
        return row;
    }
    
    private PooledRow CreateEmergencyRowDirectly()
    {
        // Último recurso: criar linha fora do pool
        Debug.LogError("⚠️ CREATING DIRECT EMERGENCY ROW (outside pool)!");
        
        GameObject rowObj = Instantiate(itemRowPrefab);
        rowObj.name = $"EmergencyRow_Direct_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        
        InventoryTableCell[] cells = rowObj.GetComponentsInChildren<InventoryTableCell>();
        
        PooledRow emergencyRow = new PooledRow
        {
            rowObject = rowObj,
            cells = cells,
            isActive = true
        };
        
        // Não adicionar ao pool para evitar corrupção
        return emergencyRow;
    }
    
    public void ReturnRow(PooledRow row)
    {
        if (row == null) return;
        
        // ⭐⭐ OTIMIZAÇÃO: Verificar se já está inativo
        if (!row.isActive) return;
        
        row.isActive = false;
        row.rowObject.SetActive(false);
        
        // ⭐⭐ OTIMIZAÇÃO: Só resetar parent se necessário
        if (row.rowObject.transform.parent != poolContainer)
        {
            row.rowObject.transform.SetParent(poolContainer);
        }
        
        availableRows.Enqueue(row);
    }
    
    public void ReturnAllRows()
    {
        // ⭐⭐ OTIMIZAÇÃO CRÍTICA: Só retornar linhas ativas
        int returnedCount = 0;
        
        foreach (var row in pooledRows)
        {
            if (row.isActive)
            {
                ReturnRow(row);
                returnedCount++;
            }
        }
        
        //Debug.Log($"[RowPool] ReturnAllRows: returned {returnedCount}/{pooledRows.Count} active rows");
    }
    
    public void ClearPool()
    {
        ReturnAllRows();
        
        foreach (var row in pooledRows)
        {
            if (row.rowObject != null)
                Destroy(row.rowObject);
        }
        
        pooledRows.Clear();
        availableRows.Clear();
    }
    
    public int GetActiveRowCount()
    {
        int count = 0;
        foreach (var row in pooledRows)
        {
            if (row.isActive) count++;
        }
        return count;
    }
    
    public int GetTotalPoolSize() => pooledRows.Count;
    public int GetAvailableRowCount() => availableRows.Count;
    
    // ⭐⭐ NOVO: Método de debug para status do pool
    [ContextMenu("[RowPool] Debug Pool Status")]
    public void DebugPoolStatus()
    {
        /*Debug.Log($"[RowPool] === POOL STATUS ===");
        Debug.Log($"Total Rows: {pooledRows.Count}");
        Debug.Log($"Available Rows: {availableRows.Count}");
        Debug.Log($"Active Rows: {GetActiveRowCount()}");
        Debug.Log($"Pool Size Configured: {initialPoolSize}");
        Debug.Log($"Expand Amount: {expandAmount}"); */
        
        // Verificar linhas problemáticas
        int nullRows = 0;
        int nullObjects = 0;
        foreach (var row in pooledRows)
        {
            if (row == null) nullRows++;
            if (row.rowObject == null) nullObjects++;
        }
        
        if (nullRows > 0) Debug.LogError($"  ❌ Null rows: {nullRows}");
        if (nullObjects > 0) Debug.LogError($"  ❌ Null GameObjects: {nullObjects}");
    }
}