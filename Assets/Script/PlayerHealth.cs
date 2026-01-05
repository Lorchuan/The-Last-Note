using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("生命值设置")]
    public int maxHealth = 100;
    private int currentHealth;
    public event System.Action<int, int> OnHealthChanged;
    [Header("UI元素")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Image damageFlashImage;
    public Text healthText;
    public float flashDuration = 0.2f;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;

    [Header("音效")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip healSound;
    private AudioSource audioSource;

    [Header("状态")]
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float invulnerableTime = 0f;
    private Coroutine flashCoroutine;

    [Header("死亡效果")]
    public GameObject deathEffect;
    public CanvasGroup gameOverUI;
    public float fadeDuration = 1.5f;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

        // 如果音频源不存在，创建一个
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D音效
            audioSource.volume = 0.7f;
        }

        InitializeUI();

        Debug.Log($"玩家生命值初始化: {currentHealth}/{maxHealth}");
    }

    void Update()
    {
        // 处理无敌时间
        if (isInvulnerable && invulnerableTime > 0)
        {
            invulnerableTime -= Time.deltaTime;
            if (invulnerableTime <= 0)
            {
                isInvulnerable = false;
                invulnerableTime = 0f;
            }
        }
    }

    void InitializeUI()
    {
        // 初始化血条
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // 初始化血条填充颜色
        if (healthFillImage != null)
        {
            UpdateHealthBarColor();
        }

        // 初始化伤害闪烁效果
        if (damageFlashImage != null)
        {
            damageFlashImage.color = new Color(1, 0, 0, 0); // 透明红色
        }

        // 初始化生命值文本
        UpdateHealthText();
    }

    // IDamageable接口实现 - 单参数版本
    public void TakeDamage(int damageAmount)
    {
        TakeDamage(damageAmount, "未知来源");
        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
    }

    // IDamageable接口实现 - 双参数版本
    public void TakeDamage(int damageAmount, string damageSource)
    {
        if (isDead || isInvulnerable || currentHealth <= 0) return;

        // 应用伤害
        currentHealth -= damageAmount;

        // 确保生命值不为负数
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"玩家受到来自 {damageSource} 的 {damageAmount} 点伤害，剩余血量: {currentHealth}");

        // 更新UI
        UpdateUI();

        // 屏幕闪烁效果
        if (damageFlashImage != null)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashDamageEffect());
        }

        // 播放受伤音效
        if (hurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 短暂无敌时间（可选）
            // SetInvulnerable(0.5f);
        }
    }

    IEnumerator FlashDamageEffect()
    {
        // 快速闪烁
        for (int i = 0; i < 3; i++)
        {
            damageFlashImage.color = new Color(1, 0, 0, 0.3f);
            yield return new WaitForSeconds(0.05f);
            damageFlashImage.color = new Color(1, 0, 0, 0);
            yield return new WaitForSeconds(0.05f);
        }

        flashCoroutine = null;
    }

    void UpdateUI()
    {
        // 更新血条
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // 更新血条颜色
        if (healthFillImage != null)
        {
            UpdateHealthBarColor();
        }

        // 更新生命值文本
        UpdateHealthText();
    }

    void UpdateHealthBarColor()
    {
        if (healthFillImage == null) return;

        float healthPercent = (float)currentHealth / maxHealth;
        healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("玩家死亡！");

        // 播放死亡音效
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 生成死亡特效
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // 禁用玩家控制（这里假设玩家有其他控制脚本）
        DisablePlayerControls();

        // 显示游戏结束UI
        if (gameOverUI != null)
        {
            StartCoroutine(ShowGameOverUI());
        }

        // 游戏结束逻辑
        GameOver();
    }

    void DisablePlayerControls()
    {
        // 禁用玩家移动
        MonoBehaviour movementScript = GetComponent<MonoBehaviour>();
        if (movementScript != null)
        {
            movementScript.enabled = false;
        }

        // 禁用魔杖控制器
        WandController wandController = GetComponent<WandController>();
        if (wandController != null)
        {
            wandController.enabled = false;
        }

        // 禁用手势系统
        GestureSystem gestureSystem = GetComponent<GestureSystem>();
        if (gestureSystem != null)
        {
            gestureSystem.enabled = false;
        }

        // 禁用技能管理器
        SkillManager skillManager = GetComponent<SkillManager>();
        if (skillManager != null)
        {
            skillManager.enabled = false;
        }

        // 禁用所有碰撞器
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    IEnumerator ShowGameOverUI()
    {
        if (gameOverUI == null) yield break;

        gameOverUI.gameObject.SetActive(true);
        gameOverUI.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            gameOverUI.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        gameOverUI.alpha = 1f;
    }

    void GameOver()
    {
        // 可以在这里添加更多游戏结束逻辑
        // 例如：停止生成敌人、暂停游戏等

        Debug.Log("游戏结束！");

        // 如果想让游戏暂停，取消下面这行的注释
        // Time.timeScale = 0f;
    }

    // 公共方法：治疗玩家
    public void Heal(int amount)
    {
        if (isDead) return;

        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        int healedAmount = currentHealth - oldHealth;

        if (healedAmount > 0)
        {
            // 更新UI
            UpdateUI();

            // 播放治疗音效
            if (healSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(healSound);
            }

            Debug.Log($"玩家恢复 {healedAmount} 点生命值，当前血量: {currentHealth}");
        }
        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
    }

    // 公共方法：完全治疗
    public void FullHeal()
    {
        if (isDead) return;

        int healedAmount = maxHealth - currentHealth;
        currentHealth = maxHealth;

        if (healedAmount > 0)
        {
            UpdateUI();

            if (healSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(healSound);
            }

            Debug.Log($"玩家完全治疗，恢复 {healedAmount} 点生命值");
        }
    }

    // 公共方法：设置无敌状态
    public void SetInvulnerable(float duration)
    {
        isInvulnerable = true;
        invulnerableTime = duration;

        Debug.Log($"玩家无敌状态持续 {duration} 秒");
    }

    // 公共方法：复活玩家
    public void Respawn()
    {
        if (!isDead) return;

        isDead = false;
        currentHealth = maxHealth;

        // 恢复玩家控制
        EnablePlayerControls();

        // 更新UI
        UpdateUI();

        Debug.Log($"玩家复活，生命值恢复至 {currentHealth}");
    }

    void EnablePlayerControls()
    {
        // 重新启用玩家移动
        MonoBehaviour movementScript = GetComponent<MonoBehaviour>();
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }

        // 重新启用魔杖控制器
        WandController wandController = GetComponent<WandController>();
        if (wandController != null)
        {
            wandController.enabled = true;
        }

        // 重新启用手势系统
        GestureSystem gestureSystem = GetComponent<GestureSystem>();
        if (gestureSystem != null)
        {
            gestureSystem.enabled = true;
        }

        // 重新启用技能管理器
        SkillManager skillManager = GetComponent<SkillManager>();
        if (skillManager != null)
        {
            skillManager.enabled = true;
        }

        // 重新启用所有碰撞器
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
    }

    // 公共方法：获取当前生命值
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // 公共方法：获取生命值百分比
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    // 公共方法：检查是否死亡
    public bool IsDead()
    {
        return isDead;
    }

    // 公共方法：检查是否无敌
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }

    // 公共方法：增加最大生命值
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;

        // 更新UI
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateHealthText();

        Debug.Log($"最大生命值增加 {amount} 点，当前: {maxHealth}");
    }

    // 调试方法：测试受伤
    [ContextMenu("测试受伤(10点)")]
    public void TestTakeDamage()
    {
        TakeDamage(10, "测试伤害");
    }

    // 调试方法：测试治疗
    [ContextMenu("测试治疗(20点)")]
    public void TestHeal()
    {
        Heal(20);
    }

    // 调试方法：测试死亡
    [ContextMenu("测试死亡")]
    public void TestDie()
    {
        TakeDamage(currentHealth, "致命伤害");
    }

    // 调试方法：重置生命值
    [ContextMenu("重置生命值")]
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateUI();
        Debug.Log("生命值已重置");
    }
}