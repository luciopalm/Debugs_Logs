using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 🏷️ Componente para headers de categoria no inventário
/// ✅ Clicável para collapse/expand
/// ✅ Estado preservado entre refreshes
/// ✅ Animação suave
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
    [SerializeField] private float arrowRotationSpeed = 10f;
    
    // Estado
    private string categoryName;
    private bool isExpanded = true;
    private Button button;
    
    // Callback para notificar InventoryTableUI
    public Action<string, bool> OnToggleCategory;
    
    private void Awake()
    {
        // Setup button
        button = gameObject.GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        // Configure button
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        
        // Add listener
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
        
        // Auto-find components if not assigned
        if (categoryText == null)
            categoryText = GetComponentInChildren<TMP_Text>();
        
        if (collapseArrow == null)
        {
            // Try to find by name
            Transform arrowTransform = transform.Find("CollapseArrow");
            if (arrowTransform != null)
                collapseArrow = arrowTransform.GetComponent<Image>();
        }
        
        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                // Try to find child named "Background"
                Transform bgTransform = transform.Find("Background");
                if (bgTransform != null)
                    background = bgTransform.GetComponent<Image>();
            }
        }
    }
    
    /// <summary>
    /// Inicializa o header com nome da categoria
    /// </summary>
    public void Initialize(string category, bool expanded = true)
    {
        categoryName = category;
        isExpanded = expanded;
        
        if (categoryText != null)
        {
            categoryText.text = category.ToUpper();
        }
        
        UpdateArrowRotation(immediate: true);
    }
    
    /// <summary>
    /// Define se está expandido ou colapsado
    /// </summary>
    public void SetExpanded(bool expanded, bool animate = true)
    {
        if (isExpanded == expanded) return;
        
        isExpanded = expanded;
        UpdateArrowRotation(immediate: !animate);
    }
    
    /// <summary>
    /// Atualiza rotação da seta
    /// </summary>
    private void UpdateArrowRotation(bool immediate = false)
    {
        if (collapseArrow == null) return;
        
        float targetRotation = isExpanded ? 0f : -90f; // ▼ = 0°, ▶ = -90°
        
        if (immediate)
        {
            collapseArrow.transform.rotation = Quaternion.Euler(0, 0, targetRotation);
        }
        else
        {
            StartCoroutine(AnimateArrowRotation(targetRotation));
        }
    }
    
    /// <summary>
    /// Anima rotação da seta
    /// </summary>
    private System.Collections.IEnumerator AnimateArrowRotation(float targetRotation)
    {
        if (collapseArrow == null) yield break;
        
        float currentRotation = collapseArrow.transform.rotation.eulerAngles.z;
        
        // Normalize angles
        if (currentRotation > 180f) currentRotation -= 360f;
        if (targetRotation > 180f) targetRotation -= 360f;
        
        float timer = 0f;
        float duration = 0.2f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = Mathf.SmoothStep(0, 1, t); // Easing
            
            float angle = Mathf.Lerp(currentRotation, targetRotation, t);
            collapseArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            yield return null;
        }
        
        collapseArrow.transform.rotation = Quaternion.Euler(0, 0, targetRotation);
    }
    
    /// <summary>
    /// Callback quando clicado
    /// </summary>
    private void OnClicked()
    {
        isExpanded = !isExpanded;
        UpdateArrowRotation(immediate: false);
        
        // Notifica InventoryTableUI
        OnToggleCategory?.Invoke(categoryName, isExpanded);
        
        Debug.Log($"📂 Category '{categoryName}' {(isExpanded ? "EXPANDED" : "COLLAPSED")}");
    }
    
    // Getters
    public string GetCategoryName() => categoryName;
    public bool IsExpanded() => isExpanded;
    
    [ContextMenu("🔄 Toggle Expand/Collapse")]
    public void DebugToggle()
    {
        OnClicked();
    }
}