// TurnBasedBattleManager.cs - VERSÃO CORRIGIDA
using UnityEngine;
using System.Collections.Generic;

namespace Combat.TurnBased
{
    public class TurnBasedBattleManager : MonoBehaviour
    {
        public static TurnBasedBattleManager Instance { get; private set; }
        
        [Header("Battle Settings")]
        [SerializeField] private BattleState currentState = BattleState.Start;
        [SerializeField] public BattleParty playerParty; // ⭐ Pode ser atribuído dinamicamente
        [SerializeField] public EnemyParty enemyParty;   // ⭐ Pode ser atribuído dinamicamente
        
        [Header("UI References")]
        [SerializeField] private GameObject battleCanvas;
        [SerializeField] private CanvasGroup fadePanel;
        
        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 1f;
        
        // Events
        public System.Action OnBattleStart;
        public System.Action OnBattleEnd;
        public System.Action<BattleState> OnStateChanged;
        
        // Battle Queue
        private Queue<BattleUnit> turnQueue = new Queue<BattleUnit>();
        private BattleUnit currentTurnUnit;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            // Inicialmente escondido
            if (battleCanvas != null)
                battleCanvas.SetActive(false);
                
            Debug.Log("✅ TurnBasedBattleManager iniciado");
        }
        
        // ⭐⭐ NOVO: Método para iniciar batalha com parâmetros dinâmicos
        public void StartBattle(BattleParty players, EnemyParty enemies)
        {
            if (currentState != BattleState.Start) 
            {
                Debug.LogWarning($"⚠️ Não pode iniciar batalha. Estado atual: {currentState}");
                return;
            }
            
            playerParty = players;
            enemyParty = enemies;
            
            Debug.Log($"🎮 Iniciando batalha:");
            Debug.Log($"   Player Party: {playerParty?.name ?? "NULL"}");
            Debug.Log($"   Enemy Party: {enemyParty?.name ?? "NULL"}");
            Debug.Log($"   Player Units: {playerParty?.partyMembers?.Count ?? 0}");
            Debug.Log($"   Enemy Units: {enemyParty?.enemies?.Count ?? 0}");
            
            StartCoroutine(BattleStartSequence());
        }

        /// <summary>
        /// Obtém os BattleUnits do player baseado no CharacterData atual
        /// </summary>
        private List<BattleUnit> GetPlayerBattleUnits()
        {
            List<BattleUnit> playerUnits = new List<BattleUnit>();
            
            // Busca o PlayerController na cena
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            
            if (playerController != null && playerController.IsUsingCharacterSystem())
            {
                // ⭐ Cria BattleUnitData do character atual
                BattleUnitData playerData = playerController.CreateBattleUnitData();
                
                // ⭐ Cria um BattleUnit temporário para a batalha
                BattleUnit playerUnit = new BattleUnit();
                playerData.ApplyToUnit(playerUnit);
                
                playerUnits.Add(playerUnit);
                
                Debug.Log($"[BattleManager] BattleUnit criado: {playerUnit.unitName}");
                Debug.Log($"   ATK: {playerUnit.attack}, DEF: {playerUnit.defense}");
            }
            else
            {
                Debug.LogWarning("[BattleManager] Usando stats padrão para batalha");
                // Cria um BattleUnit padrão como fallback
                BattleUnit defaultUnit = new BattleUnit();
                defaultUnit.unitName = "Hero";
                defaultUnit.maxHP = 100;
                defaultUnit.currentHP = 100;
                defaultUnit.attack = 10;
                defaultUnit.defense = 5;
                playerUnits.Add(defaultUnit);
            }
            
            return playerUnits;
        }
        
        // ⭐⭐ NOVO: Método para verificar se está pronto
        public bool IsReadyForBattle()
        {
            bool ready = playerParty != null && enemyParty != null;
            
            if (!ready)
            {
                Debug.LogWarning($"⚠️ BattleManager não pronto:");
                Debug.LogWarning($"   PlayerParty: {playerParty != null}");
                Debug.LogWarning($"   EnemyParty: {enemyParty != null}");
            }
            
            return ready;
        }
        
        private System.Collections.IEnumerator BattleStartSequence()
        {
            Debug.Log("⚔️ Iniciando batalha por turnos!");
            
            // Verificar se temos os componentes necessários
            if (playerParty == null || enemyParty == null)
            {
                Debug.LogError("❌ PlayerParty ou EnemyParty não configurado!");
                yield break;
            }
            
            // Fade in
            yield return StartCoroutine(FadeIn());
            
            // Inicializar unidades
            playerParty.InitializeParty();
            enemyParty.InitializeParty();
            
            // Mostrar UI
            if (battleCanvas != null)
            {
                battleCanvas.SetActive(true);
                Debug.Log("✅ BattleCanvas ativado");
            }
            
            // Mudar estado
            ChangeState(BattleState.Start);
            
            OnBattleStart?.Invoke();
            
            // Começar primeiro turno
            yield return new WaitForSeconds(1f);
            StartPlayerTurn();
        }
        
        public void EndBattle(bool playerWon)
        {
            StartCoroutine(BattleEndSequence(playerWon));
        }
        
        private System.Collections.IEnumerator BattleEndSequence(bool playerWon)
        {
            Debug.Log(playerWon ? "🎉 Vitória!" : "💔 Derrota...");
            
            ChangeState(playerWon ? BattleState.Win : BattleState.Lose);
            
            yield return new WaitForSeconds(2f);
            
            // Fade out
            yield return StartCoroutine(FadeOut());
            
            // Esconder UI
            if (battleCanvas != null)
            {
                battleCanvas.SetActive(false);
                Debug.Log("✅ BattleCanvas desativado");
            }
            
            // Limpar queue
            turnQueue.Clear();
            currentTurnUnit = null;
            
            ChangeState(BattleState.Start);
            
            OnBattleEnd?.Invoke();
            
            Debug.Log("✅ Batalha finalizada - aguardando GameController");
            
            // ⭐⭐ CORREÇÃO CRÍTICA: Chamar GameController para retornar ao FreeRoam
            if (GameController.Instance != null)
            {
                Debug.Log("🔄 Chamando GameController.ReturnToFreeRoam()");
                GameController.Instance.ReturnToFreeRoam();
            }
            else
            {
                Debug.LogError("❌ GameController.Instance é NULL!");
            }
        }
        
        // ⭐⭐ NOVO: Método para limpar referências (chamado pelo GameController)
        public void CleanupAfterBattle()
        {
            Debug.Log("🧹 Limpando referências do BattleManager");
            playerParty = null;
            enemyParty = null;
        }
        
        private void StartPlayerTurn()
        {
            ChangeState(BattleState.PlayerTurn);
            Debug.Log("🎮 Turno do Jogador!");
            
            // TODO: Ativar UI de seleção de ações
        }
        
        private void StartEnemyTurn()
        {
            ChangeState(BattleState.EnemyTurn);
            Debug.Log("👾 Turno do Inimigo!");
            
            // Processar ações dos inimigos
            ProcessEnemyActions();
        }
        
        private void ProcessEnemyActions()
        {
            if (enemyParty == null) 
            {
                Debug.LogError("❌ EnemyParty é null em ProcessEnemyActions!");
                return;
            }
            
            var enemies = enemyParty.GetAliveUnits();
            var allies = playerParty?.GetAliveUnits();
            
            Debug.Log($"=== PROCESSANDO AÇÕES DOS INIMIGOS ===");
            Debug.Log($"Inimigos vivos: {enemies.Length}");
            Debug.Log($"Aliados vivos: {allies?.Length ?? 0}");
            
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    if (allies == null || allies.Length == 0)
                    {
                        Debug.LogError("❌ Nenhum aliado vivo para atacar!");
                        continue;
                    }
                    
                    var action = enemy.SelectAction(enemies, allies);
                    
                    if (action.IsValid())
                    {
                        Debug.Log($"   {enemy.unitName} → {action.target?.unitName} (Ataque: {action.isAttack})");
                        action.Execute();
                    }
                    else
                    {
                        Debug.LogWarning($"   Ação inválida de {enemy.unitName}");
                    }
                }
            }
            
            // Verificar vitória/derrota
            CheckBattleEnd();
            
            // Voltar ao turno do jogador
            if (currentState == BattleState.EnemyTurn)
                StartPlayerTurn();
        }
        
        public void PlayerActionSelected(BattleAction action)
        {
            if (currentState != BattleState.PlayerTurn) 
            {
                Debug.LogWarning($"⚠️ Não é turno do jogador! Estado: {currentState}");
                return;
            }
            
            ChangeState(BattleState.Busy);
            
            // Executar ação
            if (action.IsValid())
            {
                Debug.Log($"🎯 Ação do jogador: {action.user?.unitName} → {action.target?.unitName}");
                action.Execute();
                
                // Verificar vitória/derrota
                CheckBattleEnd();
                
                if (currentState != BattleState.Win && currentState != BattleState.Lose)
                {
                    // Passar para turno do inimigo
                    StartEnemyTurn();
                }
            }
            else
            {
                Debug.LogError("❌ Ação do jogador inválida!");
                StartPlayerTurn(); // Voltar ao turno do jogador
            }
        }
        
        private void CheckBattleEnd()
        {
            if (playerParty == null || enemyParty == null) 
            {
                Debug.LogError("❌ Party não configurado em CheckBattleEnd!");
                return;
            }
            
            bool playersDead = playerParty.AreAllDead();
            bool enemiesDead = enemyParty.AreAllDead();
            
            Debug.Log($"🔍 Verificando fim de batalha:");
            Debug.Log($"   Players mortos: {playersDead}");
            Debug.Log($"   Enemies mortos: {enemiesDead}");
            
            if (playersDead)
            {
                EndBattle(false); // Derrota
            }
            else if (enemiesDead)
            {
                EndBattle(true); // Vitória
            }
        }
        
        private void ChangeState(BattleState newState)
        {
            BattleState previousState = currentState;
            currentState = newState;
            
            Debug.Log($"🔄 Estado da batalha: {previousState} → {newState}");
            OnStateChanged?.Invoke(newState);
        }
        
        private System.Collections.IEnumerator FadeIn()
        {
            if (fadePanel == null) yield break;
            
            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            fadePanel.alpha = 1f;
        }
        
        private System.Collections.IEnumerator FadeOut()
        {
            if (fadePanel == null) yield break;
            
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
        
        public void HandleUpdate()
        {
            // Lógica de update por frame (para inputs, etc.)
            if (currentState == BattleState.PlayerTurn)
            {
                // Processar inputs do jogador
            }
        }
        
        // Método para teste rápido
        [ContextMenu("Testar Batalha")]
        public void TestBattle()
        {
            if (playerParty == null || enemyParty == null)
            {
                Debug.LogWarning("⚠️ Configure playerParty e enemyParty no Inspector!");
                return;
            }
            
            StartBattle(playerParty, enemyParty);
        }
        
        [ContextMenu("Verificar Status")]
        public void DebugStatus()
        {
            Debug.Log("=== TURN BASED BATTLE MANAGER STATUS ===");
            Debug.Log($"Estado: {currentState}");
            Debug.Log($"PlayerParty: {playerParty?.name ?? "NULL"}");
            Debug.Log($"EnemyParty: {enemyParty?.name ?? "NULL"}");
            Debug.Log($"BattleCanvas: {battleCanvas?.name ?? "NULL"}");
            Debug.Log($"Pronto para batalha: {IsReadyForBattle()}");
        }
    }
}