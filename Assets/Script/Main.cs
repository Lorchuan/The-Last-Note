using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Linq;

public class GyroGestureRecognizer : MonoBehaviour
{
    [Header("串口设置")]
    public string portName = "COM3";
    public int baudRate = 115200;

    [Header("手势识别参数")]
    public float sampleRate = 50f; // 采样率(Hz)
    public int bufferSize = 100; // 缓冲区大小
    public float minGestureSpeed = 0.5f; // 最小手势速度阈值

    [Header("调试选项")]
    public bool debugMode = true;
    public LineRenderer trajectoryRenderer;

    // 私有变量
    private SerialPort serialPort;
    private Queue<Vector3> gyroBuffer = new Queue<Vector3>();
    private Vector3 lastPosition = Vector3.zero;
    private bool isSampling = false;

    // 预定义手势模板
    private Dictionary<string, List<Vector3>> gestureTemplates = new Dictionary<string, List<Vector3>>();

    // 事件委托
    public System.Action<string> OnGestureRecognized;

    void Start()
    {
        InitializeSerialPort();
        InitializeGestureTemplates();
        StartCoroutine(DataSamplingCoroutine());

        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.positionCount = 0;
        }
    }

    void InitializeSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            Debug.Log($"串口 {portName} 打开成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"串口打开失败: {e.Message}");
        }
    }

    void InitializeGestureTemplates()
    {
        // 圆形手势模板
        gestureTemplates["Circle"] = GenerateCircleTemplate();

        // 方形手势模板
        gestureTemplates["Square"] = GenerateSquareTemplate();

        // 三角形手势模板
        gestureTemplates["Triangle"] = GenerateTriangleTemplate();

        // 无限符号手势模板
        gestureTemplates["Infinity"] = GenerateInfinityTemplate();
    }
    // 数据采样协程
    private IEnumerator DataSamplingCoroutine()
    {
        while (true)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                ReadGyroData();
            }
            yield return new WaitForSeconds(1f / sampleRate);
        }
    }

    // 读取陀螺仪数据
    private void ReadGyroData()
    {
        try
        {
            string data = serialPort.ReadLine();
            ProcessGyroData(data);
        }
        catch (System.TimeoutException) { }
        catch (System.Exception e)
        {
            if (debugMode) Debug.LogWarning($"读取数据异常: {e.Message}");
        }
    }

    // 处理陀螺仪数据
    private void ProcessGyroData(string data)
    {
        // 假设数据格式: "gx,gy,gz" 或 "x,y,z"
        string[] values = data.Split(',');

        if (values.Length >= 3)
        {
            float x = float.Parse(values[0]);
            float y = float.Parse(values[1]);
            float z = float.Parse(values[2]);

            Vector3 currentGyro = new Vector3(x, y, z);

            // 积分得到位置（简化处理）
            Vector3 currentPosition = lastPosition + currentGyro * Time.deltaTime;

            UpdateGestureBuffer(currentPosition);
            lastPosition = currentPosition;

            // 实时手势检测
            CheckForGesture();
        }
    }

    // 更新手势缓冲区
    private void UpdateGestureBuffer(Vector3 position)
    {
        gyroBuffer.Enqueue(position);

        // 保持缓冲区大小
        while (gyroBuffer.Count > bufferSize)
        {
            gyroBuffer.Dequeue();
        }

        // 更新轨迹渲染
        UpdateTrajectoryRenderer();
    }
    // 主要手势检测函数
    private void CheckForGesture()
    {
        if (gyroBuffer.Count < 10) return; // 确保有足够的数据点

        List<Vector3> currentTrajectory = gyroBuffer.ToList();

        // 检查手势速度是否足够
        if (!IsGestureSpeedSufficient(currentTrajectory)) return;

        // 归一化轨迹以便比较
        List<Vector3> normalizedTrajectory = NormalizeTrajectory(currentTrajectory);

        // 与所有模板进行比较
        string recognizedGesture = RecognizeGesture(normalizedTrajectory);

        if (!string.IsNullOrEmpty(recognizedGesture))
        {
            OnGestureDetected(recognizedGesture);
        }
    }

    // 手势识别核心算法
    private string RecognizeGesture(List<Vector3> trajectory)
    {
        float bestScore = float.MaxValue;
        string bestMatch = "";

        foreach (var template in gestureTemplates)
        {
            float score = CalculateDTWDistance(trajectory, template.Value);

            if (score < bestScore && score < GetGestureThreshold(template.Key))
            {
                bestScore = score;
                bestMatch = template.Key;
            }
        }

        return bestMatch;
    }

    // 动态时间规整(DTW)距离计算
    private float CalculateDTWDistance(List<Vector3> sequence1, List<Vector3> sequence2)
    {
        int n = sequence1.Count;
        int m = sequence2.Count;

        float[,] dtw = new float[n + 1, m + 1];

        // 初始化
        for (int i = 0; i <= n; i++)
            for (int j = 0; j <= m; j++)
                dtw[i, j] = float.MaxValue;

        dtw[0, 0] = 0;

        // 计算DTW矩阵
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                float cost = Vector3.Distance(sequence1[i - 1], sequence2[j - 1]);
                dtw[i, j] = cost + Mathf.Min(dtw[i - 1, j],     // 插入
                                            dtw[i, j - 1],     // 删除
                                            dtw[i - 1, j - 1]); // 匹配
            }
        }

        return dtw[n, m] / (n + m); // 归一化
    }
    // 生成各种手势模板
    private List<Vector3> GenerateCircleTemplate(int points = 50)
    {
        List<Vector3> circle = new List<Vector3>();
        float radius = 2f;

        for (int i = 0; i < points; i++)
        {
            float angle = i * 2 * Mathf.PI / points;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            circle.Add(new Vector3(x, y, 0));
        }

        return circle;
    }

    private List<Vector3> GenerateSquareTemplate(int pointsPerSide = 15)
    {
        List<Vector3> square = new List<Vector3>();
        float size = 3f;

        // 上边
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            square.Add(new Vector3(Mathf.Lerp(-size, size, t), size, 0));
        }

        // 右边
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            square.Add(new Vector3(size, Mathf.Lerp(size, -size, t), 0));
        }

        // 下边
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            square.Add(new Vector3(Mathf.Lerp(size, -size, t), -size, 0));
        }

        // 左边
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            square.Add(new Vector3(-size, Mathf.Lerp(-size, size, t), 0));
        }

        return square;
    }

    private List<Vector3> GenerateTriangleTemplate(int pointsPerSide = 20)
    {
        List<Vector3> triangle = new List<Vector3>();
        float height = 4f;
        float halfBase = 2f;

        Vector3[] vertices = {
        new Vector3(0, height, 0),
        new Vector3(-halfBase, 0, 0),
        new Vector3(halfBase, 0, 0)
    };

        // 边1: 顶点到左底点
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            triangle.Add(Vector3.Lerp(vertices[0], vertices[1], t));
        }

        // 边2: 左底点到右底点
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            triangle.Add(Vector3.Lerp(vertices[1], vertices[2], t));
        }

        // 边3: 右底点到顶点
        for (int i = 0; i < pointsPerSide; i++)
        {
            float t = (float)i / (pointsPerSide - 1);
            triangle.Add(Vector3.Lerp(vertices[2], vertices[0], t));
        }

        return triangle;
    }

    private List<Vector3> GenerateInfinityTemplate(int points = 60)
    {
        List<Vector3> infinity = new List<Vector3>();

        for (int i = 0; i < points; i++)
        {
            float t = i * 2 * Mathf.PI / points;
            float x = 2f * Mathf.Sin(t);
            float y = Mathf.Sin(2f * t);
            infinity.Add(new Vector3(x, y, 0));
        }

        return infinity;
    }
    // 辅助函数
    private List<Vector3> NormalizeTrajectory(List<Vector3> trajectory)
    {
        if (trajectory.Count == 0) return trajectory;

        // 计算质心
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 point in trajectory)
        {
            centroid += point;
        }
        centroid /= trajectory.Count;

        // 居中
        List<Vector3> centered = new List<Vector3>();
        foreach (Vector3 point in trajectory)
        {
            centered.Add(point - centroid);
        }

        // 归一化尺度
        float maxDistance = 0;
        foreach (Vector3 point in centered)
        {
            maxDistance = Mathf.Max(maxDistance, point.magnitude);
        }

        if (maxDistance > 0)
        {
            List<Vector3> normalized = new List<Vector3>();
            foreach (Vector3 point in centered)
            {
                normalized.Add(point / maxDistance);
            }
            return normalized;
        }

        return centered;
    }

    private bool IsGestureSpeedSufficient(List<Vector3> trajectory)
    {
        if (trajectory.Count < 2) return false;

        float totalDistance = 0;
        for (int i = 1; i < trajectory.Count; i++)
        {
            totalDistance += Vector3.Distance(trajectory[i - 1], trajectory[i]);
        }

        float averageSpeed = totalDistance / (trajectory.Count / sampleRate);
        return averageSpeed > minGestureSpeed;
    }

    private float GetGestureThreshold(string gestureName)
    {
        switch (gestureName)
        {
            case "Circle": return 0.3f;
            case "Square": return 0.4f;
            case "Triangle": return 0.35f;
            case "Infinity": return 0.5f;
            default: return 0.4f;
        }
    }

    // 手势检测成功事件
    private void OnGestureDetected(string gestureName)
    {
        Debug.Log($"手势识别: {gestureName}");

        // 触发事件
        OnGestureRecognized?.Invoke(gestureName);

        // 根据手势执行不同功能
        switch (gestureName)
        {
            case "Circle":
                OnCircleGesture();
                break;
            case "Square":
                OnSquareGesture();
                break;
            case "Triangle":
                OnTriangleGesture();
                break;
            case "Infinity":
                OnInfinityGesture();
                break;
        }

        // 清除缓冲区重新开始
        gyroBuffer.Clear();

        if (debugMode)
        {
            Debug.Log($"<color=green>识别到手势: {gestureName}</color>");
        }
    }

    // 调试可视化
    private void UpdateTrajectoryRenderer()
    {
        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.positionCount = gyroBuffer.Count;
            trajectoryRenderer.SetPositions(gyroBuffer.ToArray());
        }
    }
    // 各种手势的响应函数
    private void OnCircleGesture()
    {
        // 圆形手势的响应
        Debug.Log("执行圆形手势功能");

        // 示例：在VR中生成一个能量球
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        sphere.transform.localScale = Vector3.one * 0.5f;

        // 添加一些视觉效果
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material.color = Color.blue;

        // 3秒后销毁
        Destroy(sphere, 3f);
    }

    private void OnSquareGesture()
    {
        // 方形手势的响应
        Debug.Log("执行方形手势功能");

        // 示例：创建防护罩
        // 这里可以添加你的VR特效
    }

    private void OnTriangleGesture()
    {
        // 三角形手势的响应
        Debug.Log("执行三角形手势功能");

        // 示例：发射攻击
        // 这里可以添加你的VR攻击逻辑
    }

    private void OnInfinityGesture()
    {
        // 无限符号手势的响应
        Debug.Log("执行无限符号手势功能");

        // 示例：切换武器或模式
        // 这里可以添加你的VR模式切换逻辑
    }

    // 清理资源
    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
