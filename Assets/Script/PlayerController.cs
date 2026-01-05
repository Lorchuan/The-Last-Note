using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5.0f;
    public float runSpeed = 8.0f;
    public float lookSensitivity = 2.0f;
    [Range(0f, 1f)] public float moveThreshold = 0.1f;

    [Header("地形跟随设置")]
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayerMask = 1;
    public float slopeLimit = 45f;
    public float stepOffset = 0.3f;

    [Header("动画控制")]
    public Animator     playerAnimator;
    public bool useMovementAnimation = true;
    public bool useRootMotion = false;

    [Header("动画调试")]
    public bool debugAnimation = true;

    [Header("参考物体")]
    public Transform playerCamera;

    // 私有变量
    private CharacterController characterController;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private float originalStepOffset;
    private Vector3 lastPosition;
    private bool wasMoving = false;
    private bool isRunning = false;
    private Vector3 rootMotionDelta;

    // 动画参数名称
    private const string ANIM_START_MOVE = "StartMove";
    private const string ANIM_STOP_MOVE = "StopMove";
    private const string ANIM_IS_WALKING = "IsWalking";
    private const string ANIM_IS_RUNNING = "IsRunning";

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        characterController.slopeLimit = slopeLimit;
        characterController.stepOffset = stepOffset;
        originalStepOffset = stepOffset;

        // 获取Animator组件
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null && GetComponentInChildren<Animator>() != null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }

        // 配置Animator的Root Motion
        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = useRootMotion;
        }

        lastPosition = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (useMovementAnimation && playerAnimator != null)
        {
            ResetAnimationState();
        }

        Debug.Log($"玩家控制器初始化完成 - Root Motion: {useRootMotion}");
    }

    void Update()
    {
        HandleGroundCheck();

        if (!useRootMotion)
        {
            HandleMovement();
        }

        HandleGravity();

        if (!InputManager.Instance.isCasting)
        {
            HandleLookRotation();
        }

        if (useMovementAnimation && playerAnimator != null)
        {
            UpdateAnimationState();
        }

        // 调试：手动控制动画（按数字键）
        HandleDebugAnimationControls();
    }

    void OnAnimatorMove()
    {
        if (useRootMotion && playerAnimator != null && useMovementAnimation)
        {
            rootMotionDelta = playerAnimator.deltaPosition;

            if (isGrounded)
            {
                Vector3 moveDirection = Vector3.zero;
                if (InputManager.Instance != null)
                {
                    moveDirection = transform.right * InputManager.Instance.moveInput.x +
                                   transform.forward * InputManager.Instance.moveInput.y;
                }

                if (moveDirection.magnitude <= moveThreshold)
                {
                    rootMotionDelta = Vector3.zero;
                }
                else
                {
                    float speedMultiplier = isRunning ? runSpeed / moveSpeed : 1f;
                    rootMotionDelta *= speedMultiplier;

                    Vector3 forwardMotion = Vector3.Project(rootMotionDelta, transform.forward);
                    Vector3 rightMotion = Vector3.Project(rootMotionDelta, transform.right);

                    float forwardInput = Mathf.Clamp(InputManager.Instance.moveInput.y, -1f, 1f);
                    float rightInput = Mathf.Clamp(InputManager.Instance.moveInput.x, -1f, 1f);

                    rootMotionDelta = (transform.forward * forwardMotion.magnitude * Mathf.Sign(forwardInput)) +
                                     (transform.right * rightMotion.magnitude * Mathf.Sign(rightInput));

                    float maxDistance = (isRunning ? runSpeed : moveSpeed) * Time.deltaTime;
                    if (rootMotionDelta.magnitude > maxDistance)
                    {
                        rootMotionDelta = rootMotionDelta.normalized * maxDistance;
                    }
                }

                characterController.Move(rootMotionDelta);
            }
        }
    }

    void HandleGroundCheck()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayerMask))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= slopeLimit)
            {
                isGrounded = true;

                if (slopeAngle > 0)
                {
                    characterController.stepOffset = Mathf.Lerp(originalStepOffset, 0.1f, slopeAngle / slopeLimit);
                }
                else
                {
                    characterController.stepOffset = originalStepOffset;
                }
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = characterController.isGrounded;
        }
    }

    // 在 PlayerController.cs 的 HandleMovement 方法中
    void HandleMovement()
    {
        if (useRootMotion) return;

        Vector3 moveDirection = Vector3.zero;
        float currentSpeed = moveSpeed;

        if (InputManager.Instance != null)
        {
            // 【核心修改】只响应前后 (forward)，不再响应左右 (right)
            // 因为 InputManager 里我们已经把 moveInput.x 锁死为 0 了，这里改不改都行，但为了保险：
            moveDirection = transform.forward * InputManager.Instance.moveInput.y;

            // 奔跑逻辑
            isRunning = Input.GetKey(KeyCode.LeftShift) || (Mathf.Abs(InputManager.Instance.moveInput.y) > 0.5f && InputManager.Instance.moveSpeedMultiplier > 1.5f);
            currentSpeed = isRunning ? runSpeed : moveSpeed;
        }

        if (moveDirection.magnitude > moveThreshold)
        {
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleLookRotation()
    {
        if (InputManager.Instance == null) return;

        Vector2 mouseLook = InputManager.Instance.lookInput;

        transform.Rotate(Vector3.up * mouseLook.x);

        xRotation -= mouseLook.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void UpdateAnimationState()
    {
        // 计算当前移动状态
        Vector3 currentPosition = transform.position;
        Vector3 positionDelta = currentPosition - lastPosition;
        float horizontalMovement = new Vector3(positionDelta.x, 0, positionDelta.z).magnitude;

        // 判断是否移动
        bool isMoving;
        if (useRootMotion)
        {
            Vector2 moveInput = InputManager.Instance != null ? InputManager.Instance.moveInput : Vector2.zero;
            isMoving = moveInput.magnitude > moveThreshold && isGrounded;
        }
        else
        {
            isMoving = horizontalMovement > 0.001f && isGrounded;
        }

        // 检测奔跑输入
        if (InputManager.Instance != null)
        {
            Vector2 moveInput = InputManager.Instance.moveInput;
            bool hasMoveInput = moveInput.magnitude > moveThreshold;

            // 逻辑修正：只要满足以下任一条件即视为奔跑
            // 1. 按住 Shift 键
            // 2. 摇杆输入值很大（InputManager里默认乘了2.0倍率）
            bool keyRun = Input.GetKey(KeyCode.LeftShift);
            bool joystickRun = (Mathf.Abs(moveInput.y) > 0.1f && InputManager.Instance.moveSpeedMultiplier > 1f);

            isRunning = hasMoveInput && (keyRun || joystickRun);
        }

        // 记录当前动画状态（用于调试）
        bool prevIsWalking = playerAnimator.GetBool(ANIM_IS_WALKING);
        bool prevIsRunning = playerAnimator.GetBool(ANIM_IS_RUNNING);

        // 根据移动状态更新动画
        if (isMoving && !wasMoving)
        {
            // 开始移动
            playerAnimator.ResetTrigger(ANIM_STOP_MOVE); // 重置停止触发器
            playerAnimator.SetTrigger(ANIM_START_MOVE);
            playerAnimator.SetBool(ANIM_IS_WALKING, !isRunning);
            playerAnimator.SetBool(ANIM_IS_RUNNING, isRunning);

            if (debugAnimation)
                Debug.Log($"动画: 开始移动, IsWalking={!isRunning}, IsRunning={isRunning}");
        }
        else if (!isMoving && wasMoving)
        {
            // 停止移动
            playerAnimator.ResetTrigger(ANIM_START_MOVE); // 重置开始触发器
            playerAnimator.SetTrigger(ANIM_STOP_MOVE);
            playerAnimator.SetBool(ANIM_IS_WALKING, false);
            playerAnimator.SetBool(ANIM_IS_RUNNING, false);

            if (debugAnimation)
                Debug.Log($"动画: 停止移动");
        }
        else if (isMoving && wasMoving)
        {
            // 持续移动中，只更新移动类型
            bool shouldBeWalking = !isRunning;
            bool shouldBeRunning = isRunning;

            if (prevIsWalking != shouldBeWalking || prevIsRunning != shouldBeRunning)
            {
                playerAnimator.SetBool(ANIM_IS_WALKING, shouldBeWalking);
                playerAnimator.SetBool(ANIM_IS_RUNNING, shouldBeRunning);

                if (debugAnimation)
                    Debug.Log($"动画: 切换移动类型, IsWalking={shouldBeWalking}, IsRunning={shouldBeRunning}");
            }
        }

        // 更新上一帧状态
        wasMoving = isMoving;
        lastPosition = currentPosition;
    }

    void ResetAnimationState()
    {
        playerAnimator.ResetTrigger(ANIM_START_MOVE);
        playerAnimator.ResetTrigger(ANIM_STOP_MOVE);
        playerAnimator.SetBool(ANIM_IS_WALKING, false);
        playerAnimator.SetBool(ANIM_IS_RUNNING, false);
        playerAnimator.Play("Idle", 0, 0f); // 强制回到Idle状态
    }

    void HandleDebugAnimationControls()
    {
        if (!debugAnimation) return;

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            TestAnimation("StartWalk");
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            TestAnimation("StartRun");
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            TestAnimation("Stop");
        }
    }

    public void TeleportTo(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        velocity = Vector3.zero;

        if (useMovementAnimation && playerAnimator != null)
        {
            ResetAnimationState();
        }
    }

    public void TestAnimation(string animationEvent)
    {
        if (playerAnimator == null)
        {
            Debug.LogWarning("Animator未分配！");
            return;
        }

        switch (animationEvent)
        {
            case "StartWalk":
                playerAnimator.ResetTrigger(ANIM_STOP_MOVE);
                playerAnimator.SetTrigger(ANIM_START_MOVE);
                playerAnimator.SetBool(ANIM_IS_WALKING, true);
                playerAnimator.SetBool(ANIM_IS_RUNNING, false);
                Debug.Log("手动触发: 开始行走");
                break;

            case "StartRun":
                playerAnimator.ResetTrigger(ANIM_STOP_MOVE);
                playerAnimator.SetTrigger(ANIM_START_MOVE);
                playerAnimator.SetBool(ANIM_IS_WALKING, false);
                playerAnimator.SetBool(ANIM_IS_RUNNING, true);
                Debug.Log("手动触发: 开始奔跑");
                break;

            case "Stop":
                playerAnimator.ResetTrigger(ANIM_START_MOVE);
                playerAnimator.SetTrigger(ANIM_STOP_MOVE);
                playerAnimator.SetBool(ANIM_IS_WALKING, false);
                playerAnimator.SetBool(ANIM_IS_RUNNING, false);
                Debug.Log("手动触发: 停止");
                break;
        }
    }

    public void ToggleRootMotion(bool enable)
    {
        useRootMotion = enable;
        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = enable;
        }
        Debug.Log($"Root Motion: {(enable ? "启用" : "禁用")}");
    }
#if UNITY_EDITOR
    void OnGUI()
    {
        if (InputManager.Instance != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(10, 300, 300, 25), $"玩家状态: {(isGrounded ? "在地面" : "空中")}", style);
            GUI.Label(new Rect(10, 320, 300, 25), $"玩家速度: {characterController.velocity.magnitude:F2}", style);
            GUI.Label(new Rect(10, 340, 300, 25), $"垂直速度: {velocity.y:F2}", style);

            if (useMovementAnimation && playerAnimator != null)
            {
                bool isWalking = playerAnimator.GetBool(ANIM_IS_WALKING);
                bool isRunning = playerAnimator.GetBool(ANIM_IS_RUNNING);
                

                GUI.Label(new Rect(10, 360, 300, 25), $"动画状态: {(wasMoving ? (isRunning ? "奔跑" : "行走") : "静止")}", style);
                GUI.Label(new Rect(10, 380, 300, 25), $"IsWalking: {isWalking}", style);
                GUI.Label(new Rect(10, 400, 300, 25), $"IsRunning: {isRunning}", style);
                
                GUI.Label(new Rect(10, 440, 300, 25), $"Root Motion: {useRootMotion}", style);
            }
        }
    }
#endif
}