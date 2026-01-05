using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    [Header("传送设置")]
    public Transform destination; // 目的地
    public bool oneTimeUse = true; // 是否一次性

    void OnTriggerEnter(Collider other)
    {
        // 确保是玩家触发（检测标签或组件）
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            if (destination != null)
            {
                TeleportPlayer(other.transform);
            }
        }
    }

    void TeleportPlayer(Transform player)
    {
        Debug.Log($"传送玩家到: {destination.name}");

        // 如果玩家有 CharacterController，直接修改 transform.position 无效，必须先禁用
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 执行传送
        player.position = destination.position;
        // 可选：是否重置朝向
        // player.rotation = destination.rotation; 

        if (cc != null) cc.enabled = true;

        // 传送后销毁传送门
        if (oneTimeUse)
        {
            Destroy(gameObject, 0.5f); // 延迟销毁给一点视觉缓冲
        }
    }
}