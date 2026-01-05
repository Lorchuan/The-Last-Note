using UnityEngine;

public class TeleportRuneTrigger : MonoBehaviour
{
    [Header("配置")]
    public Transform targetDestination; // 拖入场景中你想传送到的位置（空物体）
    public GameObject runeGlowEffect;   // 可选：踩上去发光的特效

    void Start()
    {
        if (runeGlowEffect != null) runeGlowEffect.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 告诉 SkillManager：现在允许施放传送术，且目的地是这里
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.SetTeleportTarget(targetDestination);
            }

            if (runeGlowEffect != null) runeGlowEffect.SetActive(true);
            Debug.Log("玩家进入传送阵，传送术已就绪");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 离开触发器，禁止施放传送术
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.SetTeleportTarget(null);
            }

            if (runeGlowEffect != null) runeGlowEffect.SetActive(false);
        }
    }
}