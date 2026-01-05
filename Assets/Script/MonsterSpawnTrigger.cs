using UnityEngine;
using System.Collections.Generic;

public class MonsterSpawnTrigger : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject monsterPrefab; // 怪物预制件
    public int spawnCount = 1; // 每次触发生成的怪物数量
    public bool spawnOnce = true; // 是否只生成一次
    public bool spawnOnAwake = false; // 是否在游戏开始时生成

    [Header("生成点设置")]
    public List<Transform> spawnPoints = new List<Transform>(); // 生成点列表
    public SpawnMode spawnMode = SpawnMode.RandomPoints; // 生成模式

    [Header("生成后设置")]
    public bool destroyAfterSpawn = false; // 生成后销毁触发器
    public float destroyDelay = 0f; // 销毁延迟
    public bool disableColliderAfterSpawn = true; // 生成后禁用碰撞器

    [Header("怪物属性设置")]
    public bool overrideMonsterHealth = false;
    public int monsterHealth = 60;
    public bool overrideMonsterDamage = false;
    public float monsterDamage = 10f;
    public bool overrideMonsterSpeed = false;
    public float monsterMoveSpeed = 3.5f;

    [Header("生成特效")]
    public GameObject spawnEffectPrefab; // 生成特效预制件
    public float effectDuration = 2f; // 特效持续时间

    [Header("调试")]
    public bool showDebugInfo = true;
    public Color gizmoColor = Color.green;

    // 内部变量
    private bool hasSpawned = false;
    private Collider triggerCollider;
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    // 生成模式枚举
    public enum SpawnMode
    {
        AllPoints,      // 在所有点生成
        RandomPoints,   // 在随机点生成
        Sequential,     // 按顺序生成
        CustomOrder     // 自定义顺序
    }

    void Start()
    {
        // 获取触发器碰撞器
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("怪物生成触发器需要碰撞器组件！");
        }

        // 如果设置了生成点为空，添加自身位置作为默认生成点
        if (spawnPoints.Count == 0)
        {
            spawnPoints.Add(transform);
            Debug.LogWarning("未指定生成点，将使用触发器位置作为生成点");
        }

        // 游戏开始时生成
        if (spawnOnAwake)
        {
            SpawnMonsters();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家进入触发器
        if (other.CompareTag("Player"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"玩家进入怪物生成触发器: {gameObject.name}");
            }

            // 检查是否已经生成过
            if (spawnOnce && hasSpawned)
            {
                if (showDebugInfo) Debug.Log("怪物已生成过，跳过");
                return;
            }

            // 生成怪物
            SpawnMonsters();
            hasSpawned = true;

            // 生成后禁用碰撞器
            if (disableColliderAfterSpawn && triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            // 生成后销毁触发器
            if (destroyAfterSpawn)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }

    public void SpawnMonsters()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("未分配怪物预制件！");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("没有可用的生成点！");
            return;
        }

        // 计算实际要生成的怪物数量
        int monstersToSpawn = Mathf.Min(spawnCount, spawnPoints.Count * 2);
        monstersToSpawn = Mathf.Max(1, monstersToSpawn); // 至少生成1个

        if (showDebugInfo)
        {
            Debug.Log($"开始生成怪物: 数量={monstersToSpawn}, 模式={spawnMode}");
        }

        // 根据生成模式选择生成点
        List<Transform> selectedSpawnPoints = GetSelectedSpawnPoints(monstersToSpawn);

        // 生成怪物
        for (int i = 0; i < selectedSpawnPoints.Count; i++)
        {
            Transform spawnPoint = selectedSpawnPoints[i];
            SpawnSingleMonster(spawnPoint, i);
        }
    }

    List<Transform> GetSelectedSpawnPoints(int count)
    {
        List<Transform> selectedPoints = new List<Transform>();

        switch (spawnMode)
        {
            case SpawnMode.AllPoints:
                // 在所有点生成，但不超过怪物数量
                selectedPoints.AddRange(spawnPoints);
                if (selectedPoints.Count > count)
                {
                    // 随机选择指定数量的点
                    selectedPoints = GetRandomPoints(selectedPoints, count);
                }
                break;

            case SpawnMode.RandomPoints:
                // 随机选择生成点
                selectedPoints = GetRandomPoints(spawnPoints, count);
                break;

            case SpawnMode.Sequential:
                // 按顺序选择生成点
                for (int i = 0; i < count; i++)
                {
                    selectedPoints.Add(spawnPoints[i % spawnPoints.Count]);
                }
                break;

            case SpawnMode.CustomOrder:
                // 自定义顺序（直接使用前count个点）
                for (int i = 0; i < Mathf.Min(count, spawnPoints.Count); i++)
                {
                    selectedPoints.Add(spawnPoints[i]);
                }
                break;
        }

        return selectedPoints;
    }

    List<Transform> GetRandomPoints(List<Transform> sourcePoints, int count)
    {
        List<Transform> randomPoints = new List<Transform>();
        List<Transform> tempList = new List<Transform>(sourcePoints);

        // 确保不超过可用点数
        count = Mathf.Min(count, tempList.Count);

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break;

            int randomIndex = Random.Range(0, tempList.Count);
            randomPoints.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return randomPoints;
    }

    void SpawnSingleMonster(Transform spawnPoint, int index)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning($"生成点 {index} 为空，跳过生成");
            return;
        }

        // 创建生成特效
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, spawnPoint.position, spawnPoint.rotation);
            Destroy(effect, effectDuration);
        }

        // 生成怪物
        GameObject monster = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);

        // 设置怪物名称
        monster.name = $"{monsterPrefab.name}_{Time.frameCount}_{index}";

        // 添加到已生成列表
        spawnedMonsters.Add(monster);

        // 配置怪物属性
        ConfigureMonster(monster);

        if (showDebugInfo)
        {
            Debug.Log($"生成怪物: {monster.name} 在位置: {spawnPoint.position}");
        }
    }

    void ConfigureMonster(GameObject monster)
    {
        // 尝试获取EnemyController组件并配置属性
        EnemyController enemyController = monster.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            if (overrideMonsterHealth)
            {
                // 通过反射设置私有变量
                System.Reflection.FieldInfo healthField = typeof(EnemyController).GetField("currentHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (healthField != null)
                {
                    healthField.SetValue(enemyController, monsterHealth);
                }

                // 如果maxHealth是public字段或属性
                enemyController.maxHealth = monsterHealth;
            }

            if (overrideMonsterDamage)
            {
                // 如果attackDamage是public字段
                System.Reflection.FieldInfo damageField = typeof(EnemyController).GetField("attackDamage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (damageField != null)
                {
                    damageField.SetValue(enemyController, monsterDamage);
                }
            }
        }

        // 尝试配置NavMeshAgent的速度
        if (overrideMonsterSpeed)
        {
            UnityEngine.AI.NavMeshAgent agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = monsterMoveSpeed;
            }
        }
    }

    // 手动触发生成（可在其他脚本中调用）
    public void TriggerSpawn()
    {
        if (!hasSpawned || !spawnOnce)
        {
            SpawnMonsters();
            hasSpawned = true;
        }
    }

    // 获取已生成的怪物列表
    public List<GameObject> GetSpawnedMonsters()
    {
        return new List<GameObject>(spawnedMonsters);
    }

    // 清除所有已生成的怪物
    public void ClearSpawnedMonsters()
    {
        foreach (GameObject monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
    }

    // 重置触发器状态
    public void ResetTrigger()
    {
        hasSpawned = false;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    // 在场景视图中绘制调试信息
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // 绘制触发器范围
        Gizmos.color = gizmoColor;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }

        // 绘制生成点
        Gizmos.color = Color.red;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawSphere(spawnPoint.position, 0.3f);
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);

                // 绘制连线
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, spawnPoint.position);
                Gizmos.color = Color.red;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        OnDrawGizmos();

        // 在选中时额外绘制生成点标签
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 0.5f,
                    $"生成点 {i}", style);
#endif
            }
        }
    }

    // 在Inspector中添加按钮
    [ContextMenu("测试生成怪物")]
    void TestSpawn()
    {
        SpawnMonsters();
    }

    [ContextMenu("清除生成的怪物")]
    void ClearMonsters()
    {
        ClearSpawnedMonsters();
    }

    [ContextMenu("重置触发器")]
    void ResetSpawner()
    {
        ResetTrigger();
    }
}