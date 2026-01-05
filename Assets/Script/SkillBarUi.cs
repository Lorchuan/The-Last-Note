using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillBarUI : MonoBehaviour
{
    [Header("技能槽设置")]
    public SkillSlot[] skillSlots = new SkillSlot[4];

    [Header("技能ID映射配置 (关键修复)")]
    // 这里填入你实际想在UI上显示的技能ID，顺序对应 skillSlots[0], [1], [2], [3]
    // 默认是: 2(火爆), 3(牛杂), 4(冰锥), 5(剑光)
    public int[] skillIdMapping = new int[] { 2, 3, 4, 5 };

    [Header("UI样式")]
    public Sprite defaultSkillIcon;
    public Color availableColor = Color.white;
    public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("动画效果")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.2f;
    private Dictionary<int, float> skillPulseTimers = new Dictionary<int, float>();
    private Dictionary<int, float> skillCooldownProgress = new Dictionary<int, float>();

    public SkillManager skillManager;
    private bool isInitialized = false;

    void Start()
    {
        if (!isInitialized) Initialize(skillManager);
    }

    public void Initialize(SkillManager sm)
    {
        if (sm == null) return;
        skillManager = sm;

        // 遍历所有 UI 槽位
        for (int i = 0; i < Mathf.Min(skillSlots.Length, skillIdMapping.Length); i++)
        {
            if (skillSlots[i] != null)
            {
                // 【核心修复】使用映射数组里的真实ID，而不是 i
                int realSkillId = skillIdMapping[i];

                skillSlots[i].skillId = realSkillId;
                skillSlots[i].skillData = skillManager.GetSkillData(realSkillId);

                // 设置默认图标（如果技能数据里没配）
                if (skillSlots[i].skillData != null && skillSlots[i].iconImage != null && defaultSkillIcon != null)
                {
                    // 这里你可以扩展：如果 skillData.icon != null 用 skillData.icon，否则用默认
                    skillSlots[i].iconImage.sprite = defaultSkillIcon;
                }

                // 绑定点击事件
                if (skillSlots[i].skillButton != null)
                {
                    int index = i; // 闭包捕获
                    skillSlots[i].skillButton.onClick.RemoveAllListeners();
                    skillSlots[i].skillButton.onClick.AddListener(() => OnSkillButtonClicked(index));
                }

                // 初始化 UI 状态
                if (skillSlots[i].cooldownText != null) skillSlots[i].cooldownText.gameObject.SetActive(false);
                if (skillSlots[i].cooldownOverlay != null) skillSlots[i].cooldownOverlay.SetActive(false);

                skillCooldownProgress[realSkillId] = 0f;
            }
        }

        isInitialized = true;
    }

    public void UpdateUI()
    {
        if (!isInitialized || skillManager == null) return;
        UpdateSkills();
    }

    public void UpdateSkills()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] == null || skillSlots[i].skillData == null) continue;

            SkillData skill = skillSlots[i].skillData;

            // 1. 更新冷却遮罩
            UpdateCooldownDisplay(skillSlots[i], skill);

            // 2. 更新可用性颜色
            UpdateSkillAvailability(skillSlots[i], skill);

            // 3. 更新脉冲特效
            UpdatePulseEffect(skillSlots[i], skill);
        }
    }

    void UpdateCooldownDisplay(SkillSlot slot, SkillData skill)
    {
        if (slot.cooldownOverlay == null || slot.cooldownText == null) return;

        // 检查冷却状态
        if (skill.isOnCooldown && skill.currentCooldown > 0)
        {
            slot.cooldownOverlay.SetActive(true);
            slot.cooldownText.gameObject.SetActive(true);
            slot.cooldownText.text = skill.currentCooldown.ToString("F1");

            float progress = skill.currentCooldown / skill.cooldown;
            skillCooldownProgress[slot.skillId] = progress;

            Image cooldownImage = slot.cooldownOverlay.GetComponent<Image>();
            if (cooldownImage != null)
            {
                Color c = cooldownImage.color;
                c.a = Mathf.Lerp(0.3f, 0.8f, progress); // 冷却越久越透明，或者反过来
                cooldownImage.color = c;
                cooldownImage.fillAmount = progress; // 也可以做成扇形遮罩
            }
        }
        else
        {
            slot.cooldownOverlay.SetActive(false);
            slot.cooldownText.gameObject.SetActive(false);
            skillCooldownProgress[slot.skillId] = 0f;
        }
    }

    void UpdateSkillAvailability(SkillSlot slot, SkillData skill)
    {
        if (slot.iconImage == null) return;
        bool isAvailable = !skill.isOnCooldown;
        slot.isAvailable = isAvailable;
        slot.iconImage.color = isAvailable ? availableColor : disabledColor;
    }

    void UpdatePulseEffect(SkillSlot slot, SkillData skill)
    {
        // 刚冷却好时触发脉冲
        if (!skill.isOnCooldown && !skillPulseTimers.ContainsKey(slot.skillId))
        {
            skillPulseTimers[slot.skillId] = 0f;
        }

        if (skillPulseTimers.ContainsKey(slot.skillId) && skillPulseTimers[slot.skillId] < Mathf.PI * 2)
        {
            skillPulseTimers[slot.skillId] += Time.deltaTime * pulseSpeed;
            if (slot.iconImage != null)
            {
                float pulse = Mathf.Sin(skillPulseTimers[slot.skillId]) * pulseIntensity;
                slot.iconImage.color = availableColor * (1 + pulse);
            }

            if (skillPulseTimers[slot.skillId] >= Mathf.PI * 2)
            {
                skillPulseTimers.Remove(slot.skillId);
                if (slot.iconImage != null) slot.iconImage.color = availableColor;
            }
        }
    }

    void OnSkillButtonClicked(int slotIndex)
    {
        if (!isInitialized || skillManager == null) return;
        if (slotIndex >= 0 && slotIndex < skillSlots.Length)
        {
            SkillSlot slot = skillSlots[slotIndex];
            if (slot != null && slot.skillData != null && !slot.skillData.isOnCooldown)
            {
                bool success = skillManager.CastSkill(slot.skillId);
                if (success)
                {
                    if (!skillPulseTimers.ContainsKey(slot.skillId)) skillPulseTimers[slot.skillId] = 0f;
                }
            }
        }
    }
}

[System.Serializable]
public class SkillSlot
{
    public int skillId;
    public Image iconImage;
    public GameObject cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public Button skillButton;

    [HideInInspector] public SkillData skillData;
    [HideInInspector] public bool isAvailable = true;
}