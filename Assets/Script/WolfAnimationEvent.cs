using UnityEngine;

public class WolfAnimationEventHelper : MonoBehaviour
{
    [Header("狼控制器引用")]
    public WolfController wolfController;

    [Header("动画事件时间点")]
    public float attack1HitTime = 0.3f; // 攻击1命中时间
    public float attack1EndTime = 0.8f; // 攻击1结束时间
    public float attack2HitTime = 0.4f; // 攻击2命中时间
    public float attack2EndTime = 0.9f; // 攻击2结束时间
    public float howlHitTime = 0.5f; // 嚎叫命中时间
    public float howlEndTime = 1.2f; // 嚎叫结束时间

    [Header("调试")]
    public bool showDebug = true;

    private Animator animator;
    private float currentAnimTime;
    private string currentState;
    private bool attack1HitTriggered = false;
    private bool attack1EndTriggered = false;
    private bool attack2HitTriggered = false;
    private bool attack2EndTriggered = false;
    private bool howlHitTriggered = false;
    private bool howlEndTriggered = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (wolfController == null)
        {
            wolfController = GetComponent<WolfController>();
        }
    }

    void Update()
    {
        if (animator == null || wolfController == null) return;

        // 获取当前动画状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1.0f;

        // 检查攻击1动画
        if (stateInfo.IsName("Attack1"))
        {
            // 攻击1命中事件
            if (normalizedTime >= attack1HitTime && !attack1HitTriggered)
            {
                wolfController.OnAttack1Hit();
                attack1HitTriggered = true;
                if (showDebug) Debug.Log("动画事件: 攻击1命中触发");
            }

            // 攻击1结束事件
            if (normalizedTime >= attack1EndTime && !attack1EndTriggered)
            {
                wolfController.OnAttack1End();
                attack1EndTriggered = true;
                if (showDebug) Debug.Log("动画事件: 攻击1结束触发");
            }

            // 重置标志（当动画时间回到事件点之前时）
            if (normalizedTime < attack1HitTime) attack1HitTriggered = false;
            if (normalizedTime < attack1EndTime) attack1EndTriggered = false;
        }
        // 检查攻击2动画
        else if (stateInfo.IsName("Attack2"))
        {
            // 攻击2命中事件
            if (normalizedTime >= attack2HitTime && !attack2HitTriggered)
            {
                wolfController.OnAttack2Hit();
                attack2HitTriggered = true;
                if (showDebug) Debug.Log("动画事件: 攻击2命中触发");
            }

            // 攻击2结束事件
            if (normalizedTime >= attack2EndTime && !attack2EndTriggered)
            {
                wolfController.OnAttack2End();
                attack2EndTriggered = true;
                if (showDebug) Debug.Log("动画事件: 攻击2结束触发");
            }

            // 重置标志
            if (normalizedTime < attack2HitTime) attack2HitTriggered = false;
            if (normalizedTime < attack2EndTime) attack2EndTriggered = false;
        }
        // 检查攻击3（嚎叫）动画
        else if (stateInfo.IsName("Attack3"))
        {
            // 嚎叫命中事件
            if (normalizedTime >= howlHitTime && !howlHitTriggered)
            {
                wolfController.OnHowlHit();
                howlHitTriggered = true;
                if (showDebug) Debug.Log("动画事件: 嚎叫命中触发");
            }

            // 嚎叫结束事件
            if (normalizedTime >= howlEndTime && !howlEndTriggered)
            {
                wolfController.OnHowlEnd();
                howlEndTriggered = true;
                if (showDebug) Debug.Log("动画事件: 嚎叫结束触发");
            }

            // 重置标志
            if (normalizedTime < howlHitTime) howlHitTriggered = false;
            if (normalizedTime < howlEndTime) howlEndTriggered = false;
        }
        else
        {
            // 不在攻击状态时重置所有标志
            ResetAllTriggers();
        }
    }

    void ResetAllTriggers()
    {
        attack1HitTriggered = false;
        attack1EndTriggered = false;
        attack2HitTriggered = false;
        attack2EndTriggered = false;
        howlHitTriggered = false;
        howlEndTriggered = false;
    }
}