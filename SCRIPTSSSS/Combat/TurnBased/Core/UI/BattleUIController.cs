// BattleUIController.cs - VERSÃO COM DEBUG
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace Combat.TurnBased
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject actionPanel;
        public Button attackButton;
        public Button skillsButton;
        public Button defendButton;
        public Button itemsButton;
        public Button runButton;
        
        public GameObject skillPanel;
        public Transform skillButtonContainer;
        public GameObject skillButtonPrefab;
        
        public GameObject targetPanel;
        public Transform targetButtonContainer;
        public GameObject targetButtonPrefab;
        
        [Header("Unit Info")]
        public GameObject unitInfoPanel;
        public Transform unitInfoContainer;
        public GameObject unitInfoPrefab;
        
        [Header("Messages")]
        public TMP_Text battleMessage;
        public GameObject messagePanel;
        
        private TurnBasedBattleManager battleManager;
        private BattleUnit currentSelectingUnit;
        private List<SkillData> availableSkills = new List<SkillData>();
        
        private void Start()
        {
            Debug.Log("🔍 BUSCANDO BATTLEMANAGER...");
            
            // Método 1: Pelo nome exato
            GameObject battleObj = GameObject.Find("BattleManager");
            if (battleObj != null)
            {
                battleManager = battleObj.GetComponent<TurnBasedBattleManager>();
                Debug.Log($"✅ Encontrado pelo nome: {battleObj.name}");
            }
            
            // Método 2: Lista TODOS os objetos na cena
            if (battleManager == null)
            {
                Debug.Log("📋 Listando todos os GameObjects na cena:");
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Battle") || obj.name.Contains("Manager"))
                    {
                        Debug.Log($"- {obj.name} (Tem TurnBasedBattleManager: {obj.GetComponent<TurnBasedBattleManager>() != null})");
                    }
                }
            }
            
            if (battleManager != null)
            {
                battleManager.OnStateChanged += OnBattleStateChanged;
                Debug.Log($"🎯 CONECTADO: {battleManager.gameObject.name}");
                Debug.Log($"📡 Evento OnStateChanged conectado: {battleManager.OnStateChanged != null}");
            }
            else
            {
                Debug.LogError("💥 NÃO ENCONTRADO!");
            }
            
            HideAllPanels();
            SetupButtons();
            
            Debug.Log("✅ BattleUIController iniciado");
        }
        
        private void SetupButtons()
        {
            attackButton.onClick.AddListener(() => OnAttackSelected());
            skillsButton.onClick.AddListener(() => OnSkillsSelected());
            defendButton.onClick.AddListener(() => OnDefendSelected());
            itemsButton.onClick.AddListener(() => OnItemsSelected());
            runButton.onClick.AddListener(() => OnRunSelected());
        }
        
        private void OnBattleStateChanged(BattleState state)
        {
            Debug.Log($"🔄 BattleUIController - Estado recebido: {state}");
            
            switch (state)
            {
                case BattleState.Start:
                    Debug.Log("   🎯 Chamando HideAllPanels()");
                    HideAllPanels();
                    break;
                    
                case BattleState.PlayerTurn:
                    Debug.Log("   🎯 Chamando ShowActionPanel()");
                    ShowActionPanel();
                    Debug.Log("   🎯 Chamando UpdateUnitInfo()");
                    UpdateUnitInfo();
                    break;
                    
                case BattleState.EnemyTurn:
                    Debug.Log("   🎯 Chamando HideAllPanels() - EnemyTurn");
                    HideAllPanels();
                    break;
                    
                case BattleState.Busy:
                    Debug.Log("   🎯 Chamando HideAllPanels() - Busy");
                    HideAllPanels();
                    break;
                    
                case BattleState.Win:
                    ShowMessage("Vitória!");
                    break;
                    
                case BattleState.Lose:
                    ShowMessage("Derrota...");
                    break;
                    
                case BattleState.Run:
                    ShowMessage("Fuga bem sucedida!");
                    break;
            }
        }
        
        private void ShowActionPanel()
        {
            Debug.Log("📱 ShowActionPanel() chamado");
            HideAllPanels();
            if (actionPanel != null)
            {
                actionPanel.SetActive(true);
                Debug.Log($"✅ ActionPanel ativado: {actionPanel.activeSelf}");
                
                // Forçar atualização do layout
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(actionPanel.GetComponent<RectTransform>());
            }
            else
            {
                Debug.LogError("❌ ActionPanel é null!");
            }
        }
        
        private void OnAttackSelected()
        {
            Debug.Log("⚔️ OnAttackSelected()");
            currentSelectingUnit = GetCurrentPlayerUnit();
            if (currentSelectingUnit != null)
            {
                ShowTargetSelection(false);
            }
        }
        
        private void OnSkillsSelected()
        {
            Debug.Log("🔮 OnSkillsSelected()");
            currentSelectingUnit = GetCurrentPlayerUnit();
            if (currentSelectingUnit != null)
            {
                ShowSkillSelection();
            }
        }
        
        private void OnDefendSelected()
        {
            Debug.Log("🛡️ OnDefendSelected()");
            var action = new BattleAction
            {
                user = GetCurrentPlayerUnit(),
                isDefend = true
            };
            
            if (battleManager != null)
            {
                battleManager.PlayerActionSelected(action);
            }
        }
        
        private void OnItemsSelected()
        {
            Debug.Log("🎒 OnItemsSelected()");
            // TODO: Implementar seleção de itens
            Debug.Log("Sistema de itens ainda não implementado");
            ShowMessage("Sistema de itens em desenvolvimento...");
        }
        
        private void OnRunSelected()
        {
            Debug.Log("🏃 OnRunSelected()");
            // TODO: Implementar sistema de fuga
            var action = new BattleAction
            {
                user = GetCurrentPlayerUnit(),
                isRun = true
            };
            
            if (battleManager != null)
            {
                battleManager.PlayerActionSelected(action);
                ShowMessage("Tentando fugir...");
            }
        }
        
        private void ShowSkillSelection()
        {
            Debug.Log("📱 ShowSkillSelection()");
            HideAllPanels();
            skillPanel.SetActive(true);
            
            // Limpar botões anteriores
            foreach (Transform child in skillButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Criar botões para cada habilidade
            availableSkills.Clear();
            
            if (currentSelectingUnit != null && currentSelectingUnit.skills != null && skillButtonPrefab != null)
            {
                foreach (var skill in currentSelectingUnit.skills)
                {
                    if (skill != null && currentSelectingUnit.currentMP >= skill.mpCost)
                    {
                        availableSkills.Add(skill);
                        var buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
                        var button = buttonObj.GetComponent<Button>();
                        var text = buttonObj.GetComponentInChildren<Text>();
                        
                        if (text != null)
                        {
                            text.text = $"{skill.skillName} (MP: {skill.mpCost})";
                        }
                        
                        // Para closure
                        SkillData capturedSkill = skill;
                        button.onClick.AddListener(() => OnSkillSelected(capturedSkill));
                    }
                }
            }
            
            // Botão de voltar
            if (skillButtonPrefab != null)
            {
                var backButton = Instantiate(skillButtonPrefab, skillButtonContainer);
                var backBtn = backButton.GetComponent<Button>();
                var backText = backButton.GetComponentInChildren<Text>();
                if (backText != null)
                {
                    backText.text = "Voltar";
                }
                backBtn.onClick.AddListener(() => ShowActionPanel());
            }
            
            Debug.Log($"✅ SkillPanel ativado com {availableSkills.Count} habilidades");
        }
        
        private void OnSkillSelected(SkillData skill)
        {
            Debug.Log($"🔮 OnSkillSelected: {skill?.skillName}");
            if (skill != null)
            {
                ShowTargetSelection(true, skill);
            }
        }
        
        private void ShowTargetSelection(bool isSkill, SkillData skill = null)
        {
            Debug.Log($"📱 ShowTargetSelection(isSkill: {isSkill})");
            HideAllPanels();
            targetPanel.SetActive(true);
            
            // Limpar botões anteriores
            foreach (Transform child in targetButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Verificar se battleManager existe
            if (battleManager == null)
            {
                Debug.LogError("BattleManager não encontrado!");
                return;
            }
            
            BattleUnit[] targets = new BattleUnit[0];
            
            if (isSkill && skill != null)
            {
                if (skill.CanTargetEnemies())
                {
                    if (battleManager.enemyParty != null)
                    {
                        targets = battleManager.enemyParty.GetAliveUnits();
                    }
                }
                else if (skill.CanTargetAllies())
                {
                    if (battleManager.playerParty != null)
                    {
                        targets = battleManager.playerParty.GetAliveUnits();
                    }
                }
                else
                {
                    targets = new BattleUnit[] { currentSelectingUnit };
                }
            }
            else
            {
                // Ataque normal - apenas inimigos
                if (battleManager.enemyParty != null)
                {
                    targets = battleManager.enemyParty.GetAliveUnits();
                }
            }
            
            // Criar botões para cada alvo
            if (targetButtonPrefab != null)
            {
                foreach (var target in targets)
                {
                    if (target != null && target.IsAlive())
                    {
                        var buttonObj = Instantiate(targetButtonPrefab, targetButtonContainer);
                        var button = buttonObj.GetComponent<Button>();
                        var text = buttonObj.GetComponentInChildren<Text>();
                        
                        if (text != null)
                        {
                            text.text = $"{target.unitName} (HP: {target.currentHP}/{target.maxHP})";
                        }
                        
                        // Para closure
                        BattleUnit capturedTarget = target;
                        button.onClick.AddListener(() => OnTargetSelected(capturedTarget, isSkill, skill));
                    }
                }
                
                // Botão de voltar
                var backButton = Instantiate(targetButtonPrefab, targetButtonContainer);
                var backBtn = backButton.GetComponent<Button>();
                var backText = backButton.GetComponentInChildren<Text>();
                if (backText != null)
                {
                    backText.text = "Voltar";
                }
                backBtn.onClick.AddListener(() => {
                    if (isSkill)
                    {
                        ShowSkillSelection();
                    }
                    else
                    {
                        ShowActionPanel();
                    }
                });
            }
            
            Debug.Log($"✅ TargetPanel ativado com {targets.Length} alvos");
        }
        
        private void OnTargetSelected(BattleUnit target, bool isSkill, SkillData skill)
        {
            Debug.Log($"🎯 OnTargetSelected: {target?.unitName}, isSkill: {isSkill}");
            var action = new BattleAction
            {
                user = currentSelectingUnit,
                target = target,
                skill = isSkill ? skill : null,
                isAttack = !isSkill
            };
            
            if (battleManager != null)
            {
                // ⭐⭐ REMOVER HideAllPanels() daqui
                // HideAllPanels(); // ← REMOVA ESTA LINHA
                
                battleManager.PlayerActionSelected(action);
                
                // ⭐⭐ EM VEZ DISSO, podemos apenas esconder painéis específicos
                if (targetPanel != null) targetPanel.SetActive(false);
                if (skillPanel != null) skillPanel.SetActive(false);
                
                Debug.Log("✅ Painéis de seleção fechados, ActionPanel manterá no próximo turno");
            }
        }
        
        private BattleUnit GetCurrentPlayerUnit()
        {
            if (battleManager != null && battleManager.playerParty != null)
            {
                var aliveUnits = battleManager.playerParty.GetAliveUnits();
                if (aliveUnits.Length > 0)
                {
                    Debug.Log($"🎮 Unidade atual do jogador: {aliveUnits[0]?.unitName}");
                    return aliveUnits[0];
                }
            }
            
            Debug.LogWarning("Nenhum personagem jogável encontrado!");
            return null;
        }
        
        private void UpdateUnitInfo()
        {
            Debug.Log("📊 UpdateUnitInfo()");
            
            if (unitInfoContainer == null || unitInfoPrefab == null)
            {
                Debug.LogWarning("UnitInfoContainer ou UnitInfoPrefab não configurado!");
                return;
            }
            
            // Limpar informações anteriores
            foreach (Transform child in unitInfoContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Atualizar informações dos aliados
            if (battleManager != null && battleManager.playerParty != null)
            {
                foreach (var unit in battleManager.playerParty.partyMembers)
                {
                    if (unit != null)
                    {
                        var infoObj = Instantiate(unitInfoPrefab, unitInfoContainer);
                        var texts = infoObj.GetComponentsInChildren<Text>();
                        
                        if (texts.Length >= 2)
                        {
                            texts[0].text = unit.unitName;
                            texts[1].text = $"HP: {unit.currentHP}/{unit.maxHP} MP: {unit.currentMP}/{unit.maxMP}";
                            
                            // Barra de HP
                            var hpFill = infoObj.GetComponentInChildren<Image>();
                            if (hpFill != null)
                            {
                                hpFill.fillAmount = (float)unit.currentHP / unit.maxHP;
                            }
                        }
                    }
                }
                Debug.Log($"✅ UnitInfo atualizado: {battleManager.playerParty.partyMembers.Count} membros");
            }
        }
        
        private void ShowMessage(string message)
        {
            Debug.Log($"💬 ShowMessage: {message}");
            
            if (battleMessage != null)
            {
                battleMessage.text = message;
            }
            
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
                Invoke("HideMessage", 2f);
            }
        }
        
        private void HideMessage()
        {
            Debug.Log("💬 HideMessage()");
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
        }
        
        private void HideAllPanels()
        {
            Debug.Log("📱 HideAllPanels() chamado");
            
            if (actionPanel != null)
            {
                Debug.Log($"   ActionPanel estava: {actionPanel.activeSelf}");
                actionPanel.SetActive(false);
            }
            if (skillPanel != null) 
            {
                Debug.Log($"   SkillPanel estava: {skillPanel.activeSelf}");
                skillPanel.SetActive(false);
            }
            if (targetPanel != null) 
            {
                Debug.Log($"   TargetPanel estava: {targetPanel.activeSelf}");
                targetPanel.SetActive(false);
            }
            if (messagePanel != null) 
            {
                Debug.Log($"   MessagePanel estava: {messagePanel.activeSelf}");
                messagePanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log("♻️ BattleUIController OnDestroy()");
            if (battleManager != null)
            {
                battleManager.OnStateChanged -= OnBattleStateChanged;
                Debug.Log("📡 Evento OnStateChanged desconectado");
            }
        }
        
        [ContextMenu("Testar UI de Batalha")]
        public void DebugTestBattleUI()
        {
            Debug.Log("=== TESTANDO BATTLE UI ===");
            Debug.Log($"Action Panel: {actionPanel != null}");
            Debug.Log($"Skill Panel: {skillPanel != null}");
            Debug.Log($"Target Panel: {targetPanel != null}");
            Debug.Log($"Message Panel: {messagePanel != null}");
            Debug.Log($"Battle Message: {battleMessage != null}");
        
            // Busca o BattleManager AGORA, não usa a variável privada
            TurnBasedBattleManager currentManager = FindFirstObjectByType<TurnBasedBattleManager>();
            Debug.Log($"Battle Manager: {currentManager != null}");
        
            if (battleMessage != null)
            {
                ShowMessage("Teste de Mensagem!");
            }
        }
        
        [ContextMenu("Verificar Conexão com BattleManager")]
        public void DebugCheckConnection()
        {
            Debug.Log("=== VERIFICAÇÃO DE CONEXÃO ===");
            Debug.Log($"BattleManager: {battleManager != null}");
            
            if (battleManager != null)
            {
                Debug.Log($"   Nome: {battleManager.gameObject.name}");
                
                // Tentar acessar o estado atual via reflection
                var field = battleManager.GetType().GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var state = field.GetValue(battleManager);
                    Debug.Log($"   Estado atual (via reflection): {state}");
                }
                
                Debug.Log("   Verificando eventos...");
                
                // Verificar manualmente se o evento está conectado
                if (battleManager.OnStateChanged != null)
                {
                    Debug.Log("   ✅ OnStateChanged tem subscribers");
                }
                else
                {
                    Debug.LogWarning("   ⚠️ OnStateChanged não tem subscribers");
                }
            }
            else
            {
                Debug.LogError("   ❌ BattleManager não encontrado!");
                
                // Tentar encontrar novamente
                battleManager = FindFirstObjectByType<TurnBasedBattleManager>();
                if (battleManager != null)
                {
                    Debug.Log($"   ✅ Reencontrado: {battleManager.gameObject.name}");
                    
                    // Reconectar evento
                    battleManager.OnStateChanged -= OnBattleStateChanged;
                    battleManager.OnStateChanged += OnBattleStateChanged;
                    Debug.Log("   📡 Evento reconectado");
                }
            }
            
            // Verificar painéis
            Debug.Log("=== VERIFICAÇÃO DE PAINÉIS ===");
            Debug.Log($"ActionPanel: {actionPanel?.name ?? "NULL"}");
            Debug.Log($"SkillPanel: {skillPanel?.name ?? "NULL"}");
            Debug.Log($"TargetPanel: {targetPanel?.name ?? "NULL"}");
        }
        
        [ContextMenu("Simular Segundo Turno do Jogador")]
        public void SimulateSecondPlayerTurn()
        {
            Debug.Log("=== SIMULANDO SEGUNDO TURNO DO JOGADOR ===");
            
            // Simular chamada direta
            OnBattleStateChanged(BattleState.PlayerTurn);
            
            // Verificar se os painéis estão ativos
            Debug.Log($"ActionPanel ativo após simulação: {actionPanel?.activeSelf}");
            Debug.Log($"Canvas ativo: {GetComponent<Canvas>()?.gameObject.activeSelf}");
        }
    }
}