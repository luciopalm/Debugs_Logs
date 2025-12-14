using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterRotationPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image mainCharacterPortrait;
    [SerializeField] private Image prevCharacterIcon;
    [SerializeField] private Image nextCharacterIcon;
    [SerializeField] private TMP_Text prevCharacterName;
    [SerializeField] private TMP_Text nextCharacterName;

    [Header("Animation")]
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private AnimationCurve rotationCurve;

    // Reference
    private PartyManager partyManager;
    
    // State
    private bool isRotating = false;
    private int currentRotationIndex = 0;

    private void Start()
    {
        Debug.Log("🔄 CharacterRotationPanelUI Start()");
        
        partyManager = PartyManager.Instance;
        
        if (partyManager == null)
        {
            Debug.LogError("❌ PartyManager não encontrado!");
            return;
        }

        // 🔥 CORREÇÃO 1: Conectar eventos CORRETAMENTE
        partyManager.OnActiveMemberChanged += OnActiveMemberChanged;
        partyManager.OnPartyChanged += OnPartyChanged;

        // Inicializar UI
        UpdateRotationUI();
        
        Debug.Log($"✅ CharacterRotationPanelUI inicializado. Members: {partyManager.GetMemberCount()}");
    }

    private void OnDestroy()
    {
        if (partyManager != null)
        {
            partyManager.OnActiveMemberChanged -= OnActiveMemberChanged;
            partyManager.OnPartyChanged -= OnPartyChanged;
        }
    }

    // 🔥 CORREÇÃO 2: Usar portrait, não overworldSprite
    private void UpdateRotationUI()
    {
        if (partyManager == null)
        {
            Debug.LogWarning("⚠️ PartyManager é null em UpdateRotationUI");
            return;
        }

        int memberCount = partyManager.GetMemberCount();
        
        if (memberCount == 0)
        {
            Debug.LogWarning("⚠️ Nenhum member no party");
            SetEmptyUI();
            return;
        }

        // Garantir que o índice está dentro dos limites
        currentRotationIndex = partyManager.GetActiveIndex();
        currentRotationIndex = Mathf.Clamp(currentRotationIndex, 0, memberCount - 1);

        CharacterData currentCharacter = partyManager.GetActiveMember();
        
        if (currentCharacter == null)
        {
            Debug.LogError("❌ CharacterData atual é null!");
            SetEmptyUI();
            return;
        }

        Debug.Log($"🔄 UpdateRotationUI: {currentCharacter.characterName} (Index: {currentRotationIndex})");

        // 1. NOME DO PERSONAGEM ATUAL
        if (characterNameText != null)
        {
            characterNameText.text = currentCharacter.characterName.ToUpper();
        }

        // 2. PORTRAIT PRINCIPAL (🔥 CORREÇÃO: usar portrait!)
        if (mainCharacterPortrait != null)
        {
            if (currentCharacter.portrait != null)
            {
                mainCharacterPortrait.sprite = currentCharacter.portrait;
                mainCharacterPortrait.color = Color.white;
                Debug.Log($"✅ Portrait definido: {currentCharacter.characterName}");
            }
            else if (currentCharacter.overworldSprite != null)
            {
                // Fallback para overworldSprite se portrait não existir
                mainCharacterPortrait.sprite = currentCharacter.overworldSprite;
                mainCharacterPortrait.color = Color.white;
                Debug.Log($"⚠️ Usando overworldSprite como fallback: {currentCharacter.characterName}");
            }
            else
            {
                mainCharacterPortrait.color = Color.gray;
                Debug.LogWarning($"⚠️ {currentCharacter.characterName} não tem portrait nem overworldSprite!");
            }
        }

        // 3. CALCULAR ÍNDICES ANTERIOR/PRÓXIMO (com wrap-around)
        int prevIndex = GetPrevMemberIndex();
        int nextIndex = GetNextMemberIndex();

        // 4. PERSONAGEM ANTERIOR
        CharacterData prevCharacter = partyManager.GetMemberAtIndex(prevIndex);
        if (prevCharacter != null)
        {
            if (prevCharacterIcon != null)
            {
                // Prioridade: portrait → overworldSprite
                if (prevCharacter.portrait != null)
                    prevCharacterIcon.sprite = prevCharacter.portrait;
                else if (prevCharacter.overworldSprite != null)
                    prevCharacterIcon.sprite = prevCharacter.overworldSprite;
                
                prevCharacterIcon.color = prevCharacterIcon.sprite != null ? Color.white : Color.gray;
            }
            
            if (prevCharacterName != null)
                prevCharacterName.text = prevCharacter.characterName;
        }

        // 5. PRÓXIMO PERSONAGEM
        CharacterData nextCharacter = partyManager.GetMemberAtIndex(nextIndex);
        if (nextCharacter != null)
        {
            if (nextCharacterIcon != null)
            {
                // Prioridade: portrait → overworldSprite
                if (nextCharacter.portrait != null)
                    nextCharacterIcon.sprite = nextCharacter.portrait;
                else if (nextCharacter.overworldSprite != null)
                    nextCharacterIcon.sprite = nextCharacter.overworldSprite;
                
                nextCharacterIcon.color = nextCharacterIcon.sprite != null ? Color.white : Color.gray;
            }
            
            if (nextCharacterName != null)
                nextCharacterName.text = nextCharacter.characterName;
        }

        Debug.Log($"   Prev: {prevCharacter?.characterName ?? "None"} | Next: {nextCharacter?.characterName ?? "None"}");
    }

    private int GetPrevMemberIndex()
    {
        if (partyManager == null) return 0;
        
        int memberCount = partyManager.GetMemberCount();
        if (memberCount <= 1) return 0;
        
        // Wrap-around: se 0 → vai para último
        return (currentRotationIndex - 1 + memberCount) % memberCount;
    }

    private int GetNextMemberIndex()
    {
        if (partyManager == null) return 0;
        
        int memberCount = partyManager.GetMemberCount();
        if (memberCount <= 1) return 0;
        
        // Wrap-around: se último → volta para 0
        return (currentRotationIndex + 1) % memberCount;
    }

    // 🔥 CORREÇÃO 3: NÃO chamar SetActiveMember() dentro do UpdateRotationUI!
    public void RotateToNextCharacter()
    {
        if (isRotating) return;
        if (partyManager == null) return;
        
        int memberCount = partyManager.GetMemberCount();
        if (memberCount <= 1) return;

        Debug.Log($"🔄 RotateToNextCharacter chamado (antes: {currentRotationIndex})");
        
        // Calcular próximo índice
        int nextIndex = GetNextMemberIndex();
        
        // 🔥 MUDANÇA CRÍTICA: Mudar character via PartyManager
        StartCoroutine(RotateCharacterAnimation(true, nextIndex));
    }

    public void RotateToPrevCharacter()
    {
        if (isRotating) return;
        if (partyManager == null) return;
        
        int memberCount = partyManager.GetMemberCount();
        if (memberCount <= 1) return;

        Debug.Log($"🔄 RotateToPrevCharacter chamado (antes: {currentRotationIndex})");
        
        // Calcular índice anterior
        int prevIndex = GetPrevMemberIndex();
        
        // 🔥 MUDANÇA CRÍTICA: Mudar character via PartyManager
        StartCoroutine(RotateCharacterAnimation(false, prevIndex));
    }

    // 🔥 NOVO: Coroutine para animação sem loop infinito
    private IEnumerator RotateCharacterAnimation(bool isNext, int targetIndex)
    {
        isRotating = true;
        
        Debug.Log($"🎬 Iniciando animação para index: {targetIndex}");
        
        // 1. Desativar interação dos botões durante animação
        SetButtonsInteractable(false);
        
        // 2. Pequeno delay antes de mudar (efeito visual)
        yield return new WaitForSeconds(0.05f);
        
        // 🔥 3. AGORA SIM: Mudar character (isso disparará OnActiveMemberChanged)
        partyManager.SetActiveMember(targetIndex);
        
        Debug.Log($"✅ Character mudado para index: {targetIndex}");
        
        // 4. Aguardar um frame para eventos serem processados
        yield return null;
        
        // 5. Atualizar UI (já foi feito pelo OnActiveMemberChanged, mas garantimos)
        UpdateRotationUI();
        
        // 6. Pequeno delay após mudança
        yield return new WaitForSeconds(0.1f);
        
        // 7. Reativar botões
        SetButtonsInteractable(true);
        
        isRotating = false;
        
        Debug.Log("✅ Animação completa");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        // Você pode adicionar referências aos botões se quiser desativá-los visualmente
        // Button prevBtn = prevButtonIcon?.GetComponentInParent<Button>();
        // Button nextBtn = nextButtonIcon?.GetComponentInParent<Button>();
        // if (prevBtn != null) prevBtn.interactable = interactable;
        // if (nextBtn != null) nextBtn.interactable = interactable;
    }

    private void SetEmptyUI()
    {
        if (characterNameText != null)
            characterNameText.text = "NO CHARACTER";
        
        if (mainCharacterPortrait != null)
            mainCharacterPortrait.color = Color.gray;
        
        if (prevCharacterIcon != null)
            prevCharacterIcon.color = Color.gray;
        
        if (nextCharacterIcon != null)
            nextCharacterIcon.color = Color.gray;
        
        if (prevCharacterName != null)
            prevCharacterName.text = "";
        
        if (nextCharacterName != null)
            nextCharacterName.text = "";
    }

    // 🔥 CORREÇÃO 4: Event handlers melhorados
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        Debug.Log($"🔄 OnActiveMemberChanged: {newActiveMember?.characterName ?? "NULL"}");
        
        // Pequeno delay para garantir que tudo está atualizado
        StartCoroutine(UpdateUIAfterDelay());
    }

    private IEnumerator UpdateUIAfterDelay()
    {
        yield return null; // Aguarda um frame
        UpdateRotationUI();
    }

    private void OnPartyChanged()
    {
        Debug.Log("🔄 OnPartyChanged - Atualizando Rotation UI");
        UpdateRotationUI();
    }

    // 🔥 NOVO: Método para debug
    [ContextMenu("🔍 Debug: Check Rotation Panel State")]
    public void DebugCheckState()
    {
        Debug.Log("=== CHARACTER ROTATION PANEL DEBUG ===");
        Debug.Log($"PartyManager: {(partyManager != null ? "✅" : "❌ NULL")}");
        
        if (partyManager != null)
        {
            Debug.Log($"Member Count: {partyManager.GetMemberCount()}");
            Debug.Log($"Active Index: {partyManager.GetActiveIndex()}");
            
            var activeChar = partyManager.GetActiveMember();
            Debug.Log($"Active Character: {activeChar?.characterName ?? "NULL"}");
            Debug.Log($"Has Portrait: {activeChar?.portrait != null}");
            Debug.Log($"Has OverworldSprite: {activeChar?.overworldSprite != null}");
        }
        
        Debug.Log($"isRotating: {isRotating}");
        Debug.Log($"currentRotationIndex: {currentRotationIndex}");
        Debug.Log("=====================================");
    }
}