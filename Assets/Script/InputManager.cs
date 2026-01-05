using UnityEngine;
using System.IO.Ports;
using System.Collections;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public enum InputMode { DualESP32 }
    public InputMode currentMode = InputMode.DualESP32;

    [Header("状态监控")]
    public string joystickPortName = "未连接";
    public string mpuPortName = "未连接";
    public string statusInfo = "初始化...";
    private bool hasInitialized = false;
    [Header("外部状态控制 (请在对话脚本中调用)")]
    // 对话状态开关：true=对话中(禁止施法), false=平时
    public bool isDialogueActive = false;

    [Header("输入数据")]
    public UnityEngine.Vector2 moveInput;
    public bool isCasting;
    public bool isConfirmBtn;

    // 陀螺仪数据
    public UnityEngine.Vector2 lookInput;
    public UnityEngine.Quaternion wandRotation = UnityEngine.Quaternion.identity;
    public UnityEngine.Vector3 currentGyro;

    public UnityEngine.Vector2 castingPosition = new UnityEngine.Vector2(960, 540);

    [Header("手感参数设置")]
    // 移动速度倍率：设为 2.0 模拟跑步
    public float moveSpeedMultiplier = 2.0f;

    // 【补回丢失的变量】视角转动灵敏度
    public float turnSensitivity = 100f;

    // 视角平滑时间：推荐 0.15
    public float rotationSmoothTime = 0.15f;

    public float gyroSensitivity = 2.0f;
    public float cursorSensitivity = 25.0f;

    // 内部串口对象
    private SerialPort joyPort;
    private SerialPort mpuPort;

    // 全局冷却
    private float globalCooldown = 0f;
    private bool isScanning = false;
    private UnityEngine.Vector2 virtualCursorPos = new UnityEngine.Vector2(960, 540);

    // 平滑算法变量
    private float currentTurnVelocity;
    private float currentTurnValue;
    public float rawTurnInput; // 原始转向输入
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            virtualCursorPos = new UnityEngine.Vector2(Screen.width / 2f, Screen.height / 2f);
            castingPosition = virtualCursorPos;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        virtualCursorPos = new UnityEngine.Vector2(Screen.width / 2, Screen.height / 2);
        StartCoroutine(SystemLoop());
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void Update()
    {
        if (globalCooldown > 0) globalCooldown -= Time.deltaTime;

        if (globalCooldown <= 0)
        {
            ReadPortSafe(joyPort, "JOY");
            ReadPortSafe(mpuPort, "MPU");
        }

        UpdateGameLogic();
    }

    IEnumerator SystemLoop()
    {
        while (true)
        {
            if (globalCooldown > 0)
            {
                statusInfo = $"冷却中... {globalCooldown:F1}";
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            CheckConnectionStatus();

            if ((joyPort == null || mpuPort == null) && !isScanning)
            {
                yield return StartCoroutine(ScanPortsCoroutine());
            }

            yield return new WaitForSeconds(3.0f);
        }
    }

    // =================================================
    // 核心逻辑：被动连接 (无 DTR 复位)
    // =================================================
    IEnumerator ScanPortsCoroutine()
    {
        isScanning = true;
        statusInfo = "扫描端口中...";

        string[] ports = SerialPort.GetPortNames();

        foreach (string portName in ports)
        {
            if (IsAlreadyConnected(portName)) continue;

            UnityEngine.Debug.Log($"[系统] 发现端口 {portName}，尝试被动连接...");

            SerialPort tempPort = null;
            bool openSuccess = false;

            try
            {
                tempPort = new SerialPort(portName, 115200);
                tempPort.ReadTimeout = 100;

                // 显式禁用 DTR/RTS，防止板子重启
                tempPort.DtrEnable = false;
                tempPort.RtsEnable = false;

                tempPort.Open();
                openSuccess = true;
            }
            catch
            {
                openSuccess = false;
            }

            if (!openSuccess) continue;

            // 既然不重启，稍微等一点点缓冲时间即可
            statusInfo = $"验证设备 ({portName})...";
            yield return new WaitForSeconds(0.2f);

            if (tempPort != null && tempPort.IsOpen)
            {
                bool success = false;
                string dataAccumulator = "";

                // 快速读取 5 次
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (tempPort.BytesToRead > 0)
                        {
                            string chunk = tempPort.ReadExisting();
                            dataAccumulator += chunk;
                        }
                    }
                    catch { }

                    // 识别逻辑
                    if (dataAccumulator.Contains("JS:") || (mpuPort != null && joyPort == null && dataAccumulator.Length > 10))
                    {
                        joyPort = tempPort;
                        joystickPortName = portName;
                        success = true;
                        UnityEngine.Debug.Log($"<color=green>摇杆连接成功! ({portName})</color>");
                        break;
                    }
                    else if (dataAccumulator.Contains(",") && !dataAccumulator.Contains("JS"))
                    {
                        mpuPort = tempPort;
                        mpuPortName = portName;
                        success = true;
                        UnityEngine.Debug.Log($"<color=green>陀螺仪连接成功! ({portName})</color>");
                        break;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                if (!success)
                {
                    if (dataAccumulator.Length > 0)
                        UnityEngine.Debug.Log($"[跳过] {portName} 数据不匹配: {dataAccumulator}");

                    tempPort.Close();
                }
            }
        }

        isScanning = false;
    }

    void ReadPortSafe(SerialPort port, string type)
    {
        if (port == null || !port.IsOpen) return;

        try
        {
            if (port.BytesToRead > 0)
            {
                string content = port.ReadExisting();
                if (type == "JOY") ParseJoystickData(content);
                else ParseMPUData(content);
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[{type} 异常] {e.Message}");
            if (port != null) port.Close();
            if (type == "JOY") joyPort = null; else mpuPort = null;

            globalCooldown = 5.0f;
            statusInfo = "端口异常，冷却 5秒...";
        }
    }

    void ParseJoystickData(string data)
    {
        int idx = data.LastIndexOf("JS:");
        if (idx != -1)
        {
            try
            {
                string raw = data.Substring(idx + 3);
                int endIdx = raw.IndexOf('\n');
                if (endIdx != -1) raw = raw.Substring(0, endIdx);

                string[] parts = raw.Split(':');
                string[] dirs = parts[0].Split(',');

                if (dirs.Length >= 4)
                {
                    // Y轴 (前后) -> 控制移动
                    float y = 0;
                    if (dirs[0] == "1") y = 1;
                    else if (dirs[1] == "1") y = -1;

                    // X轴 (左右) -> 控制旋转
                    float x = 0;
                    if (dirs[3] == "1") x = 1;
                    else if (dirs[2] == "1") x = -1;

                    // 【核心修改】
                    // moveInput.y 依然代表前后移动
                    // moveInput.x 强制设为 0，防止角色横向平移（螃蟹步）
                    moveInput.y = y * moveSpeedMultiplier;
                    moveInput.x = 0;

                    // 我们把 X 轴的值存到一个新变量，或者直接用 lookInput.x
                    // 这里利用已有的 lookInput 逻辑，直接通过 UpdateGameLogic 处理旋转
                    // 但我们需要一个变量把原始的 x 传出去
                    rawTurnInput = x; // 需要在类开头定义这个变量

                    // ... 按钮逻辑保持不变 ...
                
                // 按钮逻辑
                if (parts.Length > 1)
                    {
                        bool btnState = (parts[1].Trim() == "1");

                        // 对话互斥
                        if (isDialogueActive)
                        {
                            isConfirmBtn = btnState;
                            isCasting = false;
                        }
                        else
                        {
                            isConfirmBtn = btnState;
                            isCasting = btnState;
                        }
                    }
                }
            }
            catch { }
        }
    }

    void ParseMPUData(string data)
    {
        try
        {
            string[] lines = data.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(",") && !line.Contains("JS"))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length >= 6)
                    {
                        currentGyro.x = float.Parse(parts[3]);
                        currentGyro.y = float.Parse(parts[4]);
                        currentGyro.z = float.Parse(parts[5]);
                    }
                }
            }
        }
        catch { }
    }

    void UpdateGameLogic()
    {
        // 【核心修改】视角丝滑阻尼
        // 使用 rawTurnInput (摇杆左右) 来控制旋转
        float targetTurn = rawTurnInput * turnSensitivity;

        currentTurnValue = Mathf.SmoothDamp(currentTurnValue, targetTurn, ref currentTurnVelocity, rotationSmoothTime);

        // 把计算好的旋转值赋给 lookInput.x，供 PlayerController 使用
        lookInput.x = currentTurnValue * Time.deltaTime;

        // 魔杖旋转
        if (currentMode == InputMode.DualESP32)
        {
            float dt = Time.deltaTime;
            // 如果还反，改这里的符号
            float rotX = currentGyro.y;

            UnityEngine.Vector3 rotDelta = new UnityEngine.Vector3(rotX, -currentGyro.z, -currentGyro.x) * gyroSensitivity * dt * Mathf.Rad2Deg;
            wandRotation *= UnityEngine.Quaternion.Euler(rotDelta);

            UnityEngine.Vector3 euler = wandRotation.eulerAngles;
            if (euler.x > 180) euler.x -= 360;
            euler.x = Mathf.Clamp(euler.x, -80f, 80f);
            euler.z = 0f;
            wandRotation = UnityEngine.Quaternion.Euler(euler);
        }

        // 【新增】启动保护：前 10 帧强制归位到中心
        // 防止 Unity 刚启动时 Screen.width 获取不准导致坐标归零
        if (Time.frameCount < 10 || !hasInitialized)
        {
            virtualCursorPos = new UnityEngine.Vector2(Screen.width / 2f, Screen.height / 2f);
            castingPosition = virtualCursorPos;
            hasInitialized = true;
            return; // 跳过这一帧的剩余逻辑
        }

        // 3. 画符光标
        if (isCasting)
        {
            float deltaX = -currentGyro.z * cursorSensitivity;
            float deltaY = currentGyro.y * cursorSensitivity;

            virtualCursorPos += new UnityEngine.Vector2(deltaX, deltaY);

            // 限制范围
            virtualCursorPos.x = Mathf.Clamp(virtualCursorPos.x, 0, Screen.width);
            virtualCursorPos.y = Mathf.Clamp(virtualCursorPos.y, 0, Screen.height);

            castingPosition = virtualCursorPos;
        }
        else
        {
            // 不施法时，时刻重置回屏幕正中心 (1/2处)
            // 只有放在 1/2 处，左右眼合成的图像才是在“正前方”
            virtualCursorPos = new UnityEngine.Vector2(Screen.width / 2f, Screen.height / 2f);
            castingPosition = virtualCursorPos;
        }

        UnityEngine.Cursor.lockState = isCasting ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = isCasting;
    }

    bool IsAlreadyConnected(string name)
    {
        return (joyPort != null && joyPort.PortName == name) || (mpuPort != null && mpuPort.PortName == name);
    }

    void CheckConnectionStatus()
    {
        if (joyPort != null && !joyPort.IsOpen) joyPort = null;
        if (mpuPort != null && !mpuPort.IsOpen) mpuPort = null;
    }
#if UNITY_EDITOR
    void OnGUI()
    {
        UnityEngine.GUI.color = UnityEngine.Color.yellow;
        UnityEngine.GUI.Label(new UnityEngine.Rect(10, 10, 800, 30), $"状态: {statusInfo} | 对话模式: {isDialogueActive}");

        string joyName = (joyPort != null) ? joyPort.PortName : "无";
        string mpuName = (mpuPort != null) ? mpuPort.PortName : "无";

        UnityEngine.GUI.Label(new UnityEngine.Rect(10, 30, 800, 30), $"Joy: {joyName} | MPU: {mpuName}");
        UnityEngine.GUI.Label(new UnityEngine.Rect(10, 50, 800, 30), $"Input X:{moveInput.x:F1} Y:{moveInput.y:F1}");
    }
#endif
    void OnDestroy()
    {
        if (joyPort != null && joyPort.IsOpen) joyPort.Close();
        if (mpuPort != null && mpuPort.IsOpen) mpuPort.Close();
    }
}