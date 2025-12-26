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

    //  Configuração de Save/Load
    [Header("Save/Load Integration")]
    [SerializeField] private bool autoSaveOnPartyChange = false;
    [SerializeField] private bool loadFromSaveOnStart = true;
    
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

        //  Aguarda GameDataManager estar pronto antes de inicializar
        StartCoroutine(DelayedInitialization());
        
        if (partyMembers.Count > 0)
        {
            InitializePartyMembers();
        }

        LoadCustomNames();

    }
    // Inicialização atrasada para garantir ordem
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // Aguarda 1 frame para outros managers inicializarem
        yield return null;
        
        // 🔥 1. Tenta carregar do save se configurado
        if (loadFromSaveOnStart && GameDataManager.Instance != null)
        {
            // Aguarda GameDataManager terminar sua inicialização
            yield return new WaitForSeconds(0.1f);
            
            // Tenta carregar party do save
            bool loadedFromSave = TryLoadPartyFromGameData();
            
            if (loadedFromSave)
            {
                if (showDebugLogs) Debug.Log("[PartyManager] Party loaded from save");
                yield break;
            }
        }
        
        // 🔥 2. Se não carregou do save, inicializa normalmente
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

    //  ===== SAVE/LOAD INTEGRATION =====

    /// <summary>
    /// Tenta carregar a party do GameDataManager
    /// </summary>
    private bool TryLoadPartyFromGameData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[PartyManager] GameDataManager not found");
            return false;
        }
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null || gameData.isNewGame)
        {
            if (showDebugLogs) Debug.Log("[PartyManager] No save data or new game");
            return false;
        }
        
        var savedParty = gameData.playerData.characterEquipment.partyMembers;
        if (savedParty == null || savedParty.Count == 0)
        {
            if (showDebugLogs) Debug.Log("[PartyManager] No saved party members");
            return false;
        }
        
        // 🔥 1. Para cada membro salvo, encontra o CharacterData correspondente
        List<CharacterData> loadedMembers = new List<CharacterData>();
        
        foreach (var savedMember in savedParty)
        {
            CharacterData foundMember = FindCharacterData(savedMember);
            
            if (foundMember != null)
            {
                // 🔥 2. Aplica dados do save ao CharacterData (runtime apenas)
                ApplySaveDataToCharacter(foundMember, savedMember);
                loadedMembers.Add(foundMember);
                
                if (showDebugLogs)
                    Debug.Log($"[PartyManager] Loaded: {foundMember.characterName} (Lv {foundMember.currentLevel})");
            }
            else
            {
                Debug.LogWarning($"[PartyManager] Character not found: {savedMember.characterName}");
            }
        }
        
        if (loadedMembers.Count > 0)
        {
            // 🔥 3. Substitui a party atual pelos membros carregados
            partyMembers.Clear();
            partyMembers.AddRange(loadedMembers);
            
            // 🔥 4. Restaura personagem ativo
            int savedIndex = gameData.playerData.characterEquipment.activeCharacterIndex;
            if (savedIndex >= 0 && savedIndex < partyMembers.Count)
            {
                activeMemberIndex = savedIndex;
            }
            
            // 🔥 5. Notifica UI
            OnPartyChanged?.Invoke();
            OnActiveMemberChanged?.Invoke(GetActiveMember());
            
            Debug.Log($"[PartyManager] ✅ Party loaded: {loadedMembers.Count} members");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Encontra CharacterData correspondente ao membro salvo
    /// </summary>
    private CharacterData FindCharacterData(PartyMemberData savedMember)
    {
        // Se já temos partyMembers configurados no Inspector, procura neles
        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            
            // Tenta match por ScriptableObject name
            if (member.name == savedMember.characterID)
                return member;
            
            // Tenta match por characterName
            if (member.characterName == savedMember.characterName)
                return member;
        }
        
        // Se não encontrou, pode tentar carregar do Resources
        // (Opcional - depende se seus CharacterData estão em Resources)
        return null;
    }

    /// <summary>
    /// Aplica dados do save ao CharacterData (APENAS valores runtime)
    /// </summary>
    private void ApplySaveDataToCharacter(CharacterData character, PartyMemberData savedData)
    {
        if (character == null || savedData == null) return;
        
        // 🔥 APENAS VALORES RUNTIME (não modifica ScriptableObject)
        character.currentLevel = savedData.level;
        character.currentHP = savedData.currentHP;
        character.currentMP = savedData.currentMP;
        
        // 🔥 EQUIPAMENTOS são carregados pelo GameDataManager.LoadCharacterEquipmentFromData()
        // Este método já é chamado pelo GameDataManager
        
        if (showDebugLogs)
            Debug.Log($"   Applied save data to {character.characterName}: HP={character.currentHP}, MP={character.currentMP}");
    }

    /// <summary>
    /// 🔥 MÉTODO PÚBLICO para GameDataManager notificar sobre equipamentos carregados
    /// </summary>
    public void NotifyEquipmentLoaded(CharacterData character, EquipmentLoadoutData equipmentData)
    {
        if (character == null || equipmentData == null) return;
        
        Debug.Log($"[PartyManager] Equipment load notified for {character.characterName}");
        
        // Garante que o character tem currentEquipment
        if (character.currentEquipment == null)
        {
            character.currentEquipment = new InventoryManager.EquipmentLoadout();
        }
        
        // 🔥 Agora o equipamento já foi aplicado pelo GameDataManager
        // Esta notificação é apenas para debug/consistência
        
        if (character == GetActiveMember())
        {
            // Notifica UI que o equipamento do personagem ativo mudou
            OnActiveMemberChanged?.Invoke(character);
        }
    }

    /// <summary>
    /// 🔥 Salva o estado atual da party no GameDataManager
    /// </summary>
    public void SavePartyToGameData()
    {
        if (GameDataManager.Instance == null) return;
        
        Debug.Log("[PartyManager] Saving party state...");
        
        // 🔥 O GameDataManager já captura o estado da party via UpdatePartyDataToSnapshot()
        // Este método é apenas para forçar um save se necessário
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData != null && gameData.saveSlot > 0)
        {
            GameDataManager.Instance.SaveGame(gameData.saveSlot);
            Debug.Log($"[PartyManager] ✅ Party saved to slot {gameData.saveSlot}");
        }
    }

    /// <summary>
    /// 🔥 Atualiza nome do personagem ativo com o nome do jogador
    /// </summary>
   public void UpdateActiveCharacterWithPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("⚠️ Nome do jogador vazio!");
            return;
        }
        
        var activeMember = GetActiveMember();
        if (activeMember == null)
        {
            Debug.LogError("❌ Nenhum personagem ativo encontrado!");
            return;
        }
        
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  🎮 UPDATE PLAYER NAME - SISTEMA ISOLADO            ║");
        Debug.Log("╠═══════════════════════════════════════════════════════╣");
        Debug.Log($"║  Nome solicitado: '{playerName}'");
        Debug.Log($"║  Character asset: {activeMember.name}");
        Debug.Log($"║  Character.characterName ANTES: '{activeMember.characterName}'");
        
        // 🔥 CRÍTICO: NÃO modificar o ScriptableObject!
        // ❌ activeMember.characterName = playerName; // NÃO FAZER ISSO!
        
        // ✅ SOLUÇÃO: Salvar nome APENAS para esta instância
        if (GameDataManager.Instance != null)
        {
            int instanceID = GameDataManager.Instance.GetCurrentGameInstanceID();
            
            if (instanceID != -1)
            {
                // 1. PlayerPrefs (para esta instância)
                string instanceKey = $"Instance_{instanceID}_PlayerName";
                PlayerPrefs.SetString(instanceKey, playerName);
                PlayerPrefs.Save();
                
                Debug.Log($"║  ✅ PlayerPrefs atualizado: key='{instanceKey}'");
                
                // 2. GameData (já é feito pelo GameDataManager)
                var gameData = GameDataManager.Instance.GetCurrentGameData();
                if (gameData != null)
                {
                    gameData.playerData.playerName = playerName;
                    Debug.Log($"║  ✅ GameData atualizado: playerData.playerName='{playerName}'");
                }
            }
            else
            {
                Debug.LogWarning("║  ⚠️ Sem instância ID - usando chave genérica");
                PlayerPrefs.SetString("CurrentPlayerName", playerName);
                PlayerPrefs.Save();
            }
        }
        
        Debug.Log($"║  Character.characterName DEPOIS: '{activeMember.characterName}' (INALTERADO)");
        Debug.Log($"║  ✅ Nome '{playerName}' salvo para a instância");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
    }
    /// <summary>
    /// 🔥 Carrega nomes customizados ao inicializar
    /// </summary>
    public void LoadCustomNames()
    {
        if (partyMembers == null) 
        {
            Debug.LogWarning("⚠️ partyMembers é null no LoadCustomNames()");
            return;
        }
        
        Debug.Log($"🔍 Carregando nomes customizados para {partyMembers.Count} membros...");
        
        foreach (var character in partyMembers)
        {
            if (character == null) continue;
            
            string characterKey = $"CustomName_{character.name}";
            string savedName = PlayerPrefs.GetString(characterKey, "");
            
            if (!string.IsNullOrEmpty(savedName))
            {
                // Método 1: SetCustomName() se existir
                var setCustomNameMethod = character.GetType().GetMethod("SetCustomName");
                if (setCustomNameMethod != null)
                {
                    setCustomNameMethod.Invoke(character, new object[] { savedName });
                }
                
                // Método 2: Atualizar characterName diretamente
                character.characterName = savedName;
                
                Debug.Log($"   ✅ {character.name}: Nome carregado '{savedName}'");
            }
            else
            {
                Debug.Log($"   ℹ️ {character.name}: Sem nome customizado");
            }
        }
    }

    /// <summary>
    /// 🔥 Obtém o nome do jogador PARA A INSTÂNCIA ATUAL
    /// </summary>


    public string GetPlayerNameForCurrentInstance()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  📝 GET PLAYER NAME - BUSCA ISOLADA                 ║");
        Debug.Log("╠═══════════════════════════════════════════════════════╣");
        
        if (GameDataManager.Instance == null)
        {
            Debug.Log("║  ⚠️ GameDataManager NULL - retornando 'Player'");
            Debug.Log("╚═══════════════════════════════════════════════════════╝");
            return "Player";
        }
        
        int instanceID = GameDataManager.Instance.GetCurrentGameInstanceID();
        Debug.Log($"║  Instance ID: {instanceID}");
        
        // 🔥 ESTRATÉGIA 1: PlayerPrefs (mais confiável para instância)
        if (instanceID != -1)
        {
            string instanceKey = $"Instance_{instanceID}_PlayerName";
            string savedName = PlayerPrefs.GetString(instanceKey, "");
            
            Debug.Log($"║  PlayerPrefs key: '{instanceKey}'");
            Debug.Log($"║  PlayerPrefs value: '{savedName}'");
            
            if (!string.IsNullOrEmpty(savedName))
            {
                Debug.Log($"║  ✅ Nome encontrado no PlayerPrefs: '{savedName}'");
                Debug.Log("╚═══════════════════════════════════════════════════════╝");
                return savedName;
            }
        }
        
        // 🔥 ESTRATÉGIA 2: GameData (fallback)
        var currentGameData = GameDataManager.Instance.GetCurrentGameData();
        if (currentGameData?.playerData != null)
        {
            string gameDataName = currentGameData.playerData.playerName;
            Debug.Log($"║  GameData playerName: '{gameDataName}'");
            
            if (!string.IsNullOrEmpty(gameDataName))
            {
                Debug.Log($"║  ✅ Nome encontrado no GameData: '{gameDataName}'");
                Debug.Log("╚═══════════════════════════════════════════════════════╝");
                return gameDataName;
            }
        }
        
        // 🔥 ESTRATÉGIA 3: Default
        Debug.Log("║  ⚠️ Nenhum nome encontrado - usando 'Player'");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
        return "Player";
    }

    /// <summary>
    /// 🔥 Atualiza nome na UI SEM modificar ScriptableObject
    /// </summary>
    public void UpdatePlayerNameDisplay(string playerName)
    {
        // Aqui você NOTIFICA a UI para atualizar o display
        // Mas NÃO modifica character.characterName
        
        Debug.Log($"🎮 Nome do jogador exibido: {playerName}");
        // Evento para UI atualizar: OnPlayerNameChanged?.Invoke(playerName);
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

    [ContextMenu("🔍 Debug: Check Save/Load Integration")]
    public void DebugCheckSaveLoadIntegration()
    {
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log("║  🔍 PARTY SAVE/LOAD INTEGRATION      ║");
        Debug.Log("╠═══════════════════════════════════════╣");
        
        // 1. Estado atual
        Debug.Log($"║  📊 Current Party:");
        Debug.Log($"║     Members: {partyMembers.Count}");
        Debug.Log($"║     Active Index: {activeMemberIndex}");
        Debug.Log($"║     Active Member: {GetActiveMember()?.characterName ?? "NULL"}");
        
        // 2. GameDataManager status
        Debug.Log($"║");
        Debug.Log($"║  📁 GameDataManager:");
        Debug.Log($"║     Instance: {(GameDataManager.Instance != null ? "✅" : "❌")}");
        
        if (GameDataManager.Instance != null)
        {
            var gameData = GameDataManager.Instance.GetCurrentGameData();
            if (gameData != null)
            {
                Debug.Log($"║     Save Slot: {gameData.saveSlot}");
                Debug.Log($"║     isNewGame: {gameData.isNewGame}");
                Debug.Log($"║     Saved Party Members: {gameData.playerData.characterEquipment.partyMembers.Count}");
            }
            else
            {
                Debug.Log($"║     currentGameData: ❌ NULL");
            }
        }
        
        // 3. Testar conexão
        Debug.Log($"║");
        Debug.Log($"║  🔗 Connection Test:");
        
        bool canSave = GameDataManager.Instance != null;
        bool hasParty = partyMembers.Count > 0;
        
        Debug.Log($"║     Can Save: {(canSave ? "✅" : "❌")}");
        Debug.Log($"║     Has Party: {(hasParty ? "✅" : "❌")}");
        Debug.Log($"║     Auto-Save: {(autoSaveOnPartyChange ? "✅ ON" : "❌ OFF")}");
        
        Debug.Log("╚═══════════════════════════════════════╝");
    }

    [ContextMenu("💾 Force Save Party Now")]
    public void DebugForceSaveParty()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager not found!");
            return;
        }
        
        var gameData = GameDataManager.Instance.GetCurrentGameData();
        if (gameData == null)
        {
            Debug.LogError("❌ No game data to save!");
            return;
        }
        
        Debug.Log("💾 Forcing party save...");
        SavePartyToGameData();
    }

    [ContextMenu("📂 Force Load Party")]
    public void DebugForceLoadParty()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("❌ GameDataManager not found!");
            return;
        }
        
        Debug.Log("📂 Forcing party load...");
        bool loaded = TryLoadPartyFromGameData();
        
        if (loaded)
        {
            Debug.Log("✅ Party loaded successfully");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not load party from save");
        }
    }

    [ContextMenu("⚙️ Toggle Auto-Save")]
    public void DebugToggleAutoSave()
    {
        autoSaveOnPartyChange = !autoSaveOnPartyChange;
        Debug.Log($"🔄 Auto-save on party change: {(autoSaveOnPartyChange ? "✅ ENABLED" : "❌ DISABLED")}");
    }

    [ContextMenu("🔍 Debug: Verificar Nome do Personagem Ativo")]
    public void DebugCheckActiveCharacterName()
    {
        var activeMember = GetActiveMember();
        if (activeMember == null)
        {
            Debug.LogError("❌ Nenhum personagem ativo!");
            return;
        }
        
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  🎮 PARTY MANAGER - DIAGNÓSTICO DO NOME             ║");
        Debug.Log("╠═══════════════════════════════════════════════════════╣");
        Debug.Log($"║  Character asset: {activeMember.name}");
        Debug.Log($"║  Character.characterName: '{activeMember.characterName}'");
        Debug.Log($"║");
        
        // 1. Verificar GameDataManager
        if (GameDataManager.Instance != null)
        {
            int instanceID = GameDataManager.Instance.GetCurrentGameInstanceID();
            Debug.Log($"║  Instance ID atual: {instanceID}");
            
            // 2. Verificar PlayerPrefs
            if (instanceID != -1)
            {
                string instanceKey = $"Instance_{instanceID}_PlayerName";
                string savedName = PlayerPrefs.GetString(instanceKey, "NOT FOUND");
                Debug.Log($"║  PlayerPrefs key: '{instanceKey}'");
                Debug.Log($"║  PlayerPrefs value: '{savedName}'");
            }
            
            // 3. Verificar GameData
            var gameData = GameDataManager.Instance.GetCurrentGameData();
            if (gameData?.playerData != null)
            {
                Debug.Log($"║  GameData.playerName: '{gameData.playerData.playerName}'");
            }
            else
            {
                Debug.Log($"║  GameData: NULL");
            }
        }
        else
        {
            Debug.LogError("║  ❌ GameDataManager NULL!");
        }
        
        Debug.Log($"║");
        Debug.Log($"║  🎯 RESULTADO GetPlayerNameForCurrentInstance():");
        string displayName = GetPlayerNameForCurrentInstance();
        Debug.Log($"║     '{displayName}'");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
    }

    [ContextMenu("🎮 Test: Update Inventory UI with Custom Name")]
    public void DebugTestUpdateInventoryUI()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════╗");
        Debug.Log("║  🎮 TESTE: Atualizando UI com nome personalizado     ║");
        Debug.Log("╠═══════════════════════════════════════════════════════╣");
        
        string testName = GetPlayerNameForCurrentInstance();
        Debug.Log($"║  Nome obtido: '{testName}'");
        
        // Forçar atualização da UI
        OnActiveMemberChanged?.Invoke(GetActiveMember());
        
        Debug.Log("║  ✅ UI notificada para atualizar");
        Debug.Log("╚═══════════════════════════════════════════════════════╝");
    }
}