using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string characterName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite characterPortrait;
    public AudioClip voiceClip;
    public float displaySpeed = 0.05f;
    public string[] options;

    public DialogueLine(string name, string text)
    {
        characterName = name;
        dialogueText = text;
    }
}

[System.Serializable]
public class DialogueSequence
{
    public string sequenceId;
    public List<DialogueLine> lines = new List<DialogueLine>();
    public string nextSequenceId;

    // 【新增】是否自动播放下一条序列
    public bool autoPlayNext = false;

    public bool requireChoice = false;
    public Dictionary<string, string> choiceToSequence = new Dictionary<string, string>();
}

public class DialogueSystem : MonoBehaviour
{
    // --- 事件定义 ---
    public event System.Action<string> OnDialogueSequenceEnded; // 对话结束广播

    [Header("UI组件引用")]
    public RectTransform dialoguePanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image characterPortrait;
    public GameObject optionButtonPrefab;
    public Transform optionsContainer;
    public AudioSource voiceAudioSource;

    [Header("设置")]
    public float autoAdvanceDelay = 2f;
    public bool autoAdvance = false;
    public KeyCode advanceKey = KeyCode.Space;
    public Color playerNameColor = Color.cyan;
    public Color npcNameColor = Color.white;
    public float fadeSpeed = 5f;

    [Header("数据")]
    public List<DialogueSequence> dialogueSequences = new List<DialogueSequence>();

    // 内部状态
    private CanvasGroup panelCanvasGroup;
    private Coroutine currentDialogueCoroutine;
    private DialogueSequence currentSequence;
    private int currentLineIndex = 0;
    private bool isShowing = false;
    private bool isTyping = false;
    private bool isInitialized = false;
    private List<GameObject> currentOptionButtons = new List<GameObject>();
    public event System.Action<string, int> OnDialogueLineChanged;
    void Start()
    {
        if (dialoguePanel != null)
        {
            panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;
            dialoguePanel.gameObject.SetActive(false);
        }
        Initialize();
    }

    public void Initialize()
    {
        if (dialogueSequences.Count == 0) CreateDefaultDialogues();
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || !isShowing) return;
        HandleDialogueInput();
    }

    // 在 DialogueSystem 类中添加一个变量记录上一帧状态
    private bool lastConfirmState = false;

    // 修改 HandleDialogueInput 方法
    void HandleDialogueInput()
    {
        // 1. 获取当前硬件按钮状态
        bool currentConfirmState = false;
        if (InputManager.Instance != null)
        {
            currentConfirmState = InputManager.Instance.isConfirmBtn;
        }

        // 2. 检测“按下瞬间” (当前是true，上一帧是false)
        bool isExternalBtnDown = currentConfirmState && !lastConfirmState;

        // 更新状态供下一帧对比
        lastConfirmState = currentConfirmState;

        // 3. 综合判断 (键盘空格 OR 鼠标左键 OR 硬件按钮)
        if (Input.GetKeyDown(advanceKey) || Input.GetButtonDown("Fire1") || isExternalBtnDown)
        {
            if (isTyping) CompleteCurrentLine();
            else AdvanceDialogue();
        }
    }

    public void StartDialogueSequence(string sequenceId)
    {
        if (!isInitialized) return;

        DialogueSequence sequence = GetDialogueSequence(sequenceId);
        if (sequence == null)
        {
            Debug.LogWarning($"找不到对话序列: {sequenceId}");
            return;
        }

        currentSequence = sequence;
        currentLineIndex = 0;

        ShowDialoguePanel();
        DisplayLine(currentSequence.lines[0]);
    }

    // 显示单句 (用于临时测试或简单提示)
    public void ShowDialogue(string characterName, string text)
    {
        DialogueLine line = new DialogueLine(characterName, text);
        currentSequence = new DialogueSequence { sequenceId = "temp", lines = { line } };
        currentLineIndex = 0;
        ShowDialoguePanel();
        DisplayLine(line);
    }

    // 显示带选项对话
    public void ShowDialogueWithOptions(string characterName, string text, string[] options)
    {
        DialogueLine line = new DialogueLine(characterName, text) { options = options };
        currentSequence = new DialogueSequence { sequenceId = "temp_opt", lines = { line }, requireChoice = true };
        foreach (var opt in options) currentSequence.choiceToSequence[opt] = "";

        currentLineIndex = 0;
        ShowDialoguePanel();
        DisplayLine(line);
    }

    public void HideDialogue()
    {
        if (!isShowing) return;

        if (currentDialogueCoroutine != null) StopCoroutine(currentDialogueCoroutine);
        ClearOptionButtons();

        StartCoroutine(FadePanel(0f, () => {
            dialoguePanel.gameObject.SetActive(false);
            isShowing = false;
            currentSequence = null;
        }));
    }

    void ShowDialoguePanel()
    {
        if (isShowing) return;
        dialoguePanel.gameObject.SetActive(true);
        isShowing = true;
        StartCoroutine(FadePanel(1f, null));
    }

    IEnumerator FadePanel(float targetAlpha, System.Action onComplete)
    {
        if (panelCanvasGroup == null) yield break;
        float startAlpha = panelCanvasGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        panelCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    void DisplayLine(DialogueLine line)
    {
        if (line == null) return;
        ClearOptionButtons();

        if (characterNameText != null)
        {
            characterNameText.text = line.characterName;
            characterNameText.color = line.characterName == "玩家" ? playerNameColor : npcNameColor;
        }

        if (characterPortrait != null)
        {
            characterPortrait.gameObject.SetActive(line.characterPortrait != null);
            if (line.characterPortrait != null) characterPortrait.sprite = line.characterPortrait;
        }

        if (voiceAudioSource != null && line.voiceClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(line.voiceClip);
        }

        if (currentDialogueCoroutine != null) StopCoroutine(currentDialogueCoroutine);
        currentDialogueCoroutine = StartCoroutine(TypeText(line));
    }

    IEnumerator TypeText(DialogueLine line)
    {
        if (dialogueText == null) yield break;

        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line.dialogueText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(line.displaySpeed);
        }
        isTyping = false;

        if (line.options != null && line.options.Length > 0) ShowOptions(line.options);
        else if (autoAdvance && !currentSequence.requireChoice)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            AdvanceDialogue();
        }
    }

    void ShowOptions(string[] options)
    {
        if (optionButtonPrefab == null || optionsContainer == null) return;
        ClearOptionButtons();
        optionsContainer.gameObject.SetActive(true);
        foreach (string opt in options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = opt;
            Button btn = btnObj.GetComponent<Button>();
            if (btn) { string val = opt; btn.onClick.AddListener(() => OnOptionSelected(val)); }
            currentOptionButtons.Add(btnObj);
        }
    }

    void ClearOptionButtons()
    {
        foreach (var btn in currentOptionButtons) Destroy(btn);
        currentOptionButtons.Clear();
        if (optionsContainer) optionsContainer.gameObject.SetActive(false);
    }

    void OnOptionSelected(string option)
    {
        if (currentSequence != null && currentSequence.choiceToSequence.ContainsKey(option))
        {
            string nextId = currentSequence.choiceToSequence[option];
            if (!string.IsNullOrEmpty(nextId)) StartDialogueSequence(nextId);
            else HideDialogue();
        }
        else HideDialogue();
    }

    void CompleteCurrentLine()
    {
        if (currentDialogueCoroutine != null) StopCoroutine(currentDialogueCoroutine);
        if (currentSequence != null && dialogueText != null)
        {
            DialogueLine line = currentSequence.lines[currentLineIndex];
            dialogueText.text = line.dialogueText;
            isTyping = false;
            if (line.options != null && line.options.Length > 0) ShowOptions(line.options);
        }
    }

    // --- 核心修改：推进对话逻辑 ---
    void AdvanceDialogue()
    {
        if (currentSequence == null || isTyping) return;
        currentLineIndex++;

        if (currentLineIndex < currentSequence.lines.Count)
        {
            DisplayLine(currentSequence.lines[currentLineIndex]);
            if (currentLineIndex < currentSequence.lines.Count)
            {
                DisplayLine(currentSequence.lines[currentLineIndex]);

                // 【新增】广播当前进度
                OnDialogueLineChanged?.Invoke(currentSequence.sequenceId, currentLineIndex);
            }
        }
        else
        {
            // 当前序列结束，记录ID
            string finishedId = currentSequence.sequenceId;

            // 1. 检查是否开启了“自动播放下一条” 且 下一条ID不为空
            if (currentSequence.autoPlayNext && !string.IsNullOrEmpty(currentSequence.nextSequenceId))
            {
                StartDialogueSequence(currentSequence.nextSequenceId);
            }
            else
            {
                // 2. 否则结束对话，并发送事件
                HideDialogue();
                OnDialogueSequenceEnded?.Invoke(finishedId);
            }
        }
    }

    DialogueSequence GetDialogueSequence(string id) => dialogueSequences.Find(s => s.sequenceId == id);
    void CreateDefaultDialogues() { /* ... */ }

    [ContextMenu("测试显示")]
    public void ShowTestDialogue() => ShowDialogue("Debug", "测试消息");
}