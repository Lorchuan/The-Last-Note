using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GestureSystem : MonoBehaviour
{
    [Header("视觉设置 (自动适配)")]
    public Camera renderCamera;
    public LineRenderer gestureLine;

    // 距离改近一点，防止穿模
    public float drawDistance = 0.5f;

    [Header("防抖与超时")]
    public float signalLossTolerance = 0.2f;
    public float maxGestureTime = 10.0f;

    [Header("识别设置")]
    public float minMoveDistance = 0.02f; // 本地坐标系下，这个阈值要小一点
    public float directionThreshold = 0.5f;

    [Header("手势参数")]
    public float minGestureLength = 0.1f; // 本地坐标系下长度判断也要缩小
    public float circleRecognitionThreshold = 0.7f;
    public float circleClosureThreshold = 0.35f;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool enableGestureMatching = true;

    [Header("手势映射")]
    public GestureMapping[] gestureMappings;

    [System.Serializable]
    public class GestureMapping
    {
        public string gestureCode;
        public int spellId;
        public string spellName;
    }

    private List<Vector2> points = new List<Vector2>(); // 屏幕坐标(用于识别)
    private bool isRecording = false;
    private WandController wandController;
    public SkillManager skillManager;
    private float gestureStartTime;
    private float gestureLength = 0f;

    // 存储本地坐标用于画线
    private List<Vector3> localLinePoints = new List<Vector3>();

    private float lostSignalTimer = 0f;

    void Start()
    {
        wandController = GetComponent<WandController>();


        // --- 1. 智能查找相机 ---
        GameObject uiCamObj = GameObject.Find("UICamera");
        if (uiCamObj != null)
        {
            renderCamera = uiCamObj.GetComponent<Camera>();
            Debug.Log("GestureSystem: VR模式 (UICamera)");
        }
        else
        {
            renderCamera = Camera.main;
            Debug.Log("GestureSystem: 单机模式 (MainCamera)");
        }

        // --- 2. 绑定 LineRenderer 到相机 (核心修改) ---
        if (gestureLine != null && renderCamera != null)
        {
            // 【关键】把线变成相机的子物体，这样相机动，线也跟着动
            gestureLine.transform.SetParent(renderCamera.transform);

            // 归零位置，确保它就在相机视野正前方
            gestureLine.transform.localPosition = Vector3.zero;
            gestureLine.transform.localRotation = Quaternion.identity;

            // 【关键】使用本地坐标，而非世界坐标
            gestureLine.useWorldSpace = false;

            // 材质与排序设置
            if (gestureLine.material == null || gestureLine.material.name.Contains("Default-Line"))
            {
                gestureLine.material = new Material(Shader.Find("Sprites/Default"));
            }
            // 强制渲染在最上层 (防止被身体遮挡)
            gestureLine.sortingOrder = 30000;


            gestureLine.positionCount = 0;
            gestureLine.enabled = false;
        }

        // 初始化映射
        if (gestureMappings == null || gestureMappings.Length == 0)
        {
            gestureMappings = new GestureMapping[] {
                new GestureMapping { gestureCode = "RL", spellId = 2, spellName = "火爆术" },
                new GestureMapping { gestureCode = "DRU", spellId = 3, spellName = "斩尽牛杂术" },
                new GestureMapping { gestureCode = "DUR", spellId = 4, spellName = "冰锥术" },
                new GestureMapping { gestureCode = "CIRCLE", spellId = 5, spellName = "剑光" },
                new GestureMapping { gestureCode = "RDRUR", spellId = 6, spellName = "传送门" }
            };
        }
    }

    void Update()
    {
        bool isInputSignalActive = InputManager.Instance != null && InputManager.Instance.isCasting;

        if (isInputSignalActive)
        {
            lostSignalTimer = 0f;
            if (!isRecording) StartDrawing();
            else UpdateDrawing();
        }
        else if (isRecording)
        {
            lostSignalTimer += Time.deltaTime;
            if (lostSignalTimer > signalLossTolerance)
            {
                FinishDrawing();
            }
        }
    }

    void StartDrawing()
    {
        isRecording = true;
        lostSignalTimer = 0f;
        points.Clear();
        localLinePoints.Clear();
        gestureStartTime = Time.time;
        gestureLength = 0f;

        if (gestureLine != null)
        {
            gestureLine.positionCount = 0;
            gestureLine.enabled = true;
        }

        if (InputManager.Instance != null)
            AddPoint(InputManager.Instance.castingPosition);
    }

    void UpdateDrawing()
    {
        if (Time.time - gestureStartTime > maxGestureTime)
        {
            FinishDrawing();
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.isCasting)
        {
            Vector2 currentPos = InputManager.Instance.castingPosition;

            // 距离判断
            if (points.Count == 0 || Vector2.Distance(currentPos, points[points.Count - 1]) >= 10f) // 屏幕像素距离
            {
                AddPoint(currentPos);
            }
        }
    }

    void AddPoint(Vector2 screenPos)
    {
        // 1. 坏点过滤
        if (screenPos.x < 10f || screenPos.y < 10f) return;
        if (screenPos.x > Screen.width - 10f || screenPos.y > Screen.height - 10f) return;

        // 2. 记录屏幕点 (用于识别算法，保持不变)
        points.Add(screenPos);
        if (points.Count >= 2)
            gestureLength += Vector2.Distance(points[points.Count - 2], points[points.Count - 1]); // 这里算的还是像素距离

        // 3. 绘制点 (转换为本地坐标)
        if (gestureLine != null && renderCamera != null)
        {
            // 先获取射线上的世界坐标点
            Ray ray = renderCamera.ScreenPointToRay(screenPos);
            Vector3 worldPos = ray.GetPoint(drawDistance);

            // 【核心修复】将世界坐标 转为 相机本地坐标
            // 因为 LineRenderer 是相机的子物体且 useWorldSpace=false
            Vector3 localPos = renderCamera.transform.InverseTransformPoint(worldPos);

            localLinePoints.Add(localPos);
            gestureLine.positionCount = localLinePoints.Count;
            gestureLine.SetPositions(localLinePoints.ToArray());
        }
    }

    void FinishDrawing()
    {
        isRecording = false;
        if (gestureLine != null) gestureLine.enabled = false;

        // 调整识别的长度阈值 (因为像素距离很大，这里保持原样即可)
        if (points.Count < 3 || gestureLength < 50f) return;

        string gesture = AnalyzeGesture();
        if (showDebugInfo) Debug.Log($"手势识别: {gesture}");
        CastSpell(gesture);
    }

    // --- 识别算法 (保持不变) ---
    string AnalyzeGesture()
    {
        if (points.Count < 2) return "";
        if (points.Count > 10 && IsCircularGesture(points)) return "CIRCLE";
        List<string> rawDirections = new List<string>();
        Vector2 segmentStart = points[0];
        string currentDir = "";
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 delta = points[i] - points[i - 1];
            string dir = GetDirectionFromVector(delta);
            if (dir != currentDir)
            {
                float segmentLen = Vector2.Distance(segmentStart, points[i - 1]);
                if (segmentLen > 40f && !string.IsNullOrEmpty(currentDir)) rawDirections.Add(currentDir);
                currentDir = dir; segmentStart = points[i - 1];
            }
        }
        rawDirections.Add(currentDir);
        return SimplifyGesture(rawDirections);
    }
    bool IsCircularGesture(List<Vector2> gesturePoints)
    {
        if (gesturePoints.Count < 10) return false;
        float totalPathLength = 0f;
        for (int i = 1; i < gesturePoints.Count; i++) totalPathLength += Vector2.Distance(gesturePoints[i], gesturePoints[i - 1]);
        float distanceStartEnd = Vector2.Distance(gesturePoints[0], gesturePoints[gesturePoints.Count - 1]);
        if (distanceStartEnd > totalPathLength * circleClosureThreshold) return false;
        Vector2 center = Vector2.zero; foreach (var p in gesturePoints) center += p; center /= gesturePoints.Count;
        float totalRadius = 0f; float minRadius = float.MaxValue; float maxRadius = float.MinValue;
        foreach (var p in gesturePoints) { float r = Vector2.Distance(p, center); totalRadius += r; minRadius = Mathf.Min(minRadius, r); maxRadius = Mathf.Max(maxRadius, r); }
        float avgRadius = totalRadius / gesturePoints.Count;
        return (maxRadius - minRadius) < avgRadius * circleRecognitionThreshold && avgRadius > 30f;
    }
    string GetDirectionFromVector(Vector2 v)
    {
        if (v.magnitude < directionThreshold) return "";
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; if (angle < 0) angle += 360f;
        if (angle >= 337.5f || angle < 22.5f) return "R"; else if (angle >= 22.5f && angle < 67.5f) return "UR"; else if (angle >= 67.5f && angle < 112.5f) return "U"; else if (angle >= 112.5f && angle < 157.5f) return "UL"; else if (angle >= 157.5f && angle < 202.5f) return "L"; else if (angle >= 202.5f && angle < 247.5f) return "DL"; else if (angle >= 247.5f && angle < 292.5f) return "D"; else return "DR";
    }
    string SimplifyGesture(List<string> directions)
    {
        if (directions.Count == 0) return "";
        List<string> simplified = new List<string> { directions[0] };
        for (int i = 1; i < directions.Count; i++) if (directions[i] != simplified[simplified.Count - 1]) simplified.Add(directions[i]);
        return string.Join("", simplified);
    }
    void CastSpell(string gestureCode)
    {
        if (!enableGestureMatching || string.IsNullOrEmpty(gestureCode)) return;
        GestureMapping bestMatch = null; int minDistance = int.MaxValue;
        foreach (var mapping in gestureMappings)
        {
            int dist = LevenshteinDistance(gestureCode, mapping.gestureCode); int threshold = Mathf.Max(1, mapping.gestureCode.Length / 2);
            if (dist < minDistance && dist <= threshold) { minDistance = dist; bestMatch = mapping; }
        }
        if (bestMatch == null && gestureCode.Contains("CIRCLE")) ExecuteSpell(new GestureMapping { spellId = 5, spellName = "剑光" });
        else if (bestMatch != null) ExecuteSpell(bestMatch);
    }
    int LevenshteinDistance(string s, string t) { int n = s.Length; int m = t.Length; int[,] d = new int[n + 1, m + 1]; if (n == 0) return m; if (m == 0) return n; for (int i = 0; i <= n; d[i, 0] = i++) { } for (int j = 0; j <= m; d[0, j] = j++) { } for (int i = 1; i <= n; i++) { for (int j = 1; j <= m; j++) { int cost = (t[j - 1] == s[i - 1]) ? 0 : 1; d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost); } } return d[n, m]; }
    void ExecuteSpell(GestureMapping mapping)
    {
        Debug.Log($">>> 施法: {mapping.spellName} <<<");
        if (wandController != null) wandController.FireSpell(mapping.spellId);
        else if (skillManager != null) skillManager.CastSkill(mapping.spellId);
    }
}