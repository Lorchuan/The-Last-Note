using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    [Header("UI组件")]
    public Image blackScreen;
    public TextMeshProUGUI centerText;
    [Header("结局CG设置 (新增)")]
    public Image cgDisplayImage; // 拖入一个新的Image组件，放在BlackScreen上面
    public Sprite endingCGSprite; // 拖入你的CG图片
    public float cgDisplayDuration = 4.0f; // CG显示多久
    [Header("系统引用")]
    public Transform playerTransform;
    private DialogueSystem dialogueSystem;

    [Header("魔杖控制")]
    public GameObject wandObject; // 拖入玩家手里的 Wand 物体

    [Header("环境控制")]
    public AudioSource bgmSource; // 拖入一个 AudioSource 用于播放背景乐
    public float musicFadeSpeed = 1.0f;
    [Header("结局设置 (新增)")]
    public string endingDialogueId = "07_fffn"; // 必须与对话编辑器里的ID一致
    public string ffnDialogueId = "06_ffn";
    [TextArea] public string[] endingCreditsLines;  // 谢幕字幕内容
    public float creditSpeed = 3f;                  // 字幕切换间隔
    [Header("结局沙尘暴效果")]
    public GameObject[] normalCityObjects; // 正常的城市物体 (拖拽赋值)
    public GameObject[] halfBuriedObjects; // 掩埋一半的物体 (初始隐藏)
    public GameObject[] fullBuriedObjects; // 完全掩埋的物体 (初始隐藏)
    public int triggerLinePhase1 = 2; // 第几句话触发第一阶段（一半掩埋）
    public int triggerLinePhase2 = 5; // 第几句话触发第二阶段（全埋）
    void Start()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueSequenceEnded += HandleDialogueEnd;
            // 【新增】监听对话行数变化
            dialogueSystem.OnDialogueLineChanged += HandleDialogueLineChange;
        }

        // 初始状态：确保沙尘物体隐藏
        foreach (var obj in halfBuriedObjects) if (obj) obj.SetActive(false);
        foreach (var obj in fullBuriedObjects) if (obj) obj.SetActive(false);
    }
    void OnDestroy() // 别忘了注销事件
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueSequenceEnded -= HandleDialogueEnd;
            dialogueSystem.OnDialogueLineChanged -= HandleDialogueLineChange;
        }
    }
    // 【新增】处理对话行变化，触发特效
    void HandleDialogueLineChange(string sequenceId, int lineIndex)
    {
        // 只在结局对话时触发 (ID要和你编辑器里的一致)
        if (sequenceId == ffnDialogueId)
        {
            if (lineIndex == triggerLinePhase1)
            {
                StartCoroutine(TriggerSandStorm(1));
            }
            else if (lineIndex == triggerLinePhase2)
            {
                StartCoroutine(TriggerSandStorm(2));
            }
        }
    }
    IEnumerator TriggerSandStorm(int phase)
    {
        // 1. 快速白屏闪烁 (模拟风沙冲击)
        yield return FadeScreen(1f, 0.2f); // 0.2秒变黑/白

        // 2. 在黑屏瞬间切换物体
        if (phase == 1)
        {
            foreach (var obj in normalCityObjects) if (obj) obj.SetActive(false);
            foreach (var obj in halfBuriedObjects) if (obj) obj.SetActive(true);
            Debug.Log("城市被掩埋 - 阶段 1");
        }
        else if (phase == 2)
        {
            foreach (var obj in halfBuriedObjects) if (obj) obj.SetActive(false);
            foreach (var obj in fullBuriedObjects) if (obj) obj.SetActive(true);
            Debug.Log("城市被掩埋 - 阶段 2");
        }

        // 3. 屏幕恢复
        yield return FadeScreen(0f, 0.5f); // 0.5秒恢复
    }
    private void Awake()
    {
        Instance = this;
        dialogueSystem = FindObjectOfType<DialogueSystem>();

        // 初始状态设置
        if (blackScreen) blackScreen.color = new Color(0, 0, 0, 0);
        if (centerText) centerText.text = "";

        // 游戏开始时，强制隐藏魔杖
        if (wandObject != null) wandObject.SetActive(false);
    }

    // --- 1. 剧情黑屏功能 ---
    public IEnumerator PlayCinematicSequence(string[] lines, Transform teleportTarget = null)
    {
        yield return FadeScreen(1f, 1.5f);

        if (teleportTarget != null)
        {
            yield return new WaitForSeconds(0.5f);
            playerTransform.position = teleportTarget.position;
            playerTransform.rotation = teleportTarget.rotation;
            // 传送后可能需要同步物理位置
            Physics.SyncTransforms();
        }

        foreach (string line in lines)
        {
            centerText.text = line;
            yield return FadeText(centerText, 0, 1, 1f);
            float readTime = Mathf.Max(2f, line.Length * 0.15f); // 稍微增加阅读时间
            yield return new WaitForSeconds(readTime);
            yield return FadeText(centerText, 1, 0, 1f);
        }
        centerText.text = "";

        yield return FadeScreen(0f, 1.5f);
    }

    // --- 2. 对话功能 ---
    public void StartDialogue(string sequenceId)
    {
        if (dialogueSystem != null) dialogueSystem.StartDialogueSequence(sequenceId);
    }

    // --- 3. 音乐切换 (带淡入淡出) ---
    public void PlayBGM(AudioClip music)
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == music && bgmSource.isPlaying) return; // 已经是这首歌就不切了

        StopCoroutine("CrossFadeMusic");
        StartCoroutine(CrossFadeMusic(music));
    }

    IEnumerator CrossFadeMusic(AudioClip newClip)
    {
        // 淡出
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= Time.deltaTime * musicFadeSpeed;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        if (newClip != null) bgmSource.Play();

        // 淡入
        while (bgmSource.volume < startVolume)
        {
            bgmSource.volume += Time.deltaTime * musicFadeSpeed;
            yield return null;
        }
        bgmSource.volume = startVolume; // 确保回到原音量
    }

    // --- 4. 天空盒切换 ---
    public void ChangeSkybox(Material newSkybox)
    {
        if (newSkybox != null)
        {
            RenderSettings.skybox = newSkybox;
            DynamicGI.UpdateEnvironment(); // 关键：刷新光照
        }
    }

    // --- 5. 获取魔杖 ---
    public void UnlockWand()
    {
        if (wandObject != null && !wandObject.activeSelf)
        {
            wandObject.SetActive(true);
            // 可以在这里播放一个获得物品的音效
            Debug.Log("获得魔杖！");
        }
    }

    // --- 辅助渐变 ---
    IEnumerator FadeScreen(float targetAlpha, float duration)
    {
        float startAlpha = blackScreen.color.a;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            blackScreen.color = new Color(0, 0, 0, a);
            yield return null;
        }
    }

    IEnumerator FadeText(TextMeshProUGUI text, float start, float end, float duration)
    {
        float t = 0;
        Color c = text.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            c.a = Mathf.Lerp(start, end, t);
            text.color = c;
            yield return null;
        }
    }
    void HandleDialogueEnd(string sequenceId)
    {
        // 如果结束的对话正好是“结局对话ID”
        if (sequenceId == endingDialogueId)
        {
            Debug.Log("检测到终章对话结束，开始播放演职员表...");
            StartCoroutine(PlayEndingAndQuit());
        }
    }
    // --- 新增：供Boss死亡时调用的传送方法 ---
    public void TeleportPlayer(Transform targetLocation)
    {
        if (targetLocation == null) return;

        Debug.Log("传送玩家至结局区域");

        // 稍微黑屏一下再传送，体验更好
        StartCoroutine(TeleportProcess(targetLocation));
    }
    IEnumerator TeleportProcess(Transform target)
    {
        yield return FadeScreen(1f, 1f); // 变黑

        playerTransform.position = target.position;
        playerTransform.rotation = target.rotation;
        Physics.SyncTransforms(); // 强制刷新物理坐标

        yield return new WaitForSeconds(0.5f);
        yield return FadeScreen(0f, 1f); // 变亮
    }
    IEnumerator PlayEndingAndQuit()
    {
        // 1. 慢慢黑屏
        yield return FadeScreen(1f, 2f);

        // 2. 【新增】显示结局 CG
        if (cgDisplayImage != null && endingCGSprite != null)
        {
            // 淡入图片
            yield return FadeImage(cgDisplayImage, 0, 1, 1.5f);

            // 展示一段时间
            yield return new WaitForSeconds(cgDisplayDuration);

            // 淡出图片
            yield return FadeImage(cgDisplayImage, 1, 0, 1.5f);

            yield return new WaitForSeconds(0.5f);
        }

        // 3. 播放字幕
        foreach (string line in endingCreditsLines)
        {
            centerText.text = line;
            yield return FadeText(centerText, 0, 1, 1f); // 文字浮现
            yield return new WaitForSeconds(creditSpeed); // 阅读
            yield return FadeText(centerText, 1, 0, 1f); // 文字消失
        }

        centerText.text = "";
        yield return new WaitForSeconds(1f);

        // 4. 退出游戏
        Debug.Log("游戏流程结束，退出应用。");
        QuitGame();
    }

    // 辅助：淡入淡出图片
    IEnumerator FadeImage(Image img, float startAlpha, float endAlpha, float duration)
    {
        float t = 0;
        Color c = img.color;
        c.a = startAlpha;
        img.color = c;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            img.color = c;
            yield return null;
        }
    }
    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 如果在编辑器里，停止播放
#else
            Application.Quit(); // 如果在打包版里，关闭程序
#endif
    }
}