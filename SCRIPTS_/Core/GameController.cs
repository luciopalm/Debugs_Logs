using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Combat.TurnBased;
using System.Collections;
using System.Collections.Generic;

public enum GameState { FreeRoam, Dialog, TurnBasedBattle }

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    
    [SerializeField] PlayerController playerController;
    [SerializeField] BoatController boatController;
    [SerializeField] CameraManager cameraManager;
    
    [Header("Turn-Based Battle System")]
    [SerializeField] private BattleParty playerBattlePartyPrefab;
    [SerializeField] private GameObject battleCanvas;
    [SerializeField] private Transform battleSpawnPoint;
    [SerializeField] private CanvasGroup transitionPanel;
    [SerializeField] private float battleTransitionTime = 1f;
    
    [Header("Battle Units")]
    [SerializeField] private BattleUnit playerBattleUnitPrefab;
    
    [Header("Party Management")]
    [SerializeField] private List<BattleUnitData> currentPartyData = new List<BattleUnitData>();
    
    [System.Serializable]
    public class PartyMemberTemplate
    {
        public BattleUnit unitPrefab;
        public CharacterData characterData;
    }
    
    [SerializeField] private PartyMemberTemplate[] partyTemplates;
    
    private Tilemap waterTilemap;
    private GameState state;
    private Vector3 lastPlayerPosition;
    private Quaternion lastPlayerRotation;
    private bool wasInBoatMode = false;
    
    private BattleParty currentBattleParty;
    private Vector3 lastBoatPosition;
    private Quaternion lastBoatRotation;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Bootstrap garante que Managers existe
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[GameController] CRITICAL: GameDataManager not found!");
            return;
        }
        
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Water tilemap
        GameObject waterObj = GameObject.FindWithTag("Water");
        if (waterObj != null)
        {
            waterTilemap = waterObj.GetComponent<Tilemap>();
            if (waterTilemap != null && playerController != null)
            {
                playerController.SetWaterTilemap(waterTilemap);
            }
        }

        // Dialog manager events
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.OnShowDialog += () => {
                ChangeState(GameState.Dialog);
            };
            DialogManager.Instance.OnHideDialog += () => {
                if (state == GameState.Dialog)
                    ChangeState(GameState.FreeRoam);
            };
        }
        
        // UI setup
        if (transitionPanel != null)
        {
            transitionPanel.gameObject.SetActive(false);
            transitionPanel.alpha = 0f;
        }
        
        if (battleCanvas != null)
        {
            battleCanvas.SetActive(false);
        }
        
        // Party initialization
        if (currentPartyData.Count == 0 && partyTemplates != null && partyTemplates.Length > 0)
        {
            InitializePartyData();
        }
        
        ChangeState(GameState.FreeRoam);
        Debug.Log("[GameController] Initialized");
    }
    
    private void Update()
    {
        switch (state)
        {
            case GameState.FreeRoam:
                HandleFreeRoamInputs();
                break;
                
            case GameState.Dialog:
                if (DialogManager.Instance != null)
                    DialogManager.Instance.HandleUpdate();
                break;
                
            case GameState.TurnBasedBattle:
                if (TurnBasedBattleManager.Instance != null)
                {
                    TurnBasedBattleManager.Instance.HandleUpdate();
                }
                break;
        }
    }
    
    private void HandleFreeRoamInputs()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestTurnBasedBattle();
        }
    }
    
    public void StartTurnBasedBattle(EnemyParty enemyParty)
    {
        if (state == GameState.TurnBasedBattle)
        {
            Debug.LogWarning("[GameController] Already in battle!");
            return;
        }
        
        if (playerController == null || enemyParty == null)
        {
            Debug.LogError("[GameController] PlayerController or EnemyParty missing!");
            return;
        }
        
        StartCoroutine(TransitionToTurnBasedBattle(enemyParty));
    }
    
    private IEnumerator TransitionToTurnBasedBattle(EnemyParty enemyParty)
    {
        SaveCurrentState();
        SetFreeRoamControls(false);
        
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, battleTransitionTime));
        }
        
        ChangeState(GameState.TurnBasedBattle);
        CreateBattleParty();
        
        if (battleCanvas != null)
        {
            battleCanvas.SetActive(true);
        }
        
        PositionBattleUnits();
        
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, battleTransitionTime));
        }
        
        if (TurnBasedBattleManager.Instance != null && currentBattleParty != null && enemyParty != null)
        {
            TurnBasedBattleManager.Instance.StartBattle(currentBattleParty, enemyParty);
        }
        else
        {
            Debug.LogError("[GameController] Missing components for battle!");
            yield return new WaitForSeconds(1f);
            ReturnToFreeRoam();
        }
    }
    
    public void ReturnToFreeRoam()
    {
        StartCoroutine(TransitionFromBattle());
    }
    
    private IEnumerator TransitionFromBattle()
    {
        if (currentBattleParty != null)
        {
            SavePartyDataAfterBattle();
        }
        
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, battleTransitionTime * 0.5f));
        }
        
        CleanupBattle();
        RestoreFreeRoamState();
        SetFreeRoamControls(true);
        
        if (battleCanvas != null)
        {
            battleCanvas.SetActive(false);
        }
        
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, battleTransitionTime * 0.5f));
        }
        
        ChangeState(GameState.FreeRoam);
    }
    
    private IEnumerator FadeTransition(float startAlpha, float targetAlpha, float duration)
    {
        if (transitionPanel == null) yield break;
        
        if (!transitionPanel.gameObject.activeSelf)
            transitionPanel.gameObject.SetActive(true);
        
        float elapsed = 0f;
        transitionPanel.alpha = startAlpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transitionPanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        
        transitionPanel.alpha = targetAlpha;
        
        if (targetAlpha == 0f)
            transitionPanel.gameObject.SetActive(false);
    }
    
    private void SaveCurrentState()
    {
        if (playerController != null)
        {
            lastPlayerPosition = playerController.transform.position;
            lastPlayerRotation = playerController.transform.rotation;
        }
        
        wasInBoatMode = false;
        
        if (boatController != null)
        {
            bool playerDeactivated = (playerController != null && !playerController.gameObject.activeSelf);
            bool boatReportsInside = boatController.isPlayerInside;
            bool boatActive = boatController.gameObject.activeSelf;
            
            wasInBoatMode = playerDeactivated && boatReportsInside && boatActive;
            
            lastBoatPosition = boatController.transform.position;
            lastBoatRotation = boatController.transform.rotation;
        }
    }
    
    private void SetFreeRoamControls(bool active)
    {
        if (playerController != null)
        {
            playerController.enabled = active;
            playerController.canInteract = active;
            
            if (!active)
            {
                playerController.FinishAttack();
                var rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }
        
        if (boatController != null)
        {
            boatController.enabled = active;
        }
        
        if (cameraManager != null)
        {
            cameraManager.enabled = active;
        }
    }
    // ============================================
    // PARTY SYSTEM
    // ============================================
    
    private void InitializePartyData()
    {
        currentPartyData.Clear();
        
        if (partyTemplates != null)
        {
            foreach (var template in partyTemplates)
            {
                if (template.unitPrefab != null)
                {
                    BattleUnitData data = new BattleUnitData(template.unitPrefab);
                    
                    if (template.characterData != null)
                    {
                        data.unitName = template.characterData.characterName;
                        data.maxHP = template.characterData.GetMaxHPAtLevel(1);
                        data.currentHP = data.maxHP;
                        data.maxMP = 50;
                        data.currentMP = data.maxMP;
                        data.attack = template.characterData.GetAttackAtLevel(1);
                        data.defense = template.characterData.baseDefense;
                        data.speed = template.characterData.GetSpeedAtLevel(1);
                    }
                    
                    currentPartyData.Add(data);
                }
            }
        }
    }
    
    private void CreateBattleParty()
    {
        if (currentBattleParty != null)
        {
            Destroy(currentBattleParty.gameObject);
            currentBattleParty = null;
        }
        
        if (playerBattlePartyPrefab != null)
        {
            currentBattleParty = Instantiate(playerBattlePartyPrefab);
            currentBattleParty.transform.name = "BattleParty_Instance";
            currentBattleParty.ClearParty();
            
            if (currentPartyData.Count == 0)
            {
                InitializePartyData();
            }
            
            foreach (var partyMemberData in currentPartyData)
            {
                BattleUnit prefabToUse = FindPrefabForUnit(partyMemberData.unitName);
                
                if (prefabToUse != null)
                {
                    BattleUnit newUnit = Instantiate(prefabToUse, currentBattleParty.transform);
                    newUnit.transform.name = partyMemberData.unitName + "_Battle";
                    
                    partyMemberData.ApplyToUnit(newUnit);
                    newUnit.Initialize();
                    currentBattleParty.AddMember(newUnit);
                }
            }
            
            if (currentBattleParty.partyMembers.Count > 0)
            {
                currentBattleParty.InitializeParty();
            }
            else
            {
                Debug.LogError("[GameController] BattleParty has no members!");
                Destroy(currentBattleParty.gameObject);
                currentBattleParty = null;
            }
        }
    }
    
    private BattleUnit FindPrefabForUnit(string unitName)
    {
        if (partyTemplates != null)
        {
            foreach (var template in partyTemplates)
            {
                if (template.unitPrefab != null && 
                    (template.unitPrefab.unitName == unitName || 
                     (template.characterData != null && template.characterData.characterName == unitName)))
                {
                    return template.unitPrefab;
                }
            }
        }
        
        return playerBattleUnitPrefab;
    }
    
    private void SavePartyDataAfterBattle()
    {
        if (currentBattleParty != null && currentBattleParty.partyMembers != null)
        {
            foreach (var unit in currentBattleParty.partyMembers)
            {
                if (unit != null)
                {
                    BattleUnitData data = GetPartyMemberData(unit.unitName);
                    if (data != null)
                    {
                        data.UpdateFromUnit(unit);
                    }
                }
            }
        }
    }
    
    private BattleUnitData GetPartyMemberData(string unitName)
    {
        foreach (var data in currentPartyData)
        {
            if (data.unitName == unitName)
                return data;
        }
        return null;
    }
    
    private void PositionBattleUnits()
    {
        if (battleSpawnPoint != null && currentBattleParty != null)
        {
            currentBattleParty.transform.position = battleSpawnPoint.position;
            currentBattleParty.transform.rotation = battleSpawnPoint.rotation;
            
            if (currentBattleParty.formationPositions != null && currentBattleParty.formationPositions.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(currentBattleParty.partyMembers.Count, currentBattleParty.formationPositions.Length); i++)
                {
                    if (currentBattleParty.partyMembers[i] != null)
                    {
                        currentBattleParty.partyMembers[i].transform.localPosition = currentBattleParty.formationPositions[i];
                    }
                }
            }
        }
    }
    
    private void CleanupBattle()
    {
        if (currentBattleParty != null)
        {
            foreach (var member in currentBattleParty.partyMembers)
            {
                if (member != null && member.gameObject != null)
                {
                    Destroy(member.gameObject);
                }
            }
            
            Destroy(currentBattleParty.gameObject);
            currentBattleParty = null;
        }
        
        if (TurnBasedBattleManager.Instance != null)
        {
            TurnBasedBattleManager.Instance.CleanupAfterBattle();
        }
    }
    
    private void RestoreFreeRoamState()
    {
        // Always ensure boat is visible and active
        if (boatController != null && !boatController.isBoatDestroyed)
        {
            if (!boatController.gameObject.activeSelf)
            {
                boatController.gameObject.SetActive(true);
            }
            
            boatController.transform.position = lastBoatPosition;
            boatController.transform.rotation = lastBoatRotation;
        }
        
        // Restore player state
        if (playerController != null)
        {
            if (wasInBoatMode)
            {
                // Player was INSIDE boat
                playerController.transform.position = lastBoatPosition;
                playerController.transform.rotation = lastBoatRotation;
                playerController.gameObject.SetActive(false);
                
                if (boatController != null)
                {
                    boatController.isPlayerInside = true;
                    boatController.enabled = true;
                }
                
                if (cameraManager != null)
                {
                    cameraManager.SwitchToBoat();
                    cameraManager.TeleportToTarget();
                }
            }
            else
            {
                // Player was ON FOOT
                playerController.transform.position = lastPlayerPosition;
                playerController.transform.rotation = lastPlayerRotation;
                
                if (!playerController.gameObject.activeSelf)
                {
                    playerController.gameObject.SetActive(true);
                }
                
                if (boatController != null)
                {
                    boatController.isPlayerInside = false;
                }
                
                if (cameraManager != null)
                {
                    cameraManager.SwitchToPlayer();
                    cameraManager.TeleportToTarget();
                }
            }
        }
    }
    
    private void ChangeState(GameState newState)
    {
        state = newState;
    }
    
    public GameState GetCurrentState() => state;
    public PlayerController GetPlayerController() => playerController;
    public bool IsInTurnBasedBattle() => state == GameState.TurnBasedBattle;
    
    public void AddPartyMember(BattleUnitData newMemberData, BattleUnit prefab)
    {
        if (newMemberData != null)
        {
            BattleUnitData existing = GetPartyMemberData(newMemberData.unitName);
            if (existing != null) return;
            
            currentPartyData.Add(newMemberData);
            
            if (prefab != null)
            {
                var newTemplates = new PartyMemberTemplate[partyTemplates.Length + 1];
                partyTemplates.CopyTo(newTemplates, 0);
                newTemplates[partyTemplates.Length] = new PartyMemberTemplate
                {
                    unitPrefab = prefab,
                    characterData = null
                };
                partyTemplates = newTemplates;
            }
        }
    }
    
    [ContextMenu("Test Turn-Based Battle")]
    public void TestTurnBasedBattle()
    {
        if (playerController == null)
        {
            Debug.LogError("[GameController] PlayerController not found!");
            return;
        }
        
        if (state == GameState.TurnBasedBattle)
        {
            Debug.LogError("[GameController] Already in battle!");
            return;
        }
        
        if (playerController.isTouchingWater)
        {
            Debug.LogError("[GameController] Player is in water!");
            return;
        }
        
        if (currentPartyData.Count == 0)
        {
            Debug.LogError("[GameController] No party members!");
            return;
        }
        
        // Create test enemy
        Vector3 spawnPos = playerController.transform.position + new Vector3(10f, 0f, 0f);
        GameObject testEnemies = new GameObject("TestEnemyParty");
        testEnemies.transform.position = spawnPos;
        
        var enemyParty = testEnemies.AddComponent<EnemyParty>();
        
        GameObject enemyObj = new GameObject("Goblin_Warrior");
        enemyObj.transform.parent = testEnemies.transform;
        enemyObj.transform.localPosition = Vector3.zero;
        
        var enemyUnit = enemyObj.AddComponent<EnemyUnit>();
        
        // Try to load EnemyData
        EnemyData goblinData = Resources.Load<EnemyData>("Enemies/Enemy_GoblinWarrior");
        if (goblinData == null)
            goblinData = Resources.Load<EnemyData>("Enemy_GoblinWarrior");
        
        if (goblinData != null)
        {
            enemyUnit.enemyData = goblinData;
            enemyUnit.Initialize();
        }
        else
        {
            // Manual setup
            enemyUnit.unitName = "Goblin Warrior";
            enemyUnit.maxHP = 40;
            enemyUnit.currentHP = 40;
            enemyUnit.maxMP = 20;
            enemyUnit.currentMP = 20;
            enemyUnit.attack = 6;
            enemyUnit.defense = 3;
            enemyUnit.speed = 9;
            
            SkillData powerJump = Resources.Load<SkillData>("Skills/Skill_PowerJump");
            if (powerJump != null)
            {
                enemyUnit.skills = new SkillData[] { powerJump };
            }
            else
            {
                enemyUnit.skills = new SkillData[0];
            }
            
            enemyUnit.Initialize();
        }
        
        var collider = enemyObj.AddComponent<BoxCollider2D>();
        collider.enabled = false;
        
        enemyParty.AddEnemy(enemyUnit);
        enemyParty.InitializeParty();
        
        StartTurnBasedBattle(enemyParty);
    }
    
    [ContextMenu("Force Return to FreeRoam")]
    public void ForceReturnToFreeRoam()
    {
        if (state == GameState.TurnBasedBattle)
        {
            ReturnToFreeRoam();
        }
    }
}
