using UnityEngine;

public class SandstormController : MonoBehaviour
{
    [Header("配置")]
    public Transform player;           // 玩家位置
    public Transform duneTopTarget;    // 坡顶的目标点（沙尘暴完全消失的位置）
    public ParticleSystem sandstormPS; // 沙尘暴粒子系统

    [Header("渐变参数")]
    public float maxIntensityDistance = 30f; // 距离坡顶多远时，沙尘暴最大
    public float minIntensityDistance = 5f;  // 距离坡顶多远时，沙尘暴完全消失

    [Header("初始最大值")]
    public float maxEmissionRate = 100f; // 初始设计的最大粒子发射率

    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private float initialStartColorAlpha;

    void Start()
    {
        if (sandstormPS == null) sandstormPS = GetComponent<ParticleSystem>();

        emissionModule = sandstormPS.emission;
        mainModule = sandstormPS.main;
        initialStartColorAlpha = mainModule.startColor.color.a; // 记录初始透明度

        // 如果没手动拖拽，尝试自动寻找
        if (player == null && GameObject.FindWithTag("Player"))
            player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null || duneTopTarget == null ||sandstormPS==null) return;

        // 1. 计算玩家到坡顶的距离
        float distance = Vector3.Distance(player.position, duneTopTarget.position);

        // 2. 计算强度系数 (0到1之间)
        // 距离越远(>30m)系数为1，距离越近(<5m)系数为0
        float intensity = Mathf.InverseLerp(minIntensityDistance, maxIntensityDistance, distance);

        // 3. 修改粒子发射率 (控制密度)
        emissionModule.rateOverTime = maxEmissionRate * intensity;

        // 4. 修改粒子透明度 (可选，让消失更彻底)
        // 注意：粒子系统颜色修改稍微复杂点，通常只改Emission就够了，
        // 这里提供修改Alpha的方法以防万一
        Color c = mainModule.startColor.color;
        c.a = initialStartColorAlpha * intensity;
        mainModule.startColor = c;

        // 5. 如果完全到达坡顶，可以彻底关闭粒子系统以节省Update开销
        if (intensity <= 0.01f && sandstormPS.isPlaying)
        {
            sandstormPS.Stop();
            Destroy(sandstormPS);
        }
        else if (intensity > 0.01f && !sandstormPS.isPlaying)
        {
            sandstormPS.Play();
        }
    }
}