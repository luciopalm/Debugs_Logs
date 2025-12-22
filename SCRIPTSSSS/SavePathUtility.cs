// Crie um novo script: SavePathUtility.cs
using UnityEngine;
using System.IO;
using System;

public static class SavePathUtility
{
    /// <summary>
    /// ⭐⭐ OBTÉM O CAMINHO CORRETO PARA QUALQUER SLOT
    /// Usa o mesmo sistema que GameDataManager
    /// </summary>
    public static string GetSaveFilePath(int slot)
    {
        // 1. Tenta usar GameDataManager (sistema principal)
        if (GameDataManager.Instance != null)
        {
            try
            {
                // Usa reflection para chamar método privado
                var method = typeof(GameDataManager).GetMethod("GetSaveFilePath", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    string path = (string)method.Invoke(GameDataManager.Instance, new object[] { slot });
                    
                    // Debug.Log($"✅ Path obtido via GameDataManager: {path}");
                    return path;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Erro ao obter path via GameDataManager: {e.Message}");
            }
        }
        
        // 2. Fallback: Verifica se há instância ativa
        if (GameInstanceManager.Instance != null && 
            GameInstanceManager.Instance.HasSelectedGameInstance())
        {
            // Sistema novo: GameInstances/Game_XXX/SaveSlots/slot_X.json
            string instancePath = GameInstanceManager.Instance.currentGameInstancePath;
            if (!string.IsNullOrEmpty(instancePath))
            {
                string savePath = Path.Combine(instancePath, "SaveSlots", $"slot_{slot}.json");
                // Debug.Log($"✅ Path instância: {savePath}");
                return savePath;
            }
        }
        
        // 3. Último fallback: Sistema antigo
        string oldPath = Path.Combine(Application.persistentDataPath, "saves", $"save_{slot}.json");
        // Debug.Log($"⚠️ Usando fallback (antigo): {oldPath}");
        return oldPath;
    }
    
    /// <summary>
    /// Verifica se um save existe no slot
    /// </summary>
    public static bool SaveExists(int slot)
    {
        string path = GetSaveFilePath(slot);
        return File.Exists(path);
    }
}