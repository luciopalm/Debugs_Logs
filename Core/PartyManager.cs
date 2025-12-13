using UnityEngine;
using System.Collections.Generic;

// PartyManager.cs - SISTEMA DE PARTY SIMPLIFICADO
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }
    
    [Header("Party Members")]
    [SerializeField] private List<CharacterData> partyMembers = new List<CharacterData>();
    [SerializeField] private int activeMemberIndex = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Eventos para UI
    public System.Action OnPartyChanged;
    public System.Action<CharacterData> OnActiveMemberChanged;
    
    private void Awake()
    {
        // Singleton simples - assume que bootstrap garante instância única
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (showDebugLogs) Debug.Log("[PartyManager] Initialized");
        
        if (partyMembers.Count > 0)
        {
            InitializePartyMembers();
        }
    }
    
    private void InitializePartyMembers()
    {
        foreach (var member in partyMembers)
        {
            if (member != null)
            {
                member.currentLevel = 1;
                member.currentHP = member.GetCurrentMaxHP();
                member.currentMP = member.GetCurrentMaxMP();
                member.currentEquipment = new InventoryManager.EquipmentLoadout();
                
                if (showDebugLogs)
                    Debug.Log($"[PartyManager] Initialized: {member.characterName}");
            }
        }
    }
    
    // ===== PUBLIC METHODS =====
    
    public CharacterData GetActiveMember()
    {
        if (partyMembers.Count == 0) 
        {
            Debug.LogWarning("[PartyManager] No party members!");
            return null;
        }
        
        activeMemberIndex = Mathf.Clamp(activeMemberIndex, 0, partyMembers.Count - 1);
        return partyMembers[activeMemberIndex];
    }
    
    public void NextMember()
    {
        if (partyMembers.Count <= 1) return;
        
        activeMemberIndex = (activeMemberIndex + 1) % partyMembers.Count;
        
        var activeMember = GetActiveMember();
        if (showDebugLogs) Debug.Log($"[PartyManager] Switched to: {activeMember.characterName}");
        
        OnActiveMemberChanged?.Invoke(activeMember);
        OnPartyChanged?.Invoke();
    }
    
    public void PreviousMember()
    {
        if (partyMembers.Count <= 1) return;
        
        activeMemberIndex--;
        if (activeMemberIndex < 0)
            activeMemberIndex = partyMembers.Count - 1;
        
        var activeMember = GetActiveMember();
        if (showDebugLogs) Debug.Log($"[PartyManager] Switched to: {activeMember.characterName}");
        
        OnActiveMemberChanged?.Invoke(activeMember);
        OnPartyChanged?.Invoke();
    }
    
    public void SetActiveMember(int index)
    {
        if (index < 0 || index >= partyMembers.Count)
        {
            Debug.LogError($"[PartyManager] Invalid member index: {index}");
            return;
        }
        
        activeMemberIndex = index;
        var activeMember = GetActiveMember();
        
        OnActiveMemberChanged?.Invoke(activeMember);
        OnPartyChanged?.Invoke();
    }
    
    public void SetActiveMember(CharacterData member)
    {
        int index = partyMembers.IndexOf(member);
        if (index >= 0)
        {
            SetActiveMember(index);
        }
        else
        {
            Debug.LogError($"[PartyManager] Member not found: {member.characterName}");
        }
    }
    
    // ===== GETTERS =====
    
    public List<CharacterData> GetAllMembers() => new List<CharacterData>(partyMembers);
    public int GetMemberCount() => partyMembers.Count;
    public int GetActiveIndex() => activeMemberIndex;
    
    public CharacterData GetMemberAtIndex(int index)
    {
        if (index < 0 || index >= partyMembers.Count) return null;
        return partyMembers[index];
    }
    
    // ===== PARTY MANAGEMENT =====
    
    public void AddMember(CharacterData newMember)
    {
        if (partyMembers.Contains(newMember)) return;
        
        partyMembers.Add(newMember);
        
        // Initialize new member
        newMember.currentLevel = 1;
        newMember.currentHP = newMember.GetCurrentMaxHP();
        newMember.currentMP = newMember.GetCurrentMaxMP();
        newMember.currentEquipment = new InventoryManager.EquipmentLoadout();
        
        if (showDebugLogs) Debug.Log($"[PartyManager] Added: {newMember.characterName}");
        OnPartyChanged?.Invoke();
    }
    
    public void RemoveMember(CharacterData member)
    {
        if (!partyMembers.Contains(member)) return;
        
        // Don't remove if it's the last member
        if (partyMembers.Count <= 1)
        {
            Debug.LogWarning("[PartyManager] Cannot remove last party member!");
            return;
        }
        
        // If removing active member, switch to another
        int removedIndex = partyMembers.IndexOf(member);
        bool wasActive = (removedIndex == activeMemberIndex);
        
        partyMembers.Remove(member);
        
        if (wasActive)
        {
            activeMemberIndex = Mathf.Clamp(activeMemberIndex - 1, 0, partyMembers.Count - 1);
            if (showDebugLogs) 
                Debug.Log($"[PartyManager] Active member removed, switched to: {GetActiveMember().characterName}");
        }
        
        OnPartyChanged?.Invoke();
    }
    
    // ===== DEBUG METHODS =====
    
    [ContextMenu("Debug: Print Party Info")]
    public void DebugPrintPartyInfo()
    {
        Debug.Log("=== PARTY INFO ===");
        Debug.Log($"Member Count: {partyMembers.Count}");
        Debug.Log($"Active Index: {activeMemberIndex}");
        
        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            string active = (i == activeMemberIndex) ? " [ACTIVE]" : "";
            Debug.Log($"[{i}] {member.characterName}{active}");
            Debug.Log($"  HP: {member.currentHP}/{member.GetCurrentMaxHP()}");
            Debug.Log($"  ATK: {member.GetCurrentAttack()} | DEF: {member.GetCurrentDefense()}");
        }
    }
}