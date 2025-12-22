using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 🏷️ Componente para headers de categoria
/// ✅ CORRIGIDO: Clique funciona mesmo sem EventSystem configurado
/// </summary>
public class CategoryHeaderUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private Image collapseArrow;
    [SerializeField] private Image background;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
    
    private string categoryName;
    private bool isExpanded = true;
    private Button button;
    
    public Action<string, bool> OnToggleCategory;
    
    private void Awake()
    {
        Debug.Log($"🏷️ CategoryHeaderUI.Awake() - {gameObject.name}");
        
        // Setup button
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            Debug.Log("  ➕ Button adicionado");
        }
        
        // 🔥 IMPORTANTE: Remover listeners antigos antes de adicionar
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
        Debug.Log("  ✅ Listener configurado");
        
        // Configure visual
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        
        // Auto-find components
        if (categoryText == null)
        {
            categoryText = GetComponentInChildren<TMP_Text>();
            Debug.Log($"  Text: {(categoryText != null ? "✅" : "❌")}");
        }
        
        if (collapseArrow == null)
        {
            Transform arrowTransform = transform.Find("CollapseArrow");
            if (arrowTransform != null)
                collapseArrow = arrowTransform.GetComponent<Image>();
            Debug.Log($"  Arrow: {(collapseArrow != null ? "✅" : "❌")}");
        }
        
        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                Transform bgTransform = transform.Find("Background");
                if (bgTransform != null)
                    background = bgTransform.GetComponent<Image>();
            }
            Debug.Log($"  Background: {(background != null ? "✅" : "❌")}");
        }
    }
    
    public void Initialize(string category, bool expanded = true)
    {
        categoryName = category;
        isExpanded = expanded;
        
        //Debug.Log($"🔧 Initialize: {category} (expanded: {expanded})");
        
        if (categoryText != null)
        {
            categoryText.text = category.ToUpper();
        }
        
        UpdateArrowRotation(immediate: true);
    }
    
    public void SetExpanded(bool expanded, bool animate = true)
    {
        if (isExpanded == expanded) return;
        
        isExpanded = expanded;
        UpdateArrowRotation(immediate: !animate);
    }
    
    private void UpdateArrowRotation(bool immediate = false)
    {
        if (collapseArrow == null) return;
        
        float targetRotation = isExpanded ? 0f : -90f;
        
        if (immediate)
        {
            collapseArrow.transform.rotation = Quaternion.Euler(0, 0, targetRotation);
        }
        else
        {
            StartCoroutine(AnimateArrowRotation(targetRotation));
        }
    }
    
    private System.Collections.IEnumerator AnimateArrowRotation(float targetRotation)
    {
        if (collapseArrow == null) yield break;
        
        float currentRotation = collapseArrow.transform.rotation.eulerAngles.z;
        if (currentRotation > 180f) currentRotation -= 360f;
        if (targetRotation > 180f) targetRotation -= 360f;
        
        float timer = 0f;
        float duration = 0.2f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / duration);
            float angle = Mathf.Lerp(currentRotation, targetRotation, t);
            collapseArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        
        collapseArrow.transform.rotation = Quaternion.Euler(0, 0, targetRotation);
    }
    
    private void OnClicked()
    {
        Debug.Log($"🖱️ Header CLICADO: {categoryName}");
        
        isExpanded = !isExpanded;
        UpdateArrowRotation(immediate: false);
        
        Debug.Log($"  Estado: {(isExpanded ? "EXPANDIDO" : "COLAPSADO")}");
        Debug.Log($"  Callback: {(OnToggleCategory != null ? "✅" : "❌ NULL!")}");
        
        // 🔥 NOTIFICAR InventoryTableUI
        if (OnToggleCategory != null)
        {
            OnToggleCategory.Invoke(categoryName, isExpanded);
            Debug.Log($"  ✅ Callback invocado!");
        }
        else
        {
            Debug.LogError($"  ❌ OnToggleCategory é NULL! Não vai atualizar a UI!");
        }
    }
    
    // 🔥 FALLBACK: Se Button não funcionar, detectar clique manual
    private void OnMouseDown()
    {
        Debug.Log($"🖱️ OnMouseDown detectado em {categoryName}");
        OnClicked();
    }
    
    public string GetCategoryName() => categoryName;
    public bool IsExpanded() => isExpanded;
    
    [ContextMenu("🔄 Toggle Expand/Collapse")]
    public void DebugToggle()
    {
        Debug.Log($"🔄 DEBUG TOGGLE chamado em {categoryName}");
        OnClicked();
    }
}