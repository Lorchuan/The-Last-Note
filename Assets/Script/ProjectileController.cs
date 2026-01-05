using UnityEngine;
using System.Collections;

public class ProjectileController : MonoBehaviour
{
    [Header("移动设置")]
    public float speed = 20f;
    public float maxLifetime = 5f;
    public bool useGravity = false;
    public bool homing = false; // 是否追踪目标
    public Transform homingTarget;
    public float homingStrength = 5f;

    [Header("伤害设置")]
    public float damage = 10f;
    public LayerMask enemyLayer;
    public LayerMask collisionLayer = -1; // 默认所有层

    [Header("视觉效果")]
    public GameObject impactEffect;
    public ParticleSystem trailParticles;
    public Light projectileLight;

    [Header("爆炸效果")]
    public bool explodeOnImpact = false;
    public float explosionRadius = 0f;
    public float explosionDamage = 0f;
    public GameObject explosionEffect;

    [Header("音效")]
    public AudioClip launchSound;
    public AudioClip impactSound;

    // 内部变量
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasCollided = false;
    private float spawnTime;
    private Vector3 initialDirection;

    void Start()
    {
        spawnTime = Time.time;

        // 设置刚体
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 设置初始速度
        initialDirection = transform.forward;
        rb.velocity = initialDirection * speed;

        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.5f;

        // 播放发射音效
        if (launchSound != null)
        {
            audioSource.PlayOneShot(launchSound);
        }

        // 设置自动销毁
        StartCoroutine(DestroyAfterLifetime());

        // 初始化视觉效果
        InitializeVisuals();
    }

    void Update()
    {
        // 检查生命周期
        if (Time.time - spawnTime > maxLifetime && !hasCollided)
        {
            DestroyProjectile();
            return;
        }

        // 追踪目标
        if (homing && homingTarget != null && !hasCollided)
        {
            Vector3 targetDirection = (homingTarget.position - transform.position).normalized;
            Vector3 newDirection = Vector3.Lerp(rb.velocity.normalized, targetDirection, homingStrength * Time.deltaTime);
            rb.velocity = newDirection * speed;
            transform.rotation = Quaternion.LookRotation(newDirection);
        }

        // 更新视觉效果
        UpdateVisuals();
    }

    void InitializeVisuals()
    {
        // 确保有粒子效果
        if (trailParticles == null)
        {
            trailParticles = GetComponent<ParticleSystem>();
            if (trailParticles == null)
            {
                GameObject trail = new GameObject("Trail");
                trail.transform.SetParent(transform);
                trail.transform.localPosition = Vector3.zero;
                trailParticles = trail.AddComponent<ParticleSystem>();

                var main = trailParticles.main;
                main.startSize = 0.1f;
                main.startLifetime = 0.5f;
                main.startSpeed = 0f;
                main.maxParticles = 100;

                var emission = trailParticles.emission;
                emission.rateOverTime = 50;

                var shape = trailParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 5f;
                shape.radius = 0.01f;
            }
        }

        // 确保有灯光效果
        if (projectileLight == null)
        {
            projectileLight = GetComponent<Light>();
            if (projectileLight == null)
            {
                projectileLight = gameObject.AddComponent<Light>();
                projectileLight.type = LightType.Point;
                projectileLight.range = 5f;
                projectileLight.intensity = 2f;
                projectileLight.color = Color.red;
            }
        }
    }

    void UpdateVisuals()
    {
        // 根据速度更新粒子效果
        if (trailParticles != null)
        {
            var main = trailParticles.main;
            main.startSpeed = rb.velocity.magnitude * 0.1f;
        }

        // 更新灯光强度
        if (projectileLight != null)
        {
            float lifeProgress = (Time.time - spawnTime) / maxLifetime;
            projectileLight.intensity = Mathf.Lerp(2f, 0.5f, lifeProgress);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasCollided) return;

        // 检查是否为敌人
        bool isEnemy = ((1 << other.gameObject.layer) & enemyLayer) != 0;
        bool shouldCollide = ((1 << other.gameObject.layer) & collisionLayer) != 0 || isEnemy;

        if (!shouldCollide) return;

        // 标记已碰撞
        hasCollided = true;

        // 对敌人造成伤害
        if (isEnemy)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)damage);
                Debug.Log($"投射物对 {other.name} 造成 {damage} 点伤害");
            }
        }

        // 爆炸效果
        if (explodeOnImpact && explosionRadius > 0)
        {
            CreateExplosion();
        }

        // 播放碰撞音效
        if (impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }

        // 生成碰撞特效
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 销毁投射物
        DestroyProjectile();
    }

    void CreateExplosion()
    {
        // 创建爆炸效果
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // 对爆炸范围内的敌人造成伤害
        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);
        foreach (var enemy in enemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // 计算距离衰减伤害
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                float damageMultiplier = Mathf.Clamp01(1f - distance / explosionRadius);
                int explosionDamageAmount = Mathf.RoundToInt(explosionDamage * damageMultiplier);

                damageable.TakeDamage(explosionDamageAmount);
                Debug.Log($"爆炸对 {enemy.name} 造成 {explosionDamageAmount} 点伤害");
            }
        }

        
    }

    void DestroyProjectile()
    {
        // 禁用碰撞和渲染
        GetComponent<Collider>().enabled = false;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        // 停止粒子效果
        if (trailParticles != null)
        {
            trailParticles.Stop();
        }

        // 禁用灯光
        if (projectileLight != null)
        {
            projectileLight.enabled = false;
        }

        // 延迟销毁以允许音效播放完毕
        StartCoroutine(DelayedDestroy());
    }

    IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(maxLifetime);
        if (!hasCollided)
        {
            DestroyProjectile();
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制爆炸范围
        if (explodeOnImpact && explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        // 绘制移动方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    // 公共方法：设置追踪目标
    public void SetHomingTarget(Transform target)
    {
        homingTarget = target;
        homing = target != null;
    }

    // 公共方法：设置速度
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (rb != null)
        {
            rb.velocity = rb.velocity.normalized * speed;
        }
    }

    // 公共方法：获取剩余寿命
    public float GetRemainingLifetime()
    {
        return Mathf.Max(0f, maxLifetime - (Time.time - spawnTime));
    }
}