using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthRingUI : MonoBehaviour
{
    [Header("组件设置")]
    // 请确保这个Image的 Image Type = Filled, Fill Method = Radial 360
    public Image healthRingFill;
    public TextMeshProUGUI healthText;
    public GameObject lowHealthEffect;

    [Header("颜色设置")]
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Color criticalColor = Color.yellow;

    [Header("阈值")]
    public float lowHealthThreshold = 0.3f;
    public float criticalHealthThreshold = 0.1f;

    [Header("平滑")]
    public float smoothSpeed = 5f;

    // 引用
    private PlayerHealth playerHealth;
    private bool isInitialized = false;

    void Start()
    {
        if (!isInitialized)
        {
            Initialize(FindObjectOfType<PlayerHealth>());
        }
    }

    public void Initialize(PlayerHealth ph)
    {
        playerHealth = ph;

        // 自动修正图片设置，防止忘记设置
        if (healthRingFill != null)
        {
            healthRingFill.type = Image.Type.Filled;
            healthRingFill.fillMethod = Image.FillMethod.Radial360;
            healthRingFill.fillOrigin = 2; // Top
            healthRingFill.fillClockwise = false; // 逆时针或顺时针看喜好
        }

        isInitialized = true;
    }

    public void UpdateUI()
    {
        if (!isInitialized || playerHealth == null || healthRingFill == null) return;

        UpdateHealth();
    }

    public void UpdateHealth()
    {
        float targetFill = playerHealth.GetHealthPercentage();
        int currentHealth = playerHealth.GetCurrentHealth();

        // 1. 平滑更新圆环
        healthRingFill.fillAmount = Mathf.Lerp(healthRingFill.fillAmount, targetFill, Time.deltaTime * smoothSpeed);

        // 2. 更新数字
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}";
        }

        // 3. 颜色逻辑
        Color targetColor = healthyColor;
        bool showLowHealthEffect = false;

        if (targetFill <= criticalHealthThreshold)
        {
            targetColor = criticalColor;
            showLowHealthEffect = true;
        }
        else if (targetFill <= lowHealthThreshold)
        {
            targetColor = lowHealthColor;
            showLowHealthEffect = true;
        }

        healthRingFill.color = Color.Lerp(healthRingFill.color, targetColor, Time.deltaTime * smoothSpeed);

        // 4. 低血量特效
        if (lowHealthEffect != null)
        {
            if (lowHealthEffect.activeSelf != showLowHealthEffect)
                lowHealthEffect.SetActive(showLowHealthEffect);
        }
    }
}