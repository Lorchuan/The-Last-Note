using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("血条组件")]
    public CanvasGroup panelCanvasGroup; // 建议在Inspector中把整个血条面板拖给这个
    public Image healthBarFill;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI healthText;

    [Header("样式设置")]
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.red;
    public float colorTransitionSpeed = 5f;

    [Header("动画效果")]
    public float fadeSpeed = 10f; // 加快淡入淡出速度

    // 引用
    private WandController wandController;
    private IDamageable currentEnemy;
    private Transform currentEnemyTransform;
    private bool isInitialized = false;

    void Start()
    {
        // 尝试自动获取 CanvasGroup
        if (panelCanvasGroup == null) panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null && transform.childCount > 0)
            panelCanvasGroup = GetComponentInChildren<CanvasGroup>();

        // 初始完全隐藏
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

        if (!isInitialized)
        {
            Initialize(FindObjectOfType<WandController>());
        }
    }

    public void Initialize(WandController wc)
    {
        wandController = wc;
        isInitialized = true;
    }

    public void UpdateUI()
    {
        if (!isInitialized || wandController == null) return;

        UpdateEnemyHealth();
    }

    public void UpdateEnemyHealth()
    {
        // 1. 获取当前锁定的敌人
        Transform lockedEnemy = wandController.GetLockedEnemy();

        if (lockedEnemy != null)
        {
            IDamageable enemyDamageable = lockedEnemy.GetComponent<IDamageable>();

            // 只有当锁定的物体也是可受伤的物体时才显示
            if (enemyDamageable != null)
            {
                currentEnemyTransform = lockedEnemy;
                currentEnemy = enemyDamageable;

                // 更新信息
                UpdateHealthDisplay();

                // 显形
                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = Mathf.Lerp(panelCanvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            }
        }
        else
        {
            // 2. 没有锁定敌人：立即隐形
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Lerp(panelCanvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }

        // 【重要修改】移除了 UpdateHealthBarPosition()
        // 这样血条就会固定在你 Canvas 布局设置的位置（屏幕中上方），而不会飞来飞去。
    }

    void UpdateHealthDisplay()
    {
        if (currentEnemy == null) return;

        // 获取敌人名称
        if (enemyNameText != null && currentEnemyTransform != null)
        {
            enemyNameText.text = currentEnemyTransform.name;
        }

        // 获取生命值（尝试通过反射或接口扩展获取，保持兼容性）
        int currentHealth = 0;
        int maxHealth = 100;

        // 反射获取数据
        var getHealthMethod = currentEnemy.GetType().GetMethod("GetCurrentHealth");
        var getMaxHealthMethod = currentEnemy.GetType().GetMethod("GetMaxHealth"); // 假设有这个方法
        var maxHealthField = currentEnemy.GetType().GetField("maxHealth");

        if (getHealthMethod != null) currentHealth = (int)getHealthMethod.Invoke(currentEnemy, null);

        if (getMaxHealthMethod != null) maxHealth = (int)getMaxHealthMethod.Invoke(currentEnemy, null);
        else if (maxHealthField != null) maxHealth = (int)maxHealthField.GetValue(currentEnemy);

        float pct = maxHealth > 0 ? (float)currentHealth / maxHealth : 0;

        // UI 更新
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = pct;
            healthBarFill.color = Color.Lerp(damagedColor, healthyColor, pct);
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
}