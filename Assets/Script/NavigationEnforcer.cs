using UnityEngine;
using UnityEngine.AI;

public class NavigationEnforcer : MonoBehaviour
{
    [Header("导航限制设置")]
    public bool enableNavigationRestriction = true;
    public float checkInterval = 0.1f;
    public float maxDistanceFromNavMesh = 2.0f;
    public float pullBackForce = 10.0f;

    [Header("边界设置")]
    public bool useWorldBoundary = true;
    public float worldBoundaryRadius = 50f;
    public Vector3 worldCenter = Vector3.zero;

    [Header("调试可视化")]
    public bool showDebugGizmos = true;
    public Color safeColor = Color.green;
    public Color dangerColor = Color.red;
    public Color boundaryColor = Color.yellow;

    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private float lastCheckTime;
    private Vector3 lastSafePosition;
    private bool wasOffNavMesh = false;

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody>();
            lastSafePosition = playerController.transform.position;
        }
        else
        {
            Debug.LogError("未找到 PlayerController!");
        }

        Debug.Log("导航限制系统初始化完成");
    }

    void Update()
    {
        if (!enableNavigationRestriction || playerController == null) return;

        // 定期检查，避免每帧都检查
        if (Time.time - lastCheckTime >= checkInterval)
        {
            CheckNavigationConstraints();
            lastCheckTime = Time.time;
        }
    }

    void CheckNavigationConstraints()
    {
        Vector3 playerPos = playerController.transform.position;
        bool isOnNavMesh = IsPositionOnNavMesh(playerPos, maxDistanceFromNavMesh);

        // 检查世界边界
        bool isWithinBoundary = true;
        if (useWorldBoundary)
        {
            isWithinBoundary = IsWithinWorldBoundary(playerPos);
        }

        if (isOnNavMesh && isWithinBoundary)
        {
            // 玩家在安全区域
            lastSafePosition = playerPos;
            wasOffNavMesh = false;
        }
        else
        {
            // 玩家离开安全区域
            if (!wasOffNavMesh)
            {
                Debug.LogWarning($"玩家离开安全区域! 导航网格: {isOnNavMesh}, 边界: {isWithinBoundary}");
                wasOffNavMesh = true;
            }

            // 将玩家拉回安全位置
            PullPlayerToSafety();
        }
    }

    bool IsPositionOnNavMesh(Vector3 position, float maxDistance)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas);
    }

    bool IsWithinWorldBoundary(Vector3 position)
    {
        if (!useWorldBoundary) return true;

        Vector2 playerXZ = new Vector2(position.x, position.z);
        Vector2 centerXZ = new Vector2(worldCenter.x, worldCenter.z);

        return Vector2.Distance(playerXZ, centerXZ) <= worldBoundaryRadius;
    }

    void PullPlayerToSafety()
    {
        if (playerRigidbody == null) return;

        Vector3 playerPos = playerController.transform.position;
        Vector3 safePosition = FindNearestSafePosition(playerPos);

        // 计算拉回方向
        Vector3 pullDirection = (safePosition - playerPos).normalized;
        float distanceToSafe = Vector3.Distance(playerPos, safePosition);

        // 应用拉回力（距离越远，力越大）
        float forceMultiplier = Mathf.Clamp(distanceToSafe, 0.5f, 5f);
        playerRigidbody.AddForce(pullDirection * pullBackForce * forceMultiplier, ForceMode.Force);

        // 如果距离太远，直接传送
        if (distanceToSafe > 10f)
        {
            playerController.TeleportTo(safePosition);
            Debug.Log("玩家距离安全区域太远，直接传送");
        }

        Debug.DrawLine(playerPos, safePosition, Color.magenta, 2f);
    }

    Vector3 FindNearestSafePosition(Vector3 fromPosition)
    {
        // 首先尝试找到最近的导航网格点
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fromPosition, out hit, maxDistanceFromNavMesh * 2f, NavMesh.AllAreas))
        {
            // 确保找到的位置在世界边界内
            if (!useWorldBoundary || IsWithinWorldBoundary(hit.position))
            {
                return hit.position;
            }
        }

        // 如果导航网格查找失败，使用世界边界内的位置
        if (useWorldBoundary)
        {
            Vector2 fromXZ = new Vector2(fromPosition.x, fromPosition.z);
            Vector2 centerXZ = new Vector2(worldCenter.x, worldCenter.z);

            Vector2 directionToCenter = (centerXZ - fromXZ).normalized;
            Vector2 safeXZ = centerXZ + directionToCenter * (worldBoundaryRadius * 0.9f);

            // 在安全XZ位置上找到最近的导航网格点
            Vector3 testPosition = new Vector3(safeXZ.x, fromPosition.y, safeXZ.y);
            if (NavMesh.SamplePosition(testPosition, out hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return new Vector3(safeXZ.x, worldCenter.y, safeXZ.y);
        }

        // 最后手段：返回上一个安全位置
        return lastSafePosition;
    }

    // 在Scene视图中绘制调试信息
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 绘制世界边界
        if (useWorldBoundary)
        {
            Gizmos.color = boundaryColor;
            Gizmos.DrawWireSphere(worldCenter, worldBoundaryRadius);
        }

        // 绘制安全区域
        if (Application.isPlaying && playerController != null)
        {
            Vector3 playerPos = playerController.transform.position;
            bool isSafe = IsPositionOnNavMesh(playerPos, maxDistanceFromNavMesh) &&
                         (!useWorldBoundary || IsWithinWorldBoundary(playerPos));

            Gizmos.color = isSafe ? safeColor : dangerColor;
            Gizmos.DrawWireSphere(playerPos, 1f);

            // 绘制到最近安全位置的线
            if (!isSafe)
            {
                Vector3 safePos = FindNearestSafePosition(playerPos);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(playerPos, safePos);
                Gizmos.DrawWireCube(safePos, Vector3.one);
            }
        }
    }

    // 公共方法
    public void SetWorldBoundary(float radius, Vector3 center)
    {
        worldBoundaryRadius = radius;
        worldCenter = center;
        Debug.Log($"世界边界更新: 半径={radius}, 中心={center}");
    }

    public void EnableRestrictions(bool enable)
    {
        enableNavigationRestriction = enable;
        Debug.Log($"导航限制: {(enable ? "启用" : "禁用")}");
    }

    public bool IsPlayerInSafeArea()
    {
        if (playerController == null) return false;

        Vector3 playerPos = playerController.transform.position;
        return IsPositionOnNavMesh(playerPos, maxDistanceFromNavMesh) &&
               (!useWorldBoundary || IsWithinWorldBoundary(playerPos));
    }
}