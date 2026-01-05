using UnityEngine;

public class StoryTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        None,      // 纯功能触发（比如只换BGM，不说话）
        Cinematic, // 黑屏 + 传送 + 独白
        Dialogue   // 底部对话
    }

    [Header("基础设置")]
    public TriggerType type = TriggerType.None;
    public bool oneTimeOnly = true;

    [Header("Cinematic (黑屏独白)")]
    [TextArea] public string[] monologueLines;
    public Transform teleportTarget;

    [Header("Dialogue (对话ID)")]
    public string dialogueSequenceId;

    [Header("环境变化 (可选)")]
    public AudioClip changeBgmTo;    // 如果拖入，就会切换BGM
    public Material changeSkyboxTo;  // 如果拖入，就会切换天空盒
    public bool unlockWand = false;  // 如果勾选，就会给玩家魔杖

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered && oneTimeOnly) return;

        // 检测是否是玩家
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>())
        {
            triggered = true;

            // 1. 处理环境变化 (BGM, Skybox, Wand)
            if (changeBgmTo != null)
                StoryManager.Instance.PlayBGM(changeBgmTo);

            if (changeSkyboxTo != null)
                StoryManager.Instance.ChangeSkybox(changeSkyboxTo);

            if (unlockWand)
                StoryManager.Instance.UnlockWand();

            // 2. 处理剧情 (黑屏 或 对话)
            if (type == TriggerType.Cinematic)
            {
                StoryManager.Instance.StartCoroutine(
                    StoryManager.Instance.PlayCinematicSequence(monologueLines, teleportTarget)
                );
            }
            else if (type == TriggerType.Dialogue && !string.IsNullOrEmpty(dialogueSequenceId))
            {
                StoryManager.Instance.StartDialogue(dialogueSequenceId);
            }
        }
    }
}