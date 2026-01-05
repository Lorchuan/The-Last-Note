using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class WolfController : MonoBehaviour, IDamageable
{
    [Header("属性设置")]
    public int maxHealth = 100;
    private int currentHealth;
    public bool isDead = false;

    [Header("移动与AI设置")]
    public Transform targetPlayer;
    private NavMeshAgent agent;
    public float detectionRange = 15f; // 发现玩家的范围
    public float attackRange = 2.5f; // 近战攻击范围
    public float howlRange = 8f; // 嚎叫攻击范围
    public float patrolRadius = 20f; // 巡逻范围
    private bool isInCombat = false;
    private Vector3 patrolPoint;
    private float idleTimer = 0f;
    private float idleTime = 3f;

    [Header("Boss事件 (新增)")]
    public float deathEventDelay = 4.0f; // 等待死亡动画播放几秒后触发
    public UnityEvent OnDeath;           // 在Inspector里拖入传送逻辑

    [Header("攻击设置")]
    public float attackCooldown = 5f; // 攻击冷却时间
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private AttackType nextAttackType = AttackType.Combo; // 下一个攻击类型
    private int comboStep = 0; // 连招步骤 (0:未连招, 1:攻击1, 2:攻击2)

    [Header("攻击伤害")]
    public int clawDamage1 = 20; // 爪击1伤害
    public int clawDamage2 = 20; // 爪击2伤害
    public int howlDamage = 5; // 嚎叫基础伤害
    public float slowDuration = 5f; // 减速持续时间
    public float slowAmount = 0.5f; // 减速比例 (0.5 = 速度减半)

    [Header("动画控制")]
    public Animator animator;
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_BATTLEIDLE = "BattleIdle";
    private const string ANIM_ATTACK1 = "Attack1";
    private const string ANIM_ATTACK2 = "Attack2";
    private const string ANIM_ATTACK3 = "Attack3";
    private const string ANIM_HIT = "Hit";
    private const string ANIM_DIE = "Die";

    [Header("特效设置")]
    public GameObject clawEffectPrefab; // 爪击特效
    public GameObject howlEffectPrefab; // 嚎叫特效
    public Transform clawEffectPoint; // 爪击特效生成点
    public Transform howlEffectPoint; // 嚎叫特效生成点
    public GameObject hitEffectPrefab; // 受伤特效
    public GameObject deathEffectPrefab; // 死亡特效

    [Header("音效设置")]
    public AudioClip attack1Sound;
    public AudioClip attack2Sound;
    public AudioClip howlSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    private AudioSource audioSource;

    [Header("视觉反馈")]
    public Renderer[] meshRenderers; // 狼身上的所有Renderer
    private Color[] originalColors;
    public Color flashColor = Color.white;

    [Header("嚎叫攻击设置")]
    public LayerMask playerLayer = 1 << 0; // 默认层，需要根据实际设置
    public float howlAngle = 45f; // 嚎叫的扇形角度
    public float howlKnockbackForce = 3f; // 嚎叫击退力

    [Header("掉落物")]
    public GameObject[] dropItems; // 掉落物品预制体
    public float dropChance = 0.4f; // 掉落概率

    [Header("调试")]
    public bool showDebugInfo = false;
    public bool drawGizmos = true;

    // 攻击类型枚举
    private enum AttackType
    {
        Combo,    // 连招攻击 (Attack1 -> Attack2)
        Howl      // 嚎叫攻击 (Attack3)
    }

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.7f;
        }

        // 自动寻找玩家
        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetPlayer = player.transform;
            else Debug.LogWarning("未找到玩家对象，请确保玩家有Player标签");
        }

        // 获取动画组件
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 保存原始颜色
        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            originalColors = new Color[meshRenderers.Length];
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null)
                {
                    originalColors[i] = meshRenderers[i].material.color;
                }
            }
        }

        // 设置特效生成点
        if (clawEffectPoint == null) clawEffectPoint = transform;
        if (howlEffectPoint == null) howlEffectPoint = transform;

        // 初始化攻击冷却
        lastAttackTime = Time.time - attackCooldown;

        // 设置初始巡逻点
        GenerateRandomPatrolPoint();

        Debug.Log($"狼怪物初始化完成，血量: {currentHealth}/{maxHealth}");
    }

    void Update()
    {
        if (isDead || targetPlayer == null) return;

        // 计算到玩家的距离
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        // 检查玩家是否在侦测范围内
        if (distanceToPlayer <= detectionRange)
        {
            isInCombat = true;
            HandleCombatState(distanceToPlayer);
        }
        else
        {
            // 玩家不在侦测范围，退出战斗状态
            isInCombat = false;
            HandlePatrolState();
        }

        // 检查并重置攻击状态
        CheckAndResetAttackState();
    }

    void HandleCombatState(float distanceToPlayer)
    {
        // 更新动画速度参数
        if (animator != null)
        {
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat(ANIM_SPEED, speed);

            // 设置战斗状态
            animator.SetBool(ANIM_BATTLEIDLE, distanceToPlayer <= attackRange * 1.5f);
        }

        // 如果玩家在攻击范围内，停止移动并攻击
        if (distanceToPlayer <= attackRange || distanceToPlayer <= howlRange)
        {
            agent.isStopped = true;

            // 面向玩家
            Vector3 directionToPlayer = targetPlayer.position - transform.position;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            // 检查是否可以攻击
            if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
            {
                StartAttack();
            }
        }
        else
        {
            // 玩家在侦测范围但不在攻击范围，继续追击
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
        }
    }

    void HandlePatrolState()
    {
        // 设置非战斗状态动画
        if (animator != null)
        {
            animator.SetBool(ANIM_BATTLEIDLE, false);

            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat(ANIM_SPEED, speed);
        }

        // 检查是否到达巡逻点
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTime)
            {
                idleTimer = 0f;
                GenerateRandomPatrolPoint();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoint);
        }
    }

    void GenerateRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
        }
    }

    void StartAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        // 根据攻击类型触发不同动画
        switch (nextAttackType)
        {
            case AttackType.Combo:
                comboStep = 1;
                animator.SetTrigger(ANIM_ATTACK1);
                if (showDebugInfo) Debug.Log("开始连招攻击 - 攻击1");
                break;

            case AttackType.Howl:
                animator.SetTrigger(ANIM_ATTACK3);
                if (showDebugInfo) Debug.Log("开始嚎叫攻击");
                break;
        }
    }

    // 动画事件：攻击1命中
    public void OnAttack1Hit()
    {
        if (isDead) return;

        // 播放攻击音效
        if (attack1Sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attack1Sound);
        }

        // 创建爪击特效
        if (clawEffectPrefab != null)
        {
            GameObject effect = Instantiate(clawEffectPrefab, clawEffectPoint.position, clawEffectPoint.rotation);
            Destroy(effect, 2f);
        }

        // 检测并伤害玩家
        PerformClawAttack(clawDamage1);
    }

    // 动画事件：攻击1结束，触发攻击2
    public void OnAttack1End()
    {
        if (isDead || comboStep != 1) return;

        comboStep = 2;
        animator.SetTrigger(ANIM_ATTACK2);
        if (showDebugInfo) Debug.Log("连招攻击 - 攻击2");
    }

    // 动画事件：攻击2命中
    public void OnAttack2Hit()
    {
        if (isDead) return;

        // 播放攻击音效
        if (attack2Sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attack2Sound);
        }

        // 创建爪击特效
        if (clawEffectPrefab != null)
        {
            GameObject effect = Instantiate(clawEffectPrefab, clawEffectPoint.position, clawEffectPoint.rotation);
            Destroy(effect, 2f);
        }

        // 检测并伤害玩家
        PerformClawAttack(clawDamage2);
    }

    // 动画事件：攻击2结束
    public void OnAttack2End()
    {
        comboStep = 0;

        // 切换下一个攻击类型
        nextAttackType = AttackType.Howl;

        // 攻击结束
        StartCoroutine(ResetAttackState());
    }

    // 动画事件：嚎叫攻击命中
    public void OnHowlHit()
    {
        if (isDead) return;

        // 播放嚎叫音效
        if (howlSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(howlSound);
        }

        // 创建嚎叫特效
        if (howlEffectPrefab != null)
        {
            GameObject effect = Instantiate(howlEffectPrefab, howlEffectPoint.position, howlEffectPoint.rotation);
            Destroy(effect, 3f);
        }

        // 执行嚎叫攻击
        PerformHowlAttack();
    }

    // 动画事件：嚎叫攻击结束
    public void OnHowlEnd()
    {
        // 切换下一个攻击类型
        nextAttackType = AttackType.Combo;

        // 攻击结束
        StartCoroutine(ResetAttackState());
    }

    void PerformClawAttack(int damage)
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange * 1.2f) // 稍微宽松一点的范围
        {
            // 对玩家造成伤害
            IDamageable damageable = targetPlayer.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, "狼爪击");
                if (showDebugInfo) Debug.Log($"狼对玩家造成 {damage} 点爪击伤害！");
            }
        }
    }

    void PerformHowlAttack()
    {
        // 扇形范围检测玩家
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, howlRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            // 检查是否在扇形角度内
            Vector3 directionToPlayer = player.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= howlAngle / 2)
            {
                // 对玩家造成伤害
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(howlDamage, "狼嚎叫");
                    if (showDebugInfo) Debug.Log($"狼对玩家造成 {howlDamage} 点声波伤害！");
                }

                // 施加减速效果（需要玩家有相应的组件）
                MonoBehaviour playerMovement = player.GetComponent<MonoBehaviour>();
                if (playerMovement != null)
                {
                    // 这里调用玩家的减速方法，方法名需要根据实际脚本调整
                    System.Reflection.MethodInfo slowMethod = playerMovement.GetType().GetMethod("ApplySlowEffect");
                    if (slowMethod != null)
                    {
                        slowMethod.Invoke(playerMovement, new object[] { slowAmount, slowDuration });
                        if (showDebugInfo) Debug.Log($"玩家被减速 {slowAmount * 100}% 持续 {slowDuration}秒");
                    }
                }

                // 添加击退效果
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 knockbackDirection = directionToPlayer.normalized + Vector3.up * 0.3f;
                    playerRb.AddForce(knockbackDirection * howlKnockbackForce, ForceMode.Impulse);
                }
            }
        }
    }

    IEnumerator ResetAttackState()
    {
        // 等待一小段时间确保动画完全结束
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;

        if (showDebugInfo) Debug.Log($"攻击结束，下一个攻击类型: {nextAttackType}");
    }

    void CheckAndResetAttackState()
    {
        if (isAttacking && animator != null)
        {
            // 检查是否仍在攻击动画中
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 如果不在任何攻击状态，重置攻击标志
            bool isInAttackState = stateInfo.IsName("Attack1") ||
                                   stateInfo.IsName("Attack2") ||
                                   stateInfo.IsName("Attack3");

            if (!isInAttackState && stateInfo.normalizedTime >= 0.9f)
            {
                isAttacking = false;
                comboStep = 0;
            }
        }
    }

    // IDamageable接口实现
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, "未知来源");
    }

    public void TakeDamage(int damage, string damageSource)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"狼受到 {damageSource} 的 {damage} 点伤害！剩余血量: {currentHealth}/{maxHealth}");

        // 受伤闪烁效果
        StartCoroutine(FlashColor());

        // 播放受伤音效
        if (hurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // 生成受伤特效
        if (hitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }

        // 触发受伤动画
        if (animator != null && !isAttacking) // 如果不是在攻击中，播放受伤动画
        {
            animator.SetTrigger(ANIM_HIT);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("狼被击败！");

        // 停止所有移动
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 禁用碰撞器
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 触发死亡动画
        if (animator != null)
        {
            animator.SetTrigger(ANIM_DIE);
        }

        // 播放死亡音效
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 生成死亡特效
        if (deathEffectPrefab != null)
        {
            GameObject deathEffect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(deathEffect, 3f);
        }

        // 掉落物品
        DropItems();

        // 延迟销毁游戏对象
        StartCoroutine(DestroyAfterDeath());
        StartCoroutine(TriggerDeathEvent());
    }
    IEnumerator TriggerDeathEvent()
    {
        Debug.Log($"Boss已死，{deathEventDelay}秒后触发后续事件...");
        yield return new WaitForSeconds(deathEventDelay);

        // 触发在Inspector里绑定的事件 (比如传送玩家)
        OnDeath?.Invoke();

        // 销毁尸体 (可选，如果不销毁就注释掉)
        // Destroy(gameObject, 1f); 
    }
    void DropItems()
    {
        if (dropItems == null || dropItems.Length == 0) return;

        // 根据概率决定是否掉落
        if (Random.value <= dropChance)
        {
            int index = Random.Range(0, dropItems.Length);
            GameObject dropItem = dropItems[index];

            if (dropItem != null)
            {
                Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
                Instantiate(dropItem, dropPosition, Quaternion.identity);
                Debug.Log($"狼掉落物品: {dropItem.name}");
            }
        }
    }

    IEnumerator DestroyAfterDeath()
    {
        // 等待死亡动画播放
        yield return new WaitForSeconds(6f);

        // 逐渐消失效果
        float fadeTime = 1f;
        float startTime = Time.time;

        while (Time.time - startTime < fadeTime)
        {
            float progress = (Time.time - startTime) / fadeTime;

            // 逐渐透明化
            if (meshRenderers != null)
            {
                foreach (Renderer renderer in meshRenderers)
                {
                    if (renderer != null)
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            Color color = mat.color;
                            color.a = 1f - progress;
                            mat.color = color;
                        }
                    }
                }
            }

            yield return null;
        }

        // 销毁游戏对象
        Destroy(gameObject);
    }

    IEnumerator FlashColor()
    {
        if (meshRenderers != null)
        {
            // 变白
            foreach (Renderer renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.color = flashColor;
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);

            // 恢复原色
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null && i < originalColors.Length)
                {
                    foreach (Material mat in meshRenderers[i].materials)
                    {
                        mat.color = originalColors[i];
                    }
                }
            }
        }
    }

    // 在场景视图中绘制调试信息
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // 绘制侦测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 绘制近战攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制嚎叫攻击范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, howlRange);

        // 绘制嚎叫扇形范围
        if (targetPlayer != null && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 forward = transform.forward;
            Vector3 leftBoundary = Quaternion.Euler(0, -howlAngle / 2, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, howlAngle / 2, 0) * forward;

            Gizmos.DrawRay(transform.position, leftBoundary * howlRange);
            Gizmos.DrawRay(transform.position, rightBoundary * howlRange);

            // 绘制扇形弧线
            DrawArc(transform.position, transform.forward, howlAngle, howlRange);
        }
    }

    // 绘制扇形弧线的辅助方法
    void DrawArc(Vector3 center, Vector3 forward, float angle, float radius)
    {
        int segments = 20;
        float step = angle / segments;

        Vector3 prevPoint = center + Quaternion.Euler(0, -angle / 2, 0) * forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle / 2 + step * i;
            Vector3 nextPoint = center + Quaternion.Euler(0, currentAngle, 0) * forward * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // 连接弧线两端到中心
        Vector3 leftPoint = center + Quaternion.Euler(0, -angle / 2, 0) * forward * radius;
        Vector3 rightPoint = center + Quaternion.Euler(0, angle / 2, 0) * forward * radius;
        Gizmos.DrawLine(center, leftPoint);
        Gizmos.DrawLine(center, rightPoint);
    }

    // 公共方法
    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"狼恢复 {amount} 点生命值，当前: {currentHealth}");
    }

    public void SetInvulnerable(bool invulnerable)
    {
        // 这里可以添加无敌状态逻辑
        Debug.Log($"狼无敌状态: {invulnerable}");
    }
}