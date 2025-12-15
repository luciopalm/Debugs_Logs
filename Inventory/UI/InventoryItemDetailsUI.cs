using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemDetailsUI : MonoBehaviour
{
    [Header("Item Info References")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemRarityText;
    [SerializeField] private Image itemIconImage;
    
    [Header("Item Stats Panel")]
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text magicAttackText;
    [SerializeField] private TMP_Text magicDefenseText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text requiredLevelText;
    
    [Header("Party Member Stats Panel")]
    [SerializeField] private TMP_Text partyMemberHeader;
    [SerializeField] private TMP_Text memberLevelText;
    [SerializeField] private TMP_Text memberHealthText;
    [SerializeField] private TMP_Text memberManaText;
    [SerializeField] private TMP_Text memberAttackText;
    [SerializeField] private TMP_Text memberDefenseText;
    [SerializeField] private TMP_Text memberWeightText;
    [SerializeField] private TMP_Text memberSpeedText;
    [SerializeField] private TMP_Text memberMagicAttackText;
    [SerializeField] private TMP_Text memberMagicDefenseText;
    
    [Header("Description Panel")]
    [SerializeField] private TMP_Text descriptionText;
    
    [Header("Colors for Rarity")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.2f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f);
    
    private InventoryUI inventoryUI;
    private PartyManager partyManager;
    
    private void Start()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();
        partyManager = PartyManager.Instance;
        
        if (partyManager == null)
        {
            Debug.LogError("[ItemDetailsUI] PartyManager not found!");
            return;
        }
        
        partyManager.OnActiveMemberChanged += OnActiveMemberChanged;
        
        ClearItemDetails();
        UpdatePartyMemberStats();
    }
    
    private void OnDestroy()
    {
        if (partyManager != null)
        {
            partyManager.OnActiveMemberChanged -= OnActiveMemberChanged;
        }
    }
    
    private void OnActiveMemberChanged(CharacterData newActiveMember)
    {
        UpdatePartyMemberStats();
    }
    
    public void ShowItemDetails(ItemData item)
    {
        if (item == null)
        {
            ClearItemDetails();
            return;
        }
        
        // Item Name and Rarity
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.color = GetRarityColor(item.rarity);
        }
        
        if (itemRarityText != null)
        {
            itemRarityText.text = $"[{item.rarity.ToString().ToUpper()}]";
            itemRarityText.color = GetRarityColor(item.rarity);
        }
        
        // Item Icon
        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.icon;
            itemIconImage.color = item.icon != null ? Color.white : new Color(1, 1, 1, 0.1f);
        }
        
        // Basic Info
        if (typeText != null)
            typeText.text = $"Type: {GetItemTypeDisplay(item)}";
        
        if (weightText != null)
            weightText.text = $"Weight: {item.weight:F1} kg";
        
        // Combat Stats
        if (attackText != null)
            attackText.text = item.attackBonus != 0 ? $"ATK: +{item.attackBonus}" : "ATK: -";
        
        if (defenseText != null)
            defenseText.text = item.defenseBonus != 0 ? $"DEF: +{item.defenseBonus}" : "DEF: -";
        
        if (magicAttackText != null)
            magicAttackText.text = item.magicAttackBonus != 0 ? $"M.ATK: +{item.magicAttackBonus}" : "M.ATK: -";
        
        if (magicDefenseText != null)
            magicDefenseText.text = item.magicDefenseBonus != 0 ? $"M.DEF: +{item.magicDefenseBonus}" : "M.DEF: -";
        
        if (speedText != null)
            speedText.text = item.speedBonus != 0 ? $"SPD: +{item.speedBonus}" : "SPD: -";
        
        // Requirements
        if (requiredLevelText != null)
            requiredLevelText.text = item.requiredLevel > 1 ? $"Req. Level: {item.requiredLevel}" : "Req. Level: 1";
        
        // Description
        if (descriptionText != null)
        {
            if (!string.IsNullOrEmpty(item.description))
                descriptionText.text = item.description;
            else if (!string.IsNullOrEmpty(item.flavorText))
                descriptionText.text = $"<i>{item.flavorText}</i>";
            else
                descriptionText.text = "No description available.";
        }
    }
    
    public void ClearItemDetails()
    {
        if (itemNameText != null) itemNameText.text = "ITEM NAME";
        if (itemRarityText != null) itemRarityText.text = "[COMMON]";
        if (itemIconImage != null) 
        {
            itemIconImage.sprite = null;
            itemIconImage.color = new Color(1, 1, 1, 0.1f);
        }
        
        if (typeText != null) typeText.text = "Type: -";
        if (weightText != null) weightText.text = "Weight: - kg";
        if (attackText != null) attackText.text = "ATK: -";
        if (defenseText != null) defenseText.text = "DEF: -";
        if (magicAttackText != null) magicAttackText.text = "M.ATK: -";
        if (magicDefenseText != null) magicDefenseText.text = "M.DEF: -";
        if (speedText != null) speedText.text = "SPD: -";
        if (requiredLevelText != null) requiredLevelText.text = "Req. Level: -";
        if (descriptionText != null) descriptionText.text = "Select an item to see its details.";
    }
    
    public void UpdatePartyMemberStats()
    {
        CharacterData activeMember = null;
        
        if (partyManager != null)
        {
            activeMember = partyManager.GetActiveMember();
        }
        
        if (activeMember == null)
        {
            SetDefaultPartyStats();
            return;
        }
        
        // Update header
        if (partyMemberHeader != null)
        {
            partyMemberHeader.text = activeMember.characterName.ToUpper();
        }
        
        // Update stats
        if (memberLevelText != null)
            memberLevelText.text = $"Level: {activeMember.currentLevel}";
        
        if (memberHealthText != null)
            memberHealthText.text = $"HP: {activeMember.currentHP}/{activeMember.GetCurrentMaxHP()}";
        
        if (memberManaText != null)
            memberManaText.text = $"MP: {activeMember.currentMP}/{activeMember.GetCurrentMaxMP()}";
        
        int totalAttack = activeMember.GetCurrentAttack();
        int totalDefense = activeMember.GetCurrentDefense();
        int totalMagicAttack = activeMember.GetCurrentMagicAttack();
        int totalMagicDefense = activeMember.GetCurrentMagicDefense();
        int totalSpeed = activeMember.GetCurrentSpeed();
        
        if (memberAttackText != null)
            memberAttackText.text = $"Attack: {totalAttack}";
        
        if (memberDefenseText != null)
            memberDefenseText.text = $"Defense: {totalDefense}";
        
        if (memberMagicAttackText != null)
            memberMagicAttackText.text = $"M.Atk: {totalMagicAttack}";
        
        if (memberMagicDefenseText != null)
            memberMagicDefenseText.text = $"M.Def: {totalMagicDefense}";
        
        if (memberSpeedText != null)
            memberSpeedText.text = $"Speed: {totalSpeed}";
        
        float currentWeight = InventoryManager.Instance != null ? InventoryManager.Instance.CurrentWeight : 0f;
        float maxWeight = InventoryManager.Instance != null ? InventoryManager.Instance.MaxWeight : 100f;
        
        if (memberWeightText != null)
            memberWeightText.text = $"Weight: {currentWeight:F1}/{maxWeight:F1} kg";
    }
    
    private void SetDefaultPartyStats()
    {
        if (partyMemberHeader != null) partyMemberHeader.text = "ACTIVE PARTY MEMBER";
        if (memberLevelText != null) memberLevelText.text = "Level: 1";
        if (memberHealthText != null) memberHealthText.text = "HP: 50/100";
        if (memberManaText != null) memberManaText.text = "MP: 30/50";
        if (memberAttackText != null) memberAttackText.text = "Attack: 10";
        if (memberDefenseText != null) memberDefenseText.text = "Defense: 8";
        if (memberMagicAttackText != null) memberMagicAttackText.text = "M.Atk: 5";
        if (memberMagicDefenseText != null) memberMagicDefenseText.text = "M.Def: 4";
        if (memberSpeedText != null) memberSpeedText.text = "Speed: 12";
        
        float currentWeight = InventoryManager.Instance != null ? InventoryManager.Instance.CurrentWeight : 0f;
        float maxWeight = InventoryManager.Instance != null ? InventoryManager.Instance.MaxWeight : 100f;
        if (memberWeightText != null) memberWeightText.text = $"Weight: {currentWeight:F1}/{maxWeight:F1} kg";
    }
    
    private Color GetRarityColor(ItemData.ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemData.ItemRarity.Common: return commonColor;
            case ItemData.ItemRarity.Uncommon: return uncommonColor;
            case ItemData.ItemRarity.Rare: return rareColor;
            case ItemData.ItemRarity.Epic: return epicColor;
            case ItemData.ItemRarity.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }
    
    private string GetItemTypeDisplay(ItemData item)
    {
        if (item.IsEquipment())
        {
            if (item.IsWeapon()) return "Weapon";
            if (item.IsArmor()) return "Armor";
            if (item.IsAccessory()) return "Accessory";
            return "Equipment";
        }
        
        return item.itemType.ToString();
    }
    
    public void NextPartyMember()
    {
        if (partyManager != null)
        {
            partyManager.NextMember();
        }
    }
    
    public void PreviousPartyMember()
    {
        if (partyManager != null)
        {
            partyManager.PreviousMember();
        }
    }
    
    public void OnEquipmentChanged()
    {
        UpdatePartyMemberStats();
    }
    
    public void OnWeightChanged(float currentWeight, float maxWeight)
    {
        if (memberWeightText != null)
            memberWeightText.text = $"Weight: {currentWeight:F1}/{maxWeight:F1} kg";
    }

    public void ShowMultipleItemsSummary(List<ItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            ClearItemDetails();
            return;
        }
        
        if (itemNameText != null)
        {
            itemNameText.text = $"{items.Count} Items Selected";
            itemNameText.color = Color.yellow;
        }
        
        if (itemRarityText != null)
        {
            itemRarityText.text = $"[MULTI-SELECTION]";
            itemRarityText.color = Color.yellow;
        }
        
        if (descriptionText != null)
        {
            string itemList = "";
            for (int i = 0; i < Mathf.Min(items.Count, 5); i++)
            {
                itemList += $"• {items[i].itemName}\n";
            }
            if (items.Count > 5)
            {
                itemList += $"... and {items.Count - 5} more";
            }
            descriptionText.text = itemList;
        }
    }
}