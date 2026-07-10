using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>แถวสกิลเดี่ยว — แสดง icon/ชื่อ/คำอธิบาย/progress pip/ปุ่ม upgrade/สถานะ lock</summary>
public class SkillEntryRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private HeartMeterUI progressPips; // ใช้ component เดียวกับหัวใจ — concept เดียวกันคือ "N of M filled"
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI lockedReasonText;

    public int SkillId { get; private set; }
    public event Action<int> OnUpgradeClicked;

    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(SkillId));
    }

    public void Setup(SkillDefinitionSO def, int currentLevel, bool canUpgrade, bool meetsPrereq)
    {
        SkillId = def.skillId;

        if (iconImage != null) iconImage.sprite = def.icon;
        if (nameText != null) nameText.text = def.displayName;
        if (descriptionText != null) descriptionText.text = def.description;
        progressPips?.SetHearts(def.maxLevel, currentLevel);

        bool maxed = currentLevel >= def.maxLevel;
        bool locked = !meetsPrereq;

        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (locked && lockedReasonText != null && def.requiredSkill != null)
            lockedReasonText.text = $"Requires {def.requiredSkill.displayName} Lv.{def.requiredSkillLevel}";

        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(!locked && !maxed);
            upgradeButton.interactable = canUpgrade;
        }
        if (upgradeCostText != null)
            upgradeCostText.text = maxed ? "MAX" : $"Upgrade · {def.skillPointCostPerLevel} SP";
    }
}
