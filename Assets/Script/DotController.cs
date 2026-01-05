using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DotController : MonoBehaviour
{
    [Header("持续伤害设置")]
    public float damagePerTick = 5f;
    public float tickInterval = 0.5f;
    public float duration = 5f;
    public float radius = 3f;
    public LayerMask enemyLayer;

    [Header("视觉效果")]
    public ParticleSystem mainParticles;
    public Light effectLight;
    public bool showDamageNumbers = true;

    [Header("音效")]
    public AudioClip startSound;
    public AudioClip tickSound;
    public AudioClip endSound;

    // 内部变量
    private AudioSource audioSource;
    private List<GameObject> affectedEnemies = new List<GameObject>();
    private Dictionary<GameObject, int> enemyDamageCount = new Dictionary<GameObject, int>();
    private float startTime;
    private int totalTicks = 0;
    private bool isActive = true;

    void Start()
    {
        startTime = Time.time;

        // 获取或创建组件
        if (mainParticles == null)
        {
            mainParticles = GetComponent<ParticleSystem>();
        }

        if (effectLight == null)
        {
            effectLight = GetComponent<Light>();
        }

        // 计算总tick数
        totalTicks = Mathf.FloorToInt(duration / tickInterval);

        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.6f;

        // 播放开始音效
        if (startSound != null)
        {
            audioSource.PlayOneShot(startSound);
        }

        // 开始持续伤害协程
        StartCoroutine(ApplyDamageOverTime());

        // 设置自动销毁
        StartCoroutine(DestroyAfterDuration());

        Debug.Log($"DOT效果开始: {duration}秒, {totalTicks}次伤害");
    }

    void Update()
    {
        // 更新光效强度
        if (effectLight != null)
        {
            float progress = (Time.time - startTime) / duration;
            effectLight.intensity = Mathf.Lerp(2f, 0f, progress);
            effectLight.range = Mathf.Lerp(radius * 2f, radius * 0.5f, progress);
        }

        // 更新粒子效果
        if (mainParticles != null)
        {
            var main = mainParticles.main;
            main.startSize = Mathf.Lerp(0.5f, 0.1f, (Time.time - startTime) / duration);
        }
    }

    IEnumerator ApplyDamageOverTime()
    {
        float elapsedTime = 0f;
        int currentTick = 0;

        while (elapsedTime < duration && isActive)
        {
            // 应用一次伤害
            ApplyTickDamage(currentTick);

            // 播放tick音效
            if (tickSound != null && currentTick > 0)
            {
                audioSource.PlayOneShot(tickSound);
            }

            // 等待下一个tick
            elapsedTime += tickInterval;
            currentTick++;
            yield return new WaitForSeconds(tickInterval);
        }
    }

    void ApplyTickDamage(int tickNumber)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (var enemy in enemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)damagePerTick);

                // 记录伤害
                if (!enemyDamageCount.ContainsKey(enemy.gameObject))
                {
                    enemyDamageCount[enemy.gameObject] = 0;
                }
                enemyDamageCount[enemy.gameObject]++;

                // 显示伤害数字
                if (showDamageNumbers)
                {
                    ShowDamageNumber(enemy.transform.position, (int)damagePerTick);
                }

                Debug.Log($"持续伤害对 {enemy.name} 造成 {damagePerTick} 点伤害 (第{tickNumber + 1}次)");
            }
        }

        // 添加到受影响敌人列表
        foreach (var enemy in enemies)
        {
            if (!affectedEnemies.Contains(enemy.gameObject))
            {
                affectedEnemies.Add(enemy.gameObject);
            }
        }
    }

    void ShowDamageNumber(Vector3 position, int damage)
    {
        // 创建简单的伤害数字显示
        GameObject damageText = new GameObject("DamageNumber");
        damageText.transform.position = position + Vector3.up * 1f;

        TextMesh textMesh = damageText.AddComponent<TextMesh>();
        textMesh.text = damage.ToString();
        textMesh.fontSize = 20;
        textMesh.color = Color.yellow;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // 添加浮动效果
        damageText.AddComponent<FloatingText>();

        // 自动销毁
        Destroy(damageText, 1f);
    }

    IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);

        // 停止活动
        isActive = false;

        // 播放结束音效
        if (endSound != null)
        {
            audioSource.PlayOneShot(endSound);
        }

        // 停止粒子效果
        if (mainParticles != null)
        {
            mainParticles.Stop();
        }

        // 淡出光效
        float fadeTime = 1f;
        float fadeStartTime = Time.time;
        float startIntensity = effectLight != null ? effectLight.intensity : 0f;

        while (Time.time - fadeStartTime < fadeTime)
        {
            if (effectLight != null)
            {
                float progress = (Time.time - fadeStartTime) / fadeTime;
                effectLight.intensity = Mathf.Lerp(startIntensity, 0f, progress);
            }
            yield return null;
        }

        // 输出伤害统计
        PrintDamageStatistics();

        // 销毁物体
        Destroy(gameObject, 0.5f);
    }

    void PrintDamageStatistics()
    {
        Debug.Log($"=== DOT伤害统计 ===");
        Debug.Log($"持续时间: {duration}秒");
        Debug.Log($"总tick数: {totalTicks}");
        Debug.Log($"受影响敌人: {affectedEnemies.Count}个");

        foreach (var enemy in affectedEnemies)
        {
            if (enemy != null && enemyDamageCount.ContainsKey(enemy))
            {
                int ticks = enemyDamageCount[enemy];
                float totalDamage = ticks * damagePerTick;
                Debug.Log($"  {enemy.name}: {ticks}次伤害, 总计{totalDamage}点");
            }
        }

        Debug.Log($"====================");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // 公共方法：获取剩余时间
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, duration - (Time.time - startTime));
    }

    // 公共方法：获取剩余tick数
    public int GetRemainingTicks()
    {
        float elapsed = Time.time - startTime;
        float remainingTime = Mathf.Max(0f, duration - elapsed);
        return Mathf.FloorToInt(remainingTime / tickInterval);
    }

    // 公共方法：获取总伤害
    public float GetTotalDamage()
    {
        return totalTicks * damagePerTick;
    }

    // 公共方法：获取已造成的伤害
    public float GetDamageDealt()
    {
        float damageDealt = 0f;
        foreach (var entry in enemyDamageCount)
        {
            damageDealt += entry.Value * damagePerTick;
        }
        return damageDealt;
    }
}

// 浮动文本效果
public class FloatingText : MonoBehaviour
{
    private float startTime;
    private Vector3 startPosition;

    void Start()
    {
        startTime = Time.time;
        startPosition = transform.position;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        // 向上浮动
        transform.position = startPosition + Vector3.up * elapsed * 2f;

        // 淡出
        TextMesh textMesh = GetComponent<TextMesh>();
        if (textMesh != null)
        {
            Color color = textMesh.color;
            color.a = 1f - elapsed;
            textMesh.color = color;
        }

        // 旋转面向摄像机
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}