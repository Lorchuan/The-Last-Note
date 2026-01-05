using UnityEngine;
using System.Collections;

// 这个脚本需要附加到玩家对象上
public class nPlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    [Header("减速效果")]
    private float slowMultiplier = 1f; // 速度乘数 (1 = 正常速度)
    private float slowTimer = 0f;
    private bool isSlowed = false;

    [Header("减速视觉反馈")]
    public ParticleSystem slowEffectParticles;
    public Material slowedMaterial; // 减速时应用的材质
    private Material originalMaterial;
    public Renderer playerRenderer;

    private CharacterController characterController;
    private float originalWalkSpeed;
    private float originalRunSpeed;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // 保存原始速度
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;

        // 保存原始材质
        if (playerRenderer != null)
        {
            originalMaterial = playerRenderer.material;
        }

        // 初始化减速粒子效果
        if (slowEffectParticles != null)
        {
            slowEffectParticles.Stop();
        }
    }

    void Update()
    {
        // 更新减速计时器
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;

            if (slowTimer <= 0)
            {
                RemoveSlowEffect();
            }
        }
    }

    // 应用减速效果
    public void ApplySlowEffect(float slowAmount, float duration)
    {
        if (isSlowed)
        {
            // 如果已经有减速效果，刷新持续时间
            slowTimer = duration;
        }
        else
        {
            // 应用新的减速效果
            slowMultiplier = 1f - slowAmount;
            walkSpeed = originalWalkSpeed * slowMultiplier;
            runSpeed = originalRunSpeed * slowMultiplier;

            slowTimer = duration;
            isSlowed = true;

            // 触发视觉反馈
            StartCoroutine(ApplySlowVisualEffects());

            Debug.Log($"玩家被减速! 速度减少 {slowAmount * 100}%，持续 {duration}秒");
        }
    }

    IEnumerator ApplySlowVisualEffects()
    {
        // 应用减速材质
        if (playerRenderer != null && slowedMaterial != null)
        {
            playerRenderer.material = slowedMaterial;
        }

        // 播放减速粒子效果
        if (slowEffectParticles != null)
        {
            slowEffectParticles.Play();
        }

        // 等待减速效果结束
        yield return new WaitForSeconds(slowTimer);

        // 减速结束后恢复
        RemoveSlowEffect();
    }

    // 移除减速效果
    void RemoveSlowEffect()
    {
        // 恢复速度
        walkSpeed = originalWalkSpeed;
        runSpeed = originalRunSpeed;

        // 恢复材质
        if (playerRenderer != null && originalMaterial != null)
        {
            playerRenderer.material = originalMaterial;
        }

        // 停止粒子效果
        if (slowEffectParticles != null)
        {
            slowEffectParticles.Stop();
        }

        isSlowed = false;
        slowMultiplier = 1f;

        Debug.Log("玩家减速效果结束，速度恢复正常");
    }

    // 获取当前是否被减速
    public bool IsSlowed()
    {
        return isSlowed;
    }

    // 获取减速剩余时间
    public float GetSlowRemainingTime()
    {
        return Mathf.Max(0, slowTimer);
    }

    // 清除所有减速效果（例如通过净化技能）
    public void ClearSlowEffects()
    {
        RemoveSlowEffect();
        Debug.Log("玩家减速效果被清除");
    }
}