using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AoeController : MonoBehaviour
{
    [Header("伤害设置")]
    public float damage = 10f;
    public float radius = 5f;
    public float duration = 2f;
    public LayerMask enemyLayer;
    public bool isInstant = false; // 是否瞬间伤害
    public bool applyKnockback = false; // 是否击退
    public float knockbackForce = 5f; // 击退力度

    [Header("视觉效果")]
    public ParticleSystem mainParticles;
    public Light effectLight;
    public bool showGizmos = true;

    [Header("音效")]
    public AudioClip impactSound;
    public AudioClip loopSound;

    // 内部变量
    private AudioSource audioSource;
    private List<GameObject> damagedEnemies = new List<GameObject>();
    private float startTime;
    private Vector3 originalScale;

    void Start()
    {
        startTime = Time.time;
        originalScale = transform.localScale;

        // 获取或创建组件
        if (mainParticles == null)
        {
            mainParticles = GetComponent<ParticleSystem>();
        }

        if (effectLight == null)
        {
            effectLight = GetComponent<Light>();
        }

        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.7f;

        // 播放音效
        if (impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }

        if (loopSound != null && duration > 1f)
        {
            audioSource.clip = loopSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 设置AOE大小
        UpdateAOEScale();

        if (isInstant)
        {
            // 瞬间伤害技能，立即造成伤害
            DealDamage();

            // 短暂延迟后销毁
            if (duration <= 0)
            {
                duration = 0.5f;
            }
        }
        else
        {
            // 持续伤害技能，开始协程
            StartCoroutine(DamageOverTime());
        }

        // 设置自动销毁
        if (duration > 0)
        {
            StartCoroutine(DestroyAfterDuration());
        }
    }

    void Update()
    {
        // 更新AOE大小（根据持续时间缩放）
        if (!isInstant && duration > 0)
        {
            float progress = (Time.time - startTime) / duration;
            UpdateAOEScale(progress);
        }

        // 更新光效强度
        if (effectLight != null && duration > 0)
        {
            float progress = (Time.time - startTime) / duration;
            effectLight.intensity = Mathf.Lerp(2f, 0f, progress);
        }
    }

    void UpdateAOEScale(float progress = 0f)
    {
        // 根据半径设置AOE大小
        float scale = radius * 2f / 5f; // 假设原始预制体是5单位大小
        transform.localScale = originalScale * scale;

        // 如果是持续AOE，可以根据进度缩小
        if (!isInstant && progress > 0)
        {
            float shrinkFactor = Mathf.Lerp(1f, 0.5f, progress);
            transform.localScale = originalScale * scale * shrinkFactor;
        }
    }

    IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);

        // 淡出效果
        if (mainParticles != null)
        {
            mainParticles.Stop();
        }

        if (audioSource != null && audioSource.loop)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }

        // 等待粒子消失
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }

    void DealDamage()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        damagedEnemies.Clear();

        foreach (var enemy in enemies)
        {
            // 避免重复伤害
            if (damagedEnemies.Contains(enemy.gameObject)) continue;

            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)damage);
                damagedEnemies.Add(enemy.gameObject);

                Debug.Log($"AOE技能对 {enemy.name} 造成 {damage} 点伤害");

                // 击退效果
                if (applyKnockback)
                {
                    ApplyKnockback(enemy);
                }
            }
        }
    }

    IEnumerator DamageOverTime()
    {
        float elapsedTime = 0f;
        float tickInterval = 0.5f; // 每0.5秒造成一次伤害

        while (elapsedTime < duration)
        {
            DealDamage();
            elapsedTime += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
    }

    void ApplyKnockback(Collider enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (enemy.transform.position - transform.position).normalized;
            direction.y = 0.3f; // 稍微向上击飞
            rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = isInstant ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 显示伤害区域
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 显示伤害范围
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 point = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            Gizmos.DrawLine(transform.position, point);
        }
    }

    // 公共方法：获取剩余时间
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, duration - (Time.time - startTime));
    }

    // 公共方法：手动触发伤害
    public void TriggerDamage()
    {
        DealDamage();
    }

    // 公共方法：扩展AOE范围
    public void ExpandRadius(float additionalRadius, float expandTime)
    {
        StartCoroutine(ExpandRadiusCoroutine(additionalRadius, expandTime));
    }

    IEnumerator ExpandRadiusCoroutine(float additionalRadius, float expandTime)
    {
        float startRadius = radius;
        float targetRadius = radius + additionalRadius;
        float expandStartTime = Time.time;

        while (Time.time - expandStartTime < expandTime)
        {
            float progress = (Time.time - expandStartTime) / expandTime;
            radius = Mathf.Lerp(startRadius, targetRadius, progress);
            UpdateAOEScale();
            yield return null;
        }

        radius = targetRadius;
        UpdateAOEScale();
    }
}