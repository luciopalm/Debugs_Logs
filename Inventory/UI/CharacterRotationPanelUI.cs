using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CharacterRotationPanelUI : MonoBehaviour
{
    [Header("Active Character Display")]
    [SerializeField] private Image activeMemberPortrait;
    [SerializeField] private TMP_Text activeMemberName;
    [SerializeField] private GameObject activeMemberHighlight;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Image prevButtonIcon;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image nextButtonIcon;
    
    [Header("Side Character Icons (4th member)")]
    [SerializeField] private Image leftSideCharacterIcon;
    [SerializeField] private Image rightSideCharacterIcon;
    
    [Header("Visual Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color sideCharacterColor = new Color(1f, 1f, 1f, 0.3f);
    
    [Header("Animation")]
    [SerializeField] private float swapDuration = 0.3f;
    
    private PartyManager partyManager;
    private List<CharacterData> allPartyMembers = new List<CharacterData>();
    private int activeIndex = 0;
    private bool isAnimating = false;
    private Coroutine currentPortraitAnimation;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        partyManager = PartyManager.Instance;
        
        if (partyManager == null)
        {
            Debug.LogError("[CharacterRotationPanelUI] PartyManager not found!");
            return;
        }
        
        allPartyMembers = partyManager.GetAllMembers();
        activeIndex = partyManager.GetActiveIndex();
        
        SetupButtons();
        SetupButtonVisuals();
        
        partyManager.OnPartyChanged += OnPartyChanged;
        partyManager.OnActiveMemberChanged += OnActiveMemberChanged;
        
        UpdateUI();
    }
    
    private void SetupButtons()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevButtonClicked);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private void SetupButtonVisuals()
    {
        if (prevButton != null)
        {
            var prevColors = prevButton.colors;
            prevColors.normalColor = new Color(1f, 1f, 1f, 1f);
            prevColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            prevColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            prevColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            prevButton.colors = prevColors;
        }
        
        if (nextButton != null)
        {
            var nextColors = nextButton.colors;
            nextColors.normalColor = new Color(1f, 1f, 1f, 1f);
            nextColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            nextColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            nextColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            nextButton.colors = nextColors;
        }
        
        if (prevButtonIcon != null)
        {
            prevButtonIcon.color = Color.white;
        }
        
        if (nextButtonIcon != null)
        {
            nextButtonIcon.color = Color.white;
        }
    }
    
    private void UpdateUI()
    {
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🔄 UpdateRotationUI - INÍCIO        ║");
        Debug.Log($"╚═══════════════════════════════════════╝");
        
        if (partyManager == null || allPartyMembers.Count == 0)
        {
            Debug.Log("   ❌ PartyManager ou membros não encontrados");
            return;
        }
        
        activeIndex = partyManager.GetActiveIndex();
        Debug.Log($"   📊 Active Index: {activeIndex}");
        Debug.Log($"   👥 Total Members: {allPartyMembers.Count}");
        
        CharacterData activeMember = allPartyMembers[activeIndex];
        
        if (activeMember == null)
        {
            Debug.LogError("   ❌ Active member é null!");
            return;
        }
        
        Debug.Log($"   🎯 Active Member: {activeMember.characterName}");
        Debug.Log($"   📸 Portrait: {activeMember.portrait?.name ?? "NULL"}");
        Debug.Log($"   🎮 OverworldSprite: {activeMember.overworldSprite?.name ?? "NULL"}");
        
        UpdateActiveMemberDisplay(activeMember);
        UpdateNavigationButtonIcons();
        UpdateSideCharacterIcons();
        UpdateVisualStates();
        
        Debug.Log($"   ✅ UpdateUI completo");
    }
    
    private void UpdateActiveMemberDisplay(CharacterData activeMember)
    {
        if (activeMemberPortrait != null)
        {
            activeMemberPortrait.transform.localScale = Vector3.one;
            
            if (activeMember.portrait != null)
            {
                activeMemberPortrait.sprite = activeMember.portrait;
                activeMemberPortrait.color = activeColor;
                Debug.Log($"   ✅ Portrait carregado: {activeMember.portrait.name}");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Portrait é null para {activeMember.characterName}");
                activeMemberPortrait.sprite = null;
                activeMemberPortrait.color = activeMember.themeColor;
            }
            
            if (currentPortraitAnimation != null)
            {
                StopCoroutine(currentPortraitAnimation);
                currentPortraitAnimation = null;
            }
            
            if (!isAnimating)
            {
                currentPortraitAnimation = StartCoroutine(AnimatePortrait(activeMemberPortrait.transform));
            }
        }
        else
        {
            Debug.LogError("   ❌ activeMemberPortrait é null!");
        }
        
        if (activeMemberName != null)
        {
            activeMemberName.text = activeMember.characterName;
            activeMemberName.color = activeMember.themeColor;
        }
        
        if (activeMemberHighlight != null)
        {
            activeMemberHighlight.SetActive(true);
        }
    }
    
    private void UpdateNavigationButtonIcons()
    {
        if (prevButtonIcon != null)
        {
            int prevIndex = GetPreviousMemberIndex();
            if (prevIndex >= 0 && prevIndex < allPartyMembers.Count)
            {
                CharacterData prevMember = allPartyMembers[prevIndex];
                UpdateButtonIcon(prevButtonIcon, prevMember);
                prevButtonIcon.color = inactiveColor;
                Debug.Log($"   ◀️ Prev Button: {prevMember.characterName}");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Prev Index inválido: {prevIndex}");
            }
        }
        
        if (nextButtonIcon != null)
        {
            int nextIndex = GetNextMemberIndex();
            if (nextIndex >= 0 && nextIndex < allPartyMembers.Count)
            {
                CharacterData nextMember = allPartyMembers[nextIndex];
                UpdateButtonIcon(nextButtonIcon, nextMember);
                nextButtonIcon.color = inactiveColor;
                Debug.Log($"   ▶️ Next Button: {nextMember.characterName}");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Next Index inválido: {nextIndex}");
            }
        }
    }
    
    private void UpdateSideCharacterIcons()
    {
        int sideMemberIndex = GetFourthMemberIndex();
        
        if (sideMemberIndex >= 0 && sideMemberIndex < allPartyMembers.Count)
        {
            CharacterData sideMember = allPartyMembers[sideMemberIndex];
            
            if (leftSideCharacterIcon != null)
            {
                UpdateButtonIcon(leftSideCharacterIcon, sideMember);
                leftSideCharacterIcon.color = sideCharacterColor;
            }
            
            if (rightSideCharacterIcon != null)
            {
                UpdateButtonIcon(rightSideCharacterIcon, sideMember);
                rightSideCharacterIcon.color = sideCharacterColor;
            }
            
            Debug.Log($"   👥 Side Character: {sideMember.characterName}");
        }
        else
        {
            if (leftSideCharacterIcon != null) 
                leftSideCharacterIcon.color = Color.clear;
            if (rightSideCharacterIcon != null) 
                rightSideCharacterIcon.color = Color.clear;
        }
    }
    
    private void UpdateVisualStates()
    {
        bool hasMultipleMembers = allPartyMembers.Count > 1;
        
        if (prevButton != null) prevButton.interactable = hasMultipleMembers;
        if (nextButton != null) nextButton.interactable = hasMultipleMembers;
        
        Debug.Log($"   ⚙️ Buttons Interactable: {hasMultipleMembers}");
    }
    
    private void UpdateButtonIcon(Image buttonImage, CharacterData character)
    {
        if (buttonImage == null || character == null) return;
        
        if (character.portrait != null)
        {
            buttonImage.sprite = character.portrait;
        }
        else
        {
            buttonImage.sprite = null;
            buttonImage.color = character.themeColor * inactiveColor;
        }
    }
    
    private int GetPreviousMemberIndex()
    {
        if (allPartyMembers.Count <= 1) return activeIndex;
        
        int prevIndex = activeIndex - 1;
        if (prevIndex < 0) prevIndex = allPartyMembers.Count - 1;
        
        return prevIndex;
    }
    
    private int GetNextMemberIndex()
    {
        if (allPartyMembers.Count <= 1) return activeIndex;
        
        int nextIndex = activeIndex + 1;
        if (nextIndex >= allPartyMembers.Count) nextIndex = 0;
        
        return nextIndex;
    }
    
    private int GetFourthMemberIndex()
    {
        if (allPartyMembers.Count < 4) return -1;
        
        int fourthIndex = activeIndex + 2;
        if (fourthIndex >= allPartyMembers.Count) fourthIndex -= allPartyMembers.Count;
        
        return fourthIndex;
    }
    
    private void OnPrevButtonClicked()
    {
        Debug.Log($"🎯🎯🎯 OnPrevButtonClicked CLICADO! 🎯🎯🎯");
        
        if (partyManager == null)
        {
            Debug.LogError("❌ PartyManager é null!");
            return;
        }
        
        // 🔥 CORREÇÃO: Atualizar lista local ANTES de calcular
        allPartyMembers = partyManager.GetAllMembers();
        
        if (allPartyMembers.Count <= 1)
        {
            Debug.LogWarning("⚠️ Só tem 1 membro no party");
            return;
        }
        
        if (isAnimating)
        {
            Debug.LogWarning("⚠️ Já está animando!");
            return;
        }
        
        // 🔥 USAR ÍNDICE ATUAL DO PARTYMANAGER
        int currentManagerIndex = partyManager.GetActiveIndex();
        int prevIndex = currentManagerIndex - 1;
        if (prevIndex < 0) prevIndex = allPartyMembers.Count - 1;
        
        Debug.Log($"🔄 Mudando para membro anterior: índice {prevIndex} (atual: {currentManagerIndex})");
        
        // Muda o membro ativo via PartyManager
        partyManager.SetActiveMember(prevIndex);
    }

    private void OnNextButtonClicked()
    {
        Debug.Log($"🎯🎯🎯 OnNextButtonClicked CLICADO! 🎯🎯🎯");
        
        if (partyManager == null)
        {
            Debug.LogError("❌ PartyManager é null!");
            return;
        }
        
        // 🔥 CORREÇÃO: Atualizar lista local ANTES de calcular
        allPartyMembers = partyManager.GetAllMembers();
        
        if (allPartyMembers.Count <= 1)
        {
            Debug.LogWarning("⚠️ Só tem 1 membro no party");
            return;
        }
        
        if (isAnimating)
        {
            Debug.LogWarning("⚠️ Já está animando!");
            return;
        }
        
        // 🔥 USAR ÍNDICE ATUAL DO PARTYMANAGER
        int currentManagerIndex = partyManager.GetActiveIndex();
        int nextIndex = currentManagerIndex + 1;
        if (nextIndex >= allPartyMembers.Count) nextIndex = 0;
        
        Debug.Log($"🔄 Mudando para próximo membro: índice {nextIndex} (atual: {currentManagerIndex})");
        
        // Muda o membro ativo via PartyManager
        partyManager.SetActiveMember(nextIndex);
    }
    
    private IEnumerator AnimatePortrait(Transform portraitTransform)
    {
        if (portraitTransform == null) yield break;
        
        portraitTransform.localScale = Vector3.one;
        
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.1f;
        
        float duration = swapDuration * 0.3f;
        
        // Scale up
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            portraitTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        portraitTransform.localScale = targetScale;
        
        // Scale down
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            portraitTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        portraitTransform.localScale = Vector3.one;
        currentPortraitAnimation = null;
    }
    
    private IEnumerator AnimateSwap(int direction)
    {
        if (isAnimating || activeMemberPortrait == null) yield break;
        
        isAnimating = true;
        Debug.Log($"🎬 Iniciando animação de swap: direção {direction}");
        
        RectTransform portraitRT = activeMemberPortrait.rectTransform;
        Vector2 originalPos = portraitRT.anchoredPosition;
        Vector2 targetPos = originalPos + new Vector2(direction * 50f, 0f);
        
        // Slide out
        float timer = 0f;
        while (timer < swapDuration * 0.3f)
        {
            timer += Time.deltaTime;
            float t = timer / (swapDuration * 0.3f);
            portraitRT.anchoredPosition = Vector2.Lerp(originalPos, targetPos, t);
            yield return null;
        }
        
        // Atualiza UI durante a animação
        UpdateUI();
        
        portraitRT.anchoredPosition = originalPos + new Vector2(-direction * 50f, 0f);
        
        // Slide in
        timer = 0f;
        while (timer < swapDuration * 0.3f)
        {
            timer += Time.deltaTime;
            float t = timer / (swapDuration * 0.3f);
            portraitRT.anchoredPosition = Vector2.Lerp(portraitRT.anchoredPosition, originalPos, t);
            yield return null;
        }
        
        portraitRT.anchoredPosition = originalPos;
        isAnimating = false;
        Debug.Log($"✅ Animação de swap completa");
    }
    
    private void OnPartyChanged()
    {
        Debug.Log("🔄 OnPartyChanged chamado");
        allPartyMembers = partyManager.GetAllMembers();
        UpdateUI();
    }
    
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        Debug.Log($"🔄 OnActiveMemberChanged: {newActiveMember?.characterName ?? "NULL"}");
        
        if (newActiveMember == null)
        {
            Debug.LogError("❌ newActiveMember é null!");
            return;
        }
        
        if (isAnimating)
        {
            Debug.LogWarning("⚠️ Ignorando mudança durante animação");
            return;
        }
        
        // 🔥 CORREÇÃO CRÍTICA: ATUALIZAR A LISTA DE MEMBROS ANTES DE TUDO!
        allPartyMembers = partyManager.GetAllMembers();
        int newIndex = partyManager.GetActiveIndex(); // 🔥 USAR ÍNDICE DO PARTYMANAGER, NÃO IndexOf!
        
        Debug.Log($"📊 Novo índice: {newIndex} (anterior: {activeIndex})");
        Debug.Log($"👥 Total de membros: {allPartyMembers.Count}");
        
        // Verificar se o índice é válido
        if (newIndex < 0 || newIndex >= allPartyMembers.Count)
        {
            Debug.LogError($"❌ Índice inválido: {newIndex} (máx: {allPartyMembers.Count - 1})");
            return;
        }
        
        // 🔥 ATUALIZAR activeIndex ANTES de calcular direção
        activeIndex = newIndex;
        
        // Calcula direção para animação
        int direction = 0;
        if (allPartyMembers.Count > 1)
        {
            // Para primeira troca, usamos direção baseada no clique
            if (newIndex > activeIndex)
                direction = 1;
            else if (newIndex < activeIndex)
                direction = -1;
        }
        
        // Inicia animação
        if (direction != 0)
        {
            StartCoroutine(AnimateSwap(direction));
        }
        else
        {
            UpdateUI();
        }
        
        // Notifica InventoryUI
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.RefreshUI();
        }
    }
    
    private void OnDestroy()
    {
        if (partyManager != null)
        {
            partyManager.OnPartyChanged -= OnPartyChanged;
            partyManager.OnActiveMemberChanged -= OnActiveMemberChanged;
        }
    }
    
    public void Refresh()
    {
        UpdateUI();
    }

    [ContextMenu("🔄 Debug: Test Rotation")]
    public void DebugRotationTest()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🔍 ROTATION DEBUG TEST              ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        if (partyManager == null)
        {
            Debug.LogError("║  ❌ PartyManager é NULL!");
            Debug.Log("╚═══════════════════════════════════════╝");
            return;
        }
        
        Debug.Log($"║  📊 PartyManager Status:");
        Debug.Log($"║     Active Index: {partyManager.GetActiveIndex()}");
        Debug.Log($"║     Member Count: {partyManager.GetMemberCount()}");
        Debug.Log($"║     Local activeIndex: {activeIndex}");
        
        // Testar cálculos
        Debug.Log($"║");
        Debug.Log($"║  🎯 Cálculos de Índice:");
        Debug.Log($"║     Prev Index: {GetPreviousMemberIndex()}");
        Debug.Log($"║     Next Index: {GetNextMemberIndex()}");
        Debug.Log($"║     Fourth Index: {GetFourthMemberIndex()}");
        
        // Verificar membros
        Debug.Log($"║");
        Debug.Log($"║  👥 Party Members:");
        for (int i = 0; i < partyManager.GetMemberCount(); i++)
        {
            var member = partyManager.GetMemberAtIndex(i);
            string activeMark = i == partyManager.GetActiveIndex() ? " [ACTIVE]" : "";
            Debug.Log($"║     [{i}] {member?.characterName ?? "NULL"}{activeMark}");
            Debug.Log($"║         Portrait: {member?.portrait?.name ?? "NULL"}");
        }
        
        // Verificar referências
        Debug.Log($"║");
        Debug.Log($"║  🔗 Referências UI:");
        Debug.Log($"║     activeMemberPortrait: {activeMemberPortrait != null}");
        Debug.Log($"║     prevButton: {prevButton != null}");
        Debug.Log($"║     nextButton: {nextButton != null}");
        Debug.Log($"║     prevButtonIcon: {prevButtonIcon != null}");
        Debug.Log($"║     nextButtonIcon: {nextButtonIcon != null}");
        
        Debug.Log("╚═══════════════════════════════════════╝");
    }
}