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
        if (partyManager == null || allPartyMembers.Count == 0) return;
        
        activeIndex = partyManager.GetActiveIndex();
        CharacterData activeMember = allPartyMembers[activeIndex];
        
        UpdateActiveMemberDisplay(activeMember);
        UpdateNavigationButtonIcons();
        UpdateSideCharacterIcons();
        UpdateVisualStates();
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
            }
            else
            {
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
                UpdateButtonIcon(prevButtonIcon, allPartyMembers[prevIndex]);
                prevButtonIcon.color = inactiveColor;
            }
        }
        
        if (nextButtonIcon != null)
        {
            int nextIndex = GetNextMemberIndex();
            if (nextIndex >= 0 && nextIndex < allPartyMembers.Count)
            {
                UpdateButtonIcon(nextButtonIcon, allPartyMembers[nextIndex]);
                nextButtonIcon.color = inactiveColor;
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
        if (partyManager != null && allPartyMembers.Count > 1 && !isAnimating)
        {
            partyManager.PreviousMember();
        }
    }
    
    private void OnNextButtonClicked()
    {
        if (partyManager != null && allPartyMembers.Count > 1 && !isAnimating)
        {
            partyManager.NextMember();
        }
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
    }
    
    private void OnPartyChanged()
    {
        allPartyMembers = partyManager.GetAllMembers();
        UpdateUI();
    }
    
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        if (newActiveMember == null || isAnimating) return;
        
        int newIndex = allPartyMembers.IndexOf(newActiveMember);
        if (newIndex < 0) return;
        
        int direction = (newIndex > activeIndex) ? 1 : -1;
        if (Mathf.Abs(newIndex - activeIndex) > 1 && allPartyMembers.Count > 2) 
            direction *= -1;
        
        StartCoroutine(AnimateSwap(direction));
        
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
}