using UnityEngine;

public class WorldBoundaryCreator : MonoBehaviour
{
    [Header("边界设置")]
    public float boundaryRadius = 50f;
    public float wallHeight = 10f;
    public float wallThickness = 1f;
    public Material boundaryMaterial;
    public bool createInvisibleWalls = true;

    [Header("边界触发器")]
    public bool createTriggerBoundary = true;
    public float triggerRadius = 45f;

    private GameObject boundaryParent;
    private GameObject triggerBoundary;

    void Start()
    {
        CreateWorldBoundary();
        Debug.Log("世界边界创建完成");
    }

    void CreateWorldBoundary()
    {
        boundaryParent = new GameObject("WorldBoundaries");
        boundaryParent.transform.SetParent(transform);

        if (createInvisibleWalls)
        {
            CreateCircularBoundary();
        }

        if (createTriggerBoundary)
        {
            CreateTriggerBoundary();
        }
    }

    void CreateCircularBoundary()
    {
        int segments = 32; // 圆形边界的段数
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 startPos = new Vector3(
                Mathf.Cos(angle) * boundaryRadius,
                0,
                Mathf.Sin(angle) * boundaryRadius
            );

            Vector3 endPos = new Vector3(
                Mathf.Cos(nextAngle) * boundaryRadius,
                0,
                Mathf.Sin(nextAngle) * boundaryRadius
            );

            CreateBoundaryWall(startPos, endPos, i);
        }
    }

    void CreateBoundaryWall(Vector3 start, Vector3 end, int index)
    {
        GameObject wall = new GameObject($"BoundaryWall_{index}");
        wall.transform.SetParent(boundaryParent.transform);

        // 计算墙的位置和方向
        Vector3 center = (start + end) * 0.5f;
        Vector3 direction = (end - start).normalized;
        float length = Vector3.Distance(start, end);

        wall.transform.position = center;
        wall.transform.rotation = Quaternion.LookRotation(direction);

        // 添加碰撞体
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(wallThickness, wallHeight, length);
        collider.center = new Vector3(0, wallHeight * 0.5f, 0);

        // 可选：添加可视化
        if (boundaryMaterial != null)
        {
            MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
            MeshFilter filter = wall.AddComponent<MeshFilter>();
            filter.mesh = CreateWallMesh(length, wallHeight, wallThickness);
            renderer.material = boundaryMaterial;
            renderer.material.color = new Color(1, 0, 0, 0.3f);
        }

        // 添加物理材质防止滑动
        PhysicMaterial physMat = new PhysicMaterial();
        physMat.dynamicFriction = 1f;
        physMat.staticFriction = 1f;
        physMat.bounciness = 0f;
        collider.material = physMat;
    }

    void CreateTriggerBoundary()
    {
        triggerBoundary = new GameObject("TriggerBoundary");
        triggerBoundary.transform.SetParent(boundaryParent.transform);
        triggerBoundary.transform.position = Vector3.zero;

        // 添加球形触发器
        SphereCollider trigger = triggerBoundary.AddComponent<SphereCollider>();
        trigger.radius = triggerRadius;
        trigger.isTrigger = true;

        // 添加触发器脚本
        BoundaryTrigger boundaryTrigger = triggerBoundary.AddComponent<BoundaryTrigger>();
    }

    Mesh CreateWallMesh(float length, float height, float thickness)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[8];
        // 底部四个顶点
        vertices[0] = new Vector3(-thickness * 0.5f, 0, -length * 0.5f);
        vertices[1] = new Vector3(thickness * 0.5f, 0, -length * 0.5f);
        vertices[2] = new Vector3(thickness * 0.5f, 0, length * 0.5f);
        vertices[3] = new Vector3(-thickness * 0.5f, 0, length * 0.5f);
        // 顶部四个顶点
        vertices[4] = new Vector3(-thickness * 0.5f, height, -length * 0.5f);
        vertices[5] = new Vector3(thickness * 0.5f, height, -length * 0.5f);
        vertices[6] = new Vector3(thickness * 0.5f, height, length * 0.5f);
        vertices[7] = new Vector3(-thickness * 0.5f, height, length * 0.5f);

        int[] triangles = {
            // 前面
            0, 4, 5, 0, 5, 1,
            // 后面
            2, 6, 7, 2, 7, 3,
            // 左面
            3, 7, 4, 3, 4, 0,
            // 右面
            1, 5, 6, 1, 6, 2,
            // 顶部
            4, 7, 6, 4, 6, 5,
            // 底部
            0, 1, 2, 0, 2, 3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, boundaryRadius);

        if (createTriggerBoundary)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Vector3.zero, triggerRadius);
        }
    }
}

// 边界触发器脚本
public class BoundaryTrigger : MonoBehaviour
{
    public float pushForce = 15f;
    public float warningDistance = 5f;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 playerPos = other.transform.position;
            Vector3 centerToPlayer = playerPos - transform.position;
            float distanceToCenter = centerToPlayer.magnitude;
            float triggerRadius = GetComponent<SphereCollider>().radius;

            // 计算距离边界的距离
            float distanceToBoundary = triggerRadius - distanceToCenter;

            // 如果接近边界，施加推力
            if (distanceToBoundary < warningDistance)
            {
                Vector3 pushDirection = -centerToPlayer.normalized;
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float forceMultiplier = 1f - (distanceToBoundary / warningDistance);
                    rb.AddForce(pushDirection * pushForce * forceMultiplier, ForceMode.Force);
                }

                // 可视化警告
                Debug.DrawRay(playerPos, pushDirection * distanceToBoundary, Color.yellow);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.LogWarning("玩家离开安全区域！");

            // 立即将玩家传回边界内
            Vector3 playerPos = other.transform.position;
            Vector3 directionToCenter = (transform.position - playerPos).normalized;
            float triggerRadius = GetComponent<SphereCollider>().radius;

            Vector3 safePosition = transform.position + directionToCenter * (triggerRadius * 0.9f);

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TeleportTo(safePosition);
            }
            else
            {
                other.transform.position = safePosition;
            }
        }
    }
}