using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    [Header("技能列表")]
    public SkillData[] skillList;

    [Header("敌人图层")]
    public LayerMask enemyLayer;

    [Header("音效")]
    public AudioSource audioSource;

    [Header("调试")]
    public bool showDebug = true;
    public bool enableCooldown = true;

    public WandController wandController;
    private Dictionary<int, SkillData> skillDictionary = new Dictionary<int, SkillData>();
    private Transform currentTeleportTarget = null;

    public static SkillManager Instance;

    public event Action<int, float> OnCooldownUpdate;
    public event Action<int> OnSkillCast;

    void Awake()
    {
        Instance = this;
        // 【核心修复】将初始化移到 Awake，确保比 UI 先执行
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.7f;
            audioSource.volume = 0.8f;
        }
        InitializeSkills();
    }

    public void SetTeleportTarget(Transform target)
    {
        currentTeleportTarget = target;
    }

    void Start()
    {
        Debug.Log($"技能管理器初始化完成，已加载 {skillDictionary.Count} 个技能");
    }

    void InitializeSkills()
    {
        if (skillList == null || skillList.Length == 0)
        {
            CreateDefaultSkills();
        }

        skillDictionary.Clear();
        foreach (var skill in skillList)
        {
            skillDictionary[skill.skillId] = skill;
            skill.currentCooldown = 0f;
            skill.isOnCooldown = false;
        }
    }

    void CreateDefaultSkills()
    {
        // 已删除火球术(0)和闪电箭(1)，从2开始
        skillList = new SkillData[]
        {
            // 2: 火爆术
            new SkillData
            {
                skillId = 2,
                skillName = "火爆术",
                skillType = SkillType.AOE_Targeted,
                damage = 25f,
                cooldown = 0.5f,
                aoeRadius = 3f,
                duration = 1f,
                gestureCode = "RL"
            },
            // 3: 斩尽牛杂术
            new SkillData
            {
                skillId = 3,
                skillName = "斩尽牛杂术",
                skillType = SkillType.DOT_Targeted,
                damage = 5f,
                cooldown = 5.0f,
                aoeRadius = 2.5f,
                duration = 3.0f,
                gestureCode = "DRU"
            },
            // 4: 冰锥术
            new SkillData
            {
                skillId = 4,
                skillName = "冰锥术",
                skillType = SkillType.AOE_Targeted,
                damage = 10f,
                cooldown = 10.0f,
                aoeRadius = 4f,
                duration = 1f,
                gestureCode = "DUR"
            },
            // 5: 剑光
            new SkillData
            {
                skillId = 5,
                skillName = "剑光",
                skillType = SkillType.AOE_Self,
                damage = 20f,
                cooldown = 20.0f,
                aoeRadius = 5f,
                duration = 0.5f,
                gestureCode = "CIRCLE"
            },
            // 6: 传送门
            new SkillData
            {
                skillId = 6,
                skillName = "空间传送",
                skillType = SkillType.Instant,
                damage = 0f,
                cooldown = 10.0f,
                aoeRadius = 0f,
                gestureCode = "RDRUR"
            }
        };
    }

    void Update()
    {
        if (enableCooldown) UpdateCooldowns();
        HandleDebugInput();
    }

    void UpdateCooldowns()
    {
        foreach (var skill in skillList)
        {
            if (skill.isOnCooldown)
            {
                skill.currentCooldown -= Time.deltaTime;
                OnCooldownUpdate?.Invoke(skill.skillId, skill.currentCooldown / skill.cooldown);

                if (skill.currentCooldown <= 0)
                {
                    skill.isOnCooldown = false;
                    skill.currentCooldown = 0f;
                }
            }
        }
    }

    void HandleDebugInput()
    {
        // 更新调试键位，去除了F1/F2
        if (Input.GetKeyDown(KeyCode.F3)) CastSkill(2);
        if (Input.GetKeyDown(KeyCode.F4)) CastSkill(3);
        if (Input.GetKeyDown(KeyCode.F5)) CastSkill(4);
        if (Input.GetKeyDown(KeyCode.F6)) CastSkill(5);
        if (Input.GetKeyDown(KeyCode.R)) ResetAllCooldowns();
    }

    public bool CanCastSkill(int skillId)
    {
        if (!skillDictionary.ContainsKey(skillId)) return false;
        SkillData skill = skillDictionary[skillId];

        if (enableCooldown && skill.isOnCooldown) return false;
        if (skill.effectPrefab == null)
        {
            Debug.LogWarning($"技能 {skill.skillName} 缺少 Prefab，请在Inspector中赋值！");
            return false;
        }
        return true;
    }

    public bool CastSkill(int skillId)
    {
        if (!CanCastSkill(skillId)) return false;

        SkillData skill = skillDictionary[skillId];

        if (enableCooldown)
        {
            skill.isOnCooldown = true;
            skill.currentCooldown = skill.cooldown;
        }

        PlayCastSound(skill);
        OnSkillCast?.Invoke(skillId);
        Debug.Log($"释放技能: {skill.skillName} (ID: {skillId})");

        bool success = false;

        if (skillId == 6)
        {
            success = CastTeleportSkill(skill);
        }
        else
        {
            switch (skill.skillType)
            {
                case SkillType.Projectile:
                    success = CastProjectileSkill(skill);
                    break;
                case SkillType.AOE_Targeted:
                    success = CastAOETargetedSkill(skill);
                    break;
                case SkillType.AOE_Self:
                    success = CastAOESelfSkill(skill);
                    break;
                case SkillType.DOT_Targeted:
                    success = CastDOTSkill(skill);
                    break;
                case SkillType.Instant:
                    success = CastInstantSkill(skill);
                    break;
            }
        }

        if (!success && enableCooldown)
        {
            skill.isOnCooldown = false;
            skill.currentCooldown = 0f;
        }

        return success;
    }

    // --- 技能逻辑实现 ---

    // 在 SkillsManager.cs 中找到这个方法并替换
    bool CastTeleportSkill(SkillData skill)
    {
        if (currentTeleportTarget == null)
        {
            if (UIManager.Instance) UIManager.Instance.ShowMessage("无效位置：需要在传送阵上使用", 2f);
            return false;
        }

        // 1. 计算前方位置
        Vector3 spawnPos = transform.position + transform.forward * 2f;

        // 2. 【核心修复】射线检测地面，防止陷地
        RaycastHit hit;
        // 从高处向下射击
        if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out hit, 5f))
        {
            // 如果找到了地面，就把生成点定在地面上 + 垂直偏移量
            // 假设传送门高2米，中心点在中间，那么需要抬高1米
            spawnPos = hit.point + Vector3.up * 1.2f;
        }
        else
        {
            // 没找到地面（比如悬崖外），就保持原高度但稍微抬一点
            spawnPos += Vector3.up * 1.0f;
        }

        Quaternion spawnRot = Quaternion.LookRotation(-transform.forward); // 传送门面向玩家
        GameObject portalObj = Instantiate(skill.effectPrefab, spawnPos, spawnRot);

        TeleportPortal portalScript = portalObj.GetComponent<TeleportPortal>();
        if (portalScript != null) portalScript.destination = currentTeleportTarget;

        return true;
    }

    bool CastProjectileSkill(SkillData skill)
    {
        if (wandController == null || wandController.wandTip == null) return false;

        Vector3 spawnPos = wandController.wandTip.position;
        Vector3 direction = wandController.wandTip.forward;

        Transform lockedEnemy = wandController.GetLockedEnemy();
        if (lockedEnemy != null)
        {
            Vector3 targetPoint = lockedEnemy.position + Vector3.up * 1.0f;
            direction = (targetPoint - spawnPos).normalized;
        }

        GameObject projectile = Instantiate(skill.effectPrefab, spawnPos, Quaternion.LookRotation(direction));
        ProjectileController pc = projectile.GetComponent<ProjectileController>();
        if (pc == null) pc = projectile.AddComponent<ProjectileController>();

        pc.damage = skill.damage;
        pc.speed = 20f; // 统一速度，不再判断ID 0/1
        pc.enemyLayer = enemyLayer;
        pc.maxLifetime = 5f;

        if (lockedEnemy != null)
        {
            pc.SetHomingTarget(lockedEnemy);
        }

        return true;
    }

    bool CastAOETargetedSkill(SkillData skill)
    {
        Vector3 targetPos = GetTargetPosition();
        GameObject aoeEffect = Instantiate(skill.effectPrefab, targetPos, Quaternion.identity);

        AoeController aoe = aoeEffect.GetComponent<AoeController>();
        if (aoe == null) aoe = aoeEffect.AddComponent<AoeController>();

        aoe.damage = skill.damage;
        aoe.radius = skill.aoeRadius;
        aoe.duration = skill.duration;
        aoe.enemyLayer = enemyLayer;
        aoe.isInstant = (skill.duration < 0.5f);

        // 【关键修改】只在Prefab没有粒子系统时才添加默认特效
        if (skill.skillId == 2) AddFireEffect(aoeEffect);
        else if (skill.skillId == 4) AddIceEffect(aoeEffect);

        return true;
    }

    bool CastAOESelfSkill(SkillData skill)
    {
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject selfEffect = Instantiate(skill.effectPrefab, spawnPos, Quaternion.identity, transform);

        AoeController aoe = selfEffect.GetComponent<AoeController>();
        if (aoe == null) aoe = selfEffect.AddComponent<AoeController>();

        aoe.damage = skill.damage;
        aoe.radius = skill.aoeRadius;
        aoe.duration = skill.duration;
        aoe.enemyLayer = enemyLayer;
        aoe.isInstant = true;

        return true;
    }

    bool CastDOTSkill(SkillData skill)
    {
        Vector3 targetPos = GetTargetPosition();
        GameObject dotEffect = Instantiate(skill.effectPrefab, targetPos, Quaternion.identity);

        DotController dot = dotEffect.GetComponent<DotController>();
        if (dot == null) dot = dotEffect.AddComponent<DotController>();

        dot.damagePerTick = skill.damage;
        dot.tickInterval = 0.5f;
        dot.duration = skill.duration;
        dot.radius = skill.aoeRadius;
        dot.enemyLayer = enemyLayer;

        if (skill.skillId == 3) AddSwordEffect(dotEffect);

        return true;
    }

    bool CastInstantSkill(SkillData skill)
    {
        return CastAOETargetedSkill(skill);
    }

    // --- 核心修复：AddEffect 系列方法 ---
    // 逻辑：如果Prefab上已经有粒子系统，就什么都不做（信任美术资源）。
    // 只有在空物体上才动态添加代码生成的简陋特效。

    void AddFireEffect(GameObject target)
    {
        // 1. 检查是否存在粒子系统
        if (target.GetComponent<ParticleSystem>() != null) return; // 【重要】如果有，直接返回，不干扰

        // 2. 如果没有，才手动创建默认红球效果
        ParticleSystem firePs = target.AddComponent<ParticleSystem>();
        var main = firePs.main;
        main.startColor = new Color(1f, 0.5f, 0f, 1f);
        main.startSize = 0.5f;
        main.startLifetime = 0.5f;

        var emission = firePs.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        Destroy(firePs, 2f);
    }

    void AddIceEffect(GameObject target)
    {
        if (target.GetComponent<ParticleSystem>() != null) return; // 【重要】保护Prefab设置

        ParticleSystem icePs = target.AddComponent<ParticleSystem>();
        var main = icePs.main;
        main.startColor = Color.cyan;
        main.startSize = 0.5f;
        main.startLifetime = 1f;

        Destroy(icePs, 2f);
    }

    void AddSwordEffect(GameObject target)
    {
        if (target.GetComponent<ParticleSystem>() != null) return; // 【重要】保护Prefab设置

        ParticleSystem swordPs = target.AddComponent<ParticleSystem>();
        var main = swordPs.main;
        main.startColor = Color.white;
        main.startLifetime = 0.5f;

        Destroy(swordPs, 3.5f);
    }

    Vector3 GetTargetPosition()
    {
        Vector3 targetPos = Vector3.zero;

        if (wandController != null)
        {
            Transform lockedEnemy = wandController.GetLockedEnemy();
            if (lockedEnemy != null) return lockedEnemy.position;

            if (wandController.wandTip != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(wandController.wandTip.position, wandController.wandTip.forward, out hit, 100f))
                {
                    if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
                        return hit.transform.position;
                    targetPos = hit.point;
                }
                else
                {
                    targetPos = wandController.wandTip.position + wandController.wandTip.forward * 10f;
                }
            }
        }

        // 强制落地检测
        RaycastHit groundHit;
        if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out groundHit, 20f))
        {
            return groundHit.point;
        }

        return targetPos;
    }

    void PlayCastSound(SkillData skill)
    {
        if (audioSource != null && skill.castSound != null)
        {
            audioSource.PlayOneShot(skill.castSound);
        }
    }

    public void ResetAllCooldowns()
    {
        foreach (var skill in skillList) { skill.isOnCooldown = false; skill.currentCooldown = 0f; }
    }
    public SkillData GetSkillData(int skillId)
    {
        if (skillDictionary.ContainsKey(skillId))
        {
            return skillDictionary[skillId];
        }
        return null;
    }

}

[System.Serializable]
public class SkillData
{
    public string skillName;
    public int skillId;
    public float cooldown;
    public GameObject effectPrefab;
    public SkillType skillType;
    public float damage;
    public float duration; // 持续技能时长
    public float aoeRadius; // AOE技能范围
    public string gestureCode; // 对应手势代码
    public AudioClip castSound; // 施法音效

    [NonSerialized] public float currentCooldown = 0f;
    [NonSerialized] public bool isOnCooldown = false;
}
public enum SkillType
{
    Projectile,     // 投射物
    AOE_Targeted,   // 目标点AOE
    AOE_Self,       // 自身范围AOE
    DOT_Targeted,   // 目标点持续伤害
    Instant         // 立即生效
}
