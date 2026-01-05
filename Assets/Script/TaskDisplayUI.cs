using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1. 定义任务类型枚举
public enum TaskType
{
    Generic,    // 通用（杀怪/收集）
    Movement,   // 移动（到达指定地点）
    SpellCast   // 施法教学（释放指定技能）
}

public class TaskDisplayUI : MonoBehaviour
{
    [Header("任务栏设置")]
    public RectTransform taskPanel;
    public TextMeshProUGUI taskTitleText;
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI taskProgressText;
    public GameObject progressBarContainer;
    public Image progressBarBackground;
    public Image progressBarFill;
    public GameObject taskCompleteEffect;
    public float autoHideDelay = 10f;

    [Header("指引设置 (新功能)")]
    public GameObject lightPillarPrefab; // 拖入刚才做的黄色光柱Prefab
    public Transform playerTransform;    // 玩家位置引用
    private GameObject currentLightPillar; // 当前生成的光柱实例

    [Header("进度条设置")]
    public int progressBarSegments = 20;
    private Image[] fillSegments;

    [Header("任务列表")]
    public List<Task> tasks = new List<Task>();

    [Header("样式设置")]
    public Color activeTaskColor = Color.yellow;
    public Color inactiveTaskColor = Color.gray;
    public Color completedTaskColor = Color.green;
    public Color progressFillColor = Color.blue;
    public float fadeSpeed = 3f;

    // 组件引用
    private CanvasGroup canvasGroup;
    private Coroutine autoHideCoroutine;
    private Task currentActiveTask;
    private bool isInitialized = false;
    private bool isVisible = true;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 自动查找玩家
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // 监听施法事件
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillCast += HandleSkillCast;
        }

        Initialize();
    }

    void OnDestroy()
    {
        // 移除监听，防止报错
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillCast -= HandleSkillCast;
        }
    }

    public void Initialize()
    {
        if (tasks.Count == 0) CreateDefaultTasks();
        InitializeProgressBar();

        if (tasks.Count > 0) SetActiveTask(tasks[0].taskId);

        isInitialized = true;
    }

    // --- 核心逻辑更新：每帧检查移动任务 ---
    void Update()
    {
        if (!isInitialized) return;

        UpdateActiveTask();
        CheckTaskCompletion();

        // 处理移动类任务的距离检测
        if (currentActiveTask != null && currentActiveTask.isActive && !currentActiveTask.isCompleted)
        {
            if (currentActiveTask.taskType == TaskType.Movement)
            {
                CheckMovementTask();
            }
        }
    }

    // --- 新增：处理移动任务逻辑 ---
    void CheckMovementTask()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, currentActiveTask.targetPosition);

        // 如果距离小于阈值（例如2米），视为到达
        if (distance <= currentActiveTask.detectRadius)
        {
            // 直接完成任务
            UpdateTaskProgress(currentActiveTask.taskId, currentActiveTask.requiredProgress);
        }
    }

    // --- 新增：处理施法任务逻辑 ---
    void HandleSkillCast(int skillId)
    {
        if (currentActiveTask == null || !currentActiveTask.isActive || currentActiveTask.isCompleted) return;

        if (currentActiveTask.taskType == TaskType.SpellCast)
        {
            // 检查释放的技能ID是否匹配任务要求
            if (skillId == currentActiveTask.targetSpellId)
            {
                // 进度+1
                int newProgress = currentActiveTask.currentProgress + 1;
                UpdateTaskProgress(currentActiveTask.taskId, newProgress);
            }
        }
    }

    // --- 修改：设置激活任务时生成光柱 ---
    public void SetActiveTask(string taskId)
    {
        // 清理旧的光柱
        if (currentLightPillar != null) Destroy(currentLightPillar);

        if (currentActiveTask != null) currentActiveTask.isActive = false;

        Task newTask = GetTaskById(taskId);
        if (newTask != null)
        {
            newTask.isActive = true;
            currentActiveTask = newTask;

            // 如果是移动任务，生成光柱
            if (newTask.taskType == TaskType.Movement && lightPillarPrefab != null)
            {
                currentLightPillar = Instantiate(lightPillarPrefab, newTask.targetPosition, Quaternion.identity);
                Debug.Log($"已在 {newTask.targetPosition} 生成任务指引光柱");
            }
            // 如果是施法任务，不需要光柱，等待事件即可

            ShowTaskPanel();
        }
    }

    // --- 修改：任务完成时销毁光柱 ---
    public void CompleteTask(string taskId)
    {
        Task task = GetTaskById(taskId);
        if (task == null) return;

        // 如果是当前任务完成了，销毁光柱
        if (task == currentActiveTask && currentLightPillar != null)
        {
            Destroy(currentLightPillar);
        }

        task.isCompleted = true;
        task.isActive = false;
        task.currentProgress = task.requiredProgress;

        ShowTaskComplete(task.taskName);
        ActivateNextTask();
    }

    // ... 以下保持原有的UI辅助方法不变 ...
    // (InitializeProgressBar, UpdateTaskDisplay, UpdateProgressBar, ShowTaskPanel 等方法与原文件一致，省略以节省篇幅，请保留你原有的这些方法)

    // 为了完整性，这里补上必须的方法，请把原来对应的方法粘贴回来或保持不动
    void InitializeProgressBar()
    {
        if (progressBarContainer == null || progressBarBackground == null) return;
        if (fillSegments != null && fillSegments.Length > 0) return; // 避免重复初始化

        fillSegments = new Image[progressBarSegments];
        float segmentWidth = progressBarBackground.rectTransform.rect.width / progressBarSegments;
        for (int i = 0; i < progressBarSegments; i++)
        {
            GameObject segmentObj = new GameObject($"Segment_{i}");
            segmentObj.transform.SetParent(progressBarContainer.transform);
            RectTransform segmentRect = segmentObj.AddComponent<RectTransform>();
            segmentRect.pivot = new Vector2(0, 0.5f);
            segmentRect.anchorMin = new Vector2(0, 0);
            segmentRect.anchorMax = new Vector2(0, 1);
            segmentRect.sizeDelta = new Vector2(segmentWidth - 2, 0);
            segmentRect.anchoredPosition = new Vector2(i * segmentWidth, 0);
            Image segmentImage = segmentObj.AddComponent<Image>();
            segmentImage.color = new Color(progressFillColor.r, progressFillColor.g, progressFillColor.b, 0);
            fillSegments[i] = segmentImage;
        }
    }

    void UpdateTaskDisplay(Task task)
    {
        if (taskTitleText != null)
        {
            taskTitleText.text = task.taskName;
            taskTitleText.color = task.isCompleted ? completedTaskColor : (task.isActive ? activeTaskColor : inactiveTaskColor);
        }
        if (taskDescriptionText != null) taskDescriptionText.text = task.description;
        if (taskProgressText != null) taskProgressText.text = $"{task.currentProgress}/{task.requiredProgress}";
        UpdateProgressBar(task);
    }

    void UpdateProgressBar(Task task)
    {
        if (fillSegments == null) return;
        float progress = task.requiredProgress > 0 ? (float)task.currentProgress / task.requiredProgress : 0f;
        int segmentsToShow = Mathf.RoundToInt(progress * fillSegments.Length);
        for (int i = 0; i < fillSegments.Length; i++)
        {
            if (fillSegments[i] != null) fillSegments[i].color = i < segmentsToShow ? progressFillColor : new Color(progressFillColor.r, progressFillColor.g, progressFillColor.b, 0);
        }
    }

    void CheckTaskCompletion()
    {
        foreach (Task task in tasks)
        {
            if (!task.isCompleted && task.isActive && task.currentProgress >= task.requiredProgress) CompleteTask(task.taskId);
        }
    }

    void UpdateActiveTask() { if (currentActiveTask != null) UpdateTaskDisplay(currentActiveTask); }

    public void UpdateTaskProgress(string taskId, int progress, int total = -1)
    {
        Task task = GetTaskById(taskId);
        if (task == null) return;
        task.currentProgress = progress;
        if (total > 0) task.requiredProgress = total;
        if (task.isActive) UpdateTaskDisplay(task);
    }

    void ActivateNextTask()
    {
        foreach (Task task in tasks)
        {
            if (!task.isCompleted) { SetActiveTask(task.taskId); return; }
        }
    }

    public void ShowTaskComplete(string taskName)
    {
        if (taskCompleteEffect != null)
        {
            GameObject effect = Instantiate(taskCompleteEffect, transform);
            effect.transform.position = taskPanel.position;
            Destroy(effect, 2f);
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"任务完成: {taskName}", 3f);
        ShowTaskPanel();
    }

    public void ShowTaskPanel()
    {
        if (!isVisible) ToggleVisibility();
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        StartCoroutine(FadeTaskPanel(1f));
        autoHideCoroutine = StartCoroutine(AutoHideTaskPanel());
    }

    public void HideTaskPanel()
    {
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        StartCoroutine(FadeTaskPanel(0f));
    }

    public void ToggleVisibility() { isVisible = !isVisible; StartCoroutine(FadeTaskPanel(isVisible ? 1f : 0f)); }

    IEnumerator FadeTaskPanel(float targetAlpha)
    {
        if (canvasGroup == null) yield break;
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    IEnumerator AutoHideTaskPanel()
    {
        yield return new WaitForSeconds(autoHideDelay);
        if (currentActiveTask != null && !currentActiveTask.isActive) HideTaskPanel();
    }

    Task GetTaskById(string taskId)
    {
        foreach (Task task in tasks) if (task.taskId == taskId) return task;
        return null;
    }

    public void AddTask(string taskId, string taskName, string description, int requiredProgress)
    {
        Task newTask = new Task(taskId, taskName, description, requiredProgress);
        tasks.Add(newTask);
    }

    public List<Task> GetTasks() { return new List<Task>(tasks); }

    void CreateDefaultTasks()
    {
        // 示例任务：先移动，再施法
        Task t1 = new Task("move_1", "前往神庙", "跟随光柱前往神庙入口", 1);
        t1.taskType = TaskType.Movement;
        t1.targetPosition = new Vector3(10, 0, 10); // 示例坐标
        t1.detectRadius = 3.0f;
        tasks.Add(t1);

        Task t2 = new Task("spell_1", "练习火球术", "使用手势释放一次火球术", 1);
        t2.taskType = TaskType.SpellCast;
        t2.targetSpellId = 0; // 火球术ID
        tasks.Add(t2);
    }
}

// 2. 修改 Task 类结构，增加必要的字段
[System.Serializable]
public class Task
{
    public string taskId;
    public string taskName;
    public string description;
    public int currentProgress;
    public int requiredProgress;
    public bool isCompleted;
    public bool isActive;

    [Header("新功能设置")]
    public TaskType taskType = TaskType.Generic;

    // 移动任务用
    public Vector3 targetPosition;
    public float detectRadius = 2.0f; // 到达判定范围

    // 施法任务用
    public int targetSpellId = -1; // 目标技能ID

    public Task(string id, string name, string desc, int required)
    {
        taskId = id;
        taskName = name;
        description = desc;
        requiredProgress = required;
        currentProgress = 0;
        isCompleted = false;
        isActive = false;
    }
}