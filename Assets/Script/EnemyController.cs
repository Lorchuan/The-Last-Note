using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("属性")]
    public int maxHealth = 60;
    private int currentHealth;
    public bool isDead = false;

    [Header("AI设置")]
    public Transform targetPlayer;
    private NavMeshAgent agent;
    public float patrolRadius = 10f; // 巡逻范围
    public float idleTime = 2f; // 空闲时间
    private Vector3 patrolPoint;
    private float idleTimer = 0f;
    private bool isPatrolling = true;

    [Header("攻击设置")]
    public float attackRange = 2.0f;
    public float attackDamage = 10;
    public float attackCooldown = 3.0f;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    [Header("动画控制")]
    public Animator animator;
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_HIT = "Hit";
    private const string ANIM_DEATH = "isDead";

    [Header("视觉反馈")]
    public Renderer meshRenderer;
    public SkinnedMeshRenderer skinnedMeshRenderer; // 针对带骨骼的模型
    public Color flashColor = Color.white;
    private Material[] originalMaterials;
    private Color[] originalColors;

    [Header("粒子特效")]
    public GameObject deathParticlePrefab;
    public GameObject hitParticlePrefab;
    public Transform particleSpawnPoint;

    [Header("音效")]
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    private AudioSource audioSource;

    [Header("掉落物")]
    public GameObject[] dropItems; // 掉落物品预制体
    public float dropChance = 0.3f; // 掉落概率

    [Header("状态")]
    public bool showDebugInfo = false;
    public bool showGizmos = true;

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

        // 保存原始材质和颜色
        SaveOriginalMaterials();

        // 初始化粒子生成点
        if (particleSpawnPoint == null)
        {
            particleSpawnPoint = transform;
        }

        // 设置初始攻击冷却时间，让蜘蛛可以立即攻击
        lastAttackTime = Time.time - attackCooldown;

        // 设置初始巡逻点
        GenerateRandomPatrolPoint();

        Debug.Log($"{gameObject.name} 初始化完成，血量: {currentHealth}/{maxHealth}");
    }

    void SaveOriginalMaterials()
    {
        // 处理普通MeshRenderer
        if (meshRenderer != null)
        {
            originalMaterials = meshRenderer.materials;
            originalColors = new Color[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                originalColors[i] = originalMaterials[i].color;
            }
        }
        // 处理SkinnedMeshRenderer
        else if (skinnedMeshRenderer != null)
        {
            originalMaterials = skinnedMeshRenderer.materials;
            originalColors = new Color[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                originalColors[i] = originalMaterials[i].color;
            }
        }
    }

    void Update()
    {
        if (isDead || targetPlayer == null) return;

        // 计算到玩家的距离
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        bool canSeePlayer = distanceToPlayer <= patrolRadius * 1.5f;

        // AI状态机
        if (canSeePlayer)
        {
            // 发现玩家，进入战斗状态
            isPatrolling = false;
            HandleCombatState(distanceToPlayer);
        }
        else
        {
            // 未发现玩家，巡逻状态
            HandlePatrolState();
        }

        // 更新动画
        UpdateAnimations();

        // 检查并重置攻击状态
        CheckAndResetAttackState();
    }

    void HandleCombatState(float distanceToPlayer)
    {
        // 攻击逻辑
        if (distanceToPlayer <= attackRange)
        {
            // 进入攻击范围，停止移动
            agent.isStopped = true;

            // 转向玩家
            FaceTarget(targetPlayer.position);

            // 检查是否可以攻击
            if (!isAttacking && CanAttack())
            {
                StartAttack();
            }
        }
        else
        {
            // 不在攻击范围，继续追击
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);

            // 离开攻击范围时重置攻击状态
            if (isAttacking)
            {
                isAttacking = false;
                if (showDebugInfo) Debug.Log("离开攻击范围，重置攻击状态");
            }
        }
    }

    void HandlePatrolState()
    {
        if (!isPatrolling)
        {
            isPatrolling = true;
            GenerateRandomPatrolPoint();
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

    void UpdateAnimations()
    {
        if (animator != null)
        {
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat(ANIM_SPEED, speed);
        }
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // 保持水平旋转
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
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

    bool CanAttack()
    {
        // 检查是否在冷却中
        bool isCooldownOver = Time.time - lastAttackTime >= attackCooldown;

        if (showDebugInfo && !isCooldownOver)
        {
            Debug.Log($"攻击冷却中: {Time.time - lastAttackTime:F1}/{attackCooldown}s");
        }

        return isCooldownOver;
    }

    void CheckAndResetAttackState()
    {
        if (isAttacking && animator != null)
        {
            // 检查是否仍在攻击动画中
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 如果不在攻击状态且动画已经播放完毕，重置攻击标志
            if (!stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 0.9f)
            {
                isAttacking = false;
                if (showDebugInfo) Debug.Log("攻击动画结束，重置攻击状态");
            }
        }
    }

    void StartAttack()
    {
        if (!CanAttack()) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        // 触发攻击动画
        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        // 播放攻击音效
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 开始攻击！下次攻击时间: {lastAttackTime + attackCooldown:F1}");
        }
    }

    // 攻击命中方法（动画事件调用）
    public void OnAttackHit()
    {
        if (isDead || targetPlayer == null) return;

        // 检查玩家是否仍在攻击范围内
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange * 1.2f) // 稍微宽松一点的范围
        {
            // 对玩家造成伤害
            IDamageable damageable = targetPlayer.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)attackDamage);
                Debug.Log($"{gameObject.name} 对玩家造成 {attackDamage} 点伤害！");
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
        Debug.Log($"{gameObject.name} 受到 {damageSource} 的 {damage} 点伤害！剩余血量: {currentHealth}/{maxHealth}");

        // 受伤闪烁效果
        StartCoroutine(FlashColor());

        // 播放受伤音效
        if (hurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // 触发受伤动画
        if (animator != null && !isAttacking)
        {
            animator.SetTrigger(ANIM_HIT);
        }

        // 生成受伤粒子特效
        if (hitParticlePrefab != null)
        {
            Vector3 spawnPos = particleSpawnPoint.position + Vector3.up * 0.5f;
            GameObject hitParticle = Instantiate(hitParticlePrefab, spawnPos, Quaternion.identity);

            // 自动调整粒子颜色为红色
            ParticleSystem ps = hitParticle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new Color(1f, 0.3f, 0.3f, 1f);
            }

            Destroy(hitParticle, 2f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 被击败！");

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
            animator.SetTrigger(ANIM_DEATH);
        }

        // 播放死亡音效
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 生成死亡粒子特效
        if (deathParticlePrefab != null)
        {
            GameObject deathParticle = Instantiate(deathParticlePrefab,
                particleSpawnPoint.position,
                Quaternion.identity);

            ParticleSystem particleSystem = deathParticle.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = new Color(0.3f, 0.1f, 0.1f, 1f);
            }

            Destroy(deathParticle, 3f);
        }

        // 掉落物品
        DropItems();

        // 延迟销毁游戏对象
        StartCoroutine(DestroyAfterDeath());
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
                Debug.Log($"掉落物品: {dropItem.name}");
            }
        }
    }

    IEnumerator DestroyAfterDeath()
    {
        // 等待死亡动画播放
        yield return new WaitForSeconds(2f);

        // 逐渐消失效果
        float fadeTime = 1f;
        float startTime = Time.time;

        while (Time.time - startTime < fadeTime)
        {
            float progress = (Time.time - startTime) / fadeTime;

            // 逐渐透明化
            if (meshRenderer != null)
            {
                foreach (Material mat in meshRenderer.materials)
                {
                    Color color = mat.color;
                    color.a = 1f - progress;
                    mat.color = color;
                }
            }
            else if (skinnedMeshRenderer != null)
            {
                foreach (Material mat in skinnedMeshRenderer.materials)
                {
                    Color color = mat.color;
                    color.a = 1f - progress;
                    mat.color = color;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator FlashColor()
    {
        if (meshRenderer != null)
        {
            Material[] materials = meshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].color = flashColor;
            }

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < materials.Length; i++)
            {
                if (i < originalColors.Length)
                {
                    materials[i].color = originalColors[i];
                }
            }
        }
        else if (skinnedMeshRenderer != null)
        {
            Material[] materials = skinnedMeshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].color = flashColor;
            }

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < materials.Length; i++)
            {
                if (i < originalColors.Length)
                {
                    materials[i].color = originalColors[i];
                }
            }
        }
    }

    // 在场景中可视化攻击范围
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 巡逻范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // 当前巡逻点
        if (Application.isPlaying && isPatrolling)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(patrolPoint, 0.5f);
            Gizmos.DrawLine(transform.position, patrolPoint);
        }
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
        Debug.Log($"{gameObject.name} 恢复 {amount} 点生命值，当前: {currentHealth}");
    }

    public void SetInvulnerable(bool invulnerable)
    {
        // 这里可以添加无敌状态逻辑
        Debug.Log($"{gameObject.name} 无敌状态: {invulnerable}");
    }
}