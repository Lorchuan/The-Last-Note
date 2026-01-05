using UnityEngine;
using UnityEngine.UI;
using TMPro;  // 添加这行
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public SkillBarUI skillBarUI;
    public HealthRingUI healthRingUI;
    public EnemyHealthBarUI enemyHealthBarUI;
    public TaskDisplayUI taskDisplayUI;
    public EnemyMarker enemyMarker;
    public DialogueSystem dialogueSystem;
    public TMP_FontAsset messageFont;
    [Header("通用UI设置")]
    public Canvas mainCanvas;     // 现在是 Screen Space - Camera 模式的 Canvas
    public Camera uiCamera;       // 拖入 UICamera
    public bool showDebugInfo = false;

    // 单例模式
    private static UIManager instance;
    public static UIManager Instance { get { return instance; } }

    // 组件引用缓存
    private PlayerHealth playerHealth;
    private SkillManager skillManager;
    public WandController wandController;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindRequiredComponents();
        InitializeAllUI();
        Debug.Log("UI管理器初始化完成");
    }

    void FindRequiredComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            skillManager = player.GetComponent<SkillManager>();
            
        }

        if (playerHealth == null) Debug.LogWarning("未找到PlayerHealth组件");
        if (skillManager == null) Debug.LogWarning("未找到SkillManager组件");
        if (wandController == null) Debug.LogWarning("未找到WandController组件");
    }

    void InitializeAllUI()
    {
        // 确保所有UI组件都有引用
        if (skillBarUI == null) skillBarUI = FindObjectOfType<SkillBarUI>();
        if (healthRingUI == null) healthRingUI = FindObjectOfType<HealthRingUI>();
        if (enemyHealthBarUI == null) enemyHealthBarUI = FindObjectOfType<EnemyHealthBarUI>();
        if (taskDisplayUI == null) taskDisplayUI = FindObjectOfType<TaskDisplayUI>();
        if (enemyMarker == null) enemyMarker = FindObjectOfType<EnemyMarker>();
        if (dialogueSystem == null) dialogueSystem = FindObjectOfType<DialogueSystem>();

        // 初始化各个UI
        if (skillBarUI != null) skillBarUI.Initialize(skillManager);
        if (healthRingUI != null) healthRingUI.Initialize(playerHealth);
        if (enemyHealthBarUI != null) enemyHealthBarUI.Initialize(wandController);
        if (taskDisplayUI != null) taskDisplayUI.Initialize();
        if (enemyMarker != null) enemyMarker.Initialize(wandController);
        if (dialogueSystem != null) dialogueSystem.Initialize();
    }

    void Update()
    {
        // 更新所有UI
        if (skillBarUI != null && skillBarUI.isActiveAndEnabled) skillBarUI.UpdateUI();
        if (healthRingUI != null && healthRingUI.isActiveAndEnabled) healthRingUI.UpdateUI();
        if (enemyHealthBarUI != null && enemyHealthBarUI.isActiveAndEnabled) enemyHealthBarUI.UpdateUI();
        if (enemyMarker != null && enemyMarker.isActiveAndEnabled) enemyMarker.UpdateMarker();

        // 处理UI输入
        HandleUIInput();
    }

    void HandleUIInput()
    {
        // 快捷键：显示/隐藏UI
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleUI();
        }

        // 快捷键：显示/隐藏任务栏
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (taskDisplayUI != null)
            {
                taskDisplayUI.ToggleVisibility();
            }
        }

        // 快捷键：测试剧情输入
        if (Input.GetKeyDown(KeyCode.F3) && dialogueSystem != null)
        {
            dialogueSystem.ShowTestDialogue();
        }
    }

    void ToggleUI()
    {
        bool newState = !mainCanvas.gameObject.activeSelf;
        mainCanvas.gameObject.SetActive(newState);
        Debug.Log($"UI {(newState ? "显示" : "隐藏")}");
    }

    // 显示任务完成提示
    public void ShowTaskComplete(string taskName)
    {
        if (taskDisplayUI != null)
        {
            taskDisplayUI.ShowTaskComplete(taskName);
        }
    }
    IEnumerator ShowMessageVR(string message, float duration)
    {
        if (mainCanvas == null) yield break;

        GameObject messageObj = new GameObject("MessageVR");
        messageObj.transform.SetParent(mainCanvas.transform);

        // Screen Space 下，Scale=1，Pos=0 即可居中
        messageObj.transform.localScale = Vector3.one;
        messageObj.transform.localPosition = Vector3.zero;
        messageObj.transform.localRotation = Quaternion.identity;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(messageObj.transform);
        bgObj.transform.localScale = Vector3.one;
        bgObj.transform.localPosition = Vector3.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(500, 80);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(messageObj.transform);
        textObj.transform.localScale = Vector3.one;
        textObj.transform.localPosition = Vector3.zero;

        TextMeshProUGUI messageText = textObj.AddComponent<TextMeshProUGUI>();
        if (messageFont != null) messageText.font = messageFont;
        messageText.text = message;
        messageText.fontSize = 36;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = messageText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(480, 70);

        float fadeTime = 0.2f;
        float startTime = Time.time;
        while (Time.time - startTime < fadeTime)
        {
            float p = (Time.time - startTime) / fadeTime;
            messageObj.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, p);
            yield return null;
        }
        messageObj.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(duration);
        Destroy(messageObj);
    }
    // 更新任务进度
    public void UpdateTaskProgress(string taskName, int progress, int total)
    {
        if (taskDisplayUI != null)
        {
            taskDisplayUI.UpdateTaskProgress(taskName, progress, total);
        }
    }

    // 显示剧情对话
    public void ShowDialogue(string characterName, string dialogueText)
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.ShowDialogue(characterName, dialogueText);
        }
    }

    // 显示剧情对话（带选项）
    public void ShowDialogueWithOptions(string characterName, string dialogueText, string[] options)
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.ShowDialogueWithOptions(characterName, dialogueText, options);
        }
    }

    // 隐藏剧情对话
    public void HideDialogue()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.HideDialogue();
        }
    }

    // 公共方法：更新技能栏
    public void UpdateSkillBar()
    {
        if (skillBarUI != null)
        {
            skillBarUI.UpdateSkills();
        }
    }

    // 公共方法：更新生命值显示
    // --- 修正后的 UIManager.cs 部分方法 ---

    // 公共方法：更新生命值显示
    public void UpdateHealthDisplay()
    {
        if (healthRingUI != null)
        {
            // 修正：将 UpdateHealth() 改为 UpdateUI()
            healthRingUI.UpdateUI();
        }
    }

    // 公共方法：更新敌人血条
    public void UpdateEnemyHealthDisplay()
    {
        if (enemyHealthBarUI != null)
        {
            // 修正：将 UpdateEnemyHealth() 改为 UpdateUI()
            enemyHealthBarUI.UpdateUI();
        }
    }

    // 公共方法：显示伤害数字
    // --- 3D 伤害数字 (World Space) ---
    // 尽管UI是Screen Space，但伤害数字飘在怪头上，必须是3D的
    public void ShowDamageNumber(Vector3 worldPosition, int damage, bool isCritical = false)
    {
        StartCoroutine(ShowDamageNumberVR(worldPosition, damage, isCritical));
    }

    IEnumerator ShowDamageNumberVR(Vector3 worldPosition, int damage, bool isCritical)
    {
        GameObject damageTextObj = new GameObject("DamageNumber");
        damageTextObj.transform.position = worldPosition + Vector3.up * 1.5f;

        // 让文字面向 UICamera (或 Head)
        if (uiCamera != null)
        {
            damageTextObj.transform.LookAt(
                damageTextObj.transform.position + uiCamera.transform.rotation * Vector3.forward,
                uiCamera.transform.rotation * Vector3.up
            );
        }

        TextMeshPro damageText = damageTextObj.AddComponent<TextMeshPro>();
        if (messageFont != null) damageText.font = messageFont;

        damageText.text = damage.ToString();
        damageText.fontSize = isCritical ? 4 : 3;
        damageText.color = isCritical ? Color.yellow : Color.red;
        damageText.alignment = TextAlignmentOptions.Center;

        float duration = 1.2f;
        float startTime = Time.time;
        Vector3 startPos = damageTextObj.transform.position;

        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            damageTextObj.transform.position = startPos + Vector3.up * (1.2f * progress);
            Color c = damageText.color;
            c.a = 1f - progress;
            damageText.color = c;
            yield return null;
        }
        Destroy(damageTextObj);
    }
    IEnumerator ShowDamageNumberCoroutine(Vector3 worldPosition, int damage, bool isCritical)
    {
        if (mainCanvas == null || uiCamera == null) yield break;

        // 创建伤害数字对象
        GameObject damageTextObj = new GameObject("DamageNumber");
        damageTextObj.transform.SetParent(mainCanvas.transform);

        // 设置位置
        Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPosition);
        if (screenPos.z < 0)
        {
            Destroy(damageTextObj);
            yield break;
        }

        damageTextObj.transform.position = screenPos + new Vector3(0, 50, 0);

        // 添加TextMeshPro组件（修改这里）
        TextMeshProUGUI damageText = damageTextObj.AddComponent<TextMeshProUGUI>();
        damageText.text = damage.ToString();

        // 设置TextMeshPro属性
        damageText.fontSize = isCritical ? 30 : 24;
        damageText.color = isCritical ? Color.yellow : Color.red;
        damageText.alignment = TextAlignmentOptions.Center;

        // 添加Outline效果（TextMeshPro自带轮廓，不需要额外组件）
        damageText.outlineWidth = 0.2f;
        damageText.outlineColor = Color.black;

        // 浮动动画
        float duration = 1.5f;
        float startTime = Time.time;
        Vector3 startPos = damageTextObj.transform.position;

        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;

            // 向上浮动
            damageTextObj.transform.position = startPos + new Vector3(
                0,
                100 * progress,
                0
            );

            // 淡出
            Color color = damageText.color;
            color.a = 1f - progress;
            damageText.color = color;

            // 如果有关键暴击，稍微放大
            if (isCritical && progress < 0.3f)
            {
                float scale = 1f + Mathf.Sin(progress * 10f) * 0.3f;
                damageTextObj.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        Destroy(damageTextObj);
    }

    // 公共方法：显示消息提示
    public void ShowMessage(string message, float duration = 2f)
    {
        StartCoroutine(ShowMessageVR(message, duration));
    }

    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        GameObject messageObj = new GameObject("Message");
        messageObj.transform.SetParent(mainCanvas.transform);
        messageObj.transform.position = new Vector3(Screen.width / 2, Screen.height * 0.8f, 0);

        // 添加背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(messageObj.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(400, 60);
        bgRect.anchoredPosition = Vector2.zero;

        // 添加TextMeshPro文本（修改这里）
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(messageObj.transform);
        TextMeshProUGUI messageText = textObj.AddComponent<TextMeshProUGUI>();
        if (messageFont != null)
        {
            messageText.font = messageFont;
        }
        messageText.text = message;
        messageText.fontSize = 24;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = messageText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(380, 50);
        textRect.anchoredPosition = Vector2.zero;

        // ... 淡入淡出效果保持不变 ...

        // 淡入
        float fadeTime = 0.3f;
        float startTime = Time.time;

        while (Time.time - startTime < fadeTime)
        {
            float progress = (Time.time - startTime) / fadeTime;
            messageObj.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
            yield return null;
        }

        messageObj.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(duration - fadeTime * 2);

        // 淡出
        startTime = Time.time;
        while (Time.time - startTime < fadeTime)
        {
            float progress = (Time.time - startTime) / fadeTime;
            messageObj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);
            yield return null;
        }

        Destroy(messageObj);
    }
#if UNITY_EDITOR
    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        int yPos = 10;
        GUI.Label(new Rect(10, yPos, 300, 20), "=== UI系统状态 ===", style);
        yPos += 25;

        if (playerHealth != null)
        {
            GUI.Label(new Rect(10, yPos, 300, 20),
                     $"生命值: {playerHealth.GetCurrentHealth()}/{playerHealth.maxHealth} ({(playerHealth.GetHealthPercentage() * 100):F1}%)",
                     style);
            yPos += 25;
        }

        if (skillManager != null)
        {
            GUI.Label(new Rect(10, yPos, 300, 20), "技能状态:", style);
            yPos += 25;

            foreach (var skill in skillManager.skillList)
            {
                string status = skill.isOnCooldown ?
                    $"冷却: {skill.currentCooldown:F1}/{skill.cooldown}s" :
                    "就绪";

                GUI.Label(new Rect(20, yPos, 300, 20),
                         $"{skill.skillName}: {status}",
                         style);
                yPos += 20;
            }
        }

        if (wandController != null)
        {
            Transform lockedEnemy = wandController.GetLockedEnemy();
            GUI.Label(new Rect(10, yPos, 300, 20),
                     $"锁定敌人: {(lockedEnemy != null ? lockedEnemy.name : "无")}",
                     style);
        }
    }
#endif
}