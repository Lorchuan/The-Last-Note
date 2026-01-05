using UnityEngine;
using UnityEngine.UI;

public class EnemyMarker : MonoBehaviour
{
    [Header("标记物体")]
    public GameObject lockMarkerPrefab; // 这是一个世界空间的Prefab (例如一个红色的箭头模型或WorldSpace Canvas)
    public Vector3 offset = new Vector3(0, 2.5f, 0); // 在敌人头顶的高度

    [Header("动画")]
    public float rotateSpeed = 90f;
    public float floatSpeed = 2f;
    public float floatAmplitude = 0.2f;

    // 运行时实例
    private GameObject currentMarkerInstance;
    private WandController wandController;
    private bool isInitialized = false;

    void Start()
    {
        if (!isInitialized) Initialize(FindObjectOfType<WandController>());
    }

    public void Initialize(WandController wc)
    {
        wandController = wc;

        // 实例化唯一的标记物体，初始隐藏
        if (lockMarkerPrefab != null)
        {
            currentMarkerInstance = Instantiate(lockMarkerPrefab);
            currentMarkerInstance.SetActive(false);
            // 确保不随父物体销毁
            DontDestroyOnLoad(currentMarkerInstance);
        }

        isInitialized = true;
    }

    // 必须在 Update 或 LateUpdate 中调用
    public void UpdateMarker()
    {
        if (!isInitialized || wandController == null || currentMarkerInstance == null) return;

        Transform lockedTarget = wandController.GetLockedEnemy();

        if (lockedTarget != null)
        {
            if (!currentMarkerInstance.activeSelf) currentMarkerInstance.SetActive(true);

            // 1. 位置跟随：直接设置在敌人头顶
            Vector3 targetPos = lockedTarget.position + offset;

            // 添加上下浮动动画
            targetPos.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

            currentMarkerInstance.transform.position = targetPos;

            // 2. 朝向：在VR中，标记通常应该面向摄像机 (Billboard效果)
            if (Camera.main != null)
            {
                currentMarkerInstance.transform.LookAt(Camera.main.transform);
                // 修正旋转，因为LookAt是Z轴指向目标，UI通常是Z轴反向
                currentMarkerInstance.transform.Rotate(0, 180, 0);
            }

            // 或者如果你想要自转效果（比如3D箭头）：
            // currentMarkerInstance.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
        else
        {
            if (currentMarkerInstance.activeSelf) currentMarkerInstance.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (currentMarkerInstance != null) Destroy(currentMarkerInstance);
    }
}   